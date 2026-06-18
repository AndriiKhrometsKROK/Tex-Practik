// Генерує нескінченні ворожі хвилі, масштабує їх за рівнем і повідомляє GameManager про перебіг хвилі.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Wave
{
    public string waveName = "Wave";
    public WaveEnemyConfig[] enemies;
    public GameObject[] enemyPrefabs;
    public int enemyCount = 10;
    public float spawnInterval = 2f;
    public float statMultiplier = 1f;
    public bool spawnBothLanes;
}

[Serializable]
public class WaveEnemyConfig
{
    public string enemyName = "Enemy";
    public GameObject enemyPrefab;
    public string resourcePath;
    public int count = 1;
    public float startDelay;
    public float spawnInterval = 1f;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Waves")]
    public bool useDefaultWavesIfEmpty = true;
    public Wave[] waves =
    {
        new Wave
        {
            waveName = "Wave 1 - Zombies",
            enemies = new[]
            {
                new WaveEnemyConfig
                {
                    enemyName = "Zombie",
                    resourcePath = "Enemies/Zombie_Enemy",
                    count = 8,
                    startDelay = 0f,
                    spawnInterval = 1.2f
                }
            }
        },
        new Wave
        {
            waveName = "Wave 2 - Fighting Dogs",
            enemies = new[]
            {
                new WaveEnemyConfig
                {
                    enemyName = "Zombie",
                    resourcePath = "Enemies/Zombie_Enemy",
                    count = 6,
                    startDelay = 0f,
                    spawnInterval = 1f
                },
                new WaveEnemyConfig
                {
                    enemyName = "Fighting Dog",
                    resourcePath = "Enemies/Fighting Dog_Enemy",
                    count = 5,
                    startDelay = 2f,
                    spawnInterval = 0.9f
                }
            }
        },
        new Wave
        {
            waveName = "Wave 3 - Wizard Attack",
            enemies = new[]
            {
                new WaveEnemyConfig
                {
                    enemyName = "Zombie",
                    resourcePath = "Enemies/Zombie_Enemy",
                    count = 8,
                    startDelay = 0f,
                    spawnInterval = 0.85f
                },
                new WaveEnemyConfig
                {
                    enemyName = "Fighting Dog",
                    resourcePath = "Enemies/Fighting Dog_Enemy",
                    count = 6,
                    startDelay = 1.5f,
                    spawnInterval = 0.75f
                },
                new WaveEnemyConfig
                {
                    enemyName = "Wizard",
                    resourcePath = "Enemies/Wizard_Enemy",
                    count = 4,
                    startDelay = 3f,
                    spawnInterval = 1.4f
                }
            }
        }
    };

    [Header("UI")]
    public Button startNextWaveButton;

    [Header("Fallback Enemies")]
    [HideInInspector] public GameObject[] enemyPrefabs;

    public int CurrentWaveIndex { get; private set; } = -1;
    public bool IsSpawningWave { get; private set; }
    public bool HasRemainingWaves => IsFinalDistortion ||
        CampaignProgress.SelectedLevel == CampaignProgress.FinalLevel ||
        waves != null && waves.Length > 0;
    public bool IsFinalDistortion { get; private set; }

    private Coroutine _spawnCoroutine;
    private Coroutine _finalDistortionCoroutine;
    private int _spawnSequence;
    private Wave _activeWave;
    public CampaignLevelRule ActiveLevelRule { get; private set; }

    private void Awake()
    {
        LoadEnemyPrefabs();
        EnsureWaveConfig();
    }

    private void Start()
    {
        if (startNextWaveButton != null)
        {
            startNextWaveButton.onClick.AddListener(StartNextWave);
        }

        UpdateStartWaveButton();
    }

    private void OnDestroy()
    {
        if (startNextWaveButton != null)
        {
            startNextWaveButton.onClick.RemoveListener(StartNextWave);
        }
    }

    public void StartNextWave()
    {
        if (IsSpawningWave) return;
        if (GameManager.Instance != null && !GameManager.Instance.CanStartNextWave()) return;
        if (CampaignProgress.SelectedLevel == CampaignProgress.FinalLevel)
        {
            StartFinalDistortion();
            return;
        }

        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("No waves configured on EnemySpawner.");
            UpdateStartWaveButton();
            return;
        }

