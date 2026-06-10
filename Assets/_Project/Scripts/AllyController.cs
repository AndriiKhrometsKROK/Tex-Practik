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
    private float laneX;

    public UnitMovementState CurrentMovementState { get; private set; } = UnitMovementState.Moving;
    public bool IsAlive => currentHp > 0f;

    public void SetLane(BattleLane lane)
    {
        Vector3 position = transform.position;
        laneX = BattleLaneUtility.GetX(lane);
        position.x = laneX;
        transform.position = position;
        transform.localScale = Vector3.one * 0.72f;

        int sortingOrder = lane == BattleLane.Upper ? 9 : 12;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingOrder = sortingOrder;
        }
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

        if (unitData != null && !string.IsNullOrWhiteSpace(unitData.unitName))
        {
            gameObject.name = unitData.unitName;
        }
    }

    private void Update()
    {
        if (!IsAlive || unitData == null) return;
        if (CurrentMovementState == UnitMovementState.Wait) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        RefreshTarget();
        if (CanAttackTarget())
        {
            AttackTarget();
            return;
        }

        target = null;
        RefreshCastleTarget();
        if (CanAttackCastle())
        {
            AttackCastle();
            return;
        }

        castleTarget = null;
        transform.Translate(Vector3.up * currentMoveSpeed * Time.deltaTime);
        Vector3 position = transform.position;
        position.x = laneX;
        transform.position = position;
    }

    private void OnDisable()
    {
        if (barrierWaitCoroutine != null)
        {
            StopCoroutine(barrierWaitCoroutine);
            barrierWaitCoroutine = null;
        }

        target = null;
        castleTarget = null;
        CurrentMovementState = UnitMovementState.Moving;
    }

    public void WaitAtBarrier(float duration)
    {
        if (duration <= 0f || barrierWaitCoroutine != null) return;
        barrierWaitCoroutine = StartCoroutine(BarrierWaitRoutine(duration));
    }

    public void TakeDamage(float damage, bool isMagic)
    {
        if (!IsAlive || unitData == null || damage <= 0f) return;

        float finalDamage = isMagic
            ? damage * (1f - unitData.magicResistance)
            : Mathf.Max(1f, damage - unitData.armor);

        currentHp -= finalDamage;
        if (currentHp <= 0f)
        {
            Die();
        }
    }

    private void RefreshTarget()
    {
        if (CanAttackTarget() || Time.time < nextTargetScanTime) return;

        nextTargetScanTime = Time.time + targetScanInterval;
        target = FindNearestEnemy();
    }

    private EnemyAI FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        EnemyAI nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null || !enemy.IsAlive) continue;

            float distance = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
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
            Vector2.Distance(transform.position, target.transform.position) <= attackRange;
    }

    private void AttackTarget()
    {
        if (Time.time < nextAttackTime || unitData == null) return;

        float minimum = Mathf.Max(1f, unitData.minDamage);
        float maximum = Mathf.Max(minimum, unitData.maxDamage);
        target.TakeDamage(Random.Range(minimum, maximum));
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

    private bool CanAttackCastle()
    {
        return castleTarget != null &&
            castleTarget.isActiveAndEnabled &&
            castleTarget.IsAlive &&
            Vector2.Distance(transform.position, castleTarget.transform.position) <= Mathf.Max(attackRange, 2.4f);
    }

    private void AttackCastle()
    {
        if (Time.time < nextAttackTime || unitData == null) return;

        castleTarget.TakeDamage(Mathf.Max(1f, unitData.towerDamage));
        nextAttackTime = Time.time + Mathf.Max(0.15f, unitData.attackRate);
    }

    private IEnumerator BarrierWaitRoutine(float duration)
    {
        CurrentMovementState = UnitMovementState.Wait;
        yield return new WaitForSeconds(duration);

        CurrentMovementState = UnitMovementState.Moving;
        barrierWaitCoroutine = null;
    }

    private void Die()
    {
        currentHp = 0f;
        Destroy(gameObject);
    }
}
