using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public UnitData data;

    private float _currentHp;
    private float _baseMoveSpeed;
    private float _currentMoveSpeed;
    private float _nextAttackTime;
    private Coroutine _slowCoroutine;
    private Transform _targetWaypoint;
    private int _waypointIndex;

    private void Start()
    {
        if (data != null)
        {
            _currentHp = data.maxHp;
            _baseMoveSpeed = data.moveSpeed;
            _currentMoveSpeed = _baseMoveSpeed;
        }
        else
        {
            Debug.LogError("UnitData is not assigned for " + gameObject.name);
            _currentHp = 100f;
            _baseMoveSpeed = 1f;
            _currentMoveSpeed = _baseMoveSpeed;
        }

        if (Waypoints.points != null && Waypoints.points.Length > 0)
        {
            _targetWaypoint = Waypoints.points[0];
        }
        else
        {
            Debug.LogError("Waypoints not found. Add a Waypoints object to the scene.");
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

        _currentHp -= finalDamage;

        if (_currentHp <= 0f)
        {
            Die();
        }
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
        Destroy(gameObject);
    }

    private void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyKilled(data);
        }

        Destroy(gameObject);
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
}
