// Єдина точка відтворення музики й звуків: кешує кліпи, перемикає треки та виконує плавні переходи.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameMusicTrack
{
    None,
    MainMenu,
    Hub,
    Library,
    DemonConversation,
    LevelSelection,
    Preparation,
    Battle,
    Critical,
    LateHardcore,
    Boss,
    Finale
}

public enum GameSfxCue
{
    Purchase,
    Magic,
    MysteryReveal,
    Victory,
    Achievement,
    DemonDestroyed
}

public sealed class GameAudioController : MonoBehaviour
{
    private const string ResourceRoot = "Audio/SongsAndPain/";
    private const float DefaultMusicVolume = 0.55f;
    private const float DefaultSfxVolume = 0.85f;
    private const float DefaultCrossfade = 1.2f;

    private static readonly Dictionary<GameMusicTrack, string> MusicPaths = new Dictionary<GameMusicTrack, string>
    {
        { GameMusicTrack.MainMenu, "LoopOfNothingness_MainMenu" },
        { GameMusicTrack.Hub, "BetweenCycles_Hub" },
        { GameMusicTrack.Library, "BattleworldsArchive_Library" },
        { GameMusicTrack.DemonConversation, "HeWhoRemembers_DemonConversation" },
        { GameMusicTrack.LevelSelection, "CycleMap_LevelSelection" },
        { GameMusicTrack.Preparation, "BeforeTheWave_PreparingForBattle" },
        { GameMusicTrack.Battle, "ClashOfFronts_RegularBattle" },
        { GameMusicTrack.Critical, "DestroyedDefense_CriticalSituation" },
        { GameMusicTrack.LateHardcore, "EchoOfGrayMana_LateHardcore" },
        { GameMusicTrack.Boss, "SummonTheDemon_BossFight" },
        { GameMusicTrack.Finale, "TheRulesNoLongerApply_RealFinalFight" }
    };

    private static readonly Dictionary<GameSfxCue, string> SfxPaths = new Dictionary<GameSfxCue, string>
    {
        { GameSfxCue.Purchase, "Purchase" },
        { GameSfxCue.Magic, "Magic_FirstOnePointFiveSeconds" },
        { GameSfxCue.MysteryReveal, "MysteryReveal" },
        { GameSfxCue.Victory, "Victory" },
        { GameSfxCue.Achievement, "Achievement" },
        { GameSfxCue.DemonDestroyed, "DemonDestroyed" }
    };

    private readonly Dictionary<GameMusicTrack, AudioClip> musicCache = new Dictionary<GameMusicTrack, AudioClip>();
    private readonly Dictionary<GameSfxCue, AudioClip> sfxCache = new Dictionary<GameSfxCue, AudioClip>();

    private AudioSource firstMusicSource;
    private AudioSource secondMusicSource;
    private AudioSource activeMusicSource;
    private AudioSource sfxSource;
    private Coroutine crossfadeRoutine;
    private GameMusicTrack currentTrack = GameMusicTrack.None;

    private static GameAudioController instance;

    public static GameAudioController Instance
    {
        get
        {
            if (instance != null) return instance;

            GameAudioController existing = FindAnyObjectByType<GameAudioController>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            GameObject audioObject = new GameObject("Game Audio Controller");
            instance = audioObject.AddComponent<GameAudioController>();
            return instance;
        }
    }

    public static void PlayMusic(GameMusicTrack track, float crossfadeDuration = DefaultCrossfade)
    {
        Instance.PlayMusicInternal(track, crossfadeDuration);
    }

    public static void ForcePlayMusic(GameMusicTrack track)
    {
        Instance.PlayMusicInternal(track, 0f, true);
    }

    public static void PlaySfx(GameSfxCue cue, float volumeScale = 1f)
    {
        Instance.PlaySfxInternal(cue, volumeScale);
    }

    public static void ApplySavedVolumes()
    {
        if (instance != null) instance.RefreshSourceVolumes();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureListenerAudible();
        EnsureSources();
    }

    private void EnsureSources()
    {
        if (firstMusicSource != null) return;

        firstMusicSource = CreateSource("Music A", true, GetMusicVolume());
        secondMusicSource = CreateSource("Music B", true, GetMusicVolume());
        sfxSource = CreateSource("SFX", false, 1f);
        activeMusicSource = firstMusicSource;
    }

    private AudioSource CreateSource(string sourceName, bool loop, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        return source;
    }

