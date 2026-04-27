using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "TD/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Basic Stats")]
    public string unitName;
    public float maxHp;
    public float moveSpeed;
    public int goldReward;

    [Header("Combat Stats")]
    public float minDamage;
    public float maxDamage;
    public float attackRate;
    public float towerDamage; // Шкода по замку/базі

    [Header("Defensive Stats")]
    public float armor; // Зменшує фізичну шкоду
    [Range(0f, 1f)]
    public float magicResistance; // Відсоток (0.2 = 20% спротиву магії)
}