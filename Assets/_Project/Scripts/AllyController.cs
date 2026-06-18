// Керує поведінкою союзного крипа: рухом по лінії, вибором цілі, атакою, аурами та командами логістики.
using System.Collections;
using UnityEngine;

public class AllyController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.1f;
    [SerializeField, Min(0.05f)] private float targetScanInterval = 0.2f;

    private UnitData unitData;
    private float currentHp;
    private float currentMoveSpeed;
    private float nextAttackTime;
    private float nextTargetScanTime;
    private Coroutine barrierWaitCoroutine;
    private EnemyAI target;
    private EnemyCastle castleTarget;
    private DemonBossController bossTarget;
    private float laneX;
    private float permanentMultiplier = 1f;
    private float temporaryDamageMultiplier = 1f;
    private float temporarySpeedMultiplier = 1f;
    private float temporaryBuffUntil;
    private float auraDamageMultiplier = 1f;
    private float auraFlatDamageBonus;
    private float auraArmorBonus;
    private float auraSpeedMultiplier = 1f;
    private float auraLifesteal;
    private bool auraUndying;
    private float auraExpiresAt;
    private bool spiritWard;
    private float magicBarrier;
    private float invulnerableUntil;
    private bool changingLane;
    private bool returningToBase;
    private float commandedLaneX;
    private BattleLane commandedLane;
    private float logisticsReadyAt;
    private bool logisticsDelayPaid;
    private int pathIndex;

    public UnitMovementState CurrentMovementState { get; private set; } = UnitMovementState.Moving;
    public UnitBrainState BrainState { get; private set; } = UnitBrainState.Move;
    public bool IsAlive => currentHp > 0f;
    public BattleLane Lane { get; private set; } = BattleLane.Upper;
    public float MaxHealth => unitData != null ? unitData.maxHp * permanentMultiplier : 1f;
    public float CurrentHealth => currentHp;
    public bool IsUndyingAuraActive => IsUndying();

    public void SetLane(BattleLane lane)
    {
        Lane = lane;
        Vector3 position = transform.position;
        laneX = BattleLaneUtility.GetX(lane);
        position.x = BattleLaneUtility.GetPathPoint(lane, BattleLaneUtility.PathLength - 1).x;
        transform.position = position;
        transform.localScale = Vector3.one * 3.06f;
        pathIndex = BattleLaneUtility.PathLength - 1;

        int sortingOrder = lane == BattleLane.Upper ? 9 : 12;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingOrder = sortingOrder;
        }
        RuntimeCharacterVisuals.Apply(gameObject, RuntimeCharacterSkin.AllySoldier, sortingOrder);
    }

    public void SetCustomLane(float x, BattleLane lane)
    {
        Lane = lane;
        laneX = x;
        Vector3 position = transform.position;
        position.x = laneX;
        transform.position = position;
        pathIndex = FindNearestPathIndex(lane, position);
    }

    public void Initialize(UnitData data)
    {
        unitData = data;
        currentHp = unitData != null ? unitData.maxHp : 1f;
        currentMoveSpeed = unitData != null ? unitData.moveSpeed : 0f;
        nextAttackTime = 0f;
        nextTargetScanTime = 0f;
        target = null;
        castleTarget = null;
        CurrentMovementState = UnitMovementState.Moving;
        BrainState = UnitBrainState.Move;
        changingLane = false;
        returningToBase = false;
        logisticsReadyAt = 0f;
        logisticsDelayPaid = false;
        pathIndex = FindNearestPathIndex(Lane, transform.position);

        if (unitData != null && !string.IsNullOrWhiteSpace(unitData.unitName))
        {
            gameObject.name = unitData.unitName;
        }
    }

    private void Update()
    {
        if (!IsAlive || unitData == null) return;
        RefreshRuntimeModifiers();
        if (CurrentMovementState == UnitMovementState.Wait) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;
        if (HandleLogisticsCommand()) return;

        BrainState = UnitBrainState.Search;
        RefreshTarget();
        if (CanAttackTarget())
        {
            BrainState = UnitBrainState.Attack;
            AttackTarget();
            return;
        }

        target = null;
        RefreshBossTarget();
        if (bossTarget != null)
        {
            if (CanAttackBoss()) AttackBoss();
            else transform.position = Vector2.MoveTowards(
                transform.position,
                bossTarget.transform.position,
                GetMoveSpeed() * Time.deltaTime);
            return;
        }

        RefreshCastleTarget();
        if (CanAttackCastle())
        {
            AttackCastle();
            return;
        }

        castleTarget = null;
        bossTarget = null;
        BrainState = UnitBrainState.Move;
        MoveAlongLanePath();
    }

    private void OnDisable()
    {
        CombatRegistry.Unregister(this);
        if (barrierWaitCoroutine != null)
        {
            StopCoroutine(barrierWaitCoroutine);
            barrierWaitCoroutine = null;
        }

        target = null;
        castleTarget = null;
        CurrentMovementState = UnitMovementState.Moving;
        if (currentHp > 0f) BrainState = UnitBrainState.Move;
    }

    private void OnEnable()
    {
        CombatRegistry.Register(this);
    }

    public void WaitAtBarrier(float duration)
    {
        if (logisticsDelayPaid) return;
        if (duration <= 0f || barrierWaitCoroutine != null) return;
        barrierWaitCoroutine = StartCoroutine(BarrierWaitRoutine(duration));
    }

    public void ApplyRoot(float duration)
    {
        WaitAtBarrier(duration);
    }

    public void TakeDamage(float damage, bool isMagic)
    {
        TakeDamage(new DamagePacket(
            damage,
            isMagic ? DamageFamily.Magical : DamageFamily.Physical,
            isMagic ? DamageModifier.Default : DamageModifier.Default));
    }

    public float TakeDamage(DamagePacket packet)
    {
        if (!IsAlive || unitData == null || packet.Amount <= 0f) return 0f;
        if (Time.time < invulnerableUntil) return 0f;
        if (spiritWard)
        {
            spiritWard = false;
            return 0f;
        }

        if (packet.Family != DamageFamily.Physical && magicBarrier > 0f)
        {
            float absorbed = Mathf.Min(magicBarrier, packet.Amount);
            magicBarrier -= absorbed;
            packet = packet.WithAmount(packet.Amount - absorbed);
            if (packet.Amount <= 0f) return 0f;
        }

        HeroInventory heroInventory = FindAnyObjectByType<HeroInventory>();
        if (heroInventory != null) packet = heroInventory.SuppressEnemySpell(packet, transform.position);

        EntropyStatus entropy = GetComponent<EntropyStatus>();
        if (packet.Family == DamageFamily.Chaos)
        {
            entropy = entropy ?? gameObject.AddComponent<EntropyStatus>();
            if (entropy.AddStack(true))
            {
                currentHp = 0f;
                Die();
                return packet.Amount;
            }
        }

        float finalDamage = CombatResolver.Resolve(
            packet,
            unitData.armor * permanentMultiplier + GetAuraArmor(),
            unitData.magicResistance,
            entropy != null ? entropy.ResistanceReduction : 0f);
        float minimumHealth = IsUndying() ? 1f : 0f;
        currentHp = Mathf.Max(minimumHealth, currentHp - finalDamage);
        if (currentHp <= 0f) Die();
        return finalDamage;
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        currentHp = Mathf.Min(MaxHealth, currentHp + amount);
    }

    public void ApplyPermanentMultiplier(float multiplier)
    {
        permanentMultiplier *= Mathf.Clamp(multiplier, 0.1f, 10f);
        currentHp = Mathf.Min(currentHp, MaxHealth);
    }

    public void ApplyTemporaryBuff(float damageMultiplier, float speedMultiplier, float duration)
    {
        temporaryDamageMultiplier = Mathf.Max(temporaryDamageMultiplier, damageMultiplier);
        temporarySpeedMultiplier = Mathf.Max(temporarySpeedMultiplier, speedMultiplier);
        temporaryBuffUntil = Mathf.Max(temporaryBuffUntil, Time.time + Mathf.Max(0.1f, duration));
    }

    public void ApplyAura(
        float damageMultiplier,
        float flatDamageBonus,
        float armorBonus,
        float speedMultiplier,
        float lifesteal,
        bool undying)
    {
        auraDamageMultiplier = Mathf.Max(1f, damageMultiplier);
        auraFlatDamageBonus = Mathf.Max(0f, flatDamageBonus);
        auraArmorBonus = Mathf.Max(0f, armorBonus);
        auraSpeedMultiplier = Mathf.Max(1f, speedMultiplier);
        auraLifesteal = Mathf.Max(0f, lifesteal);
        auraUndying = undying;
        auraExpiresAt = Time.time + 0.75f;
    }

    public void GrantSpiritWard()
    {
        spiritWard = true;
    }

    public void GrantMagicBarrier(float amount)
    {
        magicBarrier = Mathf.Max(magicBarrier, Mathf.Max(0f, amount));
    }

    public void GrantInvulnerability(float duration)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + Mathf.Max(0.1f, duration));
    }

    public void ForceAttack()
    {
        nextAttackTime = 0f;
        RefreshTarget();
        if (CanAttackTarget()) AttackTarget();
    }

    public void CommandMoveToLane(BattleLane lane)
    {
        if (Lane == lane && !returningToBase) return;
        commandedLane = lane;
        commandedLaneX = BattleLaneUtility.GetX(lane);
        changingLane = true;
        returningToBase = false;
        BeginLogisticsDelay();
        target = null;
        bossTarget = null;
        castleTarget = null;
    }

    public void CommandReturnToBase()
    {
        returningToBase = true;
        changingLane = false;
        BeginLogisticsDelay();
        target = null;
        bossTarget = null;
        castleTarget = null;
    }

    private void RefreshTarget()
    {
        if (CanAttackTarget() || Time.time < nextTargetScanTime) return;

        nextTargetScanTime = Time.time + targetScanInterval;
        target = FindNearestEnemy();
    }

    private EnemyAI FindNearestEnemy()
    {
        EnemyAI nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (EnemyAI enemy in CombatRegistry.ActiveEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;

            float distance = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance > attackRange * attackRange) continue;
            if (distance >= nearestDistance) continue;

            nearest = enemy;
            nearestDistance = distance;
        }

        return nearest;
    }

    private bool CanAttackTarget()
    {
        return target != null &&
            target.isActiveAndEnabled &&
            target.IsAlive &&
            !IsUndying() &&
            Vector2.Distance(transform.position, target.transform.position) <= attackRange;
    }

    private void AttackTarget()
    {
        if (Time.time < nextAttackTime || unitData == null) return;

        float minimum = Mathf.Max(1f, unitData.minDamage);
        float maximum = Mathf.Max(minimum, unitData.maxDamage);
        float damage = Random.Range(minimum, maximum) *
            permanentMultiplier *
            temporaryDamageMultiplier *
            GetAuraDamageMultiplier() +
            GetAuraFlatDamageBonus();
        float dealt = target.TakeDamage(new DamagePacket(damage, DamageFamily.Physical, DamageModifier.Default, gameObject));
        if (auraLifesteal > 0f && Time.time <= auraExpiresAt)
        {
            Heal(dealt * Mathf.Min(1f, auraLifesteal));
            if (auraLifesteal > 1f)
            {
                FindAnyObjectByType<PlayerHealth>()?.Heal(dealt);
            }
        }
        nextAttackTime = Time.time + Mathf.Max(0.15f, unitData.attackRate);
    }

    private void RefreshCastleTarget()
    {
        if (CanAttackCastle()) return;

        if (castleTarget == null || !castleTarget.IsAlive)
        {
            castleTarget = FindAnyObjectByType<EnemyCastle>();
        }
    }

    private void RefreshBossTarget()
    {
        if (bossTarget != null && bossTarget.IsAlive) return;
        bossTarget = FindAnyObjectByType<DemonBossController>();
    }

    private bool CanAttackBoss()
    {
        return bossTarget != null &&
            bossTarget.IsAlive &&
            !IsUndying() &&
            Vector2.Distance(transform.position, bossTarget.transform.position) <= attackRange;
    }

    private void AttackBoss()
    {
        if (Time.time < nextAttackTime || unitData == null || IsUndying()) return;
        float minimum = Mathf.Max(1f, unitData.minDamage);
        float maximum = Mathf.Max(minimum, unitData.maxDamage);
        float damage = Random.Range(minimum, maximum) *
            permanentMultiplier *
            temporaryDamageMultiplier *
            GetAuraDamageMultiplier() +
            GetAuraFlatDamageBonus();
        float dealt = bossTarget.TakeDamage(new DamagePacket(
            damage,
            DamageFamily.Physical,
            DamageModifier.Default,
            gameObject));
        if (auraLifesteal > 0f && Time.time <= auraExpiresAt)
        {
            Heal(dealt * Mathf.Min(1f, auraLifesteal));
            if (auraLifesteal > 1f) FindAnyObjectByType<PlayerHealth>()?.Heal(dealt);
        }
        nextAttackTime = Time.time + Mathf.Max(0.15f, unitData.attackRate);
    }

    private bool CanAttackCastle()
    {
        return castleTarget != null &&
            castleTarget.isActiveAndEnabled &&
            castleTarget.IsAlive &&
            !IsUndying() &&
            Vector2.Distance(transform.position, castleTarget.transform.position) <= Mathf.Max(attackRange, 2.4f);
    }

    private void AttackCastle()
    {
        if (Time.time < nextAttackTime || unitData == null || IsUndying()) return;

        float damage = Mathf.Max(1f, unitData.towerDamage) * permanentMultiplier * temporaryDamageMultiplier;
        castleTarget.TakeDamage(damage);
        GameManager.Instance?.RewardAttackFrontHit(unitData);
        nextAttackTime = Time.time + Mathf.Max(0.15f, unitData.attackRate);
    }

    private IEnumerator BarrierWaitRoutine(float duration)
    {
        CurrentMovementState = UnitMovementState.Wait;
        BrainState = UnitBrainState.Wait;
        yield return new WaitForSeconds(duration);

        CurrentMovementState = UnitMovementState.Moving;
        BrainState = UnitBrainState.Move;
        barrierWaitCoroutine = null;
    }

    private void Die()
    {
        currentHp = 0f;
        BrainState = UnitBrainState.Dead;
        ObjectPoolManager.Return(gameObject);
    }

    private void RefreshRuntimeModifiers()
    {
        if (Time.time < temporaryBuffUntil) return;
        temporaryDamageMultiplier = 1f;
        temporarySpeedMultiplier = 1f;
    }

    private float GetMoveSpeed()
    {
        return currentMoveSpeed * permanentMultiplier * temporarySpeedMultiplier * GetAuraSpeedMultiplier();
    }

    private float GetAuraDamageMultiplier()
    {
        return Time.time <= auraExpiresAt ? auraDamageMultiplier : 1f;
    }

    private float GetAuraArmor()
    {
        return Time.time <= auraExpiresAt ? auraArmorBonus : 0f;
    }

    private float GetAuraFlatDamageBonus()
    {
        return Time.time <= auraExpiresAt ? auraFlatDamageBonus : 0f;
    }

    private float GetAuraSpeedMultiplier()
    {
        return Time.time <= auraExpiresAt ? auraSpeedMultiplier : 1f;
    }

    private bool IsUndying()
    {
        return Time.time <= auraExpiresAt && auraUndying;
    }

    private bool HandleLogisticsCommand()
    {
        if ((changingLane || returningToBase) && Time.time < logisticsReadyAt)
        {
            BrainState = UnitBrainState.Wait;
            return true;
        }

        if (changingLane)
        {
            logisticsDelayPaid = true;
            BrainState = UnitBrainState.ChangeLane;
            Vector2 destination = new Vector2(commandedLaneX, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, destination, GetMoveSpeed() * Time.deltaTime);
            if (Vector2.Distance(transform.position, destination) <= 0.05f)
            {
                SetCustomLane(commandedLaneX, commandedLane);
                changingLane = false;
                logisticsDelayPaid = false;
            }
            return true;
        }

        if (returningToBase)
        {
            logisticsDelayPaid = true;
            BrainState = UnitBrainState.ReturnToBase;
            Vector2 destination = new Vector2(laneX, -3.7f);
            transform.position = Vector2.MoveTowards(transform.position, destination, GetMoveSpeed() * Time.deltaTime);
            if (Vector2.Distance(transform.position, destination) <= 0.05f)
            {
                currentHp = MaxHealth;
                returningToBase = false;
                logisticsDelayPaid = false;
            }
            return true;
        }

        return false;
    }

    private void BeginLogisticsDelay()
    {
        bool barrierActive = BattleFlowController.Instance == null ||
            BattleFlowController.Instance.Phase == BattlePhase.SeparatedFronts;
        logisticsReadyAt = barrierActive ? Time.time + 15f : Time.time;
        logisticsDelayPaid = !barrierActive;
    }

    private void MoveAlongLanePath()
    {
        if (pathIndex >= 0)
        {
            Vector2 destination = BattleLaneUtility.GetPathPoint(Lane, pathIndex);
            transform.position = Vector2.MoveTowards(transform.position, destination, GetMoveSpeed() * Time.deltaTime);
            if (Vector2.Distance(transform.position, destination) <= 0.08f)
            {
                pathIndex--;
            }
            return;
        }

        EnemyCastle castle = FindAnyObjectByType<EnemyCastle>();
        Vector2 finalDestination = castle != null
            ? (Vector2)castle.transform.position
            : BattleLaneUtility.GetPathPoint(Lane, 0);
        transform.position = Vector2.MoveTowards(transform.position, finalDestination, GetMoveSpeed() * Time.deltaTime);
    }

    private static int FindNearestPathIndex(BattleLane lane, Vector2 position)
    {
        Vector2[] path = BattleLaneUtility.GetPath(lane);
        int nearest = path.Length - 1;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < path.Length; i++)
        {
            float distance = (path[i] - position).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearest = i;
            nearestDistance = distance;
        }

        return nearest;
    }
}
