// Зберігає спільну палітру та правила оформлення панелей, кнопок і тексту для всього інтерфейсу.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class KenamUiTheme
{
    public static readonly Color Void = Hex("070B14");
    public static readonly Color VoidSoft = Hex("0C1220");
    public static readonly Color Charcoal = Hex("171B22");
    public static readonly Color Stone = Hex("292B31");
    public static readonly Color StoneRaised = Hex("363941");
    public static readonly Color Swamp = Hex("17251F");
    public static readonly Color Moss = Hex("293A2D");

    public static readonly Color Panel = Hex("101522", 0.96f);
    public static readonly Color PanelRaised = Hex("171D2C", 0.97f);
    public static readonly Color PanelSoft = Hex("1C2232", 0.92f);

    public static readonly Color Purple = Hex("9C55FF");
    public static readonly Color PurpleMuted = Hex("5F3B91");
    public static readonly Color Mint = Hex("55E6C1");
    public static readonly Color GreyMana = Hex("778397");
    public static readonly Color Gold = Hex("C99A45");
    public static readonly Color Danger = Hex("D9365E");
    public static readonly Color DangerDark = Hex("581729");

    public static readonly Color Text = Hex("E7E5EE");
    public static readonly Color TextMuted = Hex("969AAA");
    public static readonly Color TextDark = Hex("080B14");

    public static readonly Color Ally = Mint;
    public static readonly Color Enemy = Danger;

    public static void ApplyPanel(Image image, Color color, Color outlineColor, float outlineAlpha = 0.38f)
    {
        if (image == null) return;
        image.color = color;

        Outline outline = image.GetComponent<Outline>();
        if (outline == null) outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = WithAlpha(outlineColor, outlineAlpha);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow shadow = image.GetComponent<Shadow>();
        if (shadow == null) shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
        shadow.effectDistance = new Vector2(0f, -4f);
    }

    public static void ApplyButton(Button button, Color baseColor, Color accent)
    {
        if (button == null) return;
        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (image != null) ApplyPanel(image, baseColor, accent, 0.45f);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, accent, 0.22f);
        colors.pressedColor = Color.Lerp(Color.white, Void, 0.38f);
        colors.selectedColor = Color.Lerp(Color.white, accent, 0.14f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
    }

    public static void ApplyText(TextMeshProUGUI text, Color color, bool heading = false)
    {
        if (text == null) return;
        text.color = color;
        text.fontStyle = heading ? FontStyles.Bold | FontStyles.UpperCase : FontStyles.Bold;
        text.characterSpacing = heading ? 2f : 0.2f;
    }

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color Hex(string value, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        color.a = alpha;
        return color;
    }
}
