using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;

    private Transform _target;
    private TowerData _sourceData;

    public void Setup(Transform target, TowerData data)
    {
        _target = target;
        _sourceData = data;
    }

    private void OnDisable()
    {
        _target = null;
        _sourceData = null;
    }

    private void Update()
    {
        if (_target == null)
        {
            ObjectPoolManager.Return(gameObject);
            return;
        }

        Vector3 direction = _target.position - transform.position;
        transform.position += direction.normalized * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _target.position) < 0.2f)
        {
            ApplyHitEffects(_target.gameObject);
            ObjectPoolManager.Return(gameObject);
        }
    }

    private void ApplyHitEffects(GameObject enemy)
    {
        if (_sourceData == null) return;

        if (_sourceData.aoeRadius > 0f)
        {
            ApplyAreaDamage(enemy.transform.position);
            return;
        }

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI == null) return;

        ApplyTowerEffects(enemyAI, CalculateDamage());
    }

    private void ApplyAreaDamage(Vector3 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _sourceData.aoeRadius);
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
            enemyAI.ApplySlow(_sourceData.slowFactor, GetSlowDuration());
        }

        if (_sourceData.dotDamage > 0f)
        {
            enemyAI.ApplyPoison(_sourceData.dotDamage, GetDotDuration(), GetDotTickInterval());
        }
    }

    private float GetSlowDuration()
    {
        return _sourceData.slowDuration > 0f ? _sourceData.slowDuration : 2.5f;
    }

    private float GetDotDuration()
    {
        return _sourceData.dotDuration > 0f ? _sourceData.dotDuration : 3f;
    }

    private float GetDotTickInterval()
    {
        return _sourceData.dotTickInterval > 0f ? _sourceData.dotTickInterval : 1f;
    }
}
