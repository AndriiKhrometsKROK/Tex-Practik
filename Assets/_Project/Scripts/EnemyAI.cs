using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public UnitData data;
    [SerializeField] private EnemyHealthBar healthBarPrefab;

    private float _currentHp;
    private float _baseMoveSpeed;
    private float _currentMoveSpeed;
    private float _nextAttackTime;
    private Coroutine _slowCoroutine;
    private Coroutine _poisonCoroutine;
    private Transform _targetWaypoint;
    private int _waypointIndex;
    private EnemyHealthBar _healthBar;
    private bool _isDead;

    private void OnEnable()
    {
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            _slowCoroutine = null;
        }

        if (data != null)
        {
            _currentHp = data.maxHp;
            _baseMoveSpeed = data.moveSpeed;
            _currentMoveSpeed = _baseMoveSpeed;
            _nextAttackTime = 0f;
            _isDead = false;
        }
        else
        {
            Debug.LogError("UnitData is not assigned for " + gameObject.name);
            _currentHp = 100f;
            _baseMoveSpeed = 1f;
            _currentMoveSpeed = _baseMoveSpeed;
            _nextAttackTime = 0f;
            _isDead = false;
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

        _healthBar?.Detach();
    }

    private void OnDestroy()
    {
        if (_healthBar != null)
        {
            Destroy(_healthBar.gameObject);
        }
    }

    private void Update()
    {
        if (data == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

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

    private void MoveAlongPath()
    {
        if (_targetWaypoint == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            _targetWaypoint.position,
            _currentMoveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, _targetWaypoint.position) <= 0.1f)
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
