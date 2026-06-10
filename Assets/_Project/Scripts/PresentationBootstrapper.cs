using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PresentationBootstrapper
{
    private static readonly Color Ink = new Color(0.025f, 0.027f, 0.045f, 1f);
    private static readonly Color Panel = new Color(0.055f, 0.06f, 0.095f, 0.94f);
    private static readonly Color PanelSoft = new Color(0.075f, 0.08f, 0.12f, 0.94f);
    private static readonly Color Violet = new Color(0.42f, 0.25f, 0.7f, 1f);
    private static readonly Color Gold = new Color(0.86f, 0.66f, 0.28f, 1f);
    private static readonly Color Pale = new Color(0.86f, 0.84f, 0.92f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureEventSystem();

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            BuildMainMenu();
            return;
        }

        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        EnsureGameplayPresentation(manager);
    }

    public static void EnsureGameplayPresentation(GameManager manager)
    {
        if (manager == null) return;
        BuildGameplayHud(manager);
    }

    private static void BuildMainMenu()
    {
        if (GameObject.Find("KenomArch Presentation Menu") != null) return;

        Disable("Menu Root");
        MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
        Canvas canvas = CreateCanvas("KenomArch Presentation Menu", 100);

        RectTransform background = CreatePanel("Void Background", canvas.transform, Ink);
        Stretch(background);

        RectTransform glow = CreatePanel("Violet Glow", background, new Color(Violet.r, Violet.g, Violet.b, 0.18f));
        SetRect(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 40f), new Vector2(850f, 1500f));
        glow.localRotation = Quaternion.Euler(0f, 0f, -18f);

        RectTransform card = CreatePanel("Menu Card", background, Panel);
        SetRect(card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(390f, 0f), new Vector2(600f, 720f));
        AddOutline(card.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.5f), new Vector2(2f, -2f));

        AddText(card, "Act", "АКТ II  •  ГРА ЛОРДА", 21f, Gold, -82f, 44f);
        AddText(card, "Title", "KENOMARCH", 66f, Pale, -155f, 90f);
        AddText(card, "Subtitle", "ПЕТЛЯ НЕБУТТЯ", 31f, new Color(0.65f, 0.52f, 0.9f), -225f, 52f);
        AddText(card, "Description", "Дві лінії. Один герой. Нескінченна гра, правила якої доведеться зламати.", 22f, new Color(0.67f, 0.67f, 0.76f), -315f, 110f);

        Button play = CreateButton("Play Button", card, "ПОЧАТИ ГРУ", Gold, Ink);
        SetRect(play.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 205f), new Vector2(-100f, 68f));
        if (controller != null) play.onClick.AddListener(controller.Play);

        Button settings = CreateButton("Settings Button", card, "НАЛАШТУВАННЯ", PanelSoft, Pale);
        SetRect(settings.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 120f), new Vector2(-100f, 62f));
        if (controller != null) settings.onClick.AddListener(controller.OpenSettings);

        Button exit = CreateButton("Exit Button", card, "ВИЙТИ", new Color(0.12f, 0.1f, 0.15f), new Color(0.7f, 0.67f, 0.74f));
        SetRect(exit.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 42f), new Vector2(-100f, 54f));
        if (controller != null) exit.onClick.AddListener(controller.Exit);

        RectTransform settingsPanel = CreatePanel("Presentation Settings", background, Panel);
        SetRect(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 300f));
        AddOutline(settingsPanel.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.45f), new Vector2(2f, -2f));
        AddText(settingsPanel, "Settings Title", "НАЛАШТУВАННЯ ЗВУКУ", 27f, Pale, -55f, 46f);

        Slider volume = CreateSlider("Volume", settingsPanel);
        SetRect(volume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(360f, 24f));
        volume.value = AudioListener.volume;
        if (controller != null) volume.onValueChanged.AddListener(controller.SetVolume);

        Button close = CreateButton("Close Settings", settingsPanel, "НАЗАД", Gold, Ink);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(210f, 54f));
        if (controller != null)
        {
            close.onClick.AddListener(controller.CloseSettings);
            controller.Configure("Ігрова сцена", settingsPanel.gameObject);
        }
    }

    private static void BuildGameplayHud(GameManager manager)
    {
        if (GameObject.Find("KenomArch Gameplay HUD") != null) return;

        TowerData[] towerCatalog = CollectTowerCatalog();
        DisableLegacyGameplayUi();
        EnsureGameplayWorld(manager);
        EnsureEnemyCastle(manager);

        Canvas canvas = CreateCanvas("KenomArch Gameplay HUD", 90);
        PresentationHudController controller = canvas.gameObject.AddComponent<PresentationHudController>();

        RectTransform topBar = CreatePanel("Strategic Top Bar", canvas.transform, Panel);
        SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-28f, 72f));
        AddOutline(topBar.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.55f), new Vector2(0f, -2f));

        TextMeshProUGUI health = CreateStat(topBar, "Замок", "100 / 100", 0.02f);
        TextMeshProUGUI wave = CreateStat(topBar, "Хвиля", "0 / 0", 0.28f);
        TextMeshProUGUI gold = CreateStat(topBar, "Золото", "0", 0.56f);
        TextMeshProUGUI essence = CreateStat(topBar, "Есенція", "0", 0.78f);

        RectTransform command = CreatePanel("Wave Command Card", canvas.transform, PanelSoft);
        SetRect(command, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -190f), new Vector2(326f, 214f));
        AddOutline(command.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.35f), new Vector2(-2f, -2f));

        AddText(command, "Next Wave", "НАСТУПНА ХВИЛЯ", 18f, Gold, -30f, 30f);
        TextMeshProUGUI state = AddText(command, "State", "Підготовка", 27f, Pale, -82f, 48f);
        TextMeshProUGUI preview = AddText(command, "Enemy Preview", "Склад хвилі", 15f, new Color(0.7f, 0.67f, 0.78f), -128f, 42f);

        Button ready = CreateButton("Ready Button", command, "ГОТОВО", Gold, Ink);
        SetRect(ready.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 35f), new Vector2(-34f, 56f));
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            ready.onClick.AddListener(spawner.StartNextWave);
            spawner.startNextWaveButton = ready;
            spawner.RefreshWaveButton();
            EnemyWavePreviewController previewController = command.gameObject.AddComponent<EnemyWavePreviewController>();
            previewController.Configure(spawner, manager, preview);
        }

        controller.Configure(manager, health, wave, gold, essence, state, ready);
        BuildAllyQueue(canvas.transform);
        BuildTowerDock(canvas.transform, towerCatalog);
        BuildNotification(canvas.transform);
        BuildControlHint(canvas.transform);
    }

    private static void DisableLegacyGameplayUi()
    {
        foreach (Canvas existing in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (existing.gameObject.name == "KenomArch Gameplay HUD") continue;
            existing.gameObject.SetActive(false);
        }
    }

    private static void EnsureGameplayWorld(GameManager manager)
    {
        if (GameObject.Find("KenomArch Battlefield") != null) return;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = Ink;
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        GameObject root = new GameObject("KenomArch Battlefield");
        Sprite sprite = CreateWhiteSprite();
        VisualAssetCatalog visualAssets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");

        CreateWorldPart(root.transform, "Arena", sprite, Vector2.zero, new Vector2(22f, 11f), new Color(0.035f, 0.045f, 0.07f), -20);
        bool useTinySwordsArena = visualAssets != null && visualAssets.tinyWater != null && visualAssets.tinyGround != null;
        if (useTinySwordsArena)
        {
            BuildTinySwordsArena(root.transform, visualAssets);
        }
        else
        {
            CreateWorldPart(root.transform, "Attack Lane", sprite, new Vector2(BattleLaneUtility.AttackX, 0f), new Vector2(2.7f, 11f), new Color(0.13f, 0.18f, 0.12f), -18);
            CreateWorldPart(root.transform, "Defense Lane", sprite, new Vector2(BattleLaneUtility.DefenseX, 0f), new Vector2(2.7f, 11f), new Color(0.11f, 0.16f, 0.2f), -18);
        }

        EnemySpawner enemySpawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            enemySpawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
            enemySpawner.transform.position = new Vector3(BattleLaneUtility.DefenseX, 4.6f, 0f);
        }
        else enemySpawner.transform.position = new Vector3(BattleLaneUtility.DefenseX, 4.6f, 0f);
        manager.enemySpawner = enemySpawner;
        enemySpawner.ConfigureMvpWaves();

        if (Object.FindAnyObjectByType<Waypoints>() == null)
        {
            GameObject waypoints = new GameObject("Waypoints");
            CreatePoint(waypoints.transform, "Enemy Gate", new Vector2(BattleLaneUtility.DefenseX, 4.1f));
            CreatePoint(waypoints.transform, "Center", Vector2.zero);
            CreatePoint(waypoints.transform, "Ally Gate", new Vector2(BattleLaneUtility.DefenseX, -4.4f));
            waypoints.AddComponent<Waypoints>();
        }

        AllySpawner allySpawner = Object.FindAnyObjectByType<AllySpawner>();
        if (allySpawner == null)
        {
            allySpawner = new GameObject("AllySpawner").AddComponent<AllySpawner>();
            allySpawner.transform.position = new Vector3(BattleLaneUtility.AttackX, -4.5f, 0f);
        }
        else allySpawner.transform.position = new Vector3(BattleLaneUtility.AttackX, -4.5f, 0f);

        if (Object.FindAnyObjectByType<HeroController>() == null &&
            Object.FindAnyObjectByType<PlayerMovement>() == null)
        {
            GameObject hero = CreateFallbackHero(sprite);
            hero.name = "KenomArch";
            hero.tag = "Player";
        }

        HeroController activeHero = Object.FindAnyObjectByType<HeroController>();
        if (activeHero != null)
        {
            activeHero.transform.position = new Vector3(BattleLaneUtility.DefenseX, -2.8f, 0f);
        }
        PlayerMovement keyboardMovement = Object.FindAnyObjectByType<PlayerMovement>();
        if (keyboardMovement != null) keyboardMovement.enabled = false;

        CreateAllyBase(root.transform, sprite, visualAssets);
    }

    private static void BuildTinySwordsArena(Transform parent, VisualAssetCatalog visualAssets)
    {
        Sprite water = CreateTextureSprite(visualAssets.tinyWater, new Rect(0f, 0f, 64f, 64f), 64f);
        Sprite grass = CreateTextureSprite(visualAssets.tinyGround, new Rect(48f, 164f, 160f, 56f), 64f);

        CreateTiledWorldPart(parent, "Tiny Swords Water", water, Vector2.zero, new Vector2(22f, 11f), -19);
        CreateTiledWorldPart(parent, "Attack Island", grass, new Vector2(BattleLaneUtility.AttackX, 0f), new Vector2(2.65f, 11f), -18);
        CreateTiledWorldPart(parent, "Defense Island", grass, new Vector2(BattleLaneUtility.DefenseX, 0f), new Vector2(2.65f, 11f), -18);
        CreateWorldPart(parent, "Left Build Bank", CreateWhiteSprite(), new Vector2(1.35f, 0f), new Vector2(1.55f, 7.8f), new Color(0.12f, 0.22f, 0.2f, 0.85f), -18);
        CreateWorldPart(parent, "Right Build Bank", CreateWhiteSprite(), new Vector2(5.7f, 0f), new Vector2(2.15f, 7.8f), new Color(0.12f, 0.22f, 0.2f, 0.85f), -18);
        for (float y = -3f; y <= 3f; y += 2f)
        {
            CreateBuildPad(parent, new Vector2(1f, y));
            CreateBuildPad(parent, new Vector2(5f, y));
            CreateBuildPad(parent, new Vector2(7f, y));
        }

        if (visualAssets.tinyTree == null) return;
        Sprite tree = CreateTextureSprite(visualAssets.tinyTree, new Rect(0f, 384f, 128f, 192f), 64f);
        float[] yPositions = { -3.2f, 0f, 3.2f };
        foreach (float y in yPositions)
        {
            CreateSpritePart(parent, "Attack Border Tree", tree, new Vector2(-5.1f, y), Vector2.one * 0.58f, Color.white, -12);
            CreateSpritePart(parent, "Defense Border Tree", tree, new Vector2(7.8f, y), Vector2.one * 0.58f, Color.white, -12);
        }
    }

    private static GameObject CreateFallbackHero(Sprite sprite)
    {
        VisualAssetCatalog visualAssets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");
        GameObject[] allyPrefabs = Resources.LoadAll<GameObject>("Allies");
        GameObject hero = visualAssets != null && visualAssets.heroPrefab != null
            ? Object.Instantiate(visualAssets.heroPrefab, new Vector3(BattleLaneUtility.DefenseX, -2.8f, 0f), Quaternion.identity)
            : allyPrefabs.Length > 0
            ? Object.Instantiate(allyPrefabs[0], new Vector3(BattleLaneUtility.DefenseX, -2.8f, 0f), Quaternion.identity)
            : CreateWorldPart(null, "KenomArch", sprite, new Vector2(BattleLaneUtility.DefenseX, -2.8f), new Vector2(0.65f, 0.9f), Gold, 10);

        AllyController ally = hero.GetComponent<AllyController>();
        if (ally != null)
        {
            ally.enabled = false;
            Object.Destroy(ally);
        }

        if (hero.GetComponent<Collider2D>() == null)
        {
            hero.AddComponent<BoxCollider2D>().isTrigger = true;
        }

        if (hero.GetComponent<HeroController>() == null) hero.AddComponent<HeroController>();
        if (hero.GetComponent<PlayerHealth>() == null) hero.AddComponent<PlayerHealth>();
        if (hero.GetComponent<HeroBasicAttack>() == null) hero.AddComponent<HeroBasicAttack>();

        foreach (SpriteRenderer renderer in hero.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 12);
        }
        hero.transform.localScale = Vector3.one * 0.78f;

        return hero;
    }

    private static void CreateBuildPad(Transform parent, Vector2 position)
    {
        Sprite sprite = CreateWhiteSprite();
        CreateWorldPart(parent, "Build Pad Shadow", sprite, position + new Vector2(0f, -0.06f), new Vector2(0.86f, 0.86f), new Color(0f, 0f, 0f, 0.24f), -16);
        GameObject pad = CreateWorldPart(parent, "Build Pad", sprite, position, new Vector2(0.72f, 0.72f), new Color(0.42f, 0.58f, 0.45f, 0.75f), -15);
        pad.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void BuildNotification(Transform parent)
    {
        RectTransform holder = CreatePanel("Gameplay Notification", parent, new Color(0.22f, 0.08f, 0.1f, 0.96f));
        SetRect(holder, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(440f, 58f));
        AddOutline(holder.gameObject, new Color(0.95f, 0.45f, 0.35f, 0.75f), new Vector2(2f, -2f));

        TextMeshProUGUI text = AddText(holder, "Message", string.Empty, 20f, Pale, 0f, 44f);
        Stretch(text.rectTransform);
        holder.gameObject.AddComponent<GameplayNotificationController>().Configure(text);
    }

    private static void BuildControlHint(Transform parent)
    {
        RectTransform hint = CreatePanel("Control Hint", parent, new Color(Panel.r, Panel.g, Panel.b, 0.78f));
        SetRect(hint, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 40f), new Vector2(390f, 48f));
        TextMeshProUGUI text = AddText(hint, "Text", "ЛІВА: АТАКА   •   ПРАВА: ЗАХИСТ   •   ПКМ: РУХ   •   ЛКМ: АТАКА", 15f, new Color(0.72f, 0.7f, 0.8f), 0f, 32f);
        Stretch(text.rectTransform);
    }

    private static TowerData[] CollectTowerCatalog()
    {
        List<TowerData> catalog = new List<TowerData>();
        foreach (TowerShopButton button in Resources.FindObjectsOfTypeAll<TowerShopButton>())
        {
            if (button == null || !button.gameObject.scene.IsValid()) continue;
            if (button.towerData != null && !catalog.Contains(button.towerData))
            {
                catalog.Add(button.towerData);
            }
        }

        return catalog.ToArray();
    }

    private static void BuildTowerDock(Transform parent, TowerData[] catalog)
    {
        RectTransform dock = CreatePanel("Tower Dock", parent, Panel);
        SetRect(dock, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-135f, 66f), new Vector2(760f, 112f));
        AddOutline(dock.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.5f), new Vector2(0f, 2f));

        AddText(dock, "Tower Dock Title", "ОБОРОНА", 13f, Gold, -13f, 22f);

        int count = Mathf.Min(6, catalog != null ? catalog.Length : 0);
        for (int i = 0; i < count; i++)
        {
            TowerData tower = catalog[i];
            RectTransform slot = CreatePanel("Tower " + tower.towerName, dock, PanelSoft);
            SetRect(slot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(76f + i * 122f, 45f), new Vector2(108f, 68f));
            AddOutline(slot.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.22f), new Vector2(1f, -1f));

            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot.GetComponent<Image>();
            button.onClick.AddListener(() => TowerPlacementManager.Instance?.SelectTowerToBuild(tower));

            Sprite icon = GetTowerSprite(tower);
            if (icon != null)
            {
                GameObject iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(slot, false);
                Image image = iconObject.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                SetRect(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(27f, 0f), new Vector2(44f, 44f));
            }

            TextMeshProUGUI label = AddText(slot, "Label", $"{ShortTowerName(tower.towerName)}\n{tower.cost}", 14f, Pale, 0f, 48f);
            SetRect(label.rectTransform, new Vector2(0.42f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-4f, -6f));
        }

        if (count == 0)
        {
            AddText(dock, "Empty Tower Dock", "Башти стануть доступними після налаштування каталогу", 16f, new Color(0.58f, 0.56f, 0.65f), -55f, 32f);
        }
    }

    private static string ShortTowerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Башта";
        return value.Replace("вежа", string.Empty).Replace("Вежа", string.Empty).Trim();
    }

    private static Sprite GetTowerSprite(TowerData tower)
    {
        if (tower == null || tower.towerPrefab == null) return null;
        SpriteRenderer renderer = tower.towerPrefab.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }

    private static Sprite GetUnitSprite(UnitData unit)
    {
        if (unit == null || unit.unitPrefab == null) return null;
        SpriteRenderer renderer = unit.unitPrefab.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }

    private static Sprite CreateWhiteSprite()
    {
        return Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            Texture2D.whiteTexture.width);
    }

    private static GameObject CreateWorldPart(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        Vector2 scale,
        Color color,
        int sortingOrder)
    {
        GameObject part = new GameObject(name);
        if (parent != null)
        {
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
        }
        else
        {
            part.transform.position = position;
        }
        part.transform.localScale = scale;

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return part;
    }

    private static void CreateAllyBase(Transform parent, Sprite sprite, VisualAssetCatalog visualAssets)
    {
        Vector2 basePosition = new Vector2(BattleLaneUtility.DefenseX, -4.35f);
        CreateWorldPart(parent, "Ally Castle Shadow", sprite, basePosition + new Vector2(0f, -0.35f), new Vector2(3.4f, 0.75f), new Color(0f, 0f, 0f, 0.35f), 2);
        if (visualAssets != null && visualAssets.tinyAllyCastle != null)
        {
            Sprite castle = CreateTextureSprite(visualAssets.tinyAllyCastle, new Rect(0f, 0f, 320f, 256f), 100f);
            CreateSpritePart(parent, "Tiny Ally Castle", castle, basePosition, Vector2.one * 0.82f, Color.white, 5);
            return;
        }
        if (visualAssets != null && visualAssets.allyCastle != null)
        {
            CreateSpritePart(parent, "Ally Castle", visualAssets.allyCastle, basePosition, Vector2.one * 0.72f, Color.white, 5);
            return;
        }

        CreateWorldPart(parent, "Ally Keep", sprite, basePosition, new Vector2(1.2f, 2.7f), new Color(0.16f, 0.27f, 0.48f), 3);
    }

    private static void CreateLaneStone(Transform parent, Sprite sprite, Vector2 position, Color accent, int sortingOrder)
    {
        GameObject stone = CreateWorldPart(parent, "Lane Stone", sprite, position, new Vector2(0.62f, 0.16f), new Color(accent.r, accent.g, accent.b, 0.18f), sortingOrder);
        stone.transform.rotation = Quaternion.Euler(0f, 0f, position.x % 2f > 0f ? 8f : -8f);
    }

    private static GameObject CreateSpritePart(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        Vector2 scale,
        Color color,
        int sortingOrder)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return part;
    }

    private static Sprite CreateTextureSprite(Texture2D texture, Rect rect, float pixelsPerUnit)
    {
        if (texture == null) return null;
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }

    private static GameObject CreateTiledWorldPart(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        Vector2 size,
        int sortingOrder)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;
        renderer.sortingOrder = sortingOrder;
        return part;
    }

    private static void CreatePoint(Transform parent, string name, Vector2 position)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.position = position;
    }

    private static void EnsureEnemyCastle(GameManager manager)
    {
        if (Object.FindAnyObjectByType<EnemyCastle>() != null) return;

        EnemySpawner spawner = manager.enemySpawner != null
            ? manager.enemySpawner
            : Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner == null) return;

        GameObject castle = new GameObject("Enemy Castle");
        castle.transform.position = new Vector3(BattleLaneUtility.AttackX, 4.35f, 0f);

        BoxCollider2D collider = castle.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.4f, 3.6f);
        collider.isTrigger = true;
        castle.AddComponent<EnemyCastle>();
        CreateCastleVisual(castle.transform);
    }

    private static void CreateCastleVisual(Transform parent)
    {
        VisualAssetCatalog visualAssets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");
        if (visualAssets != null && visualAssets.tinyEnemyCastle != null)
        {
            CreateWorldPart(parent, "Enemy Castle Shadow", CreateWhiteSprite(), new Vector2(0f, -0.35f), new Vector2(3.4f, 0.75f), new Color(0f, 0f, 0f, 0.38f), 2);
            Sprite castle = CreateTextureSprite(visualAssets.tinyEnemyCastle, new Rect(0f, 0f, 320f, 256f), 100f);
            CreateSpritePart(parent, "Tiny Enemy Castle", castle, Vector2.zero, Vector2.one * 0.82f, Color.white, 5);
            return;
        }
        if (visualAssets != null && visualAssets.enemyCastle != null)
        {
            CreateWorldPart(parent, "Enemy Castle Shadow", CreateWhiteSprite(), new Vector2(0.7f, -0.95f), new Vector2(3.4f, 0.75f), new Color(0f, 0f, 0f, 0.38f), 2);
            CreateSpritePart(parent, "Enemy Castle Visual", visualAssets.enemyCastle, new Vector2(-0.6f, -1.8f), Vector2.one * 0.82f, Color.white, 5);
            return;
        }

        Sprite sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            Texture2D.whiteTexture.width);

        CreateCastlePart(parent, "Keep", sprite, new Vector2(0f, 0f), new Vector2(1.7f, 2.8f), PanelSoft);
        CreateCastlePart(parent, "Left Tower", sprite, new Vector2(-1.05f, -0.15f), new Vector2(0.65f, 3.4f), Violet);
        CreateCastlePart(parent, "Right Tower", sprite, new Vector2(1.05f, -0.15f), new Vector2(0.65f, 3.4f), Violet);
        CreateCastlePart(parent, "Gate", sprite, new Vector2(0f, -0.85f), new Vector2(0.65f, 1.1f), Ink);
    }

    private static void CreateCastlePart(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 localPosition,
        Vector2 scale,
        Color color)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 5;
    }

    private static void BuildAllyQueue(Transform parent)
    {
        AllyWaveManager waveManager = Object.FindAnyObjectByType<AllyWaveManager>();
        if (waveManager == null)
        {
            waveManager = new GameObject("Ally Wave Manager").AddComponent<AllyWaveManager>();
        }

        RectTransform card = CreatePanel("Ally Wave Card", parent, PanelSoft);
        SetRect(card, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-190f, -455f), new Vector2(330f, 260f));
        AddOutline(card.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.45f), new Vector2(-2f, -2f));

        AddText(card, "Recruit Title", "ФОРМУВАННЯ ХВИЛІ", 18f, Gold, -27f, 28f);
        TextMeshProUGUI queue = AddText(card, "Queue State", "Черга порожня", 17f, Pale, -62f, 30f);
        TextMeshProUGUI lane = AddText(card, "Selected Lane", "ЛІВА ЛІНІЯ • АТАКА", 14f, new Color(0.68f, 0.65f, 0.78f), -91f, 24f);

        List<Button> buttons = new List<Button>();
        int count = Mathf.Min(3, waveManager.AvailableUnits.Count);
        for (int i = 0; i < count; i++)
        {
            UnitData unit = waveManager.AvailableUnits[i];
            RectTransform slot = CreatePanel("Recruit " + unit.unitName, card, Panel);
            SetRect(slot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -125f - i * 49f), new Vector2(-30f, 42f));
            AddOutline(slot.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.2f), new Vector2(1f, -1f));

            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot.GetComponent<Image>();
            button.onClick.AddListener(() => waveManager.QueueUnit(unit));
            buttons.Add(button);

            Sprite icon = GetUnitSprite(unit);
            if (icon != null)
            {
                GameObject iconObject = new GameObject("Portrait");
                iconObject.transform.SetParent(slot, false);
                Image image = iconObject.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                SetRect(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(42f, 42f));
            }

            TextMeshProUGUI name = AddText(slot, "Unit Name", unit.unitName, 15f, Pale, 0f, 24f);
            SetRect(name.rectTransform, new Vector2(0.18f, 0.46f), new Vector2(0.66f, 1f), new Vector2(0f, -2f), new Vector2(-4f, -2f));
            name.alignment = TextAlignmentOptions.MidlineLeft;

            TextMeshProUGUI price = AddText(slot, "Unit Price", $"{unit.essenceCost} ес.", 14f, Gold, 0f, 22f);
            SetRect(price.rectTransform, new Vector2(0.67f, 0.5f), new Vector2(0.98f, 1f), Vector2.zero, new Vector2(-4f, -2f));
            price.alignment = TextAlignmentOptions.MidlineRight;

            TextMeshProUGUI income = AddText(slot, "Unit Income", $"+{unit.goldPerSecondIncrease}/с доходу", 12f, new Color(0.56f, 0.75f, 0.65f), 0f, 18f);
            SetRect(income.rectTransform, new Vector2(0.18f, 0f), new Vector2(0.98f, 0.5f), new Vector2(0f, 1f), new Vector2(-4f, -2f));
            income.alignment = TextAlignmentOptions.MidlineLeft;
        }

        AllyQueueHudController controller = card.gameObject.AddComponent<AllyQueueHudController>();
        controller.Configure(waveManager, queue, lane, null, null, buttons);
    }

    private static TextMeshProUGUI CreateStat(Transform parent, string label, string value, float anchorX)
    {
        RectTransform holder = CreatePanel(label + " Holder", parent, Color.clear);
        SetRect(holder, new Vector2(anchorX, 0f), new Vector2(anchorX + 0.2f, 1f), Vector2.zero, new Vector2(-12f, -10f));
        AddText(holder, label + " Caption", label.ToUpperInvariant(), 14f, new Color(0.55f, 0.52f, 0.65f), -5f, 28f);
        return AddText(holder, label + " Value", value, 25f, Pale, -43f, 40f);
    }

    private static void StyleShop()
    {
        GameObject shop = GameObject.Find("ShopPanel");
        if (shop == null) return;

        Image shopImage = shop.GetComponent<Image>();
        if (shopImage != null) shopImage.color = Panel;
        AddOutline(shop, new Color(Violet.r, Violet.g, Violet.b, 0.45f), new Vector2(2f, 2f));

        foreach (Button button in shop.GetComponentsInChildren<Button>(true))
        {
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = PanelSoft;
            AddOutline(button.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.25f), new Vector2(1f, -1f));
        }
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, string value, float size, Color color, float y, float height)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = size;
        SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-100f, height));
        return text;
    }

    private static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.AddComponent<RectTransform>();
        panelObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color background, Color foreground)
    {
        RectTransform rect = CreatePanel(name, parent, background);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        TextMeshProUGUI text = AddText(rect, "Label", label, 20f, foreground, 0f, 40f);
        Stretch(text.rectTransform);
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        Slider slider = sliderObject.AddComponent<Slider>();

        RectTransform background = CreatePanel("Background", sliderObject.transform, new Color(0.12f, 0.11f, 0.16f));
        Stretch(background);
        RectTransform fillArea = CreatePanel("Fill Area", sliderObject.transform, Color.clear);
        Stretch(fillArea);
        RectTransform fill = CreatePanel("Fill", fillArea, Violet);
        Stretch(fill);
        RectTransform handle = CreatePanel("Handle", sliderObject.transform, Gold);
        SetRect(handle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(20f, 34f));

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem").AddComponent<EventSystem>();
        }
    }

    private static void Disable(string name)
    {
        GameObject target = GameObject.Find(name);
        if (target != null) target.SetActive(false);
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
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
