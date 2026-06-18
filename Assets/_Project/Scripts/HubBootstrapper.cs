// Збирає хаб у рантаймі: ландшафт, будівлі, героя, інтерактивні зони та панелі бібліотеки, демона й рівнів.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HubBootstrapper
{
    private static readonly Color Ink = KenamUiTheme.Void;
    private static readonly Color Panel = KenamUiTheme.Panel;
    private static readonly Color Gold = KenamUiTheme.Gold;
    private static readonly Color Pale = KenamUiTheme.Text;

    public static void Build()
    {
        if (GameObject.Find("KenomArch Hub") != null) return;

        EnsureEventSystem();
        ConfigureCamera();

        VisualAssetCatalog assets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");
        GameObject root = new GameObject("KenomArch Hub");
        Sprite white = CreateWhiteSprite();

        if (!RuntimeBattlefieldVisuals.TryBuildFarmHub(root.transform))
        {
            CreateWorldPart(root.transform, "Hub Ground", white, Vector2.zero, new Vector2(22f, 11f), KenamUiTheme.VoidSoft, -20);
            CreateWorldPart(root.transform, "Central Plaza Edge", white, Vector2.zero, new Vector2(6.7f, 4.9f), KenamUiTheme.Charcoal, -19);
            CreateWorldPart(root.transform, "Central Plaza", white, Vector2.zero, new Vector2(6.2f, 4.5f), KenamUiTheme.Stone, -18);
            CreatePath(root.transform, white, new Vector2(0f, 3.4f), new Vector2(2.1f, 4.2f));
            CreatePath(root.transform, white, new Vector2(-4.2f, 0.8f), new Vector2(4.8f, 1.5f));
            CreatePath(root.transform, white, new Vector2(4.2f, 0.8f), new Vector2(4.8f, 1.5f));
        }

        Texture2D houseTexture = RuntimeSpriteAssetMap.LoadTexture("Visuals/Farm/House");
        Texture2D propsTexture = RuntimeSpriteAssetMap.LoadTexture("Visuals/Props/AtlasProps");
        Sprite library = CreateFullSprite(assets != null ? assets.tinyLibrary : null, 100f)
            ?? RuntimeSpriteAssetMap.SpriteFromTopLeft(houseTexture, 4f, 3f, 72f, 86f, 32f, RuntimeSpriteAssetMap.BottomCenter);
        Sprite demonTower = CreateFullSprite(assets != null ? assets.tinyDemonTower : null, 100f)
            ?? RuntimeSpriteAssetMap.SpriteFromTopLeft(propsTexture, 10f, 2f, 101f, 182f, 64f, RuntimeSpriteAssetMap.BottomCenter);
        Sprite castle = CreateFullSprite(assets != null ? assets.tinyAllyCastle : null, 100f)
            ?? RuntimeSpriteAssetMap.SpriteFromTopLeft(propsTexture, 359f, 194f, 144f, 189f, 64f, RuntimeSpriteAssetMap.BottomCenter);
        Sprite trainingHall = CreateFullSprite(assets != null ? assets.tinyEnemyCastle : null, 100f)
            ?? CreateFullSprite(assets != null ? assets.tinyLibrary : null, 100f);

        CreateSpritePart(root.transform, "Library", library, new Vector2(-5.8f, 1.55f), Vector2.one * 1.05f, 3);
        CreateSpritePart(root.transform, "Demon Tower", demonTower, new Vector2(5.8f, 1.55f), Vector2.one * 0.98f, 3);
        CreateSpritePart(root.transform, "Level Gate", castle, new Vector2(0f, 3.82f), Vector2.one * 0.67f, 3);
        CreateSpritePart(root.transform, "Training Hall", trainingHall, new Vector2(0f, -3.65f), Vector2.one * 0.55f, 3);
        CreateWorldPart(root.transform, "Library Signal", white, new Vector2(-5.8f, 0.28f), new Vector2(2.55f, 0.08f), KenamUiTheme.Mint, 4);
        CreateWorldPart(root.transform, "Demon Signal", white, new Vector2(5.8f, 0.28f), new Vector2(2.55f, 0.08f), KenamUiTheme.Danger, 4);
        CreateWorldPart(root.transform, "Gate Signal", white, new Vector2(0f, 2.95f), new Vector2(2.65f, 0.08f), KenamUiTheme.Purple, 4);
        CreateWorldPart(root.transform, "Training Signal", white, new Vector2(0f, -2.38f), new Vector2(2.55f, 0.08f), KenamUiTheme.Gold, 4);
        CreateWorldLabel(root.transform, "БІБЛІОТЕКА", new Vector2(-5.8f, 0.02f), KenamUiTheme.Mint);
        CreateWorldLabel(root.transform, "ДЕМОН", new Vector2(5.8f, 0.02f), KenamUiTheme.Danger);
        CreateWorldLabel(root.transform, "КАРТА ЦИКЛУ", new Vector2(0f, 2.82f), KenamUiTheme.Purple);
        CreateWorldLabel(root.transform, "ПОЛІГОН", new Vector2(0f, -2.52f), KenamUiTheme.Gold);

        if (assets != null && assets.tinyTree != null && Resources.Load<Texture2D>("Visuals/Farm/MapleTree") == null)
        {
            Sprite tree = Sprite.Create(assets.tinyTree, new Rect(0f, 384f, 128f, 192f), new Vector2(0.5f, 0.35f), 64f);
            Vector2[] trees =
            {
                new Vector2(-8f, 3.6f), new Vector2(-8f, -2.8f), new Vector2(-5.5f, -3.7f),
                new Vector2(8f, 3.6f), new Vector2(8f, -2.8f), new Vector2(5.5f, -3.7f)
            };
            foreach (Vector2 position in trees) CreateSpritePart(root.transform, "Tree", tree, position, Vector2.one * 0.72f, 1);
        }

        GameObject hero = CreateHero(assets);
        Canvas canvas = CreateCanvas("Hub UI", 100);
        HubRuntimeController runtime = root.AddComponent<HubRuntimeController>();

        TextMeshProUGUI prompt = CreateText(canvas.transform, "Interaction Prompt", string.Empty, 20f, Gold);
        RectTransform promptCard = CreatePanel(canvas.transform, "Interaction Prompt Card", KenamUiTheme.PanelRaised);
        SetRect(promptCard, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(760f, 58f));
        prompt.transform.SetParent(promptCard, false);
        Stretch(prompt.rectTransform);

        TextMeshProUGUI controls = CreateText(
            canvas.transform,
            "Hub Controls",
            "ПКМ — РУХ  •  E — УВІЙТИ  •  ЛКМ ПО БУДІВЛІ — ВІДКРИТИ  •  ESC — ЗАКРИТИ",
            15f,
            KenamUiTheme.TextMuted);
        SetRect(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(900f, 28f));

        TextMeshProUGUI title = CreateText(
            canvas.transform,
            "Hub Title",
            $"ПРИХИСТОК МІЖ ЦИКЛАМИ  •  КЕ́НАМ {CampaignProgress.HeroLevel} РІВНЯ",
            25f,
            Pale);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(620f, 54f));

        GameObject libraryPanel = BuildLibraryPanel(canvas.transform);
        GameObject demonPanel = BuildDemonPanel(canvas.transform);
        GameObject levelsPanel = BuildLevelsPanel(canvas.transform);
        GameObject storyPanel = BuildStoryPanel(canvas.transform);
        GameObject trainingPanel = BuildTrainingPanel(canvas.transform);
        runtime.Configure(hero.transform, prompt, libraryPanel, demonPanel, levelsPanel, storyPanel, trainingPanel);
        storyPanel.SetActive(CampaignProgress.ShowStoryOnNextHub);
        CampaignProgress.ShowStoryOnNextHub = false;
    }

    private static GameObject CreateHero(VisualAssetCatalog assets)
    {
        GameObject hero = assets != null && assets.heroPrefab != null
            ? Object.Instantiate(assets.heroPrefab, new Vector3(0f, -1.6f, 0f), Quaternion.identity)
            : new GameObject("Hub Hero");

        hero.name = "Ке́нам";
        foreach (MonoBehaviour component in hero.GetComponents<MonoBehaviour>()) Object.Destroy(component);
        hero.AddComponent<HubHeroController>();
        HeroVisualAnimator animator = hero.AddComponent<HeroVisualAnimator>();
        animator.Configure(assets != null ? assets.tinyHeroSheet : null);
        hero.transform.localScale = Vector3.one * 2.6f;
        return hero;
    }

    private static GameObject BuildStoryPanel(Transform parent)
    {
        GameObject panel = CreateModal(parent, "Story Panel", "ПРОЛОГ");
        TextMeshProUGUI body = CreateText(panel.transform, "Story", CampaignNarrative.GetHubStory(), 22f, Pale);
        SetRect(body.rectTransform, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);
        Button close = CreateButton(panel.transform, "Story Continue", "УВІЙТИ ДО ПРИХИСТКУ");
        SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(340f, 58f));
        close.onClick.AddListener(() =>
        {
            panel.SetActive(false);
            GameAudioController.PlayMusic(GameMusicTrack.Hub);
        });
        return panel;
    }

    private static GameObject BuildTrainingPanel(Transform parent)
    {
        GameObject panel = CreateModal(parent, "Training Panel", "ПОЛІГОН АРТЕФАКТІВ");
        TextMeshProUGUI body = CreateText(
            panel.transform,
            "Training Text",
            "Окрема територія для перевірки предметів, критів, вампіризму та активних здібностей.\n\n" +
            "На полігоні золото й есенція нескінченні, хвилі не стартують, а мішень невразлива і рахує увесь завданий їй урон.",
            22f,
            Pale);
        SetRect(body.rectTransform, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.74f), Vector2.zero, Vector2.zero);

        Button enter = CreateButton(panel.transform, "Enter Training", "УВІЙТИ НА ПОЛІГОН");
        SetRect(enter.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 125f), new Vector2(330f, 58f));
        enter.onClick.AddListener(() =>
        {
            TrainingGroundState.Request();
            SceneManager.LoadScene("Ігрова сцена");
        });

        AddCloseButton(panel);
        panel.SetActive(false);
        return panel;
    }

    private static GameObject BuildLibraryPanel(Transform parent)
    {
        GameObject panel = CreateModal(parent, "Library Panel", "БІБЛІОТЕКА НЕБУТТЯ");
        TextMeshProUGUI article = CreateText(panel.transform, "Article", "Оберіть запис з архіву.", 20f, Pale);
        SetRect(article.rectTransform, new Vector2(0.34f, 0.2f), new Vector2(0.92f, 0.8f), Vector2.zero, Vector2.zero);
        article.alignment = TextAlignmentOptions.TopLeft;

        for (int i = 0; i < CampaignNarrative.LibraryEntries.Length; i++)
        {
            LoreEntry entry = CampaignNarrative.LibraryEntries[i];
            float anchorY = 0.74f - i * 0.048f;
            AddArticleButton(panel.transform, entry, anchorY, article);
        }
        AddCloseButton(panel);
        panel.SetActive(false);
        return panel;
    }

    private static GameObject BuildDemonPanel(Transform parent)
    {
        GameObject panel = CreateModal(parent, "Demon Panel", "ДЕМОН НА ВЕЖІ");
        TextMeshProUGUI body = CreateText(panel.transform, "Demon Text", CampaignNarrative.GetDemonDialogue(), 22f, Pale);
        SetRect(body.rectTransform, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.75f), Vector2.zero, Vector2.zero);

        Button talk = CreateButton(panel.transform, "Talk", "ГОВОРИТИ");
        SetRect(talk.GetComponent<RectTransform>(), new Vector2(0.3f, 0f), new Vector2(0.3f, 0f), new Vector2(0f, 110f), new Vector2(240f, 55f));
        talk.onClick.AddListener(() => body.text = "«На двадцятому рівні ти, можливо, навчишся хоча б бачити мій удар. До того часу ми обидва знаємо результат.»");

        Button challenge = CreateButton(panel.transform, "Challenge", "КИНУТИ ВИКЛИК");
        SetRect(challenge.GetComponent<RectTransform>(), new Vector2(0.7f, 0f), new Vector2(0.7f, 0f), new Vector2(0f, 110f), new Vector2(270f, 55f));
        challenge.onClick.AddListener(() =>
        {
            if (CampaignProgress.HighestUnlocked < 20)
            {
                body.text = "Демон навіть не підводиться. Один рух, і ви прокидаєтесь біля входу до прихистку. Поки що перемога неможлива.";
                return;
            }

            CampaignProgress.SelectedLevel = CampaignProgress.HighestUnlocked;
            CampaignProgress.RequestDemonChallenge();
            TrainingGroundState.Clear();
            SceneManager.LoadScene("Ігрова сцена");
        });
        AddCloseButton(panel);
        panel.SetActive(false);
        return panel;
    }

    private static GameObject BuildLevelsPanel(Transform parent)
    {
        GameObject panel = CreateModal(parent, "Levels Panel", "КАРТА ЦИКЛУ");
        RectTransform grid = CreatePanel(panel.transform, "Level Grid", KenamUiTheme.WithAlpha(KenamUiTheme.VoidSoft, 0.86f));
        SetRect(grid, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);

        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(90f, 52f);
        layout.spacing = new Vector2(10f, 10f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 8;

        for (int level = 1; level <= CampaignProgress.VisibleLevelCount; level++)
        {
            int capturedLevel = level;
            bool real = level <= CampaignProgress.FinalLevel;
            bool unlocked = CampaignProgress.CanEnter(level);
            Button button = CreateButton(grid, "Level " + level, level.ToString("00"));
            button.interactable = unlocked;
            if (unlocked)
            {
                button.onClick.AddListener(() =>
                {
                    CampaignProgress.SelectedLevel = capturedLevel;
                    TrainingGroundState.Clear();
                    SceneManager.LoadScene("Ігрова сцена");
                });
            }
        }

        AddCloseButton(panel);
        panel.SetActive(false);
        return panel;
    }

    private static void AddArticleButton(Transform parent, LoreEntry entry, float anchorY, TextMeshProUGUI article)
    {
        bool unlocked = CampaignProgress.HighestUnlocked >= entry.RequiredLevel || CampaignProgress.Act2Completed;
        string label = unlocked ? entry.Title : $"ЗАКРИТО • РІВЕНЬ {entry.RequiredLevel}";
        Button button = CreateButton(parent, entry.Title, label);
        SetRect(button.GetComponent<RectTransform>(), new Vector2(0.2f, anchorY), new Vector2(0.2f, anchorY), Vector2.zero, new Vector2(280f, 32f));
        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (labelText != null) labelText.fontSize = 13f;
        button.interactable = unlocked;
        if (unlocked)
        {
            button.onClick.AddListener(() =>
            {
                article.text = entry.Body;
                GameAudioController.PlaySfx(GameSfxCue.MysteryReveal, 0.65f);
            });
        }
    }

    private static void AddCloseButton(GameObject panel)
    {
        Button close = CreateButton(panel.transform, "Close", "ЗАКРИТИ");
        SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(220f, 50f));
        close.onClick.AddListener(() =>
        {
            panel.SetActive(false);
            GameAudioController.PlayMusic(GameMusicTrack.Hub);
        });
    }

    private static GameObject CreateModal(Transform parent, string name, string title)
    {
        RectTransform panel = CreatePanel(parent, name, KenamUiTheme.PanelRaised);
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 650f));
        RectTransform accent = CreatePanel(panel, "Top Accent", KenamUiTheme.Purple);
        SetRect(accent, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-4f, 5f));
        TextMeshProUGUI heading = CreateText(panel, "Title", title, 32f, Gold);
        SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -55f), new Vector2(-80f, 60f));
        return panel.gameObject;
    }

    private static void CreatePath(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        CreateWorldPart(parent, "Hub Path Edge", sprite, position, size + new Vector2(0.35f, 0.35f), KenamUiTheme.Charcoal, -17);
        CreateWorldPart(parent, "Hub Path", sprite, position, size, KenamUiTheme.Stone, -16);
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        camera.orthographic = true;
        camera.orthographicSize = 5.6f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = Ink;
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

    private static Canvas CreateCanvas(string name, int order)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.color = color;
        if (color.a > 0.05f) KenamUiTheme.ApplyPanel(image, color, KenamUiTheme.PurpleMuted, 0.28f);
        return rect;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = size;
        KenamUiTheme.ApplyText(text, color, size >= 18f);
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        RectTransform rect = CreatePanel(parent, name, KenamUiTheme.PanelSoft);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        KenamUiTheme.ApplyButton(button, KenamUiTheme.PanelSoft, KenamUiTheme.Gold);
        TextMeshProUGUI text = CreateText(rect, "Label", label, 18f, Pale);
        Stretch(text.rectTransform);
        return button;
    }

    private static GameObject CreateWorldPart(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 scale, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    private static GameObject CreateSpritePart(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 scale, int order)
    {
        if (sprite == null) return null;
        return CreateWorldPart(parent, name, sprite, position, scale, Color.white, order);
    }

    private static void CreateWorldLabel(Transform parent, string value, Vector2 position, Color color)
    {
        Sprite white = CreateWhiteSprite();
        CreateWorldPart(parent, value + " Label Backplate", white, position + new Vector2(0f, -0.02f), new Vector2(2.95f, 0.42f), KenamUiTheme.WithAlpha(KenamUiTheme.Panel, 0.72f), 5);

        GameObject labelObject = new GameObject(value + " Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = position;
        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.text = value;
        label.fontSize = 2.8f;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(5f, 0.6f);
        label.renderer.sortingOrder = 6;
    }

    private static Sprite CreateFullSprite(Texture2D texture, float ppu)
    {
        return texture == null ? null : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.35f), ppu, 0, SpriteMeshType.FullRect);
    }

    private static Sprite CreateWhiteSprite()
    {
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public class HubRuntimeController : MonoBehaviour
{
    private static readonly Vector2 LibraryPosition = new Vector2(-5.8f, 1.1f);
    private static readonly Vector2 DemonPosition = new Vector2(5.8f, 1.1f);
    private static readonly Vector2 LevelsPosition = new Vector2(0f, 3.4f);
    private static readonly Vector2 TrainingPosition = new Vector2(0f, -3.05f);

    private Transform hero;
    private TextMeshProUGUI prompt;
    private GameObject libraryPanel;
    private GameObject demonPanel;
    private GameObject levelsPanel;
    private GameObject storyPanel;
    private GameObject trainingPanel;

    public void Configure(Transform targetHero, TextMeshProUGUI targetPrompt, GameObject library, GameObject demon, GameObject levels, GameObject story, GameObject training)
    {
        hero = targetHero;
        prompt = targetPrompt;
        libraryPanel = library;
        demonPanel = demon;
        levelsPanel = levels;
        storyPanel = story;
        trainingPanel = training;
    }

    private void Update()
    {
        if (hero == null || prompt == null) return;

        GameObject openPanel = GetOpenPanel();
        if (openPanel != null)
        {
            prompt.text = openPanel == storyPanel
                ? "ENTER / E / ПРОБІЛ — ПРОДОВЖИТИ"
                : "ESC / E — ЗАКРИТИ";
            if (Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.E) ||
                Input.GetKeyDown(KeyCode.Return) ||
                openPanel == storyPanel && Input.GetKeyDown(KeyCode.Space))
            {
                openPanel.SetActive(false);
                GameAudioController.PlayMusic(GameMusicTrack.Hub);
            }
            return;
        }

        GameObject target = null;
        string message = string.Empty;
        if (Vector2.Distance(hero.position, LibraryPosition) < 2.2f)
        {
            target = libraryPanel;
            message = "E — УВІЙТИ ДО БІБЛІОТЕКИ";
        }
        else if (Vector2.Distance(hero.position, DemonPosition) < 2.2f)
        {
            target = demonPanel;
            message = "E — ГОВОРИТИ З ДЕМОНОМ";
        }
        else if (Vector2.Distance(hero.position, LevelsPosition) < 2.2f)
        {
            target = levelsPanel;
            message = "E — ВІДКРИТИ КАРТУ ЦИКЛУ";
        }
        else if (Vector2.Distance(hero.position, TrainingPosition) < 2.2f)
        {
            target = trainingPanel;
            message = "E — УВІЙТИ НА ПОЛІГОН";
        }

        prompt.text = string.IsNullOrEmpty(message)
            ? "ПІДІЙДІТЬ ДО БУДІВЛІ АБО НАТИСНІТЬ НА НЕЇ"
            : message;
        if (target != null && Input.GetKeyDown(KeyCode.E)) OpenPanel(target);

        if (Input.GetMouseButtonDown(0) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            GameObject clickedPanel = GetPanelNear(camera.ScreenToWorldPoint(Input.mousePosition));
            if (clickedPanel != null) OpenPanel(clickedPanel);
        }
    }

    private GameObject GetOpenPanel()
    {
        if (storyPanel != null && storyPanel.activeSelf) return storyPanel;
        if (libraryPanel != null && libraryPanel.activeSelf) return libraryPanel;
        if (demonPanel != null && demonPanel.activeSelf) return demonPanel;
        if (levelsPanel != null && levelsPanel.activeSelf) return levelsPanel;
        if (trainingPanel != null && trainingPanel.activeSelf) return trainingPanel;
        return null;
    }

    private GameObject GetPanelNear(Vector2 position)
    {
        if (Vector2.Distance(position, LibraryPosition) < 2.2f) return libraryPanel;
        if (Vector2.Distance(position, DemonPosition) < 2.2f) return demonPanel;
        if (Vector2.Distance(position, LevelsPosition) < 2.2f) return levelsPanel;
        if (Vector2.Distance(position, TrainingPosition) < 2.2f) return trainingPanel;
        return null;
    }

    private void OpenPanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);
        if (panel == libraryPanel) GameAudioController.PlayMusic(GameMusicTrack.Library);
        else if (panel == demonPanel) GameAudioController.PlayMusic(GameMusicTrack.DemonConversation);
        else if (panel == levelsPanel) GameAudioController.PlayMusic(GameMusicTrack.LevelSelection);
        else if (panel == trainingPanel) GameAudioController.PlayMusic(GameMusicTrack.Preparation);
        else GameAudioController.PlayMusic(GameMusicTrack.Hub);
    }
}
