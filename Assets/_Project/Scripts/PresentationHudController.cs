using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PresentationHudController : MonoBehaviour
{
    private GameManager gameManager;
    private IncomeManager incomeManager;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI waveText;
    private TextMeshProUGUI goldText;
    private TextMeshProUGUI essenceText;
    private TextMeshProUGUI stateText;
    private Button readyButton;
    private int currentGold;
    private int goldPerSecond;

    public void Configure(
        GameManager manager,
        TextMeshProUGUI health,
        TextMeshProUGUI wave,
        TextMeshProUGUI gold,
        TextMeshProUGUI essence,
        TextMeshProUGUI state,
        Button ready)
    {
        gameManager = manager;
        incomeManager = manager != null ? manager.incomeManager : FindAnyObjectByType<IncomeManager>();
        healthText = health;
        waveText = wave;
        goldText = gold;
        essenceText = essence;
        stateText = state;
        readyButton = ready;

        Subscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.GoldChanged -= UpdateGold;
            gameManager.BaseHealthChanged -= UpdateHealth;
            gameManager.WaveChanged -= UpdateWave;
            gameManager.NextWaveCountdownChanged -= UpdateCountdown;
            gameManager.StateChanged -= UpdateState;
        }

        if (incomeManager != null)
        {
            incomeManager.EssenceChanged -= UpdateEssence;
            incomeManager.GoldPerSecondChanged -= UpdateGoldPerSecond;
        }
    }

    private void Subscribe()
    {
        if (gameManager != null)
        {
            gameManager.GoldChanged += UpdateGold;
            gameManager.BaseHealthChanged += UpdateHealth;
            gameManager.WaveChanged += UpdateWave;
            gameManager.NextWaveCountdownChanged += UpdateCountdown;
            gameManager.StateChanged += UpdateState;
        }

        if (incomeManager != null)
        {
            incomeManager.EssenceChanged += UpdateEssence;
            incomeManager.GoldPerSecondChanged += UpdateGoldPerSecond;
        }
    }

    private void Refresh()
    {
        if (gameManager != null)
        {
            UpdateGold(gameManager.currentGold);
            UpdateHealth(gameManager.currentBaseHealth, gameManager.maxBaseHealth);
            UpdateState(gameManager.CurrentState);

            EnemySpawner spawner = gameManager.enemySpawner != null
                ? gameManager.enemySpawner
                : FindAnyObjectByType<EnemySpawner>();
            int total = spawner != null && spawner.waves != null ? spawner.waves.Length : 0;
            UpdateWave(gameManager.CurrentWaveIndex + 1, total);
        }

        if (incomeManager != null)
        {
            UpdateEssence(incomeManager.currentEssence);
            UpdateGoldPerSecond(incomeManager.GoldPerSecond);
        }
    }

    private void UpdateGold(int value)
    {
        currentGold = value;
        RefreshGoldText();
    }

    private void UpdateGoldPerSecond(int value)
    {
        goldPerSecond = value;
        RefreshGoldText();
    }

    private void RefreshGoldText()
    {
        if (goldText != null) goldText.text = $"{currentGold}  +{goldPerSecond}/с";
    }

    private void UpdateEssence(int value)
    {
        if (essenceText != null) essenceText.text = value.ToString();
    }

    private void UpdateHealth(float current, float maximum)
    {
        if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
    }

    private void UpdateWave(int current, int total)
    {
        if (waveText != null) waveText.text = $"{Mathf.Max(0, current)} / {Mathf.Max(0, total)}";
    }

    private void UpdateState(GameState state)
    {
        if (stateText != null)
        {
            stateText.text = state switch
            {
                GameState.Preparing => "Підготовка",
                GameState.WaveInProgress => "Хвиля триває",
                GameState.WaitingForNextWave => "Очікування",
                GameState.Victory => "Перемога",
                GameState.Defeat => "Поразка",
                _ => state.ToString()
            };
        }

        if (readyButton != null)
        {
            readyButton.interactable = gameManager != null && gameManager.CanStartNextWave();
        }
    }

    private void UpdateCountdown(int seconds)
    {
        if (stateText != null && gameManager != null &&
            gameManager.CurrentState == GameState.WaitingForNextWave)
        {
            stateText.text = $"До хвилі: {Mathf.Max(0, seconds)}";
        }
    }
}
