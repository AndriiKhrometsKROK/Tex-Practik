using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayHUDController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI baseHealthText;
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Final Screens")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameManager _gameManager;
    private EnemySpawner _enemySpawner;
    private int _currentWave;
    private int _totalWaves;

    public void Configure(
        TextMeshProUGUI targetGoldText,
        TextMeshProUGUI targetBaseHealthText,
        TextMeshProUGUI targetWaveText,
        GameObject targetVictoryPanel,
        GameObject targetGameOverPanel,
        string targetMainMenuSceneName)
    {
        goldText = targetGoldText;
        baseHealthText = targetBaseHealthText;
        waveText = targetWaveText;
        victoryPanel = targetVictoryPanel;
        gameOverPanel = targetGameOverPanel;
        mainMenuSceneName = targetMainMenuSceneName;
    }

    private void Start()
    {
        _gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        _enemySpawner = _gameManager != null && _gameManager.enemySpawner != null
            ? _gameManager.enemySpawner
            : FindAnyObjectByType<EnemySpawner>();

        if (_enemySpawner != null && _enemySpawner.waves != null)
        {
            _totalWaves = _enemySpawner.waves.Length;
        }

        if (_gameManager != null)
        {
            _gameManager.GoldChanged += UpdateGold;
            _gameManager.BaseHealthChanged += UpdateBaseHealth;
            _gameManager.WaveChanged += UpdateWave;
            _gameManager.StateChanged += UpdateFinalScreens;

            UpdateGold(_gameManager.currentGold);
            UpdateBaseHealth(_gameManager.currentBaseHealth, _gameManager.maxBaseHealth);
            UpdateFinalScreens(_gameManager.CurrentState);
        }

        UpdateWave(_currentWave, _totalWaves);
    }

    private void OnDestroy()
    {
        if (_gameManager == null) return;

        _gameManager.GoldChanged -= UpdateGold;
        _gameManager.BaseHealthChanged -= UpdateBaseHealth;
        _gameManager.WaveChanged -= UpdateWave;
        _gameManager.StateChanged -= UpdateFinalScreens;
    }

    public void RestartScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"Золото: {gold}";
        }
    }

    private void UpdateBaseHealth(float currentHealth, float maxHealth)
    {
        if (baseHealthText != null)
        {
            baseHealthText.text = $"Життя: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    private void UpdateWave(int waveNumber, int totalWaves)
    {
        _currentWave = Mathf.Max(0, waveNumber);
        _totalWaves = Mathf.Max(0, totalWaves);

        if (waveText != null)
        {
            waveText.text = $"Хвиля {_currentWave} / {_totalWaves}";
        }
    }

    private void UpdateFinalScreens(GameState state)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(state == GameState.Victory);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(state == GameState.Defeat);
        }
    }
}
