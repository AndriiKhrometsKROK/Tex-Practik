// Створює плаваюче число завданої шкоди та виділяє критичні удари окремим кольором.
using TMPro;
using UnityEngine;

public sealed class DamageNumberController : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.85f;
    [SerializeField] private float riseSpeed = 0.9f;

    private TextMeshPro text;
    private Color baseColor;
    private float bornAt;

    public static void Show(Vector3 worldPosition, float amount, bool critical)
    {
        if (amount <= 0f) return;

        GameObject numberObject = new GameObject(critical ? "Critical Damage Number" : "Damage Number");
        numberObject.transform.position = worldPosition + new Vector3(0f, 0.75f, 0f);
        numberObject.transform.localScale = Vector3.one * 0.12f;

        DamageNumberController controller = numberObject.AddComponent<DamageNumberController>();
        controller.Configure(Mathf.CeilToInt(amount).ToString(), critical);
    }

    private void Configure(string value, bool critical)
    {
        text = gameObject.AddComponent<TextMeshPro>();
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = critical ? 34f : 26f;
        text.fontStyle = FontStyles.Bold;
        text.sortingOrder = 200;
        baseColor = critical ? KenamUiTheme.Danger : KenamUiTheme.Gold;
        text.color = baseColor;
        bornAt = Time.time;
    }

    private void Update()
    {
        float age = Time.time - bornAt;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        float alpha = Mathf.Clamp01(1f - age / lifetime);
        Color color = baseColor;
        color.a = alpha;
        text.color = color;
    }
}
