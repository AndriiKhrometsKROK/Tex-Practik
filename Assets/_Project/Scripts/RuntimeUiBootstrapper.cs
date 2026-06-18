// Створює резервний інтерфейс меню й матчу, якщо потрібні Canvas або контролери відсутні у сцені.
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuntimeUiBootstrapper
{
    private const string MenuSceneName = "MainMenu";
    private const string GameSceneName = "Ігрова сцена";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();

        if (gameManager == null && activeScene.name == MenuSceneName)
        {
            EnsureMainMenu();
            return;
        }

        if (gameManager != null)
        {
            // PresentationBootstrapper owns the gameplay UI.
            return;
        }
    }

    private static void EnsureMainMenu()
    {
        if (Object.FindAnyObjectByType<MainMenuController>() != null) return;

        EnsureEventSystem();
        Canvas canvas = CreateCanvas("Runtime Main Menu Canvas");
        MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();

        RectTransform root = CreatePanel("Menu Root", canvas.transform, KenamUiTheme.Panel);
        Stretch(root);

        TextMeshProUGUI title = CreateText("Title", root, "Echoes of the Void", 72f, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(600f, 110f));

        Button playButton = CreateButton("Play Button", root, "Грати");
        SetRect(playButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(280f, 60f));
        playButton.onClick.AddListener(controller.Play);

        Button settingsButton = CreateButton("Settings Button", root, "Налаштування");
        SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(280f, 60f));
        settingsButton.onClick.AddListener(controller.OpenSettings);

        Button exitButton = CreateButton("Exit Button", root, "Вихід");
        SetRect(exitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -115f), new Vector2(280f, 60f));
        exitButton.onClick.AddListener(controller.Exit);

        RectTransform settingsPanel = CreatePanel("Settings Panel", root, KenamUiTheme.PanelRaised);
        SetRect(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 230f));

        TextMeshProUGUI settingsTitle = CreateText("Settings Title", settingsPanel, "Гучність", 30f, TextAlignmentOptions.Center);
        SetRect(settingsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(340f, 58f));

        Slider volumeSlider = CreateSlider("Volume Slider", settingsPanel);
        SetRect(volumeSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(320f, 30f));
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(controller.SetVolume);

        Button closeButton = CreateButton("Close Settings Button", settingsPanel, "Назад");
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(180f, 48f));
        closeButton.onClick.AddListener(controller.CloseSettings);

        controller.Configure(GameSceneName, settingsPanel.gameObject);
    }

    private static void EnsureGameplayHud()
    {
        if (Object.FindAnyObjectByType<GameplayHUDController>() != null) return;

        EnsureEventSystem();
        Canvas canvas = Object.FindAnyObjectByType<Canvas>() ?? CreateCanvas("Runtime Game UI Canvas");
        GameplayHUDController hud = canvas.gameObject.AddComponent<GameplayHUDController>();

        RectTransform hudPanel = CreatePanel("Top HUD Panel", canvas.transform, KenamUiTheme.WithAlpha(KenamUiTheme.Panel, 0.84f));
        SetRect(hudPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -32f), new Vector2(0f, 64f));

        HorizontalLayoutGroup layout = hudPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 12, 12);
        layout.spacing = 28f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI gold = CreateText("Gold Text", hudPanel, "Золото: 0", 24f, TextAlignmentOptions.Left);
        TextMeshProUGUI health = CreateText("Base Health Text", hudPanel, "Життя: 0 / 0", 24f, TextAlignmentOptions.Center);
        TextMeshProUGUI wave = CreateText("Wave Text", hudPanel, "Хвиля 0 / 0", 24f, TextAlignmentOptions.Right);

        RectTransform victory = CreateFinalPanel(canvas.transform, "Victory Panel", "Перемога", KenamUiTheme.WithAlpha(KenamUiTheme.Swamp, 0.96f), hud);
        RectTransform gameOver = CreateFinalPanel(canvas.transform, "Game Over Panel", "Поразка", KenamUiTheme.WithAlpha(KenamUiTheme.DangerDark, 0.96f), hud);
        victory.gameObject.SetActive(false);
        gameOver.gameObject.SetActive(false);

        hud.Configure(gold, health, wave, victory.gameObject, gameOver.gameObject, MenuSceneName);
    }

    private static RectTransform CreateFinalPanel(Transform parent, string name, string titleText, Color color, GameplayHUDController hud)
    {
        RectTransform panel = CreatePanel(name, parent, color);
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 260f));

        TextMeshProUGUI title = CreateText(titleText + " Text", panel, titleText, 46f, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(360f, 80f));

        Button restart = CreateButton("Restart Button", panel, "Перезапустити");
        SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(240f, 56f));
        restart.onClick.AddListener(hud.RestartScene);

        Button menu = CreateButton("Main Menu Button", panel, "Меню");
        SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(180f, 48f));
        menu.onClick.AddListener(hud.BackToMainMenu);

        return panel;
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindAnyObjectByType<EventSystem>();
        GameObject eventSystem = existing != null ? existing.gameObject : new GameObject("EventSystem");
        if (existing == null) eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.AddComponent<RectTransform>();
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        if (color.a > 0.05f) KenamUiTheme.ApplyPanel(image, color, KenamUiTheme.PurpleMuted, 0.28f);
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = KenamUiTheme.Text;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(16f, fontSize);
        text.fontSizeMax = fontSize;
        KenamUiTheme.ApplyText(text, KenamUiTheme.Text, fontSize >= 24f);
        Stretch(text.rectTransform);
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string labelText)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = KenamUiTheme.PanelSoft;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        KenamUiTheme.ApplyButton(button, KenamUiTheme.PanelSoft, KenamUiTheme.Gold);

        TextMeshProUGUI label = CreateText("Text", buttonObject.transform, labelText, 28f, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        Slider slider = sliderObject.AddComponent<Slider>();

        RectTransform background = CreatePanel("Background", sliderObject.transform, KenamUiTheme.Charcoal);
        Stretch(background);

        RectTransform fillArea = CreatePanel("Fill Area", sliderObject.transform, Color.clear);
        Stretch(fillArea);

        RectTransform fill = CreatePanel("Fill", fillArea, KenamUiTheme.Mint);
        Stretch(fill);

        RectTransform handle = CreatePanel("Handle", sliderObject.transform, Color.white);
        SetRect(handle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 36f));

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
