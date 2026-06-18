// Приймає шкоду без смерті та показує статистику ударів на тренувальному полігоні.
using TMPro;
using UnityEngine;

public class TrainingDummyController : MonoBehaviour
{
    private TextMeshPro label;
    private float totalDamage;
    private float lastDamage;
    private int hitCount;
    private int criticalCount;

    public bool IsAlive => true;

    public float TakeDamage(float amount, bool critical)
    {
        float dealt = Mathf.Max(0f, amount);
        totalDamage += dealt;
        lastDamage = dealt;
        hitCount++;
        if (critical) criticalCount++;
        UpdateLabel();
        return dealt;
    }

    public void Configure(TextMeshPro targetLabel)
    {
        label = targetLabel;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (label == null) return;

        label.text =
            $"НЕВРАЗЛИВА МІШЕНЬ\n" +
            $"Усього: {totalDamage:0}\n" +
            $"Останній удар: {lastDamage:0}\n" +
            $"Ударів: {hitCount}  Критів: {criticalCount}";
    }
}
