using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [HideInInspector] // Ховаємо з інспектора, бо завантажуємо кодом
    public GameObject[] enemyPrefabs; 
    
    public float spawnRate = 2f;   
    public float spawnDistance = 10f; 

    private float _nextSpawnTime;

    void Start()
    {
        // Автоматично завантажуємо ВСІ префаби ворогів, які лежать у папці Resources/Enemies
        enemyPrefabs = Resources.LoadAll<GameObject>("Enemies");

        if (enemyPrefabs.Length == 0)
        {
            Debug.LogError("Ворогів не знайдено! Переконайся, що ти поклав префаби в папку 'Assets/Resources/Enemies'");
        }
        else
        {
            Debug.Log($"Завантажено {enemyPrefabs.Length} типів ворогів для спавну.");
        }
    }

    void Update()
    {
        if (Time.time >= _nextSpawnTime)
        {
            SpawnEnemy();
            _nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // Вибираємо випадкового ворога
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemyToSpawn = enemyPrefabs[randomIndex];

        // Спавнимо навколо точки спавнера
        Vector2 spawnPos = Random.insideUnitCircle.normalized * spawnDistance;
        Instantiate(enemyToSpawn, (Vector2)transform.position + spawnPos, Quaternion.identity);
    }
}