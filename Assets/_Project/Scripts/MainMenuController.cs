using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Ігрова сцена";
    [SerializeField] private GameObject settingsPanel;

    private const string VolumeKey = "MasterVolume";

    private void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, AudioListener.volume);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
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
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
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
