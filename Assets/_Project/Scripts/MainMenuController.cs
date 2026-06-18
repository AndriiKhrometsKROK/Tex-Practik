// Обробляє кнопки головного меню, налаштування гучності, повний екран і запуск кампанії.
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        CampaignProgress.TryLoadAutosave();
        GameSettings.Apply();
        GameAudioController.ForcePlayMusic(GameMusicTrack.MainMenu);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Start()
    {
        GameAudioController.ForcePlayMusic(GameMusicTrack.MainMenu);
    }

    public void Configure(string targetGameSceneName, GameObject targetSettingsPanel)
    {
        gameSceneName = targetGameSceneName;
        settingsPanel = targetSettingsPanel;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void Play()
    {
        CampaignProgress.ShowStoryOnNextHub = !CampaignProgress.HasAutosave;
        TrainingGroundState.Clear();
        GameAudioController.PlaySfx(GameSfxCue.MysteryReveal, 0.6f);
        SceneManager.LoadScene(gameSceneName);
    }

    public void NewCampaign()
    {
        CampaignProgress.ResetProfile();
        CampaignProgress.ShowStoryOnNextHub = true;
        TrainingGroundState.Clear();
        GameAudioController.PlaySfx(GameSfxCue.MysteryReveal, 0.6f);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        GameAudioController.PlaySfx(GameSfxCue.Purchase, 0.45f);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            RefreshSettingsLabels();
        }
    }

    public void CloseSettings()
    {
        GameAudioController.PlaySfx(GameSfxCue.Purchase, 0.45f);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void SetVolume(float volume)
    {
        GameSettings.SetMasterVolume(volume);
    }

    public void SetMusicVolume(float volume) => GameSettings.SetMusicVolume(volume);
    public void SetSfxVolume(float volume) => GameSettings.SetSfxVolume(volume);

    public void ToggleFullscreen()
    {
        GameSettings.SetFullscreen(!Screen.fullScreen);
        GameAudioController.PlaySfx(GameSfxCue.Purchase, 0.45f);
        RefreshSettingsLabels();
    }

    public void ToggleVSync()
    {
        GameSettings.SetVSync(!GameSettings.VSync);
        RefreshSettingsLabels();
    }

    public void CycleQuality()
    {
        GameSettings.CycleQuality();
        RefreshSettingsLabels();
    }

    public void CycleResolution()
    {
        GameSettings.CycleResolution();
        RefreshSettingsLabels();
    }

    private void RefreshSettingsLabels()
    {
        SetButtonText("Fullscreen Toggle", "ПОВНИЙ ЕКРАН: " + (GameSettings.Fullscreen ? "ТАК" : "НІ"));
        SetButtonText("VSync Toggle", "V-SYNC: " + (GameSettings.VSync ? "ТАК" : "НІ"));
        string quality = QualitySettings.names.Length > 0 ? QualitySettings.names[GameSettings.Quality] : "DEFAULT";
        SetButtonText("Quality Cycle", "ЯКІСТЬ: " + quality.ToUpperInvariant());
        SetButtonText("Resolution Cycle", $"РОЗДІЛЬНІСТЬ: {Screen.width} x {Screen.height}");
    }

    private void SetButtonText(string objectName, string value)
    {
        if (settingsPanel == null) return;
        foreach (TextMeshProUGUI text in settingsPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.transform.parent != null && text.transform.parent.name == objectName)
            {
                text.text = value;
                return;
            }
        }
    }

    public void ResetProfile()
    {
        CampaignProgress.ResetProfile();
        GameAudioController.PlaySfx(GameSfxCue.Achievement, 0.65f);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
