using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "TD/Tower Data")]
public class TowerData : ScriptableObject
{
    public string towerName;
    public int cost;
    public float attackRadius;
    public float fireRate;
    public GameObject projectilePrefab;
}