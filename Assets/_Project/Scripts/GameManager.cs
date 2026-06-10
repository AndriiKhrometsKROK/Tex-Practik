using System;
using System.Collections;
using UnityEngine;
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

        currentGold = Mathf.Max(currentGold, 350);
        maxBaseHealth = Mathf.Max(maxBaseHealth, 250f);
        currentBaseHealth = maxBaseHealth;
        UpdateBaseHealthUI();

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }

        EnsureIncomeManager();

        GoldChanged?.Invoke(currentGold);
        BaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);
        StateChanged?.Invoke(CurrentState);

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
            return true;
        }

        Debug.Log("Not enough gold.");
        GameplayNotificationController.Show("Недостатньо золота");
        return false;
    }

    public bool CanStartNextWave()
    {
        return CurrentState == GameState.Preparing || CurrentState == GameState.WaitingForNextWave;
    }

    public void BeginWave(int waveNumber, int totalWaves, int enemiesToSpawn)
    {
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat) return;

        CurrentWaveIndex = waveNumber - 1;
        EnemiesRemainingToSpawn = enemiesToSpawn;
        WaveChanged?.Invoke(waveNumber, totalWaves);
        SetState(GameState.WaveInProgress);
        Debug.Log($"Wave {waveNumber}/{totalWaves} started.");
    }

    public void FinishWaveSpawning()
    {
        EnemiesRemainingToSpawn = 0;
        CheckWaveCompletion();
    }

    public void DamageBase(float amount)
    {
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat) return;
        if (amount <= 0f) return;

        currentBaseHealth = Mathf.Max(0f, currentBaseHealth - amount);
        UpdateBaseHealthUI();
        BaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);

        if (currentBaseHealth <= 0f)
        {
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

        RegisterEnemyRemoved();
    }

    public void EnemyReachedBase(UnitData data)
    {
        float damage = data != null ? data.towerDamage : 1f;
        DamageBase(damage);
        RegisterEnemyRemoved();
    }

    private void CheckWaveCompletion()
    {
        if (EnemiesRemainingToSpawn > 0 || EnemiesAlive > 0) return;

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
        SetState(GameState.Victory);
        enemySpawner?.RefreshWaveButton();
        Debug.Log("Victory! All waves cleared.");
    }

    private void SetDefeat(string reason)
    {
        SetState(GameState.Defeat);
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
}
