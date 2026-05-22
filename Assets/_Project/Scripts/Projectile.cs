using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private const float SlowDuration = 2.5f;

    public float speed = 10f;

    private Transform _target;
    private TowerData _sourceData;

    public void Setup(Transform target, TowerData data)
    {
        _target = target;
        _sourceData = data;
    }

    private void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = _target.position - transform.position;
        transform.position += direction.normalized * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _target.position) < 0.2f)
        {
            ApplyHitEffects(_target.gameObject);
            Destroy(gameObject);
        }
    }

    private void ApplyHitEffects(GameObject enemy)
    {
        if (_sourceData == null) return;

        if (_sourceData.aoeRadius > 0f)
        {
            ApplyAreaDamage();
            return;
        }

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI == null) return;

        ApplyTowerEffects(enemyAI, CalculateDamage());
    }

    private void ApplyAreaDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _sourceData.aoeRadius);
        List<EnemyAI> damagedEnemies = new List<EnemyAI>();
        float damage = CalculateDamage();

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            EnemyAI enemyAI = hit.GetComponent<EnemyAI>();
            if (enemyAI == null || damagedEnemies.Contains(enemyAI)) continue;

            ApplyTowerEffects(enemyAI, damage);
            damagedEnemies.Add(enemyAI);
        }
    }

    private float CalculateDamage()
    {
        float damage = Random.Range(_sourceData.minDamage, _sourceData.maxDamage);

        if (Random.value <= _sourceData.critChance)
        {
            damage *= 2f;
        }

        return damage;
    }

    private void ApplyTowerEffects(EnemyAI enemyAI, float damage)
    {
        enemyAI.TakeDamage(damage, _sourceData.isMagic);

        if (_sourceData.slowFactor > 0f)
        {
            enemyAI.ApplySlow(_sourceData.slowFactor, SlowDuration);
        }
    }
}
