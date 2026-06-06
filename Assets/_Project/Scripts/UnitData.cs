using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "TD/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Visual Settings")]
    [Tooltip("Префаб із візуалом (SpriteRenderer, Animator тощо) для цього юніта")]
    public GameObject unitPrefab;

    [Header("Basic Stats")]
    public string unitName;
    public float maxHp;
    public float moveSpeed;
    public int goldReward;

    [Header("Income Settings")]
    [Min(0)] public int essenceCost = 10;
    [Min(0)] public int goldPerSecondIncrease = 1;

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
