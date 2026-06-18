// Stores and applies player-facing settings independently of any particular scene.
using UnityEngine;

public static class GameSettings
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string VSyncKey = "Settings.VSync";
    private const string QualityKey = "Settings.Quality";
    private const string ResolutionWidthKey = "Settings.ResolutionWidth";
    private const string ResolutionHeightKey = "Settings.ResolutionHeight";

    public static float MasterVolume => PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);
    public static float MusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, 0.9f);
    public static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    public static bool VSync => PlayerPrefs.GetInt(VSyncKey, 1) == 1;
    public static int Quality => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1));

    public static void Apply()
    {
        AudioListener.volume = Mathf.Clamp01(MasterVolume);
        Screen.fullScreen = Fullscreen;
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        if (QualitySettings.names.Length > 0 && QualitySettings.GetQualityLevel() != Quality)
            QualitySettings.SetQualityLevel(Quality, true);
        int width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.currentResolution.height);
        Screen.SetResolution(width, height, Fullscreen);
        GameAudioController.ApplySavedVolumes();
    }

    public static void SetMasterVolume(float value) => SaveFloat(MasterVolumeKey, value);
    public static void SetMusicVolume(float value)
    {
        SaveFloat(MusicVolumeKey, value);
        GameAudioController.ApplySavedVolumes();
    }

    public static void SetSfxVolume(float value)
    {
        SaveFloat(SfxVolumeKey, value);
        GameAudioController.ApplySavedVolumes();
    }

    public static void SetFullscreen(bool value)
    {
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        Screen.fullScreen = value;
        PlayerPrefs.Save();
    }

    public static void SetVSync(bool value)
    {
        PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
        QualitySettings.vSyncCount = value ? 1 : 0;
        PlayerPrefs.Save();
    }

    public static void CycleQuality()
    {
        if (QualitySettings.names.Length == 0) return;
        int next = (Quality + 1) % QualitySettings.names.Length;
        PlayerPrefs.SetInt(QualityKey, next);
        QualitySettings.SetQualityLevel(next, true);
        PlayerPrefs.Save();
    }

    public static void CycleResolution()
    {
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0) return;

        int current = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                current = i;
        }

        Resolution next = resolutions[(current + 1) % resolutions.Length];
        PlayerPrefs.SetInt(ResolutionWidthKey, next.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, next.height);
        Screen.SetResolution(next.width, next.height, Fullscreen);
        PlayerPrefs.Save();
    }

    private static void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        if (key == MasterVolumeKey) AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.Save();
    }
}
