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
    private Transform _targetWaypoint;
    private int _waypointIndex;
    private EnemyHealthBar _healthBar;
    private bool _isDead;
    private float _nextAllyScanTime;
    private AllyController _allyTarget;
    private float _laneX;

    public UnitMovementState CurrentMovementState { get; private set; } = UnitMovementState.Moving;
    public bool IsAlive => !_isDead && gameObject.activeInHierarchy;

    public void SetLane(BattleLane lane)
    {
        _laneX = BattleLaneUtility.GetX(lane);
        Vector3 position = transform.position;
        position.x = _laneX;
        transform.position = position;
        transform.localScale = Vector3.one * 0.72f;

        int sortingOrder = lane == BattleLane.Upper ? 8 : 11;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void OnEnable()
    {
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
        }

        if (Waypoints.points != null && Waypoints.points.Length > 0)
        {
            _targetWaypoint = Waypoints.points[0];
            _waypointIndex = 0;
        }
        else
        {
            Debug.LogError("Waypoints not found. Add a Waypoints object to the scene.");
            _targetWaypoint = null;
            _waypointIndex = 0;
        }

        EnsureHealthBar();
        _healthBar?.Attach(transform, GetMaxHealth(), _currentHp);
    }

    private void OnDisable()
    {
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

        RefreshAllyTarget();
        if (CanAttackAlly())
        {
            AttackAlly();
            return;
        }

        _allyTarget = null;
        MoveAlongPath();
    }

    public void TakeDamage(float amount, bool isMagicDamage = false)
    {
        if (_isDead) return;
        if (data == null) return;

        float finalDamage = amount;

        if (isMagicDamage)
        {
            finalDamage -= finalDamage * data.magicResistance;
        }
        else
        {
            finalDamage = Mathf.Max(1f, finalDamage - data.armor);
        }

        ApplyHealthDamage(finalDamage);
    }

    public void ApplySlow(float slowFactor, float duration)
    {
        if (slowFactor <= 0f || duration <= 0f) return;

        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
        }

        _slowCoroutine = StartCoroutine(SlowRoutine(Mathf.Clamp01(slowFactor), duration));
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

    private void MoveAlongPath()
    {
        if (_targetWaypoint == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            new Vector2(_laneX, _targetWaypoint.position.y),
            _currentMoveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, new Vector2(_laneX, _targetWaypoint.position.y)) <= 0.1f)
        {
            GetNextWaypoint();
        }
    }

    private void GetNextWaypoint()
    {
        if (_waypointIndex >= Waypoints.points.Length - 1)
        {
            ReachBase();
            return;
        }

        _waypointIndex++;
        _targetWaypoint = Waypoints.points[_waypointIndex];
    }

    private void ReachBase()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyReachedBase(data);
        }

        Debug.Log(gameObject.name + " reached the base.");
        ObjectPoolManager.Return(gameObject);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

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
            float randomDamage = Random.Range(data.minDamage, data.maxDamage);
            other.GetComponent<PlayerHealth>()?.TakeDamage(randomDamage);
            _nextAttackTime = Time.time + data.attackRate;
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, allyAttackRange);
        AllyController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            AllyController ally = hit.GetComponentInParent<AllyController>();
            if (ally == null || !ally.IsAlive) continue;

            float distance = ((Vector2)ally.transform.position - (Vector2)transform.position).sqrMagnitude;
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
            Vector2.Distance(transform.position, _allyTarget.transform.position) <= allyAttackRange;
    }

    private void AttackAlly()
    {
        if (Time.time < _nextAttackTime || data == null) return;

        float minimum = Mathf.Max(1f, data.minDamage);
        float maximum = Mathf.Max(minimum, data.maxDamage);
        _allyTarget.TakeDamage(Random.Range(minimum, maximum), false);
        _nextAttackTime = Time.time + Mathf.Max(0.15f, data.attackRate);
    }

    private IEnumerator SlowRoutine(float slowFactor, float duration)
    {
        _currentMoveSpeed = _baseMoveSpeed * slowFactor;
        yield return new WaitForSeconds(duration);

        _currentMoveSpeed = _baseMoveSpeed;
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
        yield return new WaitForSeconds(duration);

        CurrentMovementState = UnitMovementState.Moving;
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
        return data != null ? data.maxHp : 100f;
    }
}
