// Зберігає прогрес кампанії, відкриті рівні, досягнення та серіалізує автозбереження.
using System;
using System.IO;
using UnityEngine;

[Serializable]
public class CampaignSaveData
{
    public int highestUnlocked = 1;
    public int selectedLevel = 1;
    public bool act2Completed;
    public int bestDemonChallenge = CampaignProgress.FinalLevel;
    public int heroLevel = 1;
    public int heroExperience;
    public int totalVictories;
    public int totalDefeats;
    public int totalEnemiesDefeated;
    public string savedAtUtc;
}

public static class CampaignProgress
{
    public const int FinalLevel = 27;
    public const int VisibleLevelCount = 40;

    private const string HighestUnlockedKey = "Campaign.HighestUnlocked";
    private const string SelectedLevelKey = "Campaign.SelectedLevel";
    private const string Act2CompletedKey = "Campaign.Act2Completed";
    private const string BestDemonChallengeKey = "Campaign.BestDemonChallenge";
    private const string PendingDemonChallengeKey = "Campaign.PendingDemonChallenge";
    private const string SaveJsonKey = "Campaign.SaveJson";
    private const string HeroLevelKey = "Campaign.HeroLevel";
    private const string HeroExperienceKey = "Campaign.HeroExperience";
    private const string TotalVictoriesKey = "Campaign.TotalVictories";
    private const string TotalDefeatsKey = "Campaign.TotalDefeats";
    private const string TotalEnemiesDefeatedKey = "Campaign.TotalEnemiesDefeated";
    private const string SaveFileName = "kenomarch_profile.json";

    public static bool ShowStoryOnNextHub { get; set; }
    public static bool HasAutosave
    {
        get
        {
            if (PlayerPrefs.HasKey(SaveJsonKey)) return true;
            try
            {
                return File.Exists(GetSavePath());
            }
            catch
            {
                return false;
            }
        }
    }

