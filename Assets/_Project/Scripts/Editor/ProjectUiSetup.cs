#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProjectUiSetup
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/Ігрова сцена.unity";

    [MenuItem("GayTD/Setup Menu And HUD")]
    public static void SetupAll()
    {
        CreateMainMenuScene();
        AddGameHudToGameScene();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
    }

    private static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.10f, 0.12f);
        cameraObject.tag = "MainCamera";

        EnsureEventSystem();

        Canvas canvas = CreateCanvas("Main Menu Canvas");
        MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();

        RectTransform root = CreatePanel("Menu Root", canvas.transform, new Color(0.06f, 0.07f, 0.08f, 0.94f));
        Stretch(root);

        TextMeshProUGUI title = CreateText("Title", root, "Gay TD", 72, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(600f, 110f));

        Button playButton = CreateButton("Play Button", root, "Грати");
        SetRect(playButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(280f, 60f));
        UnityEventTools.AddPersistentListener(playButton.onClick, controller.Play);

        Button settingsButton = CreateButton("Settings Button", root, "Налаштування");
        SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(280f, 60f));
        UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OpenSettings);

        Button exitButton = CreateButton("Exit Button", root, "Вихід");
        SetRect(exitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(280f, 60f));
        UnityEventTools.AddPersistentListener(exitButton.onClick, controller.Exit);

        RectTransform settingsPanel = CreatePanel("Settings Panel", root, new Color(0.12f, 0.13f, 0.15f, 0.98f));
        SetRect(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 230f));
        CreateText("Settings Title", settingsPanel, "Гучність", 30, TextAlignmentOptions.Center);
        Slider volumeSlider = CreateSlider("Volume Slider", settingsPanel);
        SetRect(volumeSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(320f, 30f));
        volumeSlider.value = AudioListener.volume;
        UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, controller.SetVolume);
        Button closeButton = CreateButton("Close Settings Button", settingsPanel, "Назад");
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(180f, 48f));
        UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseSettings);
        settingsPanel.gameObject.SetActive(false);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("settingsPanel").objectReferenceValue = settingsPanel.gameObject;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
    }

    private static void AddGameHudToGameScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindAnyObjectByType<Canvas>() ?? CreateCanvas("Canvas");

        DeleteSceneObjectsNamed("Top HUD Panel", "Victory Panel", "Game Over Panel");

        foreach (GameplayHUDController existingHud in Object.FindObjectsByType<GameplayHUDController>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(existingHud);
        }

        GameplayHUDController hud = canvas.gameObject.AddComponent<GameplayHUDController>();

        RectTransform hudPanel = CreatePanel("Top HUD Panel", canvas.transform, new Color(0.04f, 0.05f, 0.06f, 0.82f));
        SetRect(hudPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -32f), new Vector2(0f, 64f));
        HorizontalLayoutGroup layout = hudPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 12, 12);
        layout.spacing = 28f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI gold = CreateText("Gold Text", hudPanel, "Золото: 0", 24, TextAlignmentOptions.Left);
        TextMeshProUGUI health = CreateText("Base Health Text", hudPanel, "Життя: 0 / 0", 24, TextAlignmentOptions.Center);
        TextMeshProUGUI wave = CreateText("Wave Text", hudPanel, "Хвиля 0 / 0", 24, TextAlignmentOptions.Right);

        RectTransform victory = CreateFinalPanel(canvas.transform, "Victory Panel", "Перемога", new Color(0.05f, 0.22f, 0.12f, 0.96f), hud, true);
        RectTransform gameOver = CreateFinalPanel(canvas.transform, "Game Over Panel", "Поразка", new Color(0.24f, 0.05f, 0.05f, 0.96f), hud, false);
        victory.gameObject.SetActive(false);
        gameOver.gameObject.SetActive(false);

        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("goldText").objectReferenceValue = gold;
        serializedHud.FindProperty("baseHealthText").objectReferenceValue = health;
        serializedHud.FindProperty("waveText").objectReferenceValue = wave;
        serializedHud.FindProperty("victoryPanel").objectReferenceValue = victory.gameObject;
        serializedHud.FindProperty("gameOverPanel").objectReferenceValue = gameOver.gameObject;
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
    }

    private static void DeleteSceneObjectsNamed(params string[] names)
    {
        HashSet<string> namesToDelete = new HashSet<string>(names);
        List<GameObject> objectsToDelete = new List<GameObject>();

        foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (sceneObject == null) continue;
            if (!sceneObject.scene.IsValid()) continue;
            if (!namesToDelete.Contains(sceneObject.name)) continue;

            objectsToDelete.Add(sceneObject);
        }

        foreach (GameObject sceneObject in objectsToDelete)
        {
            if (sceneObject != null)
            {
                Object.DestroyImmediate(sceneObject);
            }
        }
    }

    private static RectTransform CreateFinalPanel(Transform parent, string name, string titleText, Color color, GameplayHUDController hud, bool victory)
    {
        RectTransform panel = CreatePanel(name, parent, color);
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 260f));

        TextMeshProUGUI title = CreateText(titleText + " Text", panel, titleText, 46, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(360f, 80f));

        Button restart = CreateButton("Restart Button", panel, "Перезапустити");
        SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(240f, 56f));
        UnityEventTools.AddPersistentListener(restart.onClick, hud.RestartScene);

        Button menu = CreateButton("Main Menu Button", panel, "Меню");
        SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(180f, 48f));
        UnityEventTools.AddPersistentListener(menu.onClick, hud.BackToMainMenu);

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
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
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
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(16f, fontSize);
        text.fontSizeMax = fontSize;
        Stretch(text.rectTransform);
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string labelText)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.23f, 0.36f, 0.42f, 1f);
        Button button = buttonObject.AddComponent<Button>();

        TextMeshProUGUI label = CreateText("Text", buttonObject.transform, labelText, 28, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        Slider slider = sliderObject.AddComponent<Slider>();

        RectTransform background = CreatePanel("Background", sliderObject.transform, new Color(0.2f, 0.2f, 0.2f, 1f));
        Stretch(background);

        RectTransform fillArea = CreatePanel("Fill Area", sliderObject.transform, Color.clear);
        Stretch(fillArea);
        RectTransform fill = CreatePanel("Fill", fillArea, new Color(0.36f, 0.67f, 0.51f, 1f));
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

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
    }
}
#endif
