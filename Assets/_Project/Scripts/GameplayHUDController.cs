// Оновлює базовий HUD матчу та керує панелями перемоги, поразки й повернення до меню.
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private string victoryTitle = "Перемога";
    [SerializeField] private string gameOverTitle = "Поразка";

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

    private void Awake()
    {
        AutoFindFinalPanels();
        PrepareFinalPanel(victoryPanel, victoryTitle);
        PrepareFinalPanel(gameOverPanel, gameOverTitle);
        SetFinalScreensVisible(false, false);
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
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        TrainingGroundState.Clear();
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
            waveText.text = _totalWaves <= 0
                ? $"Хвиля {_currentWave} / ∞"
                : $"Хвиля {_currentWave} / {_totalWaves}";
        }
    }

    private void UpdateFinalScreens(GameState state)
    {
        bool showVictory = state == GameState.Victory;
        bool showGameOver = state == GameState.Defeat;

        SetFinalScreensVisible(showVictory, showGameOver);

        if (showVictory || showGameOver)
        {
            Time.timeScale = 0f;
        }
        else if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    private void SetFinalScreensVisible(bool showVictory, bool showGameOver)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(showVictory);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(showGameOver);
        }
    }

    private void AutoFindFinalPanels()
    {
        if (victoryPanel == null)
        {
            victoryPanel = FindSceneObjectByName("Victory Panel");
        }

        if (gameOverPanel == null)
        {
            gameOverPanel = FindSceneObjectByName("Game Over Panel");
        }
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (sceneObject == null) continue;
            if (!sceneObject.scene.IsValid()) continue;
            if (sceneObject.name == objectName) return sceneObject;
        }

        return null;
    }

    private void PrepareFinalPanel(GameObject panel, string title)
    {
        if (panel == null) return;

        foreach (TextMeshProUGUI text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.GetComponentInParent<Button>() == null)
            {
                text.text = title;
                break;
            }
        }

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.onClick.GetPersistentEventCount() > 0) continue;

            if (button.name.Contains("Restart"))
            {
                button.onClick.RemoveListener(RestartScene);
                button.onClick.AddListener(RestartScene);
            }
            else if (button.name.Contains("Menu"))
            {
                button.onClick.RemoveListener(BackToMainMenu);
                button.onClick.AddListener(BackToMainMenu);
            }
        }
    }
}
