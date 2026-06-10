using UnityEngine;

public class HeroBasicAttack : MonoBehaviour
{
    [SerializeField, Min(1f)] private float damage = 35f;
    [SerializeField, Min(0.1f)] private float attackRange = 3.2f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.05f;

    private float nextAttackTime;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || Time.time < nextAttackTime) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        EnemyAI enemy = FindEnemyNear(mouseWorld);
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            nextAttackTime = Time.time + attackCooldown;
            return;
        }

        EnemyCastle castle = FindAnyObjectByType<EnemyCastle>();
        if (castle != null && castle.IsAlive &&
            Vector2.Distance(transform.position, castle.transform.position) <= attackRange &&
            Vector2.Distance(mouseWorld, castle.transform.position) <= 2f)
        {
            castle.TakeDamage(damage);
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private EnemyAI FindEnemyNear(Vector2 mouseWorld)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorld, 0.8f);
        foreach (Collider2D hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy != null && enemy.IsAlive &&
                Vector2.Distance(transform.position, enemy.transform.position) <= attackRange)
            {
                return enemy;
            }
        }

        return null;
    }
}
