using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "TD/Unit Data")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public float maxHp;
    public float moveSpeed;
    public int goldReward;
}
