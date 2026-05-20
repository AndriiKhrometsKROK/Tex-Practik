using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public UnitData data;
    
    private float _currentHp;
    private float _nextAttackTime;
    
    // Змінні для системи Waypoints
    private Transform _targetWaypoint;
    private int _waypointIndex = 0;

    void Start()
    {
        if (data != null)
        {
            _currentHp = data.maxHp;
        }
        else
        {
            Debug.LogError("UnitData не призначено для " + gameObject.name);
            _currentHp = 100f; // Запобіжник
        }

        // Встановлюємо першу точку маршруту як ціль
        if (Waypoints.points != null && Waypoints.points.Length > 0)
        {
            _targetWaypoint = Waypoints.points[0];
        }
        else
        {
            Debug.LogError("Маршрут не знайдено! Додай об'єкт зі скриптом Waypoints на сцену.");
        }
    }

    void Update()
    {
        if (data == null) return;
        
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (_targetWaypoint == null) return;

        // Рухаємось до поточної цільової точки
        transform.position = Vector2.MoveTowards(transform.position, _targetWaypoint.position, data.moveSpeed * Time.deltaTime);

        // Якщо ворог дійшов до точки (з мінімальною похибкою 0.1f)
        if (Vector2.Distance(transform.position, _targetWaypoint.position) <= 0.1f)
        {
            GetNextWaypoint();
        }
    }

    private void GetNextWaypoint()
    {
        // Якщо дійшли до останньої точки маршруту (тобто до бази гравця)
        if (_waypointIndex >= Waypoints.points.Length - 1)
        {
            ReachBase();
            return;
        }

        // Перемикаємось на наступну точку
        _waypointIndex++;
        _targetWaypoint = Waypoints.points[_waypointIndex];
    }

    private void ReachBase()
    {
        // ТУТ В МАЙБУТНЬОМУ МОЖНА ДОДАТИ: GameManager.Instance.TakeLife(1);
        Debug.Log(gameObject.name + " дійшов до бази!");
        Destroy(gameObject); // Знищуємо ворога, бо він пройшов маршрут
    }

    public void TakeDamage(float amount, bool isMagicDamage = false)
    {
        float finalDamage = amount;

        if (isMagicDamage)
        {
            // Магічний спротив
            finalDamage -= finalDamage * data.magicResistance; 
        }
        else
        {
            // Броня
            finalDamage = Mathf.Max(1f, finalDamage - data.armor);
        }

        _currentHp -= finalDamage;
        
        if (_currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (GameManager.Instance != null && data != null)
        {
            GameManager.Instance.AddGold(data.goldReward);
        }

        Destroy(gameObject);
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (data == null) return;

        // Залишив логіку нанесення шкоди, якщо ворог стикається з гравцем або перешкодами
        if (other.CompareTag("Player") && Time.time >= _nextAttackTime)
        {
            float randomDamage = Random.Range(data.minDamage, data.maxDamage);
            other.GetComponent<PlayerHealth>()?.TakeDamage(randomDamage);
            _nextAttackTime = Time.time + data.attackRate;
        }
    }
}