using System;
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
    public event Action<GameState> StateChanged;

    [Header("Economy")]
    public int currentGold = 100;

    [Header("Base Health")]
    public float maxBaseHealth = 100f;
    public float currentBaseHealth;
    public Slider baseHealthSlider;

    [Header("Waves")]
    public EnemySpawner enemySpawner;
    public bool autoStartWaves = true;

    public GameState CurrentState { get; private set; } = GameState.Preparing;
    public int CurrentWaveIndex { get; private set; } = -1;
    public int EnemiesAlive { get; private set; }
    public int EnemiesRemainingToSpawn { get; private set; }

    public bool IsGameActive =>
        CurrentState == GameState.Preparing ||
        CurrentState == GameState.WaveInProgress ||
        CurrentState == GameState.WaitingForNextWave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentBaseHealth = maxBaseHealth;
        UpdateBaseHealthUI();

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }

        GoldChanged?.Invoke(currentGold);
        BaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);
        StateChanged?.Invoke(CurrentState);

        if (autoStartWaves)
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
        Debug.Log("Wave cleared. Press the next wave button to continue.");
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
        StateChanged?.Invoke(CurrentState);
    }

    private void UpdateBaseHealthUI()
    {
        if (baseHealthSlider == null) return;

        baseHealthSlider.maxValue = maxBaseHealth;
        baseHealthSlider.value = currentBaseHealth;
    }
}
