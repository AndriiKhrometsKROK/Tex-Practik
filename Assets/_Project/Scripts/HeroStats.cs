// Зберігає базові й підсумкові характеристики героя та перераховує їх після зміни екіпірування.
using System;
using System.Collections.Generic;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public event Action StatsChanged;
    public event Action<float, float> ManaChanged;

    [Header("Core")]
    [Min(1f)] public float baseAttackDamage = 35f;
    [Min(0f)] public float baseSpellPower = 50f;
    [Min(0f)] public float baseArmor = 8f;
    [Range(0f, 0.95f)] public float baseMagicResistance = 0.2f;
    [Min(1f)] public float maxMana = 200f;
    [Min(0f)] public float manaRegeneration = 8f;

    public float CurrentMana { get; private set; }
    public float PermanentMultiplier { get; private set; } = 1f;
    public float FrontPenaltyMultiplier { get; private set; } = 1f;
    public float TemporaryDamageMultiplier { get; private set; } = 1f;
    public float TemporaryAttackSpeedMultiplier { get; private set; } = 1f;
    public float TemporaryArmorMultiplier { get; private set; } = 1f;
    public float TemporaryMagicResistanceBonus { get; private set; }
    public float MagicBarrier { get; private set; }
    public bool BlocksNextHit { get; private set; }
    public bool PhysicalImmune { get; private set; }
    public bool Invulnerable { get; private set; }
    public bool EchoSilenced { get; private set; }
    public bool GreyManaUnlocked { get; private set; }
    public DamageFamily MimickedFamily { get; private set; } = DamageFamily.Physical;
    public int AdaptedDamageCount => adaptedDamage.Count;

    public float AttackDamage => baseAttackDamage * PermanentMultiplier * FrontPenaltyMultiplier * TemporaryDamageMultiplier;
    public float SpellPower => baseSpellPower * PermanentMultiplier * FrontPenaltyMultiplier;
    public float Armor => baseArmor * PermanentMultiplier * FrontPenaltyMultiplier * TemporaryArmorMultiplier;
    public float MagicResistance => Mathf.Clamp(
        baseMagicResistance * PermanentMultiplier * FrontPenaltyMultiplier + TemporaryMagicResistanceBonus,
        0f,
        1f);
    public float AttackSpeedMultiplier => TemporaryAttackSpeedMultiplier;

    private float physicalImmuneUntil;
    private float invulnerableUntil;
    private float echoSilencedUntil;
    private float temporaryBuffUntil;
    private float temporaryDefenseUntil;
    private readonly Dictionary<DamageFamily, int> damageExposure = new Dictionary<DamageFamily, int>();
    private readonly HashSet<DamageFamily> adaptedDamage = new HashSet<DamageFamily>();

    private void Awake()
    {
        CurrentMana = maxMana;
        GreyManaUnlocked = CampaignProgress.Act2Completed;
    }

    private void Update()
    {
        if (CurrentMana < maxMana)
        {
            CurrentMana = Mathf.Min(maxMana, CurrentMana + manaRegeneration * Time.deltaTime);
            ManaChanged?.Invoke(CurrentMana, maxMana);
        }

        bool changed = false;
        if (PhysicalImmune && Time.time >= physicalImmuneUntil)
        {
            PhysicalImmune = false;
            changed = true;
        }

        if (Invulnerable && Time.time >= invulnerableUntil)
        {
            Invulnerable = false;
            changed = true;
        }

        if (EchoSilenced && Time.time >= echoSilencedUntil)
        {
            EchoSilenced = false;
            changed = true;
        }

        if (Time.time >= temporaryBuffUntil &&
            (TemporaryDamageMultiplier != 1f || TemporaryAttackSpeedMultiplier != 1f))
        {
            TemporaryDamageMultiplier = 1f;
            TemporaryAttackSpeedMultiplier = 1f;
            changed = true;
        }

        if (Time.time >= temporaryDefenseUntil &&
            (TemporaryArmorMultiplier != 1f || TemporaryMagicResistanceBonus != 0f))
        {
            TemporaryArmorMultiplier = 1f;
            TemporaryMagicResistanceBonus = 0f;
            changed = true;
        }

        if (changed) StatsChanged?.Invoke();
    }

    public bool SpendMana(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (CurrentMana < amount) return false;
        CurrentMana -= amount;
        ManaChanged?.Invoke(CurrentMana, maxMana);
        return true;
    }

    public void RestoreMana(float amount)
    {
        if (amount <= 0f) return;
        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        ManaChanged?.Invoke(CurrentMana, maxMana);
    }

    public void ApplyFrontPenalty()
    {
        FrontPenaltyMultiplier = 0.75f;
        StatsChanged?.Invoke();
    }

    public void AddPermanentPower(float multiplier)
    {
        PermanentMultiplier *= Mathf.Max(1f, multiplier);
        StatsChanged?.Invoke();
    }

    public void ApplyTemporaryBuff(float damageMultiplier, float attackSpeedMultiplier, float duration)
    {
        TemporaryDamageMultiplier = Mathf.Max(TemporaryDamageMultiplier, damageMultiplier);
        TemporaryAttackSpeedMultiplier = Mathf.Max(TemporaryAttackSpeedMultiplier, attackSpeedMultiplier);
        temporaryBuffUntil = Mathf.Max(temporaryBuffUntil, Time.time + Mathf.Max(0.1f, duration));
        StatsChanged?.Invoke();
    }

    public void GrantSpiritWard()
    {
        BlocksNextHit = true;
        StatsChanged?.Invoke();
    }

    public bool ConsumeSpiritWard()
    {
        if (!BlocksNextHit) return false;
        BlocksNextHit = false;
        StatsChanged?.Invoke();
        return true;
    }

    public void GrantPhysicalImmunity(float duration)
    {
        PhysicalImmune = true;
        physicalImmuneUntil = Mathf.Max(physicalImmuneUntil, Time.time + duration);
        StatsChanged?.Invoke();
    }

    public void GrantInvulnerability(float duration)
    {
        Invulnerable = true;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
        StatsChanged?.Invoke();
    }

    public void GrantBkb(float duration)
    {
        TemporaryArmorMultiplier = Mathf.Max(TemporaryArmorMultiplier, 6f);
        TemporaryMagicResistanceBonus = 1f;
        temporaryDefenseUntil = Mathf.Max(temporaryDefenseUntil, Time.time + Mathf.Max(0.1f, duration));
        StatsChanged?.Invoke();
    }

    public void GrantMagicBarrier(float amount)
    {
        MagicBarrier = Mathf.Max(MagicBarrier, Mathf.Max(0f, amount));
        StatsChanged?.Invoke();
    }

    public DamagePacket AbsorbMagicBarrier(DamagePacket packet)
    {
        if (MagicBarrier <= 0f || packet.Family == DamageFamily.Physical) return packet;

        float absorbed = Mathf.Min(MagicBarrier, packet.Amount);
        MagicBarrier -= absorbed;
        StatsChanged?.Invoke();
        return packet.WithAmount(packet.Amount - absorbed);
    }

    public void SilenceEcho(float duration)
    {
        EchoSilenced = true;
        echoSilencedUntil = Mathf.Max(echoSilencedUntil, Time.time + duration);
        StatsChanged?.Invoke();
    }

    public void UnlockGreyMana()
    {
        GreyManaUnlocked = true;
        GameplayNotificationController.Show("Сіра мана розблокована. Адаптація почалася.");
        StatsChanged?.Invoke();
    }

    public bool ObserveAndCheckAdaptation(DamagePacket packet)
    {
        MimickedFamily = packet.Family;
        if (!GreyManaUnlocked) return false;
        if (adaptedDamage.Contains(packet.Family)) return true;

        damageExposure.TryGetValue(packet.Family, out int exposure);
        exposure++;
        damageExposure[packet.Family] = exposure;
        if (exposure >= 3)
        {
            adaptedDamage.Add(packet.Family);
            GameplayNotificationController.Show($"Адаптація завершена: імунітет до {packet.Family}");
        }
        return false;
    }
}
