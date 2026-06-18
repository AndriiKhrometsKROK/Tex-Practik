// Описує типи шкоди, пакет атаки та централізований розрахунок броні, опору й бойових цілей.
using UnityEngine;

public enum DamageFamily
{
    Physical,
    Magical,
    Hybrid,
    Pure,
    Chaos
}

public enum DamageModifier
{
    Default,
    Piercing,
    Light,
    Dark,
    Mixed,
    Pure,
    Chaos
}

public readonly struct DamagePacket
{
    public readonly float Amount;
    public readonly DamageFamily Family;
    public readonly DamageModifier Modifier;
    public readonly GameObject Source;
    public readonly bool IgnoreArmor;
    public readonly bool IsSpell;

    public DamagePacket(
        float amount,
        DamageFamily family = DamageFamily.Physical,
        DamageModifier modifier = DamageModifier.Default,
        GameObject source = null,
        bool ignoreArmor = false,
        bool isSpell = false)
    {
        Amount = Mathf.Max(0f, amount);
        Family = family;
        Modifier = modifier;
        Source = source;
        IgnoreArmor = ignoreArmor;
        IsSpell = isSpell;
    }

    public DamagePacket WithAmount(float amount)
    {
        return new DamagePacket(amount, Family, Modifier, Source, IgnoreArmor, IsSpell);
    }
}

public static class CombatResolver
{
    // Уся шкода проходить через один розрахунок, щоб герой, башти, крипи та бос однаково враховували захист.
    public static float Resolve(
        DamagePacket packet,
        float armor,
        float magicResistance,
        float allResistanceReduction = 0f)
    {
        float amount = Mathf.Max(0f, packet.Amount);
        float physicalArmor = Mathf.Max(0f, armor * (1f - Mathf.Clamp01(allResistanceReduction)));
        float magicalResistance = Mathf.Clamp01(
            magicResistance * (1f - Mathf.Clamp01(allResistanceReduction)));

        switch (packet.Family)
        {
            case DamageFamily.Pure:
            case DamageFamily.Chaos:
                return amount;
            case DamageFamily.Magical:
                return amount * (1f - magicalResistance);
            case DamageFamily.Hybrid:
                float physicalHalf = packet.IgnoreArmor
                    ? amount * 0.5f
                    : ReducePhysical(amount * 0.5f, physicalArmor);
                float magicalHalf = amount * 0.5f * (1f - magicalResistance);
                return physicalHalf + magicalHalf;
            default:
                return packet.IgnoreArmor ? amount : ReducePhysical(amount, physicalArmor);
        }
    }

    // Формула броні має спадну ефективність, тому великі значення не дають абсолютної невразливості.
    private static float ReducePhysical(float amount, float armor)
    {
        if (amount <= 0f) return 0f;
        return Mathf.Max(1f, amount - armor);
    }
}

public static class CombatTargets
{
    public static bool Damage(GameObject target, DamagePacket packet)
    {
        if (target == null || packet.Amount <= 0f) return false;

        EnemyAI enemy = target.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(packet);
            return true;
        }

        AllyController ally = target.GetComponentInParent<AllyController>();
        if (ally != null)
        {
            ally.TakeDamage(packet);
            return true;
        }

        PlayerHealth player = target.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(packet);
            return true;
        }

        DemonBossController boss = target.GetComponentInParent<DemonBossController>();
        if (boss != null)
        {
            boss.TakeDamage(packet);
            return true;
        }

        EnemyCastle castle = target.GetComponentInParent<EnemyCastle>();
        if (castle != null)
        {
            castle.TakeDamage(packet.Amount);
            return true;
        }

        FrontTower frontTower = target.GetComponentInParent<FrontTower>();
        if (frontTower != null)
        {
            frontTower.TakeDamage(packet.Amount);
            return true;
        }

        return false;
    }
}
