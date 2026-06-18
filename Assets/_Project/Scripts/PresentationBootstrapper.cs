// Програмно будує головне меню, бойову сцену, полігон і HUD, щоб проєкт працював без ручної верстки сцен.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PresentationBootstrapper
{
    private static readonly Color Ink = KenamUiTheme.Void;
    private static readonly Color Panel = KenamUiTheme.Panel;
    private static readonly Color PanelSoft = KenamUiTheme.PanelRaised;
    private static readonly Color Violet = KenamUiTheme.Purple;
    private static readonly Color Gold = KenamUiTheme.Gold;
    private static readonly Color Pale = KenamUiTheme.Text;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapInitialScene()
    {
        BootstrapScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootstrapScene(scene);
    }

    // Назва активної сцени визначає, який набір рантайм-об'єктів потрібно зібрати.
    private static void BootstrapScene(Scene scene)
    {
        EnsureEventSystem();

        if (scene.name == "MainMenu")
        {
            GameAudioController.ForcePlayMusic(GameMusicTrack.MainMenu);
            BuildMainMenu();
            return;
        }

        if (scene.name == "SampleScene")
        {
            GameAudioController.PlayMusic(GameMusicTrack.Hub);
            HubBootstrapper.Build();
            return;
        }

        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        EnsureGameplayPresentation(manager);
    }

    public static void EnsureGameplayPresentation(GameManager manager)
    {
        if (manager == null) return;
        if (TrainingGroundState.IsActive)
        {
            BuildTrainingGroundPresentation(manager);
            GameplayMechanicsBootstrapper.Ensure(manager);
            return;
        }

        BuildGameplayHud(manager);
        GameplayMechanicsBootstrapper.Ensure(manager);
    }

    // Меню створюється кодом поверх старого Canvas, щоб усі версії сцени мали однаковий вигляд і поведінку.
    private static void BuildMainMenu()
    {
        if (GameObject.Find("KenomArch Presentation Menu") != null) return;

        Disable("Menu Root");
        MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
        Canvas canvas = CreateCanvas("KenomArch Presentation Menu", 100);
        if (controller == null) controller = canvas.gameObject.AddComponent<MainMenuController>();
        DisableOldMainMenuCanvases(canvas);

        RectTransform background = CreatePanel("Void Background", canvas.transform, Ink);
        Stretch(background);

        RectTransform glow = CreatePanel("Violet Glow", background, new Color(Violet.r, Violet.g, Violet.b, 0.18f));
        SetRect(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 40f), new Vector2(850f, 1500f));
        glow.localRotation = Quaternion.Euler(0f, 0f, -18f);

        BuildMainMenuPreview(background);

        RectTransform card = CreatePanel("Menu Card", background, Panel);
        SetRect(card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(390f, 0f), new Vector2(600f, 720f));
        AddOutline(card.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.5f), new Vector2(2f, -2f));

        AddText(card, "Act", "ACT II  *  TOWER WAR", 21f, Gold, -82f, 44f);
        AddText(card, "Title", "ECHOES OF THE VOID", 48f, Pale, -155f, 90f);
        AddText(card, "Subtitle", "LOOP OF NOTHINGNESS", 29f, Violet, -225f, 52f);
        AddText(card, "Description", "Дві лінії. Один герой. Нескінченна гра, правила якої доведеться зламати.", 22f, KenamUiTheme.TextMuted, -315f, 110f);

        Button play = CreateButton("Play Button", card, "НОВА КАМПАНІЯ", Gold, Ink);
        SetRect(play.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 306f), new Vector2(-100f, 66f));
        if (controller != null) play.onClick.AddListener(controller.NewCampaign);

        Button continueCampaign = CreateButton("Continue Button", card, "ПРОДОВЖИТИ", PanelSoft, Pale);
        SetRect(continueCampaign.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 224f), new Vector2(-100f, 60f));
        if (controller != null) continueCampaign.onClick.AddListener(controller.Play);

        Button settings = CreateButton("Settings Button", card, "НАЛАШТУВАННЯ", PanelSoft, Pale);
        SetRect(settings.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 150f), new Vector2(-100f, 58f));
        if (controller != null) settings.onClick.AddListener(controller.OpenSettings);

        Button exit = CreateButton("Exit Button", card, "ВИЙТИ", KenamUiTheme.DangerDark, Pale);
        SetRect(exit.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 82f), new Vector2(-100f, 52f));
        if (controller != null) exit.onClick.AddListener(controller.Exit);

        RectTransform settingsPanel = CreatePanel("Presentation Settings", background, Panel);
        SetRect(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 650f));
        AddOutline(settingsPanel.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.45f), new Vector2(2f, -2f));
        AddText(settingsPanel, "Settings Title", "НАЛАШТУВАННЯ", 30f, Pale, -42f, 48f);
        AddText(settingsPanel, "Master Caption", "Загальна гучність", 18f, KenamUiTheme.TextMuted, -102f, 30f);

        Slider volume = CreateSlider("Master Volume", settingsPanel);
        SetRect(volume.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -137f), new Vector2(460f, 24f));
        volume.value = GameSettings.MasterVolume;
        if (controller != null) volume.onValueChanged.AddListener(controller.SetVolume);

        AddText(settingsPanel, "Music Caption", "Музика", 18f, KenamUiTheme.TextMuted, -174f, 30f);
        Slider music = CreateSlider("Music Volume", settingsPanel);
        SetRect(music.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -209f), new Vector2(460f, 24f));
        music.value = GameSettings.MusicVolume;
        if (controller != null) music.onValueChanged.AddListener(controller.SetMusicVolume);

        AddText(settingsPanel, "Sfx Caption", "Звукові ефекти", 18f, KenamUiTheme.TextMuted, -246f, 30f);
        Slider sfx = CreateSlider("SFX Volume", settingsPanel);
        SetRect(sfx.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -281f), new Vector2(460f, 24f));
        sfx.value = GameSettings.SfxVolume;
        if (controller != null) sfx.onValueChanged.AddListener(controller.SetSfxVolume);

        Button fullscreen = CreateButton("Fullscreen Toggle", settingsPanel, "ПОВНИЙ ЕКРАН: " + (GameSettings.Fullscreen ? "ТАК" : "НІ"), PanelSoft, Pale);
        SetRect(fullscreen.GetComponent<RectTransform>(), new Vector2(0.3f, 0.5f), new Vector2(0.3f, 0.5f), new Vector2(0f, -30f), new Vector2(260f, 48f));
        if (controller != null) fullscreen.onClick.AddListener(controller.ToggleFullscreen);

        Button vsync = CreateButton("VSync Toggle", settingsPanel, "V-SYNC: " + (GameSettings.VSync ? "ТАК" : "НІ"), PanelSoft, Pale);
        SetRect(vsync.GetComponent<RectTransform>(), new Vector2(0.7f, 0.5f), new Vector2(0.7f, 0.5f), new Vector2(0f, -30f), new Vector2(260f, 48f));
        if (controller != null) vsync.onClick.AddListener(controller.ToggleVSync);

        string qualityName = QualitySettings.names.Length > 0 ? QualitySettings.names[GameSettings.Quality] : "DEFAULT";
        Button quality = CreateButton("Quality Cycle", settingsPanel, "ЯКІСТЬ: " + qualityName.ToUpperInvariant(), PanelSoft, Pale);
        SetRect(quality.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -92f), new Vector2(350f, 48f));
        if (controller != null) quality.onClick.AddListener(controller.CycleQuality);

        Button resolution = CreateButton("Resolution Cycle", settingsPanel, $"РОЗДІЛЬНІСТЬ: {Screen.width} x {Screen.height}", PanelSoft, Pale);
        SetRect(resolution.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -154f), new Vector2(390f, 48f));
        if (controller != null) resolution.onClick.AddListener(controller.CycleResolution);

        Button reset = CreateButton("Reset Profile", settingsPanel, "СКИНУТИ ПРОФІЛЬ", KenamUiTheme.DangerDark, Pale);
        SetRect(reset.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -216f), new Vector2(300f, 48f));
        if (controller != null) reset.onClick.AddListener(controller.ResetProfile);

        Button close = CreateButton("Close Settings", settingsPanel, "НАЗАД", Gold, Ink);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(220f, 52f));
        if (controller != null)
        {
            close.onClick.AddListener(controller.CloseSettings);
            controller.Configure("SampleScene", settingsPanel.gameObject);
        }

        settingsPanel.gameObject.SetActive(false);
        GameAudioController.ForcePlayMusic(GameMusicTrack.MainMenu);
    }

    private static void DisableOldMainMenuCanvases(Canvas presentationCanvas)
    {
        foreach (Canvas existing in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (existing == null || existing == presentationCanvas) continue;
            if (existing.gameObject.name == "KenomArch Presentation Menu") continue;
            existing.gameObject.SetActive(false);
        }
    }

    private static void BuildMainMenuPreview(Transform parent)
    {
        VisualAssetCatalog assets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");

        RectTransform preview = CreatePanel("Menu World Preview", parent, KenamUiTheme.WithAlpha(PanelSoft, 0.82f));
        SetRect(preview, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-520f, 0f), new Vector2(780f, 720f));
        AddOutline(preview.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.42f), new Vector2(-2f, -2f));

        RectTransform voidPanel = CreatePanel("Preview Void", preview, KenamUiTheme.WithAlpha(Ink, 0.74f));
        SetRect(voidPanel, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

        RectTransform ground = CreatePanel("Preview Ground", preview, KenamUiTheme.WithAlpha(KenamUiTheme.Swamp, 0.42f));
        SetRect(ground, new Vector2(0.1f, 0.17f), new Vector2(0.9f, 0.82f), Vector2.zero, Vector2.zero);

        RectTransform verticalRoad = CreatePanel("Preview Vertical Road", preview, KenamUiTheme.WithAlpha(Gold, 0.23f));
        SetRect(verticalRoad, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(76f, 420f));
        RectTransform horizontalRoad = CreatePanel("Preview Horizontal Road", preview, KenamUiTheme.WithAlpha(Gold, 0.23f));
        SetRect(horizontalRoad, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 76f));
        RectTransform crossing = CreatePanel("Preview Crossing", preview, KenamUiTheme.WithAlpha(Gold, 0.32f));
        SetRect(crossing, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));

        AddText(preview, "Preview Title", "ПРИХИСТОК МІЖ ЦИКЛАМИ", 24f, Gold, -42f, 42f);
        AddText(preview, "Preview Caption", "бібліотека • демон • карта циклу • полігон", 17f, KenamUiTheme.TextMuted, -82f, 30f);

        Sprite castle = CreateUiSprite(assets != null ? assets.tinyAllyCastle : null, 100f, RuntimeSpriteAssetMap.Center)
            ?? LoadUiSprite("ui_icons/kingdom");
        Sprite library = CreateUiSprite(assets != null ? assets.tinyLibrary : null, 100f, RuntimeSpriteAssetMap.Center)
            ?? LoadUiSprite("ui_icons/paper_square");
        Sprite demon = CreateUiSprite(assets != null ? assets.tinyDemonTower : null, 100f, RuntimeSpriteAssetMap.Center)
            ?? LoadUiSprite("ui_icons/dragon");
        Sprite training = CreateUiSprite(assets != null ? assets.tinyEnemyCastle : null, 100f, RuntimeSpriteAssetMap.Center)
            ?? LoadUiSprite("ui_icons/crossed_swords");

        AddMenuSprite(preview, "Preview Level Castle", castle, new Vector2(0f, 150f), new Vector2(185f, 145f), Color.white);
        AddMenuSprite(preview, "Preview Library", library, new Vector2(-250f, -5f), new Vector2(150f, 135f), Color.white);
        AddMenuSprite(preview, "Preview Demon", demon, new Vector2(250f, -5f), new Vector2(150f, 170f), Color.white);
        AddMenuSprite(preview, "Preview Training", training, new Vector2(0f, -198f), new Vector2(170f, 135f), Color.white);
        AddMenuSprite(preview, "Preview Hero Mark", LoadUiSprite("ui_icons/knight_helmet_round"), new Vector2(0f, -68f), new Vector2(76f, 76f), Color.white);

        AddMenuSprite(preview, "Preview Map Icon", LoadUiSprite("ui_icons/map_icon"), new Vector2(-112f, -292f), new Vector2(56f, 56f), Color.white);
        AddMenuSprite(preview, "Preview Shop Icon", LoadUiSprite("ui_icons/gold_shop_bag"), new Vector2(0f, -292f), new Vector2(56f, 56f), Color.white);
        AddMenuSprite(preview, "Preview Fight Icon", LoadUiSprite("ui_icons/crossed_swords"), new Vector2(112f, -292f), new Vector2(56f, 56f), Color.white);
    }

    private static void BuildGameplayHud(GameManager manager)
    {
        if (TrainingGroundState.IsActive)
        {
            BuildTrainingGroundPresentation(manager);
            return;
        }

        if (GameObject.Find("KenomArch Gameplay HUD") != null) return;

        TowerData[] towerCatalog = CollectTowerCatalog();
        DisableLegacyGameplayUi();
        EnsureGameplayWorld(manager);
        EnsureEnemyCastle(manager);
        EnemyAutoTower.EnsureScriptedTowers();

        Canvas canvas = CreateCanvas("KenomArch Gameplay HUD", 90);
        PresentationHudController controller = canvas.gameObject.AddComponent<PresentationHudController>();

        RectTransform topBar = CreatePanel("Strategic Top Bar", canvas.transform, Panel);
        SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-28f, 72f));
        AddOutline(topBar.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.55f), new Vector2(0f, -2f));

        TextMeshProUGUI health = CreateStat(topBar, "Замок", "100 / 100", 0.02f);
        TextMeshProUGUI wave = CreateStat(topBar, "Хвиля", "0 / 0", 0.28f);
        TextMeshProUGUI gold = CreateStat(topBar, "Золото", "0", 0.56f);
        TextMeshProUGUI essence = CreateStat(topBar, "Есенція", "0", 0.78f);
        health.color = KenamUiTheme.Mint;
        wave.color = KenamUiTheme.Purple;
        gold.color = KenamUiTheme.Gold;
        essence.color = KenamUiTheme.GreyMana;

        RectTransform command = CreatePanel("Wave Command Card", canvas.transform, PanelSoft);
        SetRect(command, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -150f), new Vector2(300f, 166f));
        AddOutline(command.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.35f), new Vector2(-2f, -2f));

        AddText(command, "Next Wave", $"РІВЕНЬ {CampaignProgress.SelectedLevel} • ХВИЛІ ∞", 16f, Gold, -24f, 26f);
        TextMeshProUGUI state = AddText(command, "State", "Підготовка", 24f, Pale, -63f, 40f);
        TextMeshProUGUI preview = AddText(command, "Enemy Preview", "Склад хвилі", 13f, KenamUiTheme.TextMuted, -101f, 34f);

        Button ready = CreateButton("Ready Button", command, "ГОТОВО", Gold, Ink);
        SetRect(ready.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 24f), new Vector2(-34f, 44f));
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
        BuildHeroAbilityBar(canvas.transform);
        BuildNotification(canvas.transform);
        BuildControlHint(canvas.transform);
    }

    private static void BuildTrainingGroundPresentation(GameManager manager)
    {
        DisableLegacyGameplayUi();
        DisableTrainingCombatObjects(manager);
        EnsureTrainingGroundWorld();

        if (GameObject.Find("KenomArch Training HUD") != null) return;

        Canvas canvas = CreateCanvas("KenomArch Training HUD", 90);
        RectTransform hint = CreatePanel("Training Hint", canvas.transform, KenamUiTheme.WithAlpha(Panel, 0.86f));
        SetRect(hint, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(720f, 58f));
        AddOutline(hint.gameObject, KenamUiTheme.WithAlpha(Gold, 0.45f), new Vector2(0f, 2f));
        TextMeshProUGUI text = AddText(hint, "Text", "ПОЛІГОН: ЛКМ - бити мішень   •   Q/W/E - навички   •   ESC - повернутися у хаб", 18f, KenamUiTheme.TextMuted, 0f, 42f);
        Stretch(text.rectTransform);

        Button back = CreateButton("Training Back Button", canvas.transform, "BACK TO HUB", Gold, Ink);
        SetRect(back.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 50f), new Vector2(260f, 50f));
        back.onClick.AddListener(TrainingGroundController.ReturnToHub);

        BuildNotification(canvas.transform);
    }

    private static void DisableTrainingCombatObjects(GameManager manager)
    {
        if (manager != null)
        {
            manager.enemySpawner = null;
            manager.autoStartWaves = false;
        }

        foreach (EnemySpawner spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
        {
            spawner.StopAllSpawning();
            spawner.gameObject.SetActive(false);
        }

        foreach (AllySpawner spawner in Object.FindObjectsByType<AllySpawner>(FindObjectsSortMode.None))
        {
            spawner.gameObject.SetActive(false);
        }

        foreach (EnemyCastle castle in Object.FindObjectsByType<EnemyCastle>(FindObjectsSortMode.None))
        {
            Object.Destroy(castle.gameObject);
        }

        foreach (Barrier barrier in Object.FindObjectsByType<Barrier>(FindObjectsSortMode.None))
        {
            Object.Destroy(barrier.gameObject);
        }

        GameObject enemyTowers = GameObject.Find("Enemy Scripted Towers");
        if (enemyTowers != null) Object.Destroy(enemyTowers);
    }

    private static void EnsureTrainingGroundWorld()
    {
        if (GameObject.Find("KenomArch Training Ground") != null) return;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = Ink;
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        GameObject root = new GameObject("KenomArch Training Ground");
        Sprite sprite = CreateWhiteSprite();
        VisualAssetCatalog visualAssets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");

        if (!RuntimeBattlefieldVisuals.TryBuildTrainingGround(root.transform))
        {
            CreateWorldPart(root.transform, "Training Void", sprite, Vector2.zero, new Vector2(18f, 11f), Ink, -30);
            CreateWorldPart(root.transform, "Training Field", sprite, Vector2.zero, new Vector2(14f, 8.2f), KenamUiTheme.Swamp, -20);
            CreateWorldPart(root.transform, "Training Arena", sprite, Vector2.zero, new Vector2(6f, 3.2f), KenamUiTheme.StoneRaised, -18);
        }

        HeroController activeHero = Object.FindAnyObjectByType<HeroController>();
        PlayerMovement keyboardMovement = Object.FindAnyObjectByType<PlayerMovement>();
        if (activeHero == null && keyboardMovement == null)
        {
            GameObject hero = CreateFallbackHero(sprite);
            hero.name = "KenomArch";
            hero.tag = "Player";
            activeHero = hero.GetComponent<HeroController>();
        }
        else if (activeHero == null && keyboardMovement != null)
        {
            activeHero = keyboardMovement.gameObject.AddComponent<HeroController>();
        }

        if (activeHero != null)
        {
            GameObject hero = activeHero.gameObject;
            hero.name = "KenomArch";
            hero.tag = "Player";
            activeHero.transform.position = new Vector3(0f, -2.45f, 0f);
            hero.transform.localScale = Vector3.one * 3.06f;
            if (hero.GetComponent<Collider2D>() == null) hero.AddComponent<BoxCollider2D>().isTrigger = true;
            if (hero.GetComponent<PlayerHealth>() == null) hero.AddComponent<PlayerHealth>();
            if (hero.GetComponent<HeroBasicAttack>() == null) hero.AddComponent<HeroBasicAttack>();
            if (hero.GetComponent<HeroAbilityController>() == null) hero.AddComponent<HeroAbilityController>();
            HeroVisualAnimator animator = hero.GetComponent<HeroVisualAnimator>() ?? hero.AddComponent<HeroVisualAnimator>();
            animator.Configure(visualAssets != null ? visualAssets.tinyHeroSheet : null);
            foreach (SpriteRenderer renderer in hero.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 14);
            }
        }

        if (keyboardMovement != null) keyboardMovement.enabled = false;
    }

    private static void DisableLegacyGameplayUi()
    {
        foreach (Canvas existing in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (existing.gameObject.name == "KenomArch Gameplay HUD") continue;
            existing.gameObject.SetActive(false);
        }
    }

    // Відновлюємо відсутні об'єкти бою, маршрути, бази та героя, не вимагаючи ручної підготовки сцени.
    private static void EnsureGameplayWorld(GameManager manager)
    {
        if (GameObject.Find("KenomArch Battlefield") != null) return;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = Ink;
            camera.orthographic = true;
            camera.orthographicSize = 7.2f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        GameObject root = new GameObject("KenomArch Battlefield");
        Sprite sprite = CreateWhiteSprite();
        VisualAssetCatalog visualAssets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");

        bool useTinySwordsArena = visualAssets != null && visualAssets.tinyWater != null && visualAssets.tinyGround != null;
        if (RuntimeBattlefieldVisuals.TryBuildFarmBattlefield(root.transform))
        {
            // Farm/Tiny pass owns the ground, roads and build pads.
        }
        else if (useTinySwordsArena)
        {
            CreateWorldPart(root.transform, "Arena", sprite, Vector2.zero, new Vector2(22f, 11f), KenamUiTheme.VoidSoft, -20);
            BuildTinySwordsArena(root.transform, visualAssets);
        }
        else
        {
            CreateWorldPart(root.transform, "Arena", sprite, Vector2.zero, new Vector2(22f, 11f), KenamUiTheme.VoidSoft, -20);
            CreateWorldPart(root.transform, "Attack Lane", sprite, new Vector2(BattleLaneUtility.AttackX, 0f), new Vector2(2.7f, 11f), KenamUiTheme.Charcoal, -18);
            CreateWorldPart(root.transform, "Defense Lane", sprite, new Vector2(BattleLaneUtility.DefenseX, 0f), new Vector2(2.7f, 11f), KenamUiTheme.Swamp, -18);
        }

        EnemySpawner enemySpawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            enemySpawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
            enemySpawner.transform.position = new Vector3(BattleLaneUtility.DefenseX, 4.35f, 0f);
        }
        else enemySpawner.transform.position = new Vector3(BattleLaneUtility.DefenseX, 4.35f, 0f);
        manager.enemySpawner = enemySpawner;
        enemySpawner.ConfigureCampaignWaves();

        if (Object.FindAnyObjectByType<Waypoints>() == null)
        {
            GameObject waypoints = new GameObject("Waypoints");
            CreatePoint(waypoints.transform, "Enemy Gate", new Vector2(BattleLaneUtility.DefenseX, 4.1f));
            CreatePoint(waypoints.transform, "Center", Vector2.zero);
            CreatePoint(waypoints.transform, "Ally Gate", new Vector2(0f, -4.1f));
            waypoints.AddComponent<Waypoints>();
        }

        if (Object.FindAnyObjectByType<Barrier>() == null)
        {
            GameObject barrier = CreateWorldPart(
                root.transform,
                "Spatial Front Barrier",
                sprite,
                new Vector2(0f, 0.7f),
                new Vector2(0.22f, 6.2f),
                KenamUiTheme.WithAlpha(Violet, 0.28f),
                -10);
            BoxCollider2D barrierCollider = barrier.AddComponent<BoxCollider2D>();
            barrierCollider.isTrigger = true;
            barrier.AddComponent<Rigidbody2D>();
            barrier.AddComponent<Barrier>();
        }

        AllySpawner allySpawner = Object.FindAnyObjectByType<AllySpawner>();
        if (allySpawner == null)
        {
            allySpawner = new GameObject("AllySpawner").AddComponent<AllySpawner>();
            allySpawner.transform.position = new Vector3(BattleLaneUtility.AttackX, -3.7f, 0f);
        }
        else allySpawner.transform.position = new Vector3(BattleLaneUtility.AttackX, -3.7f, 0f);

        HeroController activeHero = Object.FindAnyObjectByType<HeroController>();
        PlayerMovement keyboardMovement = Object.FindAnyObjectByType<PlayerMovement>();
        if (activeHero == null && keyboardMovement == null)
        {
            GameObject hero = CreateFallbackHero(sprite);
            hero.name = "KenomArch";
            hero.tag = "Player";
            activeHero = hero.GetComponent<HeroController>();
        }
        else if (activeHero == null && keyboardMovement != null)
        {
            activeHero = keyboardMovement.gameObject.AddComponent<HeroController>();
        }

        if (activeHero != null)
        {
            GameObject hero = activeHero.gameObject;
            hero.name = "KenomArch";
            hero.tag = "Player";
            activeHero.transform.position = new Vector3(0f, -2.7f, 0f);
            if (hero.GetComponent<Collider2D>() == null) hero.AddComponent<BoxCollider2D>().isTrigger = true;
            if (hero.GetComponent<PlayerHealth>() == null) hero.AddComponent<PlayerHealth>();
            if (hero.GetComponent<HeroBasicAttack>() == null) hero.AddComponent<HeroBasicAttack>();
            HeroVisualAnimator animator = hero.GetComponent<HeroVisualAnimator>() ?? hero.AddComponent<HeroVisualAnimator>();
            animator.Configure(visualAssets != null ? visualAssets.tinyHeroSheet : null);
            if (hero.GetComponent<HeroAbilityController>() == null) hero.AddComponent<HeroAbilityController>();
            hero.transform.localScale = Vector3.one * 3.06f;
        }
        if (keyboardMovement != null) keyboardMovement.enabled = false;

        CreateAllyBase(root.transform, sprite, visualAssets);
    }

    private static void BuildTinySwordsArena(Transform parent, VisualAssetCatalog visualAssets)
    {
        Sprite white = CreateWhiteSprite();
        Color land = KenamUiTheme.Swamp;
        Color raisedLand = KenamUiTheme.Moss;
        Color roadEdge = KenamUiTheme.Charcoal;
        Color road = KenamUiTheme.Stone;
        Color attackAccent = KenamUiTheme.Danger;
        Color defenseAccent = KenamUiTheme.Mint;

        CreateWorldPart(parent, "Main Land", white, Vector2.zero, new Vector2(18.5f, 10.4f), land, -19);
        CreateWorldPart(parent, "Left Grove", white, new Vector2(-7.15f, 0.2f), new Vector2(3.2f, 8.7f), raisedLand, -18);
        CreateWorldPart(parent, "Right Grove", white, new Vector2(7.15f, 0.2f), new Vector2(3.2f, 8.7f), raisedLand, -18);

        CreateWorldPart(parent, "Ally Plateau Shadow", white, new Vector2(0f, -3.86f), new Vector2(6.2f, 2.2f), roadEdge, -17);
        CreateWorldPart(parent, "Ally Plateau", white, new Vector2(0f, -3.72f), new Vector2(5.75f, 1.82f), KenamUiTheme.StoneRaised, -16);
        CreateWorldPart(parent, "Enemy Plateau Shadow", white, new Vector2(0f, 4.32f), new Vector2(7.5f, 1.35f), roadEdge, -17);
        CreateWorldPart(parent, "Enemy Plateau", white, new Vector2(0f, 4.42f), new Vector2(7.1f, 1.08f), KenamUiTheme.StoneRaised, -16);

        CreateWorldPart(parent, "Fork Stem Edge", white, new Vector2(0f, -1.55f), new Vector2(1.82f, 3.15f), roadEdge, -16);
        CreateWorldPart(parent, "Fork Stem", white, new Vector2(0f, -1.55f), new Vector2(1.42f, 3f), road, -15);
        CreateAngledPath(parent, white, "Attack Fork Edge", new Vector2(-1.55f, -0.25f), new Vector2(4.1f, 1.7f), 32f, roadEdge, -16);
        CreateAngledPath(parent, white, "Defense Fork Edge", new Vector2(1.55f, -0.25f), new Vector2(4.1f, 1.7f), -32f, roadEdge, -16);
        CreateAngledPath(parent, white, "Attack Fork", new Vector2(-1.55f, -0.25f), new Vector2(3.78f, 1.34f), 32f, road, -15);
        CreateAngledPath(parent, white, "Defense Fork", new Vector2(1.55f, -0.25f), new Vector2(3.78f, 1.34f), -32f, road, -15);

        CreateWorldPart(parent, "Attack Front Edge", white, new Vector2(BattleLaneUtility.AttackX, 2.2f), new Vector2(1.95f, 5.45f), roadEdge, -16);
        CreateWorldPart(parent, "Defense Front Edge", white, new Vector2(BattleLaneUtility.DefenseX, 2.2f), new Vector2(1.95f, 5.45f), roadEdge, -16);
        CreateWorldPart(parent, "Attack Front", white, new Vector2(BattleLaneUtility.AttackX, 2.2f), new Vector2(1.52f, 5.35f), road, -15);
        CreateWorldPart(parent, "Defense Front", white, new Vector2(BattleLaneUtility.DefenseX, 2.2f), new Vector2(1.52f, 5.35f), road, -15);
        CreateWorldPart(parent, "Attack Front Accent", white, new Vector2(BattleLaneUtility.AttackX - 0.67f, 2.2f), new Vector2(0.12f, 5.25f), new Color(attackAccent.r, attackAccent.g, attackAccent.b, 0.72f), -14);
        CreateWorldPart(parent, "Defense Front Accent", white, new Vector2(BattleLaneUtility.DefenseX + 0.67f, 2.2f), new Vector2(0.12f, 5.25f), new Color(defenseAccent.r, defenseAccent.g, defenseAccent.b, 0.72f), -14);

        CreateWorldPart(parent, "Defense Build Field Shadow", white, new Vector2(6f, 0.1f), new Vector2(3.55f, 7.65f), KenamUiTheme.WithAlpha(KenamUiTheme.Void, 0.85f), -17);
        CreateWorldPart(parent, "Defense Build Field", white, new Vector2(6f, 0.1f), new Vector2(3.2f, 7.3f), KenamUiTheme.Swamp, -16);
        CreateWorldPart(parent, "Defense Build Accent", white, new Vector2(4.38f, 0.1f), new Vector2(0.08f, 7.2f), new Color(defenseAccent.r, defenseAccent.g, defenseAccent.b, 0.45f), -14);

        for (float y = -3f; y <= 3f; y += 2f)
        {
            CreateBuildPad(parent, new Vector2(5f, y));
            CreateBuildPad(parent, new Vector2(7f, y));
        }

        for (float y = 0.35f; y <= 3.8f; y += 0.85f)
        {
            CreateLaneStone(parent, white, new Vector2(BattleLaneUtility.AttackX, y), attackAccent, -13);
            CreateLaneStone(parent, white, new Vector2(BattleLaneUtility.DefenseX, y), defenseAccent, -13);
        }
        CreateLaneStone(parent, white, new Vector2(-1.25f, -0.28f), attackAccent, -13);
        CreateLaneStone(parent, white, new Vector2(1.25f, -0.28f), defenseAccent, -13);

        if (visualAssets.tinyTree == null) return;
        Sprite tree = CreateTextureSprite(visualAssets.tinyTree, new Rect(0f, 384f, 128f, 192f), 64f);
        Vector2[] treePositions =
        {
            new Vector2(-7.7f, 3.15f), new Vector2(-7.55f, -2.7f), new Vector2(-5.85f, 1.8f),
            new Vector2(7.7f, 3.15f), new Vector2(7.55f, -2.7f)
        };
        foreach (Vector2 position in treePositions)
            CreateSpritePart(parent, "Border Tree", tree, position, Vector2.one * 0.5f, Color.white, -12);
    }

    private static void CreateAngledPath(
        Transform parent,
        Sprite sprite,
        string name,
        Vector2 position,
        Vector2 scale,
        float angle,
        Color color,
        int sortingOrder)
    {
        GameObject path = CreateWorldPart(parent, name, sprite, position, scale, color, sortingOrder);
        path.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static GameObject CreateFallbackHero(Sprite sprite)
    {
        VisualAssetCatalog visualAssets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");
        GameObject[] allyPrefabs = Resources.LoadAll<GameObject>("Allies");
        GameObject hero = visualAssets != null && visualAssets.heroPrefab != null
            ? Object.Instantiate(visualAssets.heroPrefab, new Vector3(0f, -2.7f, 0f), Quaternion.identity)
            : allyPrefabs.Length > 0
            ? Object.Instantiate(allyPrefabs[0], new Vector3(0f, -2.7f, 0f), Quaternion.identity)
            : CreateWorldPart(null, "KenomArch", sprite, new Vector2(0f, -2.7f), new Vector2(0.65f, 0.9f), Gold, 10);

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
        if (hero.GetComponent<HeroAbilityController>() == null) hero.AddComponent<HeroAbilityController>();
        HeroVisualAnimator visualAnimator = hero.GetComponent<HeroVisualAnimator>() ?? hero.AddComponent<HeroVisualAnimator>();
        visualAnimator.Configure(visualAssets != null ? visualAssets.tinyHeroSheet : null);

        foreach (SpriteRenderer renderer in hero.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 12);
        }
        hero.transform.localScale = Vector3.one * 3.06f;

        return hero;
    }

    private static void BuildHeroAbilityBar(Transform parent)
    {
        HeroAbilityController abilities = Object.FindAnyObjectByType<HeroAbilityController>();
        if (abilities == null) return;

        RectTransform bar = CreatePanel("Hero Ability Bar", parent, Panel);
        SetRect(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(390f, 92f));
        AddOutline(bar.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.35f), new Vector2(0f, 2f));

        TextMeshProUGUI pulse = CreateAbilitySlot(bar, "Pulse", "Q\nІмпульс", -125f);
        TextMeshProUGUI blink = CreateAbilitySlot(bar, "Blink", "W\nСтрибок", 0f);
        TextMeshProUGUI recovery = CreateAbilitySlot(bar, "Recovery", "E\nВідновлення", 125f);
        abilities.ConfigureUi(pulse, blink, recovery);
    }

    private static TextMeshProUGUI CreateAbilitySlot(Transform parent, string name, string value, float x)
    {
        RectTransform slot = CreatePanel(name, parent, PanelSoft);
        SetRect(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(112f, 68f));
        AddOutline(slot.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.4f), new Vector2(1f, -1f));
        TextMeshProUGUI text = AddText(slot, "Label", value, 15f, Pale, 0f, 60f);
        Stretch(text.rectTransform);
        return text;
    }

    private static void CreateBuildPad(Transform parent, Vector2 position)
    {
        Sprite sprite = CreateWhiteSprite();
        CreateWorldPart(parent, "Build Pad Shadow", sprite, position + new Vector2(0f, -0.06f), new Vector2(0.86f, 0.86f), new Color(0f, 0f, 0f, 0.24f), -16);
        GameObject pad = CreateWorldPart(parent, "Build Pad", sprite, position, new Vector2(0.72f, 0.72f), KenamUiTheme.WithAlpha(KenamUiTheme.Mint, 0.55f), -15);
        pad.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void BuildNotification(Transform parent)
    {
        RectTransform holder = CreatePanel("Gameplay Notification", parent, KenamUiTheme.WithAlpha(KenamUiTheme.DangerDark, 0.96f));
        SetRect(holder, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(440f, 58f));
        AddOutline(holder.gameObject, KenamUiTheme.WithAlpha(KenamUiTheme.Danger, 0.75f), new Vector2(2f, -2f));

        TextMeshProUGUI text = AddText(holder, "Message", string.Empty, 20f, Pale, 0f, 44f);
        Stretch(text.rectTransform);
        holder.gameObject.AddComponent<GameplayNotificationController>().Configure(text);
    }

    private static void BuildControlHint(Transform parent)
    {
        RectTransform hint = CreatePanel("Control Hint", parent, KenamUiTheme.WithAlpha(Panel, 0.78f));
        SetRect(hint, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 40f), new Vector2(390f, 48f));
        TextMeshProUGUI text = AddText(hint, "Text", "ЛІВА: АТАКА   •   ПРАВА: ЗАХИСТ   •   ПКМ: РУХ   •   ЛКМ: АТАКА", 15f, KenamUiTheme.TextMuted, 0f, 32f);
        Stretch(text.rectTransform);
    }

    // Каталог збирається з Resources і сортується один раз перед побудовою панелі башт.
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

        if (catalog.Count == 0)
        {
            foreach (GameObject prefab in Resources.LoadAll<GameObject>("Towers"))
            {
                TowerController controller = prefab.GetComponent<TowerController>();
                if (controller != null && controller.data != null && !catalog.Contains(controller.data))
                    catalog.Add(controller.data);
            }
        }

        ConfigureTowerArchetypes(catalog);
        return catalog.ToArray();
    }

    private static void ConfigureTowerArchetypes(List<TowerData> catalog)
    {
        if (catalog.Count > 0)
        {
            catalog[0].archetype = TowerArchetype.SentinelPylon;
            catalog[0].damageFamily = DamageFamily.Physical;
            catalog[0].damageModifier = DamageModifier.Piercing;
        }
        if (catalog.Count > 1)
        {
            catalog[1].archetype = TowerArchetype.LightObelisk;
            catalog[1].damageFamily = DamageFamily.Magical;
            catalog[1].damageModifier = DamageModifier.Light;
            catalog[1].isMagic = true;
        }
        if (catalog.Count > 2)
        {
            catalog[2].archetype = TowerArchetype.DistortionPrism;
            catalog[2].damageFamily = catalog[2].towerLevel >= 3 ? DamageFamily.Chaos : DamageFamily.Hybrid;
            catalog[2].damageModifier = catalog[2].towerLevel >= 3 ? DamageModifier.Chaos : DamageModifier.Mixed;
            catalog[2].slowFactor = catalog[2].towerLevel >= 2 ? 0.85f : catalog[2].slowFactor;
        }

        foreach (TowerData tower in catalog)
        {
            if (tower == null) continue;
            tower.attackRadius = Mathf.Clamp(tower.attackRadius, 1.25f, 2.45f);
        }
    }

    private static void BuildTowerDock(Transform parent, TowerData[] catalog)
    {
        RectTransform dock = CreatePanel("Tower Dock", parent, Panel);
        SetRect(dock, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, -145f), new Vector2(272f, 530f));
        AddOutline(dock.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.5f), new Vector2(-2f, -2f));

        AddText(dock, "Tower Dock Title", "БУДІВЛІ ЗАХИСТУ", 17f, Gold, -22f, 28f);

        int count = Mathf.Min(6, catalog != null ? catalog.Length : 0);
        for (int i = 0; i < count; i++)
        {
            TowerData tower = catalog[i];
            RectTransform slot = CreatePanel("Tower " + tower.towerName, dock, PanelSoft);
            SetRect(slot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f - i * 72f), new Vector2(236f, 60f));
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
                SetRect(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(38f, 38f));
            }

            TextMeshProUGUI label = AddText(slot, "Label", $"{ShortTowerName(tower.towerName)}\n{tower.cost}", 13f, Pale, 0f, 44f);
            SetRect(label.rectTransform, new Vector2(0.42f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-4f, -6f));
        }

        if (count == 0)
        {
            AddText(dock, "Empty Tower Dock", "Башти стануть доступними після налаштування каталогу", 16f, KenamUiTheme.TextMuted, -55f, 32f);
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
        Sprite runtimeIcon = RuntimeTowerVisuals.GetIconSprite(tower);
        if (runtimeIcon != null) return runtimeIcon;

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
        Vector2 basePosition = new Vector2(0f, -4.05f);
        CreateWorldPart(parent, "Ally Castle Shadow", sprite, basePosition + new Vector2(0f, -0.35f), new Vector2(3.4f, 0.75f), new Color(0f, 0f, 0f, 0.35f), 2);

        Texture2D props = RuntimeSpriteAssetMap.LoadTexture("Visuals/Props/AtlasProps");
        Sprite citadel = RuntimeSpriteAssetMap.SpriteFromTopLeft(props, 359f, 194f, 144f, 189f, 64f, RuntimeSpriteAssetMap.BottomCenter);
        if (citadel != null)
        {
            CreateSpritePart(parent, "Ally Citadel", citadel, basePosition + new Vector2(0f, -0.15f), Vector2.one * 0.74f, Color.white, 5);
            return;
        }

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

        CreateWorldPart(parent, "Ally Keep", sprite, basePosition, new Vector2(1.2f, 2.7f), KenamUiTheme.StoneRaised, 3);
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
        castle.transform.position = new Vector3(0f, 4.25f, 0f);

        BoxCollider2D collider = castle.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.4f, 3.6f);
        collider.isTrigger = true;
        castle.AddComponent<EnemyCastle>();
        CreateCastleVisual(castle.transform);
    }

    private static void CreateCastleVisual(Transform parent)
    {
        Texture2D props = RuntimeSpriteAssetMap.LoadTexture("Visuals/Props/AtlasProps");
        Sprite citadel = RuntimeSpriteAssetMap.SpriteFromTopLeft(props, 359f, 194f, 144f, 189f, 64f, RuntimeSpriteAssetMap.BottomCenter);
        if (citadel != null)
        {
            CreateWorldPart(parent, "Enemy Castle Shadow", CreateWhiteSprite(), new Vector2(0f, -0.35f), new Vector2(3.4f, 0.75f), new Color(0f, 0f, 0f, 0.38f), 2);
            CreateSpritePart(parent, "Enemy Citadel", citadel, new Vector2(0f, -0.15f), Vector2.one * 0.74f, Color.white, 5);
            return;
        }

        VisualAssetCatalog visualAssets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");
        if (visualAssets != null && visualAssets.tinyAllyCastle != null)
        {
            CreateWorldPart(parent, "Enemy Castle Shadow", CreateWhiteSprite(), new Vector2(0f, -0.35f), new Vector2(3.4f, 0.75f), new Color(0f, 0f, 0f, 0.38f), 2);
            Sprite castle = CreateTextureSprite(visualAssets.tinyAllyCastle, new Rect(0f, 0f, 320f, 256f), 100f);
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
        SetRect(card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, -70f), new Vector2(270f, 350f));
        AddOutline(card.gameObject, new Color(Violet.r, Violet.g, Violet.b, 0.45f), new Vector2(-2f, -2f));

        AddText(card, "Recruit Title", "ФОРМУВАННЯ ХВИЛІ", 18f, Gold, -27f, 28f);
        TextMeshProUGUI queue = AddText(card, "Queue State", "Черга порожня", 17f, Pale, -62f, 30f);
        TextMeshProUGUI lane = AddText(card, "Selected Lane", "ЛІВА ЛІНІЯ • АТАКА", 14f, KenamUiTheme.Danger, -91f, 24f);

        Button attackLane = CreateButton("Attack Lane", card, "АТАКА", KenamUiTheme.DangerDark, Pale);
        SetRect(attackLane.GetComponent<RectTransform>(), new Vector2(0.28f, 0f), new Vector2(0.28f, 0f), new Vector2(0f, 25f), new Vector2(104f, 36f));
        attackLane.onClick.AddListener(() => waveManager.SelectLane(BattleLane.Upper));
        Button defenseLane = CreateButton("Defense Lane", card, "ЗАХИСТ", KenamUiTheme.Swamp, Pale);
        SetRect(defenseLane.GetComponent<RectTransform>(), new Vector2(0.72f, 0f), new Vector2(0.72f, 0f), new Vector2(0f, 25f), new Vector2(104f, 36f));
        defenseLane.onClick.AddListener(() => waveManager.SelectLane(BattleLane.Lower));

        List<Button> buttons = new List<Button>();
        int count = Mathf.Min(3, waveManager.AvailableUnits.Count);
        for (int i = 0; i < count; i++)
        {
            UnitData unit = waveManager.AvailableUnits[i];
            RectTransform slot = CreatePanel("Recruit " + unit.unitName, card, Panel);
            SetRect(slot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -118f - i * 47f), new Vector2(-24f, 40f));
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

            TextMeshProUGUI income = AddText(slot, "Unit Income", $"+{unit.goldPerSecondIncrease}/с доходу", 12f, KenamUiTheme.Mint, 0f, 18f);
            SetRect(income.rectTransform, new Vector2(0.18f, 0f), new Vector2(0.98f, 0.5f), new Vector2(0f, 1f), new Vector2(-4f, -2f));
            income.alignment = TextAlignmentOptions.MidlineLeft;
        }

        AllyQueueHudController controller = card.gameObject.AddComponent<AllyQueueHudController>();
        controller.Configure(waveManager, queue, lane, attackLane, defenseLane, buttons);
    }

    private static TextMeshProUGUI CreateStat(Transform parent, string label, string value, float anchorX)
    {
        RectTransform holder = CreatePanel(label + " Holder", parent, Color.clear);
        SetRect(holder, new Vector2(anchorX, 0f), new Vector2(anchorX + 0.2f, 1f), Vector2.zero, new Vector2(-12f, -10f));
        AddText(holder, label + " Caption", label.ToUpperInvariant(), 14f, KenamUiTheme.TextMuted, -5f, 28f);
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
        KenamUiTheme.ApplyText(text, color, size >= 17f);
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
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        if (color.a > 0.05f) KenamUiTheme.ApplyPanel(image, color, KenamUiTheme.PurpleMuted, 0.24f);
        return rect;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color background, Color foreground)
    {
        RectTransform rect = CreatePanel(name, parent, background);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        KenamUiTheme.ApplyButton(button, background, background == Gold ? Gold : Violet);
        TextMeshProUGUI text = AddText(rect, "Label", label, 20f, foreground, 0f, 40f);
        Stretch(text.rectTransform);
        return button;
    }

    private static void AddMenuSprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        if (sprite == null) return;

        GameObject spriteObject = new GameObject(name);
        spriteObject.transform.SetParent(parent, false);
        RectTransform rect = spriteObject.AddComponent<RectTransform>();
        Image image = spriteObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
    }

    private static Sprite LoadUiSprite(string relativePath)
    {
        return RuntimeSpriteAssetMap.LoadSprite("Visuals/Curated/" + relativePath, 100f, RuntimeSpriteAssetMap.Center);
    }

    private static Sprite CreateUiSprite(Texture2D texture, float ppu, Vector2 pivot)
    {
        if (texture == null) return null;
        RuntimeSpriteAssetMap.PreparePixelTexture(texture);
        return RuntimeSpriteAssetMap.FullSprite(texture, ppu, pivot);
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
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        }
#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
        if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
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
