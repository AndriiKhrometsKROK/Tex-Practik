using UnityEngine;

[CreateAssetMenu(fileName = "NewTowerData", menuName = "TD/Tower Data")]
public class TowerData : ScriptableObject
{
    public string towerName;
    public int cost;
    public float attackRadius;
    public float fireRate;
    public GameObject projectilePrefab;
    public GameObject towerPrefab;

    [Header("Upgrade")]
    public TowerData upgradedTowerData;
    public int upgradeCost;

    [Header("Damage")]
    public float minDamage;
    public float maxDamage;
    public bool isMagic;

    [Header("Special Effects")]
    public float critChance;
    public float slowFactor;
    public float slowDuration = 2.5f;
    public float aoeRadius;
    public float dotDamage;
    public float dotDuration = 3f;
    public float dotTickInterval = 1f;
    public float armorShred;
    public float stunChance;
}
