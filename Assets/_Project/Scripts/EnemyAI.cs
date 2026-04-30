using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public UnitData data;
    
    private float _currentHp;
    private float _nextAttackTime;
    private Transform _player;

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

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        if (_player == null || data == null) return;
        transform.position = Vector2.MoveTowards(transform.position, _player.position, data.moveSpeed * Time.deltaTime);
    }

    // Додали параметр isMagicDamage, щоб відрізняти тип атаки гравця
    public void TakeDamage(float amount, bool isMagicDamage = false)
    {
        float finalDamage = amount;

        if (isMagicDamage)
        {
            // Магічний спротив (зменшує шкоду у відсотках)
            finalDamage -= finalDamage * data.magicResistance; 
        }
        else
        {
            // Броня (просто віднімає одиниці шкоди, але не менше 1)
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
    // 1. Телефонуємо в банк (GameManager.Instance)
    // 2. Просимо додати золото (AddGold)
    // 3. Беремо кількість золота з налаштувань цього ворога (data.goldReward)
        if (GameManager.Instance != null && data != null)
        {
            GameManager.Instance.AddGold(data.goldReward);
        }

    // Видаляємо ворога зі сцени
    Destroy(gameObject);
    }
    

    void OnTriggerStay2D(Collider2D other)
    {
        if (data == null) return;

        if (other.CompareTag("Player") && Time.time >= _nextAttackTime)
        {
            // Вираховуємо випадкову шкоду від мінімальної до максимальної
            float randomDamage = Random.Range(data.minDamage, data.maxDamage);
            
            other.GetComponent<PlayerHealth>()?.TakeDamage(randomDamage);
            _nextAttackTime = Time.time + data.attackRate;
        }
        
        // Якщо додати тег "Tower" або "Castle", можна наносити шкоду будівлям
        /*
        if (other.CompareTag("Tower") && Time.time >= _nextAttackTime)
        {
            other.GetComponent<TowerHealth>()?.TakeDamage(data.towerDamage);
            _nextAttackTime = Time.time + data.attackRate;
        }
        */
    }
}