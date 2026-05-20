using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [HideInInspector] 
    public GameObject[] enemyPrefabs; 
    
    public float spawnRate = 2f;   

    private float _nextSpawnTime;

    void Start()
    {
        enemyPrefabs = Resources.LoadAll<GameObject>("Enemies");

        if (enemyPrefabs.Length == 0)
        {
            Debug.LogError("Ворогів не знайдено! Переконайся, що ти поклав префаби в папку 'Assets/Resources/Enemies'");
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

        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemyToSpawn = enemyPrefabs[randomIndex];

        // Спавнимо ворога рівно на позиції Spawner-а (який має стояти на початку шляху)
        Instantiate(enemyToSpawn, transform.position, Quaternion.identity);
    }
}