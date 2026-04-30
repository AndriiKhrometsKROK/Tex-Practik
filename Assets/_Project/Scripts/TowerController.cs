using UnityEngine;

public class TowerController : MonoBehaviour
{
    public TowerData data;
    private float _nextFireTime;

    void Update()
    {
        if (data == null) return;

        // Шукаємо найближчого ворога в радіусі
        GameObject target = FindNearestEnemy();

        if (target != null && Time.time >= _nextFireTime)
        {
            Shoot(target);
            _nextFireTime = Time.time + data.fireRate;
        }
    }

    GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDistance = data.attackRadius;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    void Shoot(GameObject target)
    {
        GameObject projGO = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
        Projectile proj = projGO.GetComponent<Projectile>();
        
        if (proj != null)
        {
            // Передаємо дані вежі в снаряд, щоб він знав, як бити
            proj.Setup(target.transform, data);
        }
    }
}