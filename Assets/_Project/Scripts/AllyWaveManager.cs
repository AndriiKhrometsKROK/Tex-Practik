using System;
using System.Collections.Generic;
using UnityEngine;

public class AllyWaveManager : MonoBehaviour
{
    public event Action QueueChanged;

    [SerializeField] private AllySpawner allySpawner;
    [SerializeField] private UnitData[] availableUnits;
    [SerializeField, Min(1)] private int catalogSize = 3;

    private readonly List<QueuedAlly> queuedUnits = new List<QueuedAlly>();
    private GameManager subscribedGameManager;

    public IReadOnlyList<QueuedAlly> QueuedUnits => queuedUnits;
    public IReadOnlyList<UnitData> AvailableUnits => availableUnits;
    public BattleLane SelectedLane { get; private set; } = BattleLane.Upper;

    private void Awake()
    {
        BuildFallbackCatalog();
        ResolveSpawner();
        SubscribeToGameManager();
    }

    private void OnDestroy()
    {
        if (subscribedGameManager != null)
        {
            subscribedGameManager.WaveChanged -= ReleaseQueuedWave;
        }
    }

    public bool QueueUnit(UnitData unit)
    {
        if (unit == null) return false;

        IncomeManager income = IncomeManager.Instance != null
            ? IncomeManager.Instance
            : FindAnyObjectByType<IncomeManager>();

        if (income == null ||
            !income.SpendEssenceForCreepIncome(
                Mathf.Max(0, unit.essenceCost),
                Mathf.Max(0, unit.goldPerSecondIncrease)))
        {
            GameplayNotificationController.Show("Недостатньо есенції");
            return false;
        }

        queuedUnits.Add(new QueuedAlly(unit, SelectedLane));
        QueueChanged?.Invoke();
        return true;
    }

    public void SelectLane(BattleLane lane)
    {
        SelectedLane = BattleLane.Upper;
    }

    private void ReleaseQueuedWave(int waveNumber, int totalWaves)
    {
        if (queuedUnits.Count == 0) return;

        ResolveSpawner();
        if (allySpawner == null) return;

        QueuedAlly[] wave = queuedUnits.ToArray();
        queuedUnits.Clear();
        QueueChanged?.Invoke();
        StartCoroutine(allySpawner.SpawnWave(wave));
    }

    private void SubscribeToGameManager()
    {
        subscribedGameManager = GameManager.Instance != null
            ? GameManager.Instance
            : FindAnyObjectByType<GameManager>();

        if (subscribedGameManager != null)
        {
            subscribedGameManager.WaveChanged += ReleaseQueuedWave;
        }
    }

    private void ResolveSpawner()
    {
        if (allySpawner != null) return;

        allySpawner = FindAnyObjectByType<AllySpawner>();
        if (allySpawner != null) return;

        GameObject spawnerObject = new GameObject("Ally Wave Spawner");
        GameObject player = GameObject.FindWithTag("Player");
        spawnerObject.transform.position = player != null ? player.transform.position : Vector3.zero;
        allySpawner = spawnerObject.AddComponent<AllySpawner>();
    }

    private void BuildFallbackCatalog()
    {
        if (availableUnits != null && availableUnits.Length > 0) return;

        GameObject[] prefabs = Resources.LoadAll<GameObject>("Allies");
        int count = Mathf.Min(Mathf.Max(1, catalogSize), prefabs.Length);
        availableUnits = new UnitData[count];

        for (int i = 0; i < count; i++)
        {
            UnitData unit = ScriptableObject.CreateInstance<UnitData>();
            unit.unitName = GetUkrainianName(i);
            unit.unitPrefab = prefabs[i];
            unit.maxHp = 70f + i * 45f;
            unit.moveSpeed = 1.4f + i * 0.2f;
            unit.essenceCost = 10 + i * 8;
            unit.goldPerSecondIncrease = 1 + i;
            unit.minDamage = 7f + i * 4f;
            unit.maxDamage = 11f + i * 6f;
            unit.attackRate = Mathf.Max(0.55f, 1.2f - i * 0.15f);
            unit.towerDamage = 2f + i * 2f;
            unit.armor = i * 2f;
            availableUnits[i] = unit;
        }
    }

    private static string GetUkrainianName(int index)
    {
        return index switch
        {
            0 => "Алхімік",
            1 => "Лицар",
            2 => "Монах",
            _ => "Союзник"
        };
    }
}

public readonly struct QueuedAlly
{
    public UnitData Unit { get; }
    public BattleLane Lane { get; }

    public QueuedAlly(UnitData unit, BattleLane lane)
    {
        Unit = unit;
        Lane = lane;
    }
}
