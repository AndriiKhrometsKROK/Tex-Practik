using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public UnitData data;
    public float speed = 2f;
    public float damage = 10f;
    public float attackRate = 1f;

    private float _nextAttackTime;
    private Transform _player;

    void Start()
    {
        if (data != null) speed = data.moveSpeed;

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        if (_player == null) return;
        transform.position = Vector2.MoveTowards(transform.position, _player.position, speed * Time.deltaTime);
    }

    // ЭТОТ МЕТОД МЫ ДОБАВИЛИ. Его теперь видит магия.
    public void TakeDamage(float amount)
    {
        // Пока просто удаляем зомби при любом попадании
        Destroy(gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time >= _nextAttackTime)
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            _nextAttackTime = Time.time + attackRate;
        }
    }
}