// Нараховує досвід за перемоги, підвищує рівень героя та застосовує зростання характеристик.
using System;
using UnityEngine;

public class HeroProgressionController : MonoBehaviour
{
    public event Action<int, int, int> ProgressChanged;

    private HeroStats stats;
    private PlayerHealth health;
    private GameManager gameManager;
    private int appliedLevel;

    public int Level => CampaignProgress.HeroLevel;
    public int Experience => CampaignProgress.HeroExperience;
    public int ExperienceToNext => CampaignProgress.ExperienceToNextLevel(Level);

    private void Awake()
    {
        stats = GetComponent<HeroStats>() ?? gameObject.AddComponent<HeroStats>();
        health = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        ApplyFullProgression();
        gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        if (gameManager != null) gameManager.EnemyDefeated += HandleEnemyDefeated;
        ProgressChanged?.Invoke(Level, Experience, ExperienceToNext);
    }

    private void OnDestroy()
    {
        if (gameManager != null) gameManager.EnemyDefeated -= HandleEnemyDefeated;
    }

    private void HandleEnemyDefeated(UnitData defeated)
    {
        int experience = defeated != null ? Mathf.Max(8, defeated.goldReward * 2 + 8) : 8;
        int gained = CampaignProgress.AddHeroExperience(experience);
        if (gained > 0)
        {
            ApplyNewLevels(gained);
            GameplayNotificationController.Show($"Рівень Ке́нама підвищено: {Level}");
        }
        ProgressChanged?.Invoke(Level, Experience, ExperienceToNext);
    }

    private void ApplyFullProgression()
    {
        appliedLevel = Mathf.Max(1, Level);
        float multiplier = Mathf.Pow(1.04f, appliedLevel - 1);
        stats.AddPermanentPower(multiplier);
        health?.ApplyPermanentGrowth(Mathf.Pow(1.035f, appliedLevel - 1));
    }

    private void ApplyNewLevels(int count)
    {
        for (int i = 0; i < count; i++)
        {
            stats.AddPermanentPower(1.04f);
            health?.ApplyPermanentGrowth(1.035f);
            appliedLevel++;
        }
    }
}
