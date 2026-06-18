// Центральний координатор матчу: ресурси, хвилі, стан гри, здоров'я бази, перемога й поразка.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Preparing,
    WaveInProgress,
    WaitingForNextWave,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<int> GoldChanged;
    public event Action<float, float> BaseHealthChanged;
    public event Action<int, int> WaveChanged;
    public event Action<int> NextWaveCountdownChanged;
    public event Action<GameState> StateChanged;
    public event Action<UnitData> EnemyDefeated;

    [Header("Economy")]
    public int currentGold = 100;
    public IncomeManager incomeManager;

    [Header("Base Health")]
    public float maxBaseHealth = 100f;
    public float currentBaseHealth;
    public Slider baseHealthSlider;

    [Header("Waves")]
    public EnemySpawner enemySpawner;
    public bool autoStartWaves = true;
    [Min(1f)] public float nextWaveDelay = 20f;

    public GameState CurrentState { get; private set; } = GameState.Preparing;
    public int CurrentWaveIndex { get; private set; } = -1;
    public int EnemiesAlive { get; private set; }
    public int EnemiesRemainingToSpawn { get; private set; }
    public float NextWaveTimeRemaining { get; private set; }

    private int _lastCountdownSecond = -1;
    private int _lastRewardedWaveIndex = -1;
    private BattleFlowController battleFlow;
    private bool trainingMode;

    public bool IsGameActive =>
        CurrentState == GameState.Preparing ||
        CurrentState == GameState.WaveInProgress ||
        CurrentState == GameState.WaitingForNextWave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            EnsureIncomeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PresentationBootstrapper.EnsureGameplayPresentation(this);
        StartCoroutine(EnsurePresentationNextFrame());
        trainingMode = TrainingGroundState.IsActive;

        int campaignLevel = CampaignProgress.SelectedLevel;
        currentGold = Mathf.Max(currentGold, 500 + (campaignLevel - 1) * 75);
        maxBaseHealth = Mathf.Max(maxBaseHealth, 250f);
        if (trainingMode)
        {
            currentGold = 999999;
            maxBaseHealth = Mathf.Max(maxBaseHealth, 999999f);
            autoStartWaves = false;
        }
        currentBaseHealth = maxBaseHealth;
        UpdateBaseHealthUI();

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }
        ApplyLevelPacing();

        EnsureIncomeManager();
        if (!trainingMode)
        {
            EnsureBattleFlow();
        }
        else
        {
            foreach (BattleFlowController flow in FindObjectsByType<BattleFlowController>(FindObjectsSortMode.None))
            {
                Destroy(flow.gameObject);
            }
        }

        GoldChanged?.Invoke(currentGold);
        BaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);
        StateChanged?.Invoke(CurrentState);

        if (trainingMode)
        {
            TrainingGroundController trainingGround = gameObject.AddComponent<TrainingGroundController>();
            trainingGround.Configure(this);
        }

        if (autoStartWaves)
        {
            SetState(GameState.WaitingForNextWave);
            enemySpawner?.RefreshWaveButton();
        }
    }

    private IEnumerator EnsurePresentationNextFrame()
    {
        yield return null;
        PresentationBootstrapper.EnsureGameplayPresentation(this);
    }

    private void Update()
    {
        if ((CurrentState == GameState.Victory || CurrentState == GameState.Defeat) &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("SampleScene");
            return;
        }

        if (CurrentState != GameState.WaitingForNextWave || !autoStartWaves) return;

        NextWaveTimeRemaining = Mathf.Max(0f, NextWaveTimeRemaining - Time.deltaTime);
        int seconds = Mathf.CeilToInt(NextWaveTimeRemaining);
        if (seconds != _lastCountdownSecond)
        {
            _lastCountdownSecond = seconds;
            NextWaveCountdownChanged?.Invoke(seconds);
        }

        if (NextWaveTimeRemaining <= 0f)
        {
            enemySpawner?.StartNextWave();
        }
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        currentGold += amount;
        GoldChanged?.Invoke(currentGold);
        Debug.Log("Gold added. Current gold: " + currentGold);
    }

    public bool SpendGold(int amount)
    {
        if (amount < 0) return false;

        if (currentGold >= amount)
        {
            currentGold -= amount;
            GoldChanged?.Invoke(currentGold);
            Debug.Log("Purchase successful. Gold left: " + currentGold);
            GameAudioController.PlaySfx(GameSfxCue.Purchase, 0.7f);
            return true;
        }

        Debug.Log("Not enough gold.");
        GameplayNotificationController.Show("Недостатньо золота");
        return false;
    }

    public void SetGoldForTesting(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        GoldChanged?.Invoke(currentGold);
    }

    public bool CanStartNextWave()
    {
        return CurrentState == GameState.Preparing || CurrentState == GameState.WaitingForNextWave;
    }

    // Фіксуємо очікувану кількість ворогів, щоб завершити хвилю лише після спавну та видалення кожного з них.
    public void BeginWave(int waveNumber, int totalWaves, int enemiesToSpawn)
    {
        if (trainingMode) return;
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat) return;

        CurrentWaveIndex = waveNumber - 1;
        EnemiesRemainingToSpawn = enemiesToSpawn;
        battleFlow?.HandleWaveStarted();
        WaveChanged?.Invoke(waveNumber, totalWaves);
        SetState(GameState.WaveInProgress);
        Debug.Log($"Wave {waveNumber}/{totalWaves} started.");
    }

    public void BeginBossBattle()
    {
        SetState(GameState.WaveInProgress);
    }

    public void FinishWaveSpawning()
    {
        EnemiesRemainingToSpawn = 0;
        CheckWaveCompletion();
    }

    public void DamageBase(float amount)
    {
        if (trainingMode) return;
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat) return;
        if (amount <= 0f) return;

        currentBaseHealth = Mathf.Max(0f, currentBaseHealth - amount);
        UpdateBaseHealthUI();
        BaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);

        if (currentBaseHealth <= 0f)
        {
            if (battleFlow != null && battleFlow.TryTriggerForcedFinale())
            {
                currentBaseHealth = maxBaseHealth;
                UpdateBaseHealthUI();
                BaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);
                return;
            }

            SetDefeat("Base destroyed.");
        }
    }

    public void PlayerDied()
    {
        SetDefeat("Player died.");
    }

    public void RegisterEnemySpawned()
    {
        EnemiesAlive++;
        if (EnemiesRemainingToSpawn > 0)
        {
            EnemiesRemainingToSpawn--;
        }
    }

    public void EnemyKilled(UnitData data)
    {
        if (data != null)
        {
            AddGold(data.goldReward);
        }

        EnemyDefeated?.Invoke(data);
        RegisterEnemyRemoved();
    }

    public void EnemyReachedBase(UnitData data, float damageMultiplier = 1f)
    {
        float damage = (data != null ? data.towerDamage : 1f) * Mathf.Max(0.1f, damageMultiplier);
        DamageBase(damage);
        RegisterEnemyRemoved();
    }

    public void RegisterEnemyRemovedWithoutReward()
    {
        RegisterEnemyRemoved();
    }

    public void RewardAttackFrontHit(UnitData data)
    {
        if (data == null) return;
        AddGold(Mathf.Max(1, Mathf.CeilToInt(data.essenceCost * 0.05f)));
    }

    // Хвиля вважається завершеною, коли спавнер закінчив роботу, а живих зареєстрованих ворогів не залишилося.
    private void CheckWaveCompletion()
    {
        if (EnemiesRemainingToSpawn > 0 || EnemiesAlive > 0) return;
        if (enemySpawner != null && enemySpawner.IsFinalDistortion) return;
        if (battleFlow != null &&
            (battleFlow.Phase == BattlePhase.BossBattle || battleFlow.Phase == BattlePhase.Finale))
        {
            return;
        }

        RewardWaveClear();
        if (enemySpawner == null || !enemySpawner.HasRemainingWaves)
        {
            SetVictory();
            return;
        }

        SetState(GameState.WaitingForNextWave);
        enemySpawner.RefreshWaveButton();
        Debug.Log("Wave cleared. The next wave is preparing.");
    }

    private void RegisterEnemyRemoved()
    {
        EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1);
        CheckWaveCompletion();
    }

    public void SetVictory()
    {
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat) return;
        if (trainingMode) return;
        CampaignProgress.CompleteSelectedLevel();
        SetState(GameState.Victory);
        GameAudioController.PlaySfx(GameSfxCue.Victory, 0.9f);
        enemySpawner?.StopAllSpawning();
        enemySpawner?.RefreshWaveButton();
        Debug.Log("Victory! All waves cleared.");
    }

    private void SetDefeat(string reason)
    {
        if (CurrentState == GameState.Defeat) return;
        CampaignProgress.RecordDefeat();
        SetState(GameState.Defeat);
        enemySpawner?.StopAllSpawning();
        Debug.Log("Defeat: " + reason);
    }

    private void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        if (newState == GameState.WaitingForNextWave)
        {
            NextWaveTimeRemaining = Mathf.Max(1f, nextWaveDelay);
            _lastCountdownSecond = Mathf.CeilToInt(NextWaveTimeRemaining);
            NextWaveCountdownChanged?.Invoke(_lastCountdownSecond);
        }
        else
        {
            NextWaveTimeRemaining = 0f;
            _lastCountdownSecond = -1;
        }

        StateChanged?.Invoke(CurrentState);
    }

    private void UpdateBaseHealthUI()
    {
        if (baseHealthSlider == null) return;

        baseHealthSlider.maxValue = maxBaseHealth;
        baseHealthSlider.value = currentBaseHealth;
    }

    private void EnsureIncomeManager()
    {
        if (incomeManager != null) return;

        if (!TryGetComponent(out incomeManager))
        {
            incomeManager = FindAnyObjectByType<IncomeManager>();
        }

        if (incomeManager == null)
        {
            incomeManager = gameObject.AddComponent<IncomeManager>();
        }
    }

    private void EnsureBattleFlow()
    {
        battleFlow = FindAnyObjectByType<BattleFlowController>();
        if (battleFlow == null)
        {
            battleFlow = new GameObject("Battle Flow Controller").AddComponent<BattleFlowController>();
        }
        battleFlow.Configure(this, enemySpawner);
    }

    // Рівень кампанії впливає не лише на ворогів, а й на стартовий темп економіки та підготовки.
    private void ApplyLevelPacing()
    {
        CampaignLevelRule rule = CampaignLevelRules.Get(CampaignProgress.SelectedLevel);
        float levelT = Mathf.Clamp01((rule.Level - 1f) / Mathf.Max(1f, CampaignProgress.FinalLevel - 1f));
        nextWaveDelay = Mathf.Lerp(24f, 11f, levelT);
        if (rule.Mutators.HasFlag(LevelMutator.Relentless))
        {
            nextWaveDelay = Mathf.Max(8f, nextWaveDelay * 0.75f);
        }
    }

    private void RewardWaveClear()
    {
        if (CurrentWaveIndex < 0 || CurrentWaveIndex == _lastRewardedWaveIndex) return;
        _lastRewardedWaveIndex = CurrentWaveIndex;
        int essenceReward = 18 + Mathf.Max(0, CurrentWaveIndex) * 4;
        incomeManager?.AddEssence(essenceReward);
        GameplayNotificationController.Show($"Хвилю очищено: +{essenceReward} есенції");
    }
}
