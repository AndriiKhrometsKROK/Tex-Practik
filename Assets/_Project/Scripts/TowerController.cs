// Шукає цілі в радіусі башти, стріляє за кулдауном і застосовує властивості TowerData.
using UnityEngine;

public class TowerController : MonoBehaviour
{
    public TowerData data;
    private float _nextFireTime;
    private int _shotCount;
    private float _damageMultiplier = 1f;
    private float _fireRateMultiplier = 1f;
    private float _buffUntil;
    private string _runtimeVisualKey;

    private void OnEnable()
    {
        CombatRegistry.Register(this);
    }

    private void OnDisable()
    {
        CombatRegistry.Unregister(this);
    }

    void Update()
    {
        if (data == null) return;
        RefreshRuntimeVisualIfNeeded();

        if (Time.time >= _buffUntil)
        {
            _damageMultiplier = 1f;
            _fireRateMultiplier = 1f;
        }

        if (data.damageModifier == DamageModifier.Light && TryHealAlly())
        {
            return;
        }

        // Шукаємо найближчого ворога в радіусі
        GameObject target = FindNearestEnemy();

        if (target != null && Time.time >= _nextFireTime)
        {
            Shoot(target);
            _nextFireTime = Time.time + data.fireRate / Mathf.Max(0.1f, _fireRateMultiplier);
        }
    }

    GameObject FindNearestEnemy()
    {
        GameObject nearest = null;
        float minDistance = data.attackRadius;

        foreach (EnemyAI enemy in CombatRegistry.ActiveEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy.gameObject;
            }
        }

        DemonBossController boss = FindAnyObjectByType<DemonBossController>();
        if (boss != null && boss.IsAlive)
        {
            float bossDistance = Vector2.Distance(transform.position, boss.transform.position);
            if (bossDistance < minDistance) nearest = boss.gameObject;
        }
        return nearest;
    }

    void Shoot(GameObject target)
    {
        _shotCount++;
        GameObject projGO = ObjectPoolManager.Spawn(data.projectilePrefab, transform.position, Quaternion.identity);
        if (projGO == null) return;

        Projectile proj = projGO.GetComponent<Projectile>();
        
        if (proj != null)
        {
            // Передаємо дані вежі в снаряд, щоб він знав, як бити
            bool deterministicPiercingCrit =
                data.damageModifier == DamageModifier.Piercing &&
                data.towerLevel >= 3 &&
                _shotCount % 5 == 0;
            proj.Setup(target.transform, data, _damageMultiplier, deterministicPiercingCrit);
        }
    }

    public void ApplyTemporaryBuff(float damageMultiplier, float fireRateMultiplier, float duration)
    {
        _damageMultiplier = Mathf.Max(_damageMultiplier, damageMultiplier);
        _fireRateMultiplier = Mathf.Max(_fireRateMultiplier, fireRateMultiplier);
        _buffUntil = Mathf.Max(_buffUntil, Time.time + Mathf.Max(0.1f, duration));
    }

    public void RefreshRuntimeVisual()
    {
        _runtimeVisualKey = null;
        RefreshRuntimeVisualIfNeeded();
    }

    private void RefreshRuntimeVisualIfNeeded()
    {
        string key = RuntimeTowerVisuals.GetVisualKey(gameObject, data);
        if (_runtimeVisualKey == key) return;

        RuntimeTowerVisuals.ApplyIfKnown(gameObject, data);
        _runtimeVisualKey = key;
    }

    private bool TryHealAlly()
    {
        if (Time.time < _nextFireTime) return false;

        AllyController target = null;
        float lowestRatio = 1f;
        float healAmount = Random.Range(data.minDamage, data.maxDamage) * _damageMultiplier;
        foreach (AllyController ally in CombatRegistry.ActiveAllies)
        {
            if (!ally.IsAlive || Vector2.Distance(transform.position, ally.transform.position) > data.attackRadius) continue;
            float ratio = ally.MaxHealth > 0f ? ally.CurrentHealth / ally.MaxHealth : 1f;
            if (ratio < lowestRatio)
            {
                lowestRatio = ratio;
                target = ally;
            }
        }

        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        bool canHealPlayer = player != null &&
            player.currentHealth > 0f &&
            player.currentHealth < player.maxHealth &&
            Vector2.Distance(transform.position, player.transform.position) <= data.attackRadius;
        FrontTower frontTower = BattleFlowController.Instance != null
            ? BattleFlowController.Instance.DefenseFrontTower
            : null;
        bool canHealFront = frontTower != null && frontTower.IsAlive && frontTower.CurrentHealth < frontTower.MaxHealth;

        if (target == null && !canHealPlayer && !canHealFront) return false;
        target?.Heal(healAmount);
        if (target != null && data.towerLevel >= 3 && target.CurrentHealth / Mathf.Max(1f, target.MaxHealth) < 0.3f)
        {
            target.GrantSpiritWard();
        }
        if (target == null && canHealPlayer)
        {
            player.Heal(healAmount);
            if (data.towerLevel >= 3) player.GetComponent<HeroStats>()?.GrantSpiritWard();
        }
        else if (target == null && canHealFront)
        {
            frontTower.Heal(healAmount);
        }

        if (data.towerLevel >= 2)
        {
            foreach (AllyController ally in CombatRegistry.ActiveAllies)
            {
                if (ally != target && Vector2.Distance(transform.position, ally.transform.position) <= data.attackRadius)
                    ally.Heal(Mathf.Max(1f, data.minDamage * 0.12f));
            }
            if (canHealPlayer) player.Heal(Mathf.Max(1f, data.minDamage * 0.12f));
            if (canHealFront) frontTower.Heal(Mathf.Max(1f, data.minDamage * 0.12f));
        }
        _nextFireTime = Time.time + data.fireRate / Mathf.Max(0.1f, _fireRateMultiplier);
        return true;
    }
}
