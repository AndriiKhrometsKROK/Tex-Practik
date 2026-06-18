// Керує ворожим юнітом: рухом по маршруту, пошуком союзників, атаками, опорами та ефектами стану.
using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public UnitData data;
    [SerializeField] private EnemyHealthBar healthBarPrefab;
    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float allyAttackRange = 1.1f;
    [SerializeField, Min(0.05f)] private float allyScanInterval = 0.2f;

    private float _currentHp;
    private float _baseMoveSpeed;
    private float _currentMoveSpeed;
    private float _nextAttackTime;
    private Coroutine _slowCoroutine;
    private Coroutine _poisonCoroutine;
    private Coroutine _barrierWaitCoroutine;
    private int _slowStacks;
    private int _waypointIndex;
    private EnemyHealthBar _healthBar;
    private bool _isDead;
    private float _nextAllyScanTime;
    private AllyController _allyTarget;
    private float _laneX;
    private BattleLane _lane = BattleLane.Lower;
    private float _statMultiplier = 1f;
    private float _runtimeArmorBonus;
    private float _runtimeMagicResistanceBonus;
    private float _attackSpeedMultiplier = 1f;
    private float _itemResistanceReduction;
    private float _itemDebuffUntil;
    private bool _removedWithoutReward;

    public UnitMovementState CurrentMovementState { get; private set; } = UnitMovementState.Moving;
    public UnitBrainState BrainState { get; private set; } = UnitBrainState.Move;
    public bool IsAlive => !_isDead && gameObject.activeInHierarchy;
    public float CurrentHealth => _currentHp;
    public float MaxHealth => GetMaxHealth();

    public void SetLane(BattleLane lane)
    {
        _lane = lane;
        _laneX = BattleLaneUtility.GetX(lane);
        Vector3 position = transform.position;
        position.x = BattleLaneUtility.GetPathPoint(lane, 0).x;
        position.y = BattleLaneUtility.GetPathPoint(lane, 0).y;
        transform.position = position;
        _waypointIndex = 0;
        transform.localScale = Vector3.one * 3.06f;

        int sortingOrder = lane == BattleLane.Upper ? 8 : 11;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingOrder = sortingOrder;
        }
        RuntimeCharacterVisuals.Apply(gameObject, RuntimeCharacterSkin.EnemyOrc, sortingOrder);
    }

    private void OnEnable()
    {
        CombatRegistry.Register(this);
        _statMultiplier = 1f;
        _runtimeArmorBonus = 0f;
        _runtimeMagicResistanceBonus = 0f;
        _attackSpeedMultiplier = 1f;
        _itemResistanceReduction = 0f;
        _itemDebuffUntil = 0f;
        _slowStacks = 0;
        _removedWithoutReward = false;
        GetComponent<EntropyStatus>()?.ResetStacks();
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            _slowCoroutine = null;
        }

        if (_barrierWaitCoroutine != null)
        {
            StopCoroutine(_barrierWaitCoroutine);
            _barrierWaitCoroutine = null;
        }

        if (data != null)
        {
            _currentHp = data.maxHp;
            _baseMoveSpeed = data.moveSpeed;
            _currentMoveSpeed = _baseMoveSpeed;
            _nextAttackTime = 0f;
            _isDead = false;
            _allyTarget = null;
            _nextAllyScanTime = 0f;
            CurrentMovementState = UnitMovementState.Moving;
            SetBrainState(UnitBrainState.Move);
        }
        else
        {
            Debug.LogError("UnitData is not assigned for " + gameObject.name);
            _currentHp = 100f;
            _baseMoveSpeed = 1f;
            _currentMoveSpeed = _baseMoveSpeed;
            _nextAttackTime = 0f;
            _isDead = false;
            _allyTarget = null;
            _nextAllyScanTime = 0f;
            CurrentMovementState = UnitMovementState.Moving;
            SetBrainState(UnitBrainState.Move);
        }

        _waypointIndex = 0;

        EnsureHealthBar();
        _healthBar?.Attach(transform, GetMaxHealth(), _currentHp);
    }

    private void OnDisable()
    {
        CombatRegistry.Unregister(this);
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            _slowCoroutine = null;
        }

        if (_poisonCoroutine != null)
        {
            StopCoroutine(_poisonCoroutine);
            _poisonCoroutine = null;
        }

        if (_barrierWaitCoroutine != null)
        {
            StopCoroutine(_barrierWaitCoroutine);
            _barrierWaitCoroutine = null;
        }

        CurrentMovementState = UnitMovementState.Moving;
        if (!_isDead) SetBrainState(UnitBrainState.Move);
        _allyTarget = null;
        if (_healthBar != null)
        {
            _healthBar.Detach();
        }
    }

    private void OnDestroy()
    {
        if (_healthBar != null)
        {
            Destroy(_healthBar.gameObject);
            _healthBar = null;
        }
    }

    private void Update()
    {
        if (data == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;
        if (CurrentMovementState == UnitMovementState.Wait) return;

        SetBrainState(UnitBrainState.Search);
        RefreshAllyTarget();
        if (CanAttackAlly())
        {
            SetBrainState(UnitBrainState.Attack);
            AttackAlly();
            return;
        }

        _allyTarget = null;
        SetBrainState(UnitBrainState.Move);
        MoveAlongPath();
    }

    public float TakeDamage(float amount, bool isMagicDamage = false)
    {
        return TakeDamage(new DamagePacket(
            amount,
            isMagicDamage ? DamageFamily.Magical : DamageFamily.Physical,
            DamageModifier.Default));
    }

    public float TakeDamage(DamagePacket packet)
    {
        if (_isDead || data == null || packet.Amount <= 0f) return 0f;

        EntropyStatus entropy = GetComponent<EntropyStatus>();
        if (packet.Family == DamageFamily.Chaos)
        {
            entropy = entropy ?? gameObject.AddComponent<EntropyStatus>();
            if (entropy.AddStack(true))
            {
                float remaining = _currentHp;
                ApplyHealthDamage(_currentHp);
                return remaining;
            }
        }

        float resistanceReduction = entropy != null ? entropy.ResistanceReduction : 0f;
        HeroInventory heroInventory = FindAnyObjectByType<HeroInventory>();
        if (heroInventory != null && heroInventory.HasVeil &&
            Vector2.Distance(heroInventory.transform.position, transform.position) <= 5.5f)
        {
            resistanceReduction += 0.25f;
        }
        if (heroInventory != null && heroInventory.Has(HeroItemId.Gleipnir) &&
            Vector2.Distance(heroInventory.transform.position, transform.position) <= 5.5f)
        {
            resistanceReduction += 0.12f;
        }
        if (Time.time <= _itemDebuffUntil) resistanceReduction += _itemResistanceReduction;

        float finalDamage = CombatResolver.Resolve(
            packet,
            data.armor * Mathf.Sqrt(_statMultiplier) + _runtimeArmorBonus,
            Mathf.Clamp01(data.magicResistance + _runtimeMagicResistanceBonus),
            resistanceReduction);
        ApplyHealthDamage(finalDamage);
        return finalDamage;
    }

    public void ApplySlow(float slowFactor, float duration)
    {
        if (slowFactor <= 0f || duration <= 0f) return;

        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
        }

        _slowStacks = Mathf.Min(3, _slowStacks + 1);
        float stackedFactor = Mathf.Pow(Mathf.Clamp01(slowFactor), _slowStacks);
        _slowCoroutine = StartCoroutine(SlowRoutine(stackedFactor, duration));
    }

    public void ApplyPoison(float damagePerTick, float duration, float tickInterval = 1f)
    {
        if (_isDead) return;
        if (damagePerTick <= 0f || duration <= 0f) return;

        if (_poisonCoroutine != null)
        {
            StopCoroutine(_poisonCoroutine);
        }

        _poisonCoroutine = StartCoroutine(PoisonRoutine(damagePerTick, duration, Mathf.Max(0.1f, tickInterval)));
    }

    public void WaitAtBarrier(float duration)
    {
        if (_isDead) return;
        if (duration <= 0f) return;
        if (_barrierWaitCoroutine != null) return;

        _barrierWaitCoroutine = StartCoroutine(BarrierWaitRoutine(duration));
    }

    public void ApplyRoot(float duration)
    {
        WaitAtBarrier(duration);
    }

    public void ApplyItemResistanceDebuff(float reduction, float duration)
    {
        _itemResistanceReduction = Mathf.Max(_itemResistanceReduction, Mathf.Clamp01(reduction));
        _itemDebuffUntil = Mathf.Max(_itemDebuffUntil, Time.time + Mathf.Max(0.1f, duration));
    }

    public void ApplyStatMultiplier(float multiplier)
    {
        multiplier = Mathf.Max(0.1f, multiplier);
        float previousMax = GetMaxHealth();
        float healthRatio = previousMax > 0f ? _currentHp / previousMax : 1f;
        _statMultiplier = multiplier;
        _baseMoveSpeed = data != null ? data.moveSpeed * Mathf.Sqrt(multiplier) : _baseMoveSpeed;
        _currentMoveSpeed = _baseMoveSpeed;
        _currentHp = GetMaxHealth() * healthRatio;
        _healthBar?.Attach(transform, GetMaxHealth(), _currentHp);
    }

    public void ApplyLevelRule(CampaignLevelRule rule)
    {
        if (rule.Mutators.HasFlag(LevelMutator.Armored))
        {
            _runtimeArmorBonus += 4f + rule.Level * 0.35f;
        }

        if (rule.Mutators.HasFlag(LevelMutator.MagicWard))
        {
            _runtimeMagicResistanceBonus += Mathf.Min(0.35f, 0.08f + rule.Level * 0.008f);
        }

        if (rule.Mutators.HasFlag(LevelMutator.Haste))
        {
            _baseMoveSpeed *= 1.2f;
            _currentMoveSpeed = _baseMoveSpeed;
            _attackSpeedMultiplier *= 1.22f;
        }

        if (rule.Mutators.HasFlag(LevelMutator.Elite))
        {
            ApplyStatMultiplier(_statMultiplier * 1.12f);
            _runtimeArmorBonus += 3f;
            _attackSpeedMultiplier *= 1.1f;
        }
    }

    public void RemoveWithoutReward()
    {
        if (_isDead || _removedWithoutReward) return;
        _removedWithoutReward = true;
        _isDead = true;
        SetBrainState(UnitBrainState.Dead);
        GameManager.Instance?.RegisterEnemyRemovedWithoutReward();
        ObjectPoolManager.Return(gameObject);
    }

    private void MoveAlongPath()
    {
        Vector2 destination = BattleLaneUtility.GetPathPoint(_lane, _waypointIndex);

        transform.position = Vector2.MoveTowards(
            transform.position,
            destination,
            _currentMoveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, destination) <= 0.1f)
        {
            GetNextWaypoint();
        }
    }

    private void GetNextWaypoint()
    {
        if (_waypointIndex >= BattleLaneUtility.PathLength - 1)
        {
            ReachBase();
            return;
        }

        _waypointIndex++;
    }

    private void ReachBase()
    {
        if (GameManager.Instance != null)
        {
            bool intercepted = BattleFlowController.Instance != null &&
                BattleFlowController.Instance.HandleEnemyReachedDefense(data, _statMultiplier);
            if (intercepted)
            {
                GameManager.Instance.RegisterEnemyRemovedWithoutReward();
            }
            else
            {
                GameManager.Instance.EnemyReachedBase(data, _statMultiplier);
            }
        }

        Debug.Log(gameObject.name + " reached the base.");
        ObjectPoolManager.Return(gameObject);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        SetBrainState(UnitBrainState.Dead);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyKilled(data);
        }

        ObjectPoolManager.Return(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (data == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        if (other.CompareTag("Player") && Time.time >= _nextAttackTime)
        {
            float randomDamage = Random.Range(data.minDamage, data.maxDamage) * _statMultiplier;
            other.GetComponent<PlayerHealth>()?.TakeDamage(new DamagePacket(
                randomDamage,
                DamageFamily.Physical,
                DamageModifier.Default,
                gameObject));
            _nextAttackTime = Time.time + GetAttackInterval();
        }
    }

    private void RefreshAllyTarget()
    {
        if (CanAttackAlly() || Time.time < _nextAllyScanTime) return;

        _nextAllyScanTime = Time.time + allyScanInterval;
        _allyTarget = FindNearestAlly();
    }

    private AllyController FindNearestAlly()
    {
        AllyController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (AllyController ally in CombatRegistry.ActiveAllies)
        {
            if (ally == null || !ally.IsAlive || ally.IsUndyingAuraActive) continue;

            float distance = ((Vector2)ally.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance > allyAttackRange * allyAttackRange) continue;
            if (distance >= nearestDistance) continue;

            nearest = ally;
            nearestDistance = distance;
        }

        return nearest;
    }

    private bool CanAttackAlly()
    {
        return _allyTarget != null &&
            _allyTarget.isActiveAndEnabled &&
            _allyTarget.IsAlive &&
            !_allyTarget.IsUndyingAuraActive &&
            Vector2.Distance(transform.position, _allyTarget.transform.position) <= allyAttackRange;
    }

    private void AttackAlly()
    {
        if (Time.time < _nextAttackTime || data == null) return;

        float minimum = Mathf.Max(1f, data.minDamage);
        float maximum = Mathf.Max(minimum, data.maxDamage);
        _allyTarget.TakeDamage(new DamagePacket(
            Random.Range(minimum, maximum) * _statMultiplier,
            DamageFamily.Physical,
            DamageModifier.Default,
            gameObject));
        _nextAttackTime = Time.time + GetAttackInterval();
    }

    private IEnumerator SlowRoutine(float slowFactor, float duration)
    {
        _currentMoveSpeed = _baseMoveSpeed * slowFactor;
        yield return new WaitForSeconds(duration);

        _currentMoveSpeed = _baseMoveSpeed;
        _slowStacks = 0;
        _slowCoroutine = null;
    }

    private IEnumerator PoisonRoutine(float damagePerTick, float duration, float tickInterval)
    {
        float elapsed = 0f;

        while (elapsed < duration && !_isDead)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            ApplyHealthDamage(damagePerTick);
        }

        _poisonCoroutine = null;
    }

    private IEnumerator BarrierWaitRoutine(float duration)
    {
        CurrentMovementState = UnitMovementState.Wait;
        SetBrainState(UnitBrainState.Wait);
        yield return new WaitForSeconds(duration);

        CurrentMovementState = UnitMovementState.Moving;
        SetBrainState(UnitBrainState.Move);
        _barrierWaitCoroutine = null;
    }

    private void ApplyHealthDamage(float amount)
    {
        if (_isDead) return;
        if (amount <= 0f) return;

        _currentHp -= amount;
        _healthBar?.UpdateHealth(_currentHp, GetMaxHealth());

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    private void EnsureHealthBar()
    {
        if (_healthBar != null) return;

        if (healthBarPrefab == null)
        {
            healthBarPrefab = Resources.Load<EnemyHealthBar>("UI/EnemyHealthBar");
        }

        if (healthBarPrefab != null)
        {
            _healthBar = Instantiate(healthBarPrefab);
        }
        else
        {
            GameObject healthBarObject = new GameObject(gameObject.name + " Health Bar");
            _healthBar = healthBarObject.AddComponent<EnemyHealthBar>();
        }
    }

    private float GetMaxHealth()
    {
        return data != null ? data.maxHp * _statMultiplier : 100f * _statMultiplier;
    }

    private float GetAttackInterval()
    {
        return Mathf.Max(0.15f, data.attackRate / (Mathf.Sqrt(_statMultiplier) * _attackSpeedMultiplier));
    }

    private void SetBrainState(UnitBrainState state)
    {
        BrainState = state;
    }
}
