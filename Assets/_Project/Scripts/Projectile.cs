using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    private Transform _target;
    private TowerData _sourceData;

    public void Setup(Transform target, TowerData data)
    {
        _target = target;
        _sourceData = data;
    }

    void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Самонаведення на ворога
        Vector3 dir = _target.position - transform.position;
        transform.position += dir.normalized * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _target.position) < 0.2f)
        {
            ApplyDamage(_target.gameObject);
            Destroy(gameObject);
        }
    }

    void ApplyDamage(GameObject enemy)
    {
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai == null || _sourceData == null) return;

        float damage = Random.Range(_sourceData.minDamage, _sourceData.maxDamage);

        // Логіка крита (Archer)
        if (Random.value < _sourceData.critChance) damage *= 2;

        // Нанесення урону
        ai.TakeDamage(damage, _sourceData.isMagic);

        // Тут можна додати логіку уповільнення або AOE вибуху
        if (_sourceData.aoeRadius > 0)
        {
            // Логіка вибуху для Fire Tower
        }
    }
}