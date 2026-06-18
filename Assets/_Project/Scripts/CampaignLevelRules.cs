// Формує правила складності рівнів: множники характеристик, нагороди та спеціальні модифікатори ворогів.
using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum LevelMutator
{
    None = 0,
    Swarm = 1 << 0,
    Armored = 1 << 1,
    Haste = 1 << 2,
    MagicWard = 1 << 3,
    MixedFronts = 1 << 4,
    Relentless = 1 << 5,
    Elite = 1 << 6
}

public readonly struct CampaignLevelRule
{
    public readonly int Level;
    public readonly string Title;
    public readonly int WaveCount;
    public readonly float StatMultiplier;
    public readonly float SpawnRateMultiplier;
    public readonly LevelMutator Mutators;

    public CampaignLevelRule(
        int level,
        string title,
        int waveCount,
        float statMultiplier,
        float spawnRateMultiplier,
        LevelMutator mutators)
    {
        Level = level;
        Title = title;
        WaveCount = waveCount;
        StatMultiplier = statMultiplier;
        SpawnRateMultiplier = spawnRateMultiplier;
        Mutators = mutators;
    }

    public string BuildDescription()
    {
        return CampaignLevelRules.Describe(Mutators);
    }
}

public static class CampaignLevelRules
{
    private readonly struct EnemyPlan
    {
        public readonly string Label;
        public readonly string ResourcePath;
        public readonly int UnlockLevel;
        public readonly float Weight;
        public readonly float Interval;

        public EnemyPlan(string label, string resourcePath, int unlockLevel, float weight, float interval)
        {
            Label = label;
            ResourcePath = resourcePath;
            UnlockLevel = unlockLevel;
            Weight = weight;
            Interval = interval;
        }
    }

    private static readonly string[] Titles =
    {
        "Пробудження",
        "Перший наказ",
        "Голодні брами",
        "Собачий марш",
        "Зламаний стрій",
        "Пил старої війни",
        "Броньований натиск",
        "Швидкі тіні",
        "Змішана варта",
        "Десятий дзвін",
        "Магічний заслін",
        "Роздвоєна атака",
        "Важкий цикл",
        "Невпинна хода",
        "Порожні лицарі",
        "Облога пам'яті",
        "Два фронти",
        "Елітна варта",
        "Передчуття Дворецького",
        "Виклик відкрито",
        "Перша тріщина",
        "Втрата швидкості",
        "Згасла пасива",
        "Подовжені ритуали",
        "Мерехтіння форми",
        "Критичне виснаження",
        "Смертельна експонента"
    };

    private static readonly EnemyPlan[] EnemyRoster =
    {
        new EnemyPlan("Зомбі", "Enemies/Zombie_Enemy", 1, 1.0f, 1.18f),
        new EnemyPlan("Безрукий зомбі", "Enemies/Zombie without arms_Enemy", 2, 0.9f, 1.05f),
        new EnemyPlan("Облізлий пес", "Enemies/Scruffy Dog_Enemy", 3, 0.78f, 0.92f),
        new EnemyPlan("Бойовий пес", "Enemies/Fighting Dog_Enemy", 5, 0.72f, 0.86f),
        new EnemyPlan("Старий лицар", "Enemies/Knight in worn armor_Enemy", 7, 0.9f, 1.26f),
        new EnemyPlan("Зомбі-чаклун", "Enemies/Wizard Zombie_Enemy", 10, 0.62f, 1.38f),
        new EnemyPlan("Воїн-жаба", "Enemies/Warrior Frog_Enemy", 13, 0.76f, 1.08f),
        new EnemyPlan("Чаклун", "Enemies/Wizard_Enemy", 16, 0.55f, 1.5f)
    };

    public static CampaignLevelRule Get(int level)
    {
        level = Mathf.Clamp(level, 1, CampaignProgress.FinalLevel);
        if (level == CampaignProgress.FinalLevel)
        {
            return new CampaignLevelRule(
                level,
                Titles[level - 1],
                int.MaxValue,
                3.2f,
                0.6f,
                LevelMutator.Swarm |
                LevelMutator.Armored |
                LevelMutator.Haste |
                LevelMutator.MagicWard |
                LevelMutator.MixedFronts |
                LevelMutator.Relentless |
                LevelMutator.Elite);
        }

        float t = (level - 1f) / (CampaignProgress.FinalLevel - 2f);
        int waveCount = GetTemplateWaveCount(level);
        float statMultiplier = 0.72f + 1.75f * t + 0.55f * t * t;
        float spawnRateMultiplier = Mathf.Lerp(1.28f, 0.66f, Mathf.Pow(t, 0.82f));
        LevelMutator mutators = GetMutators(level);

        return new CampaignLevelRule(
            level,
            Titles[level - 1],
            waveCount,
            statMultiplier,
            spawnRateMultiplier,
            mutators);
    }

    public static string Describe(LevelMutator mutators)
    {
        List<string> values = new List<string>();
        if (mutators.HasFlag(LevelMutator.Swarm)) values.Add("рій");
        if (mutators.HasFlag(LevelMutator.Armored)) values.Add("броня");
        if (mutators.HasFlag(LevelMutator.Haste)) values.Add("прискорення");
        if (mutators.HasFlag(LevelMutator.MagicWard)) values.Add("магічний захист");
        if (mutators.HasFlag(LevelMutator.MixedFronts)) values.Add("обидва фронти");
        if (mutators.HasFlag(LevelMutator.Relentless)) values.Add("швидкий темп");
        if (mutators.HasFlag(LevelMutator.Elite)) values.Add("еліта");
        return values.Count == 0 ? "навчальні правила" : string.Join(", ", values);
    }

