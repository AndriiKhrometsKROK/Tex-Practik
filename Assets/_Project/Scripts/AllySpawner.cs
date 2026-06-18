// Створює союзних юнітів із черги та випускає їх на вибрану бойову лінію.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllySpawner : MonoBehaviour
{
    [Header("Spawn Position")]
    public Transform spawnPoint;

    [Header("Economy")]
    [SerializeField] private bool spendEssenceToSpawn = true;
    [SerializeField] private int defaultEssenceCost = 10;
    [SerializeField] private int defaultGoldPerSecondIncrease = 1;
    [SerializeField] private IncomeManager incomeManager;

    public void SpawnAlly(UnitData data)
    {
        TrySpawnAlly(data);
    }

    public bool TrySpawnAlly(UnitData data)
    {
        if (data == null)
        {
            Debug.LogError("Cannot spawn ally with null UnitData.");
            return false;
        }

        if (data.unitPrefab == null)
        {
            Debug.LogError($"UnitData {data.name} does not have a Unit Prefab assigned.");
            return false;
        }

        int essenceCost = GetEssenceCost(data);
        int incomeIncrease = GetGoldPerSecondIncrease(data);

        if (spendEssenceToSpawn)
        {
            ResolveIncomeManager();
            if (incomeManager == null)
            {
                Debug.LogError("No IncomeManager found on the scene.");
                return false;
            }

            if (!incomeManager.SpendEssence(essenceCost))
            {
                return false;
            }
        }

        SpawnQueuedAlly(data);

        if (spendEssenceToSpawn && incomeManager != null)
        {
            incomeManager.IncreaseGoldPerSecond(incomeIncrease);
        }

        return true;
    }

    public IEnumerator SpawnWave(IEnumerable<QueuedAlly> units, float spawnInterval = 0.35f)
    {
        if (units == null) yield break;

        foreach (QueuedAlly unit in units)
        {
            SpawnQueuedAlly(unit.Unit, unit.Lane);
            yield return new WaitForSeconds(Mathf.Max(0.05f, spawnInterval));
        }
    }

    public GameObject SpawnQueuedAlly(UnitData data, BattleLane lane = BattleLane.Upper)
    {
        if (data == null || data.unitPrefab == null) return null;

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        position.x = BattleLaneUtility.GetX(lane);
        GameObject spawnedUnit = ObjectPoolManager.Spawn(data.unitPrefab, position, Quaternion.identity);
        if (spawnedUnit == null) return null;
        EnsureTriggerCollider(spawnedUnit);

        if (spawnedUnit.TryGetComponent<AllyController>(out var controller))
        {
            controller.Initialize(data);
            controller.SetLane(lane);
            if (BattleFlowController.Instance != null)
            {
                controller.ApplyPermanentMultiplier(BattleFlowController.Instance.AllyPowerMultiplier);
            }
        }
        else
        {
            Debug.LogWarning($"Prefab {data.unitPrefab.name} does not have AllyController.");
        }

        return spawnedUnit;
    }

    private void EnsureTriggerCollider(GameObject spawnedUnit)
    {
        if (spawnedUnit.GetComponent<Collider2D>() != null) return;

        BoxCollider2D collider = spawnedUnit.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
    }

    private int GetEssenceCost(UnitData data)
    {
        return data != null && data.essenceCost > 0
            ? data.essenceCost
            : Mathf.Max(0, defaultEssenceCost);
    }

    private int GetGoldPerSecondIncrease(UnitData data)
    {
        return data != null && data.goldPerSecondIncrease > 0
            ? data.goldPerSecondIncrease
            : Mathf.Max(0, defaultGoldPerSecondIncrease);
    }

    private void ResolveIncomeManager()
    {
        if (incomeManager != null) return;

        incomeManager = IncomeManager.Instance != null
            ? IncomeManager.Instance
            : FindAnyObjectByType<IncomeManager>();
    }
}
