// Рухає снаряд до цілі та застосовує шкоду, уповільнення, отруту й інші ефекти влучання.
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;

    private Transform _target;
    private TowerData _sourceData;
    private float _damageMultiplier = 1f;
    private bool _ignoreArmor;

    public void Setup(Transform target, TowerData data)
    {
        Setup(target, data, 1f, false);
    }

    public void Setup(Transform target, TowerData data, float damageMultiplier, bool ignoreArmor)
    {
        _target = target;
        _sourceData = data;
        _damageMultiplier = Mathf.Max(0f, damageMultiplier);
        _ignoreArmor = ignoreArmor;
    }

    private void OnDisable()
    {
        _target = null;
        _sourceData = null;
        _damageMultiplier = 1f;
        _ignoreArmor = false;
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
        if (enemyAI == null)
        {
            DemonBossController boss = enemy.GetComponentInParent<DemonBossController>();
            if (boss != null)
            {
                DamageFamily family = _sourceData.damageFamily;
                if (_sourceData.isMagic && family == DamageFamily.Physical) family = DamageFamily.Magical;
                boss.TakeDamage(new DamagePacket(
                    CalculateDamage(),
                    family,
                    _sourceData.damageModifier,
                    gameObject,
                    _ignoreArmor));
            }
            return;
        }

        ApplyTowerEffects(enemyAI, CalculateDamage());
        ApplyPiercingSplash(enemyAI);
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
        float damage = Random.Range(_sourceData.minDamage, _sourceData.maxDamage) * _damageMultiplier;

        if (Random.value <= _sourceData.critChance)
        {
            damage *= 2f;
        }

        return damage;
    }

    private void ApplyTowerEffects(EnemyAI enemyAI, float damage)
    {
        DamageFamily family = _sourceData.damageFamily;
        if (_sourceData.isMagic && family == DamageFamily.Physical) family = DamageFamily.Magical;
        DamagePacket packet = new DamagePacket(
            damage,
            family,
            _sourceData.damageModifier,
            gameObject,
            _ignoreArmor,
            false);
        enemyAI.TakeDamage(packet);

        if (_sourceData.slowFactor > 0f)
        {
            enemyAI.ApplySlow(_sourceData.slowFactor, GetSlowDuration());
        }

        if (_sourceData.dotDamage > 0f)
        {
            enemyAI.ApplyPoison(_sourceData.dotDamage, GetDotDuration(), GetDotTickInterval());
        }

        if (_sourceData.stunChance > 0f && Random.value <= _sourceData.stunChance)
        {
            enemyAI.ApplyRoot(0.8f);
        }
    }

    private void ApplyPiercingSplash(EnemyAI primary)
    {
        if (_sourceData == null ||
            _sourceData.damageModifier != DamageModifier.Piercing ||
            _sourceData.towerLevel < 2)
        {
            return;
        }

        Vector2 direction = ((Vector2)primary.transform.position - (Vector2)transform.position).normalized;
        Vector2 center = (Vector2)primary.transform.position + direction * 0.8f;
        float splash = CalculateDamage() * 0.35f;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, 1.1f))
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy == primary || !enemy.IsAlive) continue;
            enemy.TakeDamage(new DamagePacket(
                splash,
                DamageFamily.Physical,
                DamageModifier.Piercing,
                gameObject));
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