    public static int HighestUnlocked =>
        Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedKey, 1), 1, FinalLevel);

    public static int SelectedLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(SelectedLevelKey, 1), 1, FinalLevel);
        set
        {
            PlayerPrefs.SetInt(SelectedLevelKey, Mathf.Clamp(value, 1, FinalLevel));
            SaveAutosave();
        }
    }

    public static bool Act2Completed => PlayerPrefs.GetInt(Act2CompletedKey, 0) == 1;
    public static int BestDemonChallenge =>
        Mathf.Clamp(PlayerPrefs.GetInt(BestDemonChallengeKey, FinalLevel), 20, FinalLevel);
    public static int HeroLevel => Mathf.Clamp(PlayerPrefs.GetInt(HeroLevelKey, 1), 1, 30);
    public static int HeroExperience => Mathf.Max(0, PlayerPrefs.GetInt(HeroExperienceKey, 0));
    public static int TotalVictories => Mathf.Max(0, PlayerPrefs.GetInt(TotalVictoriesKey, 0));
    public static int TotalDefeats => Mathf.Max(0, PlayerPrefs.GetInt(TotalDefeatsKey, 0));
    public static int TotalEnemiesDefeated => Mathf.Max(0, PlayerPrefs.GetInt(TotalEnemiesDefeatedKey, 0));

    public static bool CanEnter(int level)
    {
        return level >= 1 && level <= HighestUnlocked && level <= FinalLevel;
    }

    public static void CompleteSelectedLevel()
    {
        int completedLevel = SelectedLevel;
        int next = Mathf.Min(FinalLevel, completedLevel + 1);
        if (next > HighestUnlocked)
        {
            PlayerPrefs.SetInt(HighestUnlockedKey, next);
        }
        PlayerPrefs.SetInt(SelectedLevelKey, next);

        if (completedLevel >= FinalLevel)
        {
            PlayerPrefs.SetInt(Act2CompletedKey, 1);
        }
        PlayerPrefs.SetInt(TotalVictoriesKey, TotalVictories + 1);
        SaveAutosave();
    }

    public static void CompleteAct2ByDemonChallenge(int level)
    {
        int challengeLevel = Mathf.Clamp(level, 20, FinalLevel);
        PlayerPrefs.SetInt(Act2CompletedKey, 1);
        if (challengeLevel < BestDemonChallenge)
            PlayerPrefs.SetInt(BestDemonChallengeKey, challengeLevel);
        SaveAutosave();
    }

    public static int AddHeroExperience(int amount)
    {
        if (amount <= 0) return 0;
        PlayerPrefs.SetInt(TotalEnemiesDefeatedKey, TotalEnemiesDefeated + 1);
        if (HeroLevel >= 30)
        {
            SaveAutosave();
            return 0;
        }

        int level = HeroLevel;
        int experience = HeroExperience + amount;
        int levelsGained = 0;
        while (level < 30 && experience >= ExperienceToNextLevel(level))
        {
            experience -= ExperienceToNextLevel(level);
            level++;
            levelsGained++;
        }

        PlayerPrefs.SetInt(HeroLevelKey, level);
        PlayerPrefs.SetInt(HeroExperienceKey, experience);
        if (levelsGained > 0) SaveAutosave();
        return levelsGained;
    }

    public static int ExperienceToNextLevel(int level)
    {
        return 55 + Mathf.Max(1, level) * 25;
    }

    public static void RecordDefeat()
    {
        PlayerPrefs.SetInt(TotalDefeatsKey, TotalDefeats + 1);
        SaveAutosave();
    }

    public static void RequestDemonChallenge()
    {
        PlayerPrefs.SetInt(PendingDemonChallengeKey, 1);
        SaveAutosave();
    }

    public static bool ConsumePendingDemonChallenge()
    {
        bool pending = PlayerPrefs.GetInt(PendingDemonChallengeKey, 0) == 1;
        if (pending)
        {
            PlayerPrefs.DeleteKey(PendingDemonChallengeKey);
            SaveAutosave();
        }
        return pending;
    }

    // Зберігаємо простий JSON у persistentDataPath, щоб файл не залежав від конкретної сцени або платформи.
    public static void SaveAutosave()
    {
        CampaignSaveData data = new CampaignSaveData
        {
            highestUnlocked = HighestUnlocked,
            selectedLevel = SelectedLevel,
            act2Completed = Act2Completed,
            bestDemonChallenge = BestDemonChallenge,
            heroLevel = HeroLevel,
            heroExperience = HeroExperience,
            totalVictories = TotalVictories,
            totalDefeats = TotalDefeats,
            totalEnemiesDefeated = TotalEnemiesDefeated,
            savedAtUtc = DateTime.UtcNow.ToString("O")
        };
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SaveJsonKey, json);
        try
        {
            File.WriteAllText(GetSavePath(), json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Campaign file autosave failed: " + exception.Message);
        }
        PlayerPrefs.Save();
    }

    // Пошкоджене або застаріле збереження не повинно блокувати запуск: у такому разі лишається новий профіль.
    public static bool TryLoadAutosave()
    {
        string json = null;
        try
        {
            string path = GetSavePath();
            if (File.Exists(path)) json = File.ReadAllText(path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Campaign file autosave could not be read: " + exception.Message);
        }

        if (string.IsNullOrWhiteSpace(json) && PlayerPrefs.HasKey(SaveJsonKey))
        {
            json = PlayerPrefs.GetString(SaveJsonKey);
        }
        if (string.IsNullOrWhiteSpace(json)) return false;

        CampaignSaveData data;
        try
        {
            data = JsonUtility.FromJson<CampaignSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Campaign autosave could not be loaded: " + exception.Message);
            return false;
        }

        if (data == null) return false;
        PlayerPrefs.SetInt(HighestUnlockedKey, Mathf.Clamp(data.highestUnlocked, 1, FinalLevel));
        PlayerPrefs.SetInt(SelectedLevelKey, Mathf.Clamp(data.selectedLevel, 1, FinalLevel));
        PlayerPrefs.SetInt(Act2CompletedKey, data.act2Completed ? 1 : 0);
        PlayerPrefs.SetInt(BestDemonChallengeKey, Mathf.Clamp(data.bestDemonChallenge, 20, FinalLevel));
        PlayerPrefs.SetInt(HeroLevelKey, Mathf.Clamp(data.heroLevel, 1, 30));
        PlayerPrefs.SetInt(HeroExperienceKey, Mathf.Max(0, data.heroExperience));
        PlayerPrefs.SetInt(TotalVictoriesKey, Mathf.Max(0, data.totalVictories));
        PlayerPrefs.SetInt(TotalDefeatsKey, Mathf.Max(0, data.totalDefeats));
        PlayerPrefs.SetInt(TotalEnemiesDefeatedKey, Mathf.Max(0, data.totalEnemiesDefeated));
        PlayerPrefs.Save();
        return true;
    }

    public static void ResetProfile()
    {
        PlayerPrefs.DeleteKey(HighestUnlockedKey);
        PlayerPrefs.DeleteKey(SelectedLevelKey);
        PlayerPrefs.DeleteKey(Act2CompletedKey);
        PlayerPrefs.DeleteKey(BestDemonChallengeKey);
        PlayerPrefs.DeleteKey(PendingDemonChallengeKey);
        PlayerPrefs.DeleteKey(SaveJsonKey);
        PlayerPrefs.DeleteKey(HeroLevelKey);
        PlayerPrefs.DeleteKey(HeroExperienceKey);
        PlayerPrefs.DeleteKey(TotalVictoriesKey);
        PlayerPrefs.DeleteKey(TotalDefeatsKey);
        PlayerPrefs.DeleteKey(TotalEnemiesDefeatedKey);
        try
        {
            string path = GetSavePath();
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Campaign save file could not be deleted: " + exception.Message);
        }
        PlayerPrefs.Save();
    }

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }
}