        int nextWaveIndex = CurrentWaveIndex + 1;
        CurrentWaveIndex = nextWaveIndex;
        Wave wave = waves[Mathf.Abs(CurrentWaveIndex) % waves.Length];
        _activeWave = wave;
        IsSpawningWave = true;
        UpdateStartWaveButton();

        GameManager.Instance?.BeginWave(CurrentWaveIndex + 1, 0, GetEnemyCount(wave));
        _spawnCoroutine = StartCoroutine(SpawnWaveRoutine(wave));
    }

    public GameObject SpawnEnemy(GameObject enemyPrefab = null)
    {
        GameObject prefabToSpawn = enemyPrefab;
        if (prefabToSpawn == null)
        {
            prefabToSpawn = GetRandomFallbackEnemy();
        }

        if (prefabToSpawn == null) return null;

        return ObjectPoolManager.Spawn(prefabToSpawn, transform.position, Quaternion.identity);
    }

    public void RefreshWaveButton()
    {
        UpdateStartWaveButton();
    }

    public void StopAllSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        if (_finalDistortionCoroutine != null)
        {
            StopCoroutine(_finalDistortionCoroutine);
            _finalDistortionCoroutine = null;
        }
        IsSpawningWave = false;
        IsFinalDistortion = false;
        UpdateStartWaveButton();
    }

    public void ConfigureMvpWaves()
    {
        ConfigureCampaignWaves();
    }

    public void ConfigureCampaignWaves()
    {
        ActiveLevelRule = CampaignLevelRules.Get(CampaignProgress.SelectedLevel);
        if (CampaignProgress.SelectedLevel != CampaignProgress.FinalLevel)
        {
            waves = CampaignLevelRules.BuildWaves(CampaignProgress.SelectedLevel);
        }
    }

    // Конфігурації всередині хвилі можуть мати власну затримку та інтервал, тому спавн виконується корутиною.
    private IEnumerator SpawnWaveRoutine(Wave wave)
    {
        if (HasConfiguredEnemies(wave))
        {
            foreach (WaveEnemyConfig enemyConfig in wave.enemies)
            {
                if (enemyConfig == null || enemyConfig.count <= 0) continue;

                if (enemyConfig.startDelay > 0f)
                {
                    yield return new WaitForSeconds(enemyConfig.startDelay);
                }

                for (int i = 0; i < enemyConfig.count; i++)
                {
                    SpawnConfiguredEnemy(enemyConfig);

                    if (i < enemyConfig.count - 1)
                    {
                        yield return new WaitForSeconds(Mathf.Max(0.1f, enemyConfig.spawnInterval));
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < wave.enemyCount; i++)
            {
                SpawnEnemyAndRegister(GetRandomEnemyForWave(wave), GetWaveSpawnMultiplier(wave));

                if (i < wave.enemyCount - 1)
                {
                    yield return new WaitForSeconds(Mathf.Max(0.1f, wave.spawnInterval));
                }
            }
        }

        IsSpawningWave = false;
        _activeWave = null;
        _spawnCoroutine = null;
        GameManager.Instance?.FinishWaveSpawning();
        UpdateStartWaveButton();
    }

    private void SpawnConfiguredEnemy(WaveEnemyConfig enemyConfig)
    {
        GameObject prefab = enemyConfig.enemyPrefab;
        if (prefab == null && !string.IsNullOrWhiteSpace(enemyConfig.resourcePath))
        {
            prefab = Resources.Load<GameObject>(enemyConfig.resourcePath);
        }

        SpawnEnemyAndRegister(prefab, GetWaveSpawnMultiplier(_activeWave));
    }

    private void SpawnEnemyAndRegister(GameObject prefab, float statMultiplier = 1f)
    {
        GameObject spawnedEnemy = SpawnEnemy(prefab);
        if (spawnedEnemy != null)
        {
            EnemyAI enemy = spawnedEnemy.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                bool splitFronts = _activeWave != null && _activeWave.spawnBothLanes;
                BattleLane lane = splitFronts || BattleFlowController.Instance != null && BattleFlowController.Instance.IsTotalWar
                    ? (_spawnSequence++ % 2 == 0 ? BattleLane.Upper : BattleLane.Lower)
                    : BattleLane.Lower;
                enemy.SetLane(lane);
                enemy.ApplyStatMultiplier(statMultiplier);
                enemy.ApplyLevelRule(ActiveLevelRule);
            }

            GameManager.Instance?.RegisterEnemySpawned();
        }
    }

    private int GetEnemyCount(Wave wave)
    {
        if (!HasConfiguredEnemies(wave))
        {
            return Mathf.Max(0, wave != null ? wave.enemyCount : 0);
        }

        int count = 0;
        foreach (WaveEnemyConfig enemyConfig in wave.enemies)
        {
            if (enemyConfig != null)
            {
                count += Mathf.Max(0, enemyConfig.count);
            }
        }

        return count;
    }

    private bool HasConfiguredEnemies(Wave wave)
    {
        return wave != null && wave.enemies != null && wave.enemies.Length > 0;
    }

    private GameObject GetRandomEnemyForWave(Wave wave)
    {
        if (wave.enemyPrefabs != null && wave.enemyPrefabs.Length > 0)
        {
            return wave.enemyPrefabs[UnityEngine.Random.Range(0, wave.enemyPrefabs.Length)];
        }

        return GetRandomFallbackEnemy();
    }

    private GameObject GetRandomFallbackEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            LoadEnemyPrefabs();
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;

        return enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
    }

    private void LoadEnemyPrefabs()
    {
        enemyPrefabs = Resources.LoadAll<GameObject>("Enemies");

        if (enemyPrefabs.Length == 0)
        {
            Debug.LogError("Enemies not found. Put enemy prefabs in Assets/Resources/Enemies.");
        }
    }

    // Якщо в сцені немає ручного списку хвиль, створюємо стабільну конфігурацію з ресурсних префабів.
    private void EnsureWaveConfig()
    {
        if (!useDefaultWavesIfEmpty) return;

        bool hasUsableConfig = waves != null &&
            waves.Length > 0 &&
            Array.Exists(waves, wave => HasConfiguredEnemies(wave) || wave.enemyPrefabs != null && wave.enemyPrefabs.Length > 0);

        if (hasUsableConfig) return;

        waves = new[]
        {
            new Wave
            {
                waveName = "Wave 1 - Zombies",
                enemies = new[]
                {
                    CreateEnemyConfig("Zombie", "Enemies/Zombie_Enemy", 8, 0f, 1.2f)
                }
            },
            new Wave
            {
                waveName = "Wave 2 - Fighting Dogs",
                enemies = new[]
                {
                    CreateEnemyConfig("Zombie", "Enemies/Zombie_Enemy", 6, 0f, 1f),
                    CreateEnemyConfig("Fighting Dog", "Enemies/Fighting Dog_Enemy", 5, 2f, 0.9f)
                }
            },
            new Wave
            {
                waveName = "Wave 3 - Wizard Attack",
                enemies = new[]
                {
                    CreateEnemyConfig("Zombie", "Enemies/Zombie_Enemy", 8, 0f, 0.85f),
                    CreateEnemyConfig("Fighting Dog", "Enemies/Fighting Dog_Enemy", 6, 1.5f, 0.75f),
                    CreateEnemyConfig("Wizard", "Enemies/Wizard_Enemy", 4, 3f, 1.4f)
                }
            }
        };
    }

    private WaveEnemyConfig CreateEnemyConfig(string enemyName, string resourcePath, int count, float startDelay, float spawnInterval)
    {
        return new WaveEnemyConfig
        {
            enemyName = enemyName,
            enemyPrefab = Resources.Load<GameObject>(resourcePath),
            resourcePath = resourcePath,
            count = count,
            startDelay = startDelay,
            spawnInterval = spawnInterval
        };
    }

    private void StartFinalDistortion()
    {
        if (IsFinalDistortion) return;
        ActiveLevelRule = CampaignLevelRules.Get(CampaignProgress.FinalLevel);
        IsFinalDistortion = true;
        IsSpawningWave = true;
        CurrentWaveIndex = CampaignProgress.FinalLevel - 1;
        GameManager.Instance?.BeginWave(CampaignProgress.FinalLevel, CampaignProgress.FinalLevel, 0);
        _finalDistortionCoroutine = StartCoroutine(FinalDistortionRoutine());
        UpdateStartWaveButton();
    }

    private IEnumerator FinalDistortionRoutine()
    {
        float startedAt = Time.time;
        int batch = 0;
        while (IsFinalDistortion)
        {
            float elapsed = Time.time - startedAt;
            float statMultiplier = ActiveLevelRule.StatMultiplier * (1f + Mathf.Pow(elapsed / 30f, 2f));
            int count = 8 + batch * 2;
            for (int i = 0; i < count && IsFinalDistortion; i++)
            {
                SpawnEnemyAndRegister(GetRandomFallbackEnemy(), statMultiplier);
                yield return new WaitForSeconds(Mathf.Max(0.15f, 0.65f - batch * 0.025f));
            }

            batch++;
            float nextBatchAt = startedAt + batch * 30f;
            while (IsFinalDistortion && Time.time < nextBatchAt)
            {
                yield return null;
            }
        }
        _finalDistortionCoroutine = null;
    }

    private float GetStandardSpawnMultiplier()
    {
        float rageMultiplier = BattleFlowController.Instance != null
            ? BattleFlowController.Instance.GetEnemySpawnMultiplier()
            : 1f;
        return GetLevelBaseMultiplier() * rageMultiplier;
    }

    private float GetWaveSpawnMultiplier(Wave wave)
    {
        return GetStandardSpawnMultiplier() *
            Mathf.Max(0.1f, wave != null ? wave.statMultiplier : 1f) *
            GetEndlessCycleMultiplier();
    }

    private float GetLevelBaseMultiplier()
    {
        return ActiveLevelRule.Level > 0 ? ActiveLevelRule.StatMultiplier : 1f;
    }

    // Після проходження базового набору хвиль складність продовжує зростати без верхньої межі.
    private float GetEndlessCycleMultiplier()
    {
        if (waves == null || waves.Length == 0 || CurrentWaveIndex < 0) return 1f;

        int completedCycles = CurrentWaveIndex / Mathf.Max(1, waves.Length);
        if (completedCycles <= 0) return 1f;

        float levelPressure = ActiveLevelRule.Level > 0
            ? Mathf.Clamp01(ActiveLevelRule.Level / (float)CampaignProgress.FinalLevel)
            : 0.25f;
        float perCycle = Mathf.Lerp(0.08f, 0.18f, levelPressure);
        return 1f + completedCycles * perCycle;
    }

    public GameObject SpawnBossMinion(float statMultiplier)
    {
        GameObject spawnedEnemy = SpawnEnemy(GetRandomFallbackEnemy());
        if (spawnedEnemy == null) return null;

        EnemyAI enemy = spawnedEnemy.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            BattleLane lane = _spawnSequence++ % 2 == 0 ? BattleLane.Upper : BattleLane.Lower;
            enemy.SetLane(lane);
            enemy.ApplyStatMultiplier(Mathf.Max(0.1f, statMultiplier));
            enemy.ApplyLevelRule(ActiveLevelRule);
        }

        GameManager.Instance?.RegisterEnemySpawned();
        return spawnedEnemy;
    }

    private static void SetEnemyCount(Wave wave, int enemyIndex, int count)
    {
        if (wave == null || wave.enemies == null || enemyIndex < 0 || enemyIndex >= wave.enemies.Length) return;
        if (wave.enemies[enemyIndex] == null) return;

        wave.enemies[enemyIndex].count = Mathf.Max(0, count);
    }

    private void UpdateStartWaveButton()
    {
        if (startNextWaveButton == null) return;

        bool canStart = !IsSpawningWave &&
            HasRemainingWaves &&
            (GameManager.Instance == null || GameManager.Instance.CanStartNextWave());

        startNextWaveButton.interactable = canStart;
    }
}
