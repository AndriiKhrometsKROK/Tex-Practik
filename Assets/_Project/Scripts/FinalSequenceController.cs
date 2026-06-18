// Відтворює фінальну сюжетну сцену та після завершення повертає керування основному потоку гри.
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalSequenceController : MonoBehaviour
{
    private static readonly string[] FinaleLines =
    {
        "Демон падає. Разом із ним завмирає нескінченна хвиля.",
        "Ке́нам дивиться на уламки замку й нарешті бачить повторення.",
        "Цей світ не був в'язницею. Він був грою, яка боялася завершитися.",
        "Сіра мана стирає останнє правило петлі.",
        "У темряві на мить з'являється знайоме жіноче обличчя.",
        "АКТ II ЗАВЕРШЕНО"
    };

    private TextMeshProUGUI lineText;
    private TextMeshProUGUI hintText;
    private bool advanceRequested;
    private Action finished;

    public static void Play(Action onFinished)
    {
        FinalSequenceController existing = FindAnyObjectByType<FinalSequenceController>();
        if (existing != null) return;

        GameObject sequenceObject = new GameObject("Final Sequence");
        FinalSequenceController sequence = sequenceObject.AddComponent<FinalSequenceController>();
        sequence.finished = onFinished;
        sequence.BuildUi();
        sequence.StartCoroutine(sequence.PlayRoutine());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            advanceRequested = true;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(transform, false);
        RectTransform backdropRect = backdrop.AddComponent<RectTransform>();
        Stretch(backdropRect);
        Image backdropImage = backdrop.AddComponent<Image>();
        backdropImage.color = KenamUiTheme.WithAlpha(KenamUiTheme.Void, 0.985f);

        lineText = CreateText(backdropRect, "Line", 42f, KenamUiTheme.Text);
        lineText.alignment = TextAlignmentOptions.Center;
        lineText.rectTransform.anchorMin = new Vector2(0.12f, 0.25f);
        lineText.rectTransform.anchorMax = new Vector2(0.88f, 0.75f);
        lineText.rectTransform.offsetMin = Vector2.zero;
        lineText.rectTransform.offsetMax = Vector2.zero;

        hintText = CreateText(backdropRect, "Hint", 18f, KenamUiTheme.Purple);
        hintText.text = "ЛКМ / ПРОБІЛ — продовжити";
        hintText.rectTransform.anchorMin = new Vector2(0.25f, 0.08f);
        hintText.rectTransform.anchorMax = new Vector2(0.75f, 0.14f);
        hintText.rectTransform.offsetMin = Vector2.zero;
        hintText.rectTransform.offsetMax = Vector2.zero;
    }

    private IEnumerator PlayRoutine()
    {
        Time.timeScale = 0f;
        foreach (string line in FinaleLines)
        {
            lineText.text = line;
            advanceRequested = false;
            float elapsed = 0f;
            while (!advanceRequested && elapsed < 4.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        Time.timeScale = 1f;
        finished?.Invoke();
        Destroy(gameObject);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, float size, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        KenamUiTheme.ApplyText(text, color, size >= 24f);
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