    public static Wave[] BuildWaves(int level)
    {
        CampaignLevelRule rule = Get(level);
        if (level == CampaignProgress.FinalLevel) return Array.Empty<Wave>();

        Wave[] waves = new Wave[rule.WaveCount];
        for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
        {
            List<EnemyPlan> pool = GetEnemyPool(level, waveIndex);
            int totalCount = GetTotalEnemyCount(level, waveIndex, rule);
            float totalWeight = 0f;
            foreach (EnemyPlan plan in pool) totalWeight += Mathf.Max(0.1f, plan.Weight);

            List<WaveEnemyConfig> enemies = new List<WaveEnemyConfig>();
            int assigned = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                EnemyPlan plan = pool[i];
                int count = i == pool.Count - 1
                    ? Mathf.Max(1, totalCount - assigned)
                    : Mathf.Max(1, Mathf.RoundToInt(totalCount * plan.Weight / totalWeight));
                assigned += count;

                enemies.Add(new WaveEnemyConfig
                {
                    enemyName = plan.Label,
                    resourcePath = plan.ResourcePath,
                    count = count,
                    startDelay = i * GetTypeStartDelay(rule),
                    spawnInterval = Mathf.Max(0.18f, plan.Interval * rule.SpawnRateMultiplier)
                });
            }

            waves[waveIndex] = new Wave
            {
                waveName = $"{rule.Title}: хвиля {waveIndex + 1}",
                enemies = enemies.ToArray(),
                statMultiplier = GetWaveStatMultiplier(level, waveIndex, waves.Length, rule),
                spawnBothLanes = ShouldSplitLanes(level, waveIndex, waves.Length, rule)
            };
        }

        return waves;
    }

    private static int GetTemplateWaveCount(int level)
    {
        if (level <= 5) return 3;
        if (level <= 11) return 4;
        if (level <= 18) return 5;
        return 6;
    }

    private static LevelMutator GetMutators(int level)
    {
        LevelMutator mutators = LevelMutator.None;

        if (level is 3 or 6 or 10 || level >= 14 && level % 3 == 2)
            mutators |= LevelMutator.Swarm;
        if (level >= 7 && level % 4 != 1)
            mutators |= LevelMutator.Armored;
        if (level >= 8 && level % 5 != 0)
            mutators |= LevelMutator.Haste;
        if (level >= 11 && level % 3 != 0)
            mutators |= LevelMutator.MagicWard;
        if (level >= 12 && (level % 4 == 0 || level >= 17))
            mutators |= LevelMutator.MixedFronts;
        if (level >= 15)
            mutators |= LevelMutator.Relentless;
        if (level >= 18 && level % 2 == 0 || level >= 22)
            mutators |= LevelMutator.Elite;

        return mutators;
    }

    private static List<EnemyPlan> GetEnemyPool(int level, int waveIndex)
    {
        int maxTypes = Mathf.Clamp(1 + level / 5 + waveIndex / 2, 1, 4);
        List<EnemyPlan> unlocked = new List<EnemyPlan>();
        foreach (EnemyPlan plan in EnemyRoster)
        {
            if (level >= plan.UnlockLevel) unlocked.Add(plan);
        }

        List<EnemyPlan> pool = new List<EnemyPlan>();
        if (unlocked.Count == 0)
        {
            pool.Add(EnemyRoster[0]);
            return pool;
        }

        int start = Mathf.Abs(level + waveIndex) % unlocked.Count;
        for (int i = 0; i < unlocked.Count && pool.Count < maxTypes; i++)
        {
            pool.Add(unlocked[(start + i) % unlocked.Count]);
        }

        return pool;
    }

    private static int GetTotalEnemyCount(int level, int waveIndex, CampaignLevelRule rule)
    {
        float count = 4.2f + level * 0.58f + waveIndex * (1.15f + level * 0.018f);

        if (rule.Mutators.HasFlag(LevelMutator.Swarm)) count *= 1.24f;
        if (rule.Mutators.HasFlag(LevelMutator.Elite)) count *= 0.78f;
        if (level <= 2) count *= 0.82f;

        return Mathf.Clamp(Mathf.RoundToInt(count), 3, 32);
    }

    private static float GetTypeStartDelay(CampaignLevelRule rule)
    {
        return rule.Mutators.HasFlag(LevelMutator.Relentless) ? 0.28f : 0.52f;
    }

    private static float GetWaveStatMultiplier(int level, int waveIndex, int waveCount, CampaignLevelRule rule)
    {
        float waveT = waveCount <= 1 ? 0f : waveIndex / (float)(waveCount - 1);
        float multiplier = 0.92f + waveT * 0.22f;

        if (rule.Mutators.HasFlag(LevelMutator.Elite) && waveIndex == waveCount - 1)
            multiplier += 0.12f;
        if (level <= 3)
            multiplier -= 0.08f;

        return Mathf.Max(0.72f, multiplier);
    }

    private static bool ShouldSplitLanes(int level, int waveIndex, int waveCount, CampaignLevelRule rule)
    {
        if (!rule.Mutators.HasFlag(LevelMutator.MixedFronts)) return false;
        if (level < 17) return waveIndex == waveCount - 1;
        return waveIndex >= Mathf.Max(1, waveCount - 2);
    }
}
