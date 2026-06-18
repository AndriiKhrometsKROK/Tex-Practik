// Керує босом-демоном: фазами, здібностями, отриманням шкоди та ефектом накопичення ентропії.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum DemonBossPhase
{
    Dominant,
    Fractured,
    Exhausted
}

public class DemonBossController : MonoBehaviour
{
    public event Action<float, float> HealthChanged;
    public event Action<DemonBossPhase> PhaseChanged;

    [SerializeField] private float baseHealth = 5000f;
    [SerializeField] private float baseDamage = 80f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float attackRange = 3.2f;
    [SerializeField] private float moveSpeed = 1.6f;
    [SerializeField] private float armor = 18f;
    [SerializeField, Range(0f, 0.95f)] private float magicResistance = 0.35f;

    private float currentHealth;
    private float damage;
    private float power;
    private float nextAttackTime;
    private float nextPulseTime;
    private float nextChainsTime;
    private float nextSummonTime;
    private float nextCastleStrikeTime;
    private bool dead;
    private SpriteRenderer visual;

    public float CurrentHealth => currentHealth;
    public float MaxHealth { get; private set; }
    public bool IsAlive => !dead && currentHealth > 0f;
    public float Power => power;
    public DemonBossPhase Phase { get; private set; } = DemonBossPhase.Dominant;

    public static DemonBossController Spawn(float powerMultiplier)
    {
        GameObject bossObject = new GameObject("Demon Butler Boss");
        bossObject.tag = "Enemy";
        bossObject.transform.position = new Vector3(0f, 1.2f, 0f);
        DemonBossController boss = bossObject.AddComponent<DemonBossController>();
        boss.Configure(powerMultiplier);

        SpriteRenderer renderer = bossObject.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        renderer.color = KenamUiTheme.PurpleMuted;
        renderer.sortingOrder = 30;
        boss.visual = renderer;
        bossObject.transform.localScale = new Vector3(1.4f, 2.2f, 1f);

        CircleCollider2D collider = bossObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        return boss;
    }

    public void Configure(float powerMultiplier)
    {
        power = Mathf.Clamp(powerMultiplier, 0.1f, 1f);
        MaxHealth = baseHealth * power;
        currentHealth = MaxHealth;
        damage = baseDamage * power;
        moveSpeed *= Mathf.Lerp(0.65f, 1f, power);
        attackInterval /= Mathf.Lerp(0.7f, 1f, power);
        float cooldownPenalty = power <= 0.6f ? 1.45f : 1f;
        nextPulseTime = Time.time + 4f * cooldownPenalty;
        nextChainsTime = Time.time + 7f * cooldownPenalty;
        nextSummonTime = Time.time + 10f * cooldownPenalty;
        nextCastleStrikeTime = Time.time + 12f * cooldownPenalty;
        UpdatePhase();
    }