    // Повторний запит того самого треку відновлює гучність; forceRestart додатково перезапускає кліп із початку.
    private void PlayMusicInternal(GameMusicTrack track, float crossfadeDuration, bool forceRestart = false)
    {
        EnsureListenerAudible();
        EnsureSources();

        AudioClip clip = LoadMusic(track);
        if (track != GameMusicTrack.None && clip == null) return;

        if (track == currentTrack && activeMusicSource != null && activeMusicSource.clip == clip && !forceRestart)
        {
            RestoreMusicSource(activeMusicSource);
            return;
        }

        if (forceRestart && clip != null && crossfadeDuration <= 0f)
        {
            if (crossfadeRoutine != null)
            {
                StopCoroutine(crossfadeRoutine);
                crossfadeRoutine = null;
            }

            if (activeMusicSource == null) activeMusicSource = firstMusicSource;
            activeMusicSource.Stop();
            activeMusicSource.clip = clip;
            RestoreMusicSource(activeMusicSource);
            activeMusicSource.Play();
            currentTrack = track;
            return;
        }

        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(CrossfadeTo(track, clip, Mathf.Max(0f, crossfadeDuration)));
    }

    private static void RestoreMusicSource(AudioSource source)
    {
        if (source == null) return;

        source.mute = false;
        source.loop = true;
        source.volume = GetMusicVolume();
        source.ignoreListenerPause = true;
        if (!source.isPlaying && source.clip != null) source.Play();
    }

    private static void EnsureListenerAudible()
    {
        AudioListener.pause = false;
        if (AudioListener.volume <= 0.01f)
        {
            AudioListener.volume = 0.85f;
        }
    }

    // Два AudioSource дозволяють одночасно приглушувати старий трек і піднімати гучність нового.
    private IEnumerator CrossfadeTo(GameMusicTrack track, AudioClip clip, float duration)
    {
        AudioSource from = activeMusicSource;
        AudioSource to = from == firstMusicSource ? secondMusicSource : firstMusicSource;

        if (clip != null)
        {
            to.clip = clip;
            to.volume = 0f;
            to.loop = true;
            to.Play();
        }

        float fromStart = from != null ? from.volume : 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, t);
            if (clip != null) to.volume = Mathf.Lerp(0f, GetMusicVolume(), t);
            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.clip = null;
            from.volume = GetMusicVolume();
        }

        if (clip != null)
        {
            to.volume = GetMusicVolume();
            activeMusicSource = to;
        }

        currentTrack = track;
        crossfadeRoutine = null;
    }

    private void PlaySfxInternal(GameSfxCue cue, float volumeScale)
    {
        EnsureSources();

        AudioClip clip = LoadSfx(cue);
        if (clip == null) return;
        float volume = Mathf.Clamp01(volumeScale) * GetSfxVolume();
        if (cue == GameSfxCue.Magic)
        {
            StartCoroutine(PlayLimitedSfx(clip, volume, 1.5f));
            return;
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    private IEnumerator PlayLimitedSfx(AudioClip clip, float volume, float duration)
    {
        GameObject sourceObject = new GameObject("Limited SFX");
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.Play();

        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, duration));

        if (source != null) source.Stop();
        if (sourceObject != null) Destroy(sourceObject);
    }

    private AudioClip LoadMusic(GameMusicTrack track)
    {
        if (track == GameMusicTrack.None) return null;
        if (musicCache.TryGetValue(track, out AudioClip clip)) return clip;
        if (!MusicPaths.TryGetValue(track, out string path)) return null;

        clip = Resources.Load<AudioClip>(ResourceRoot + path);
        if (clip == null) Debug.LogWarning("Music clip not found: " + ResourceRoot + path);
        musicCache[track] = clip;
        return clip;
    }

    private AudioClip LoadSfx(GameSfxCue cue)
    {
        if (sfxCache.TryGetValue(cue, out AudioClip clip)) return clip;
        if (!SfxPaths.TryGetValue(cue, out string path)) return null;

        clip = Resources.Load<AudioClip>(ResourceRoot + path);
        if (clip == null) Debug.LogWarning("SFX clip not found: " + ResourceRoot + path);
        sfxCache[cue] = clip;
        return clip;
    }

    private void RefreshSourceVolumes()
    {
        if (firstMusicSource != null && firstMusicSource.isPlaying) firstMusicSource.volume = GetMusicVolume();
        if (secondMusicSource != null && secondMusicSource.isPlaying) secondMusicSource.volume = GetMusicVolume();
        if (sfxSource != null) sfxSource.volume = 1f;
    }

    private static float GetMusicVolume() => DefaultMusicVolume * GameSettings.MusicVolume;
    private static float GetSfxVolume() => DefaultSfxVolume * GameSettings.SfxVolume;
}
