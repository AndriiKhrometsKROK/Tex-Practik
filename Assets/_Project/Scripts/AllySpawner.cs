using UnityEngine;

public class AllySpawner : MonoBehaviour
{
    [Header("Spawn Position")]
    public Transform spawnPoint;

    /// <summary>
    /// Головний метод створення юніта. Передайте сюди потрібний ScriptableObject персонажа.
    /// </summary>
    public void SpawnAlly(UnitData data)
    {
        if (data == null)
        {
            Debug.LogError("Спроба заспавнити юніта з порожніми даними (Null Data)!");
            return;
        }

        if (data.unitPrefab == null)
        {
            Debug.LogError($"У файлі даних {data.name} не призначено візуальний Unit Prefab!");
            return;
        }

        // 1. Створюємо префаб, який вказано всередині ScriptableObject
        Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject spawnedUnit = Instantiate(data.unitPrefab, position, Quaternion.identity);

        // 2. Передаємо створеному об'єкту його цифрові характеристики
        if (spawnedUnit.TryGetComponent<AllyController>(out var controller))
        {
            controller.Initialize(data);
        }
        else
        {
            Debug.LogWarning($"На префабі {data.unitPrefab.name} відсутній скрипт AllyController!");
        }
    }
}