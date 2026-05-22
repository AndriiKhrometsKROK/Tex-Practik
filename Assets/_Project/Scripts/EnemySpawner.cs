using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Wave
{
    public string waveName = "Wave";
    public GameObject[] enemyPrefabs;
    public int enemyCount = 10;
    public float spawnInterval = 2f;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Waves")]
    public Wave[] waves =
    {
        new Wave()
    };

    [Header("UI")]
    public Button startNextWaveButton;

    [Header("Fallback Enemies")]
    [HideInInspector] public GameObject[] enemyPrefabs;

    public int CurrentWaveIndex { get; private set; } = -1;
    public bool IsSpawningWave { get; private set; }
    public bool HasRemainingWaves => waves != null && CurrentWaveIndex + 1 < waves.Length;

    private Coroutine _spawnCoroutine;

    private void Awake()
    {
        LoadEnemyPrefabs();
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

        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("No waves configured on EnemySpawner.");
            GameManager.Instance?.SetVictory();
            UpdateStartWaveButton();
            return;
        }

        int nextWaveIndex = CurrentWaveIndex + 1;
        if (nextWaveIndex >= waves.Length)
        {
            GameManager.Instance?.SetVictory();
            UpdateStartWaveButton();
            return;
        }

        CurrentWaveIndex = nextWaveIndex;
        Wave wave = waves[CurrentWaveIndex];
        IsSpawningWave = true;
        UpdateStartWaveButton();

        GameManager.Instance?.BeginWave(CurrentWaveIndex + 1, waves.Length, Mathf.Max(0, wave.enemyCount));
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

        return Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
    }

    public void RefreshWaveButton()
    {
        UpdateStartWaveButton();
    }

    private IEnumerator SpawnWaveRoutine(Wave wave)
    {
        for (int i = 0; i < wave.enemyCount; i++)
        {
            GameObject spawnedEnemy = SpawnEnemy(GetRandomEnemyForWave(wave));
            if (spawnedEnemy != null)
            {
                GameManager.Instance?.RegisterEnemySpawned();
            }

            if (i < wave.enemyCount - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.1f, wave.spawnInterval));
            }
        }

        IsSpawningWave = false;
        _spawnCoroutine = null;
        GameManager.Instance?.FinishWaveSpawning();
        UpdateStartWaveButton();
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

    private void UpdateStartWaveButton()
    {
        if (startNextWaveButton == null) return;

        bool canStart = !IsSpawningWave &&
            HasRemainingWaves &&
            (GameManager.Instance == null || GameManager.Instance.CanStartNextWave());

        startNextWaveButton.interactable = canStart;
    }
}
