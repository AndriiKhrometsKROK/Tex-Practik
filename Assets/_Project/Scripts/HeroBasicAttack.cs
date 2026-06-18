// Виконує звичайну атаку Кенама з кулдауном, критами, вампіризмом і модифікаторами предметів.
using UnityEngine;

public class HeroBasicAttack : MonoBehaviour
{
    [SerializeField, Min(1f)] private float damage = 35f;
    [SerializeField, Min(0.1f)] private float attackRange = 3.2f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.05f;

    private float nextAttackTime;
    private HeroStats stats;
    private HeroInventory inventory;

    private void Awake()
    {
        stats = GetComponent<HeroStats>() ?? gameObject.AddComponent<HeroStats>();
        inventory = GetComponent<HeroInventory>() ?? gameObject.AddComponent<HeroInventory>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || Time.time < nextAttackTime) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        TrainingDummyController dummy = FindTrainingDummyNear(mouseWorld);
        if (dummy != null)
        {
            GetComponent<HeroVisualAnimator>()?.PlayAttack();
            HeroAttackResult attack = RollAttack();
            float dealt = dummy.TakeDamage(attack.Damage, attack.Critical);
            DamageNumberController.Show(dummy.transform.position, dealt, attack.Critical);
            inventory?.HealFromPhysicalDamage(dealt);
            nextAttackTime = Time.time + attackCooldown / Mathf.Max(0.1f, stats != null ? stats.AttackSpeedMultiplier : 1f);
            return;
        }

        EnemyAI enemy = FindEnemyNear(mouseWorld);
        if (enemy != null)
        {
            GetComponent<HeroVisualAnimator>()?.PlayAttack();
            HeroAttackResult attack = RollAttack();
            DamageFamily family = stats != null && stats.GreyManaUnlocked
                ? stats.MimickedFamily
                : DamageFamily.Physical;
            float dealt = enemy.TakeDamage(new DamagePacket(
                attack.Damage,
                family,
                DamageModifier.Default,
                gameObject));
            DamageNumberController.Show(enemy.transform.position, dealt, attack.Critical);
            inventory?.HealFromPhysicalDamage(dealt);
            if (inventory != null && inventory.HasBattleFury) ApplyBattleFurySplash(enemy, attack.Damage * 0.45f);
            nextAttackTime = Time.time + attackCooldown / Mathf.Max(0.1f, stats != null ? stats.AttackSpeedMultiplier : 1f);
            return;
        }

        DemonBossController boss = FindBossNear(mouseWorld);
        if (boss != null)
        {
            GetComponent<HeroVisualAnimator>()?.PlayAttack();
            HeroAttackResult attack = RollAttack();
            float dealt = boss.TakeDamage(new DamagePacket(
                attack.Damage,
                stats != null && stats.GreyManaUnlocked ? stats.MimickedFamily : DamageFamily.Physical,
                DamageModifier.Default,
                gameObject));
            DamageNumberController.Show(boss.transform.position, dealt, attack.Critical);
            inventory?.HealFromPhysicalDamage(dealt);
            nextAttackTime = Time.time + attackCooldown / Mathf.Max(0.1f, stats != null ? stats.AttackSpeedMultiplier : 1f);
            return;
        }

        EnemyCastle castle = FindAnyObjectByType<EnemyCastle>();
        if (castle != null && castle.IsAlive &&
            Vector2.Distance(transform.position, castle.transform.position) <= attackRange &&
            Vector2.Distance(mouseWorld, castle.transform.position) <= 2f)
        {
            GetComponent<HeroVisualAnimator>()?.PlayAttack();
            HeroAttackResult attack = RollAttack();
            castle.TakeDamage(attack.Damage);
            DamageNumberController.Show(castle.transform.position, attack.Damage, attack.Critical);
            nextAttackTime = Time.time + attackCooldown / Mathf.Max(0.1f, stats != null ? stats.AttackSpeedMultiplier : 1f);
        }
    }

    private HeroAttackResult RollAttack()
    {
        float baseDamage = stats != null ? stats.AttackDamage : damage;
        return inventory != null
            ? inventory.RollHeroAttack(baseDamage)
            : new HeroAttackResult(baseDamage, false);
    }

    private EnemyAI FindEnemyNear(Vector2 mouseWorld)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorld, 0.8f);
        foreach (Collider2D hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy != null && enemy.IsAlive &&
                Vector2.Distance(transform.position, enemy.transform.position) <= attackRange)
            {
                return enemy;
            }
        }

        return null;
    }

    private DemonBossController FindBossNear(Vector2 mouseWorld)
    {
        DemonBossController boss = FindAnyObjectByType<DemonBossController>();
        if (boss == null || !boss.IsAlive) return null;
        return Vector2.Distance(transform.position, boss.transform.position) <= attackRange &&
            Vector2.Distance(mouseWorld, boss.transform.position) <= 1.4f
            ? boss
            : null;
    }

    private TrainingDummyController FindTrainingDummyNear(Vector2 mouseWorld)
    {
        TrainingDummyController dummy = FindAnyObjectByType<TrainingDummyController>();
        if (dummy == null || !dummy.IsAlive) return null;
        return Vector2.Distance(transform.position, dummy.transform.position) <= attackRange &&
            Vector2.Distance(mouseWorld, dummy.transform.position) <= 1.4f
            ? dummy
            : null;
    }

    private void ApplyBattleFurySplash(EnemyAI primary, float splashDamage)
    {
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy == primary || !enemy.IsAlive) continue;
            if (Vector2.Distance(primary.transform.position, enemy.transform.position) <= 1.8f)
            {
                enemy.TakeDamage(new DamagePacket(
                    splashDamage,
                    DamageFamily.Physical,
                    DamageModifier.Piercing,
                    gameObject));
            }
        }
    }
}