    private void Update()
    {
        if (!IsAlive) return;

        UpdatePhase();
        UpdateExhaustedVisual();
        TryUseAbilities();

        Transform target = FindClosestTarget();
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance <= attackRange)
            {
                if (Time.time >= nextAttackTime)
                {
                    DamageTarget(target, damage, DamageFamily.Pure, DamageModifier.Pure);
                    nextAttackTime = Time.time + attackInterval;
                }
                return;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime);
            return;
        }

        if (GameManager.Instance != null && Time.time >= nextAttackTime)
        {
            GameManager.Instance.DamageBase(damage);
            nextAttackTime = Time.time + attackInterval;
        }
    }

    public float TakeDamage(DamagePacket packet)
    {
        if (!IsAlive || packet.Amount <= 0f) return 0f;

        float entropyReduction = GetComponent<EntropyStatus>()?.ResistanceReduction ?? 0f;
        float finalDamage = CombatResolver.Resolve(packet, armor, magicResistance, entropyReduction);
        if (packet.Family == DamageFamily.Chaos)
        {
            EntropyStatus entropy = GetComponent<EntropyStatus>() ?? gameObject.AddComponent<EntropyStatus>();
            entropy.AddStack(false);
            entropyReduction = entropy.ResistanceReduction;
            finalDamage += MaxHealth * 0.025f;
        }
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        UpdatePhase();
        if (currentHealth <= 0f) Die();
        return finalDamage;
    }

    public void TakePercentDamage(float percent)
    {
        if (!IsAlive || percent <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - MaxHealth * percent);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        UpdatePhase();
        if (currentHealth <= 0f) Die();
    }

    private void TryUseAbilities()
    {
        float cooldownMultiplier = Phase == DemonBossPhase.Exhausted ? 1.5f :
            Phase == DemonBossPhase.Fractured ? 1.2f : 1f;

        if (Time.time >= nextPulseTime)
        {
            CastVoidPulse();
            nextPulseTime = Time.time + 8f * cooldownMultiplier;
        }

        if (power >= 0.4f && Time.time >= nextChainsTime)
        {
            CastChains();
            nextChainsTime = Time.time + 12f * cooldownMultiplier;
        }

        if (power >= 0.6f && Phase != DemonBossPhase.Exhausted && Time.time >= nextSummonTime)
        {
            SummonServants();
            nextSummonTime = Time.time + 18f * cooldownMultiplier;
        }

        if (power > 0.7f && Time.time >= nextCastleStrikeTime)
        {
            StrikeCastle();
            nextCastleStrikeTime = Time.time + 16f * cooldownMultiplier;
        }
    }

    private void CastVoidPulse()
    {
        float pulseDamage = damage * (Phase == DemonBossPhase.Dominant ? 0.8f : 0.55f);
        float radius = GetAbilityRadius(5f);
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player != null && Vector2.Distance(transform.position, player.transform.position) <= radius)
        {
            player.TakeDamage(new DamagePacket(
                pulseDamage,
                DamageFamily.Magical,
                DamageModifier.Dark,
                gameObject,
                false,
                true));
        }

        foreach (AllyController ally in new List<AllyController>(CombatRegistry.ActiveAllies))
        {
            if (ally != null && ally.IsAlive &&
                Vector2.Distance(transform.position, ally.transform.position) <= radius)
            {
                ally.TakeDamage(new DamagePacket(
                    pulseDamage,
                    DamageFamily.Magical,
                    DamageModifier.Dark,
                    gameObject,
                    false,
                    true));
            }
        }
        GameplayNotificationController.Show("Демон выпускає хвилю порожнечі.");
    }

    private void CastChains()
    {
        float radius = GetAbilityRadius(6.5f);
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player != null && Vector2.Distance(transform.position, player.transform.position) <= radius)
        {
            player.TakeDamage(new DamagePacket(
                damage * 0.45f,
                DamageFamily.Hybrid,
                DamageModifier.Mixed,
                gameObject,
                false,
                true));
        }

        foreach (AllyController ally in new List<AllyController>(CombatRegistry.ActiveAllies))
        {
            if (ally == null || !ally.IsAlive ||
                Vector2.Distance(transform.position, ally.transform.position) > radius) continue;
            ally.ApplyRoot(2.5f);
            ally.TakeDamage(new DamagePacket(
                damage * 0.3f,
                DamageFamily.Hybrid,
                DamageModifier.Mixed,
                gameObject,
                false,
                true));
        }
        GameplayNotificationController.Show("Просторові кайдани зупинили армію.");
    }

    private void SummonServants()
    {
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner == null) return;

        int count = Phase == DemonBossPhase.Dominant ? 4 : 2;
        for (int i = 0; i < count; i++)
        {
            spawner.SpawnBossMinion(Mathf.Max(0.7f, power * 2.5f));
        }
        GameplayNotificationController.Show("Демон закликав своїх слуг.");
    }

    private void StrikeCastle()
    {
        GameManager.Instance?.DamageBase(Mathf.Max(8f, damage * 0.35f));
        GameplayNotificationController.Show("Демон б'є безпосередньо по головному замку.");
    }

    private Transform FindClosestTarget()
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player != null)
        {
            closest = player.transform;
            closestDistance = Vector2.Distance(transform.position, player.transform.position);
        }

        foreach (AllyController ally in CombatRegistry.ActiveAllies)
        {
            if (ally == null || !ally.IsAlive || ally.IsUndyingAuraActive) continue;
            float distance = Vector2.Distance(transform.position, ally.transform.position);
            if (distance >= closestDistance) continue;
            closest = ally.transform;
            closestDistance = distance;
        }
        return closest;
    }

    private void DamageTarget(Transform target, float amount, DamageFamily family, DamageModifier modifier)
    {
        if (target == null) return;
        DamagePacket packet = new DamagePacket(amount, family, modifier, gameObject, family == DamageFamily.Pure, true);
        PlayerHealth player = target.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(packet);
            return;
        }
        target.GetComponent<AllyController>()?.TakeDamage(packet);
    }

    private void UpdatePhase()
    {
        if (MaxHealth <= 0f) return;
        float ratio = currentHealth / MaxHealth;
        DemonBossPhase nextPhase = ratio <= 0.35f || power <= 0.4f
            ? DemonBossPhase.Exhausted
            : ratio <= 0.7f || power <= 0.7f
                ? DemonBossPhase.Fractured
                : DemonBossPhase.Dominant;
        if (nextPhase == Phase) return;
        Phase = nextPhase;
        PhaseChanged?.Invoke(Phase);
        GameplayNotificationController.Show(
            Phase == DemonBossPhase.Exhausted
                ? "Форма Демона розпадається."
                : "У захисті Демона з'явилися тріщини.");
    }

    private void UpdateExhaustedVisual()
    {
        if (visual == null || power > 0.5f) return;
        float alpha = Phase == DemonBossPhase.Exhausted
            ? Mathf.Lerp(0.2f, 0.8f, Mathf.PingPong(Time.time * 4f, 1f))
            : Mathf.Lerp(0.55f, 0.9f, Mathf.PingPong(Time.time * 2f, 1f));
        Color color = visual.color;
        color.a = alpha;
        visual.color = color;
    }

    private float GetAbilityRadius(float baseRadius)
    {
        return baseRadius * Mathf.Lerp(0.55f, 1f, power);
    }

    private void Die()
    {
        if (dead) return;
        dead = true;
        GameAudioController.PlaySfx(GameSfxCue.DemonDestroyed, 0.95f);
        BattleFlowController.Instance?.BossDefeated();
        Destroy(gameObject);
    }
}

public class EntropyStatus : MonoBehaviour
{
    public int Stacks { get; private set; }
    public float ResistanceReduction => Mathf.Clamp01(Stacks * 0.04f);

    public bool AddStack(bool regularCreep)
    {
        Stacks = Mathf.Min(10, Stacks + 1);
        return regularCreep && Stacks >= 10;
    }

    public void ResetStacks()
    {
        Stacks = 0;
    }
}
