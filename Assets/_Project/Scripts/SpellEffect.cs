using UnityEngine;

public class SpellEffect : MonoBehaviour
{
    public float damage = 50f;
    public float radius = 2f;
    public float lifetime = 3f;
    public bool isAOE = false;

    void Start()
    {
        if (isAOE)
        {
            ApplyAOE();
            Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject, lifetime);
        }
    }

    void ApplyAOE()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                // ВЫЗЫВАЕМ ТОТ САМЫЙ МЕТОД:
                enemy.GetComponent<EnemyAI>()?.TakeDamage(damage);
                Debug.Log("Магия бахнула по: " + enemy.name);
            }
        }
    }

    void Update()
    {
        if (!isAOE)
        {
            transform.position += transform.right * 10f * Time.deltaTime;
        }
    }
}