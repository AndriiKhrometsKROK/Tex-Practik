using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);
    [SerializeField] private Vector2 size = new Vector2(0.9f, 0.12f);
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private Color fillColor = new Color(0.25f, 0.95f, 0.35f, 0.95f);

    private Transform _target;
    private float _maxHealth = 1f;

    private void Awake()
    {
        EnsureSlider();
    }

    public void Attach(Transform target, float maxHealth, float currentHealth)
    {
        _target = target;
        gameObject.SetActive(true);
        UpdateHealth(currentHealth, maxHealth);
        UpdatePosition();
    }

    public void Detach()
    {
        _target = null;
        gameObject.SetActive(false);
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        EnsureSlider();

        _maxHealth = Mathf.Max(1f, maxHealth);
        slider.value = Mathf.Clamp01(currentHealth / _maxHealth);
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_target == null) return;

        transform.position = _target.position + worldOffset;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    private void EnsureSlider()
    {
        if (slider != null) return;

        slider = GetComponentInChildren<Slider>(true);
        if (slider != null) return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("World Space Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;
        canvasRect.localScale = Vector3.one;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvas.transform, false);
        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        Stretch(backgroundRect);

        Image background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;

        GameObject sliderObject = new GameObject("Slider");
        sliderObject.transform.SetParent(canvas.transform, false);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        Stretch(sliderRect);

        slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        GameObject fillAreaObject = new GameObject("Fill Area");
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
        Stretch(fillAreaRect);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        Stretch(fillRect);

        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;

        slider.fillRect = fillRect;
        slider.targetGraphic = fill;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
