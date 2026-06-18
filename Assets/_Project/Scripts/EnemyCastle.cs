// Представляє замок противника, приймає шкоду та завершує рівень після його руйнування.
using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyCastle : MonoBehaviour
{
    public event Action<float, float> HealthChanged;

    [SerializeField, Min(1f)] private float maxHealth = 500f;
    [SerializeField, Min(0.1f)] private float retaliationRange = 3.5f;
    [SerializeField, Min(1f)] private float retaliationDamage = 35f;
    [SerializeField, Min(0.1f)] private float retaliationInterval = 1.25f;

    private float currentHealth;
    private float nextAttackTime;
    private EnemyHealthBar healthBar;
    private Transform healthBarAnchor;
    private FrontTower frontTowerProxy;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
        GetComponent<Collider2D>().isTrigger = true;
        EnsureHealthBar();
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthBar?.UpdateHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (!IsAlive || Time.time < nextAttackTime) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, retaliationRange);
        foreach (Collider2D hit in hits)
        {
            AllyController ally = hit.GetComponentInParent<AllyController>();
            if (ally != null && ally.IsAlive)
            {
                ally.TakeDamage(retaliationDamage, true);
                nextAttackTime = Time.time + retaliationInterval;
                return;
            }

            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(retaliationDamage);
                nextAttackTime = Time.time + retaliationInterval;
                return;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        if (BattleFlowController.Instance != null &&
            (BattleFlowController.Instance.Phase == BattlePhase.BossBattle ||
             BattleFlowController.Instance.Phase == BattlePhase.Finale))
        {
            return;
        }
        if (frontTowerProxy != null &&
            frontTowerProxy.IsAlive &&
            BattleFlowController.Instance != null &&
            BattleFlowController.Instance.Phase == BattlePhase.SeparatedFronts)
        {
            frontTowerProxy.TakeDamage(amount);
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthBar?.UpdateHealth(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            GameManager.Instance?.SetVictory();
            gameObject.SetActive(false);
        }
    }

    public void ConfigureAsFrontTower(FrontTower tower)
    {
        frontTowerProxy = tower;
    }

    private void EnsureHealthBar()
    {
        EnemyHealthBar prefab = Resources.Load<EnemyHealthBar>("UI/EnemyHealthBar");
        healthBar = prefab != null
            ? Instantiate(prefab)
            : new GameObject("Enemy Castle Health Bar").AddComponent<EnemyHealthBar>();

        healthBarAnchor = new GameObject("Health Bar Anchor").transform;
        healthBarAnchor.SetParent(transform, false);
        healthBarAnchor.localPosition = new Vector3(0f, 2.2f, 0f);
        healthBar.Attach(healthBarAnchor, maxHealth, currentHealth);
    }

    private void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
    }
}
