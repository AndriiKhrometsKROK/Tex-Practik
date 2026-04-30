using UnityEngine;

[CreateAssetMenu(fileName = "NewTowerData", menuName = "TD/Tower Data")]
public class TowerData : ScriptableObject
{
    public string towerName;
    public int cost;
    public float attackRadius;
    public float fireRate;
    public GameObject projectilePrefab;

    [Header("Налаштування урону")]
    public float minDamage;
    public float maxDamage;
    public bool isMagic;

    [Header("Особливості (залишайте 0, якщо не потрібно)")]
    public float critChance;       // Для Archer (напр. 0.2 для 20%)
    public float slowFactor;      // Для Ice (напр. 0.5 для уповільнення на 50%)
    public float aoeRadius;       // Для Fire (радіус вибуху)
    public float dotDamage;       // Для Fire/Frog (урон в секунду)
    public float armorShred;      // Для Alchemist (мінус броня)
    public float stunChance;      // Для Wooden (шанс оглушити)
}