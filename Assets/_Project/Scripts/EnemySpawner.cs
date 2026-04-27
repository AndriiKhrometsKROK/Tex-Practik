using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // Префаб зомби
    public float spawnRate = 2f;   // Раз в сколько секунд спавним
    public float spawnDistance = 10f; // Радиус появления врагов

    private float _nextSpawnTime;

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
        // Случайная точка на окружности вокруг спавнера
        Vector2 spawnPos = Random.insideUnitCircle.normalized * spawnDistance;
        Instantiate(enemyPrefab, (Vector2)transform.position + spawnPos, Quaternion.identity);
    }
}