// Підключає до сцени механіки героя, предметів, магазину, активних слотів і спеціального HUD без ручного налаштування префабів.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayMechanicsBootstrapper
{
    private enum ItemShopCategory
    {
        Utility,
        Army,
        Magic,
        Physical,
        Immortality
    }

    // Метод можна викликати повторно: він додає лише відсутні компоненти й не створює дублікати HUD.
    public static void Ensure(GameManager manager)
    {
        if (manager == null || GameObject.Find("Mechanics Runtime") != null) return;

        GameObject root = new GameObject("Mechanics Runtime");
        HeroController hero = Object.FindAnyObjectByType<HeroController>();
        if (hero == null) return;

        HeroAbilityController oldAbilities = hero.GetComponent<HeroAbilityController>();
        if (oldAbilities != null) oldAbilities.enabled = false;
        PlayerShooting oldShooting = hero.GetComponent<PlayerShooting>();
        if (oldShooting != null) oldShooting.enabled = false;
        GameObject oldAbilityBar = GameObject.Find("Hero Ability Bar");
        if (oldAbilityBar != null) oldAbilityBar.SetActive(false);

        HeroStats stats = hero.GetComponent<HeroStats>() ?? hero.gameObject.AddComponent<HeroStats>();
        HeroProgressionController progression = hero.GetComponent<HeroProgressionController>() ??
            hero.gameObject.AddComponent<HeroProgressionController>();
        HeroInventory inventory = hero.GetComponent<HeroInventory>() ?? hero.gameObject.AddComponent<HeroInventory>();
        EchoSpellbookController echo = hero.GetComponent<EchoSpellbookController>() ??
            hero.gameObject.AddComponent<EchoSpellbookController>();

        if (TrainingGroundState.IsActive)
        {
            BuildTrainingMechanicsHud(root.transform, stats, inventory, echo);
            return;
        }

        BattleFlowController flow = Object.FindAnyObjectByType<BattleFlowController>();
        if (flow == null) flow = root.AddComponent<BattleFlowController>();
        flow.Configure(manager, manager.enemySpawner);
        root.AddComponent<GameAutosaveController>();
        DynamicAudioController audio = root.AddComponent<DynamicAudioController>();
        audio.Configure(manager, flow);

        BuildMechanicsHud(root.transform, flow, stats, inventory, echo);
    }

    private static void BuildTrainingMechanicsHud(
        Transform parent,
        HeroStats stats,
        HeroInventory inventory,
        EchoSpellbookController echo)
    {
        GameObject canvasObject = new GameObject("Training Mechanics HUD");
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 121;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform echoPanel = CreatePanel(canvas.transform, "Training Echo Panel", KenamUiTheme.PanelRaised);
        SetRect(echoPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 122f), new Vector2(430f, 74f));
        TextMeshProUGUI echoes = CreateText(echoPanel, "Echoes", "-  -  -", 22f, KenamUiTheme.Purple);
        SetRect(echoes.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI spell = CreateText(echoPanel, "Spell", "Q/W/E - Еха, R - виклик", 13f, KenamUiTheme.Text);
        SetRect(spell.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);
        echo.ConfigureUi(echoes, spell);

        Button shopToggle = CreateButton(canvas.transform, "Training Shop Toggle", "АРСЕНАЛ [B]", KenamUiTheme.PurpleMuted);
        SetRect(shopToggle.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-805f, 112f), new Vector2(132f, 58f));
        BuildActiveItemBar(canvas.transform, inventory);

        RectTransform statusPanel = CreatePanel(canvas.transform, "Training Item Status", KenamUiTheme.WithAlpha(KenamUiTheme.Panel, 0.8f));
        SetRect(statusPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -126f), new Vector2(270f, 116f));
        TextMeshProUGUI status = CreateText(statusPanel, "Status", string.Empty, 12f, KenamUiTheme.Text);
        SetRect(status.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, -20f));

        RectTransform shop = BuildShop(canvas.transform, inventory);
        shop.gameObject.SetActive(false);
        TrainingMechanicsHudController controller = canvasObject.AddComponent<TrainingMechanicsHudController>();
        controller.Configure(inventory, stats, status, shop.gameObject);
        shopToggle.onClick.AddListener(controller.ToggleShop);
    }

    private static void BuildMechanicsHud(
        Transform parent,
        BattleFlowController flow,
        HeroStats stats,
        HeroInventory inventory,
        EchoSpellbookController echo)
    {
        GameObject canvasObject = new GameObject("Mechanics HUD");
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform echoPanel = CreatePanel(canvas.transform, "Echo Panel", KenamUiTheme.PanelRaised);
        SetRect(echoPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(430f, 74f));
        TextMeshProUGUI echoes = CreateText(echoPanel, "Echoes", "—  —  —", 22f, KenamUiTheme.Purple);
        SetRect(echoes.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI spell = CreateText(echoPanel, "Spell", "Q/W/E — Еха, R — виклик", 13f, KenamUiTheme.Text);
        SetRect(spell.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);
        echo.ConfigureUi(echoes, spell);

        RectTransform statusPanel = CreatePanel(canvas.transform, "Rules Panel", KenamUiTheme.Panel);
        SetRect(statusPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -126f), new Vector2(270f, 116f));
        TextMeshProUGUI phase = CreateText(statusPanel, "Phase", string.Empty, 12f, KenamUiTheme.Text);
        SetRect(phase.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, -20f));

        Button challenge = CreateButton(canvas.transform, "Challenge Demon", "КИНУТИ ВИКЛИК", KenamUiTheme.DangerDark);
        SetRect(challenge.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(270f, 46f));
        challenge.onClick.AddListener(flow.ChallengeDemon);

        Button shopToggle = CreateButton(canvas.transform, "Shop Toggle", "АРСЕНАЛ [B]", KenamUiTheme.PurpleMuted);
        SetRect(shopToggle.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-805f, 44f), new Vector2(112f, 58f));
        BuildActiveItemBar(canvas.transform, inventory);

        AllyWaveManager waveManager = Object.FindAnyObjectByType<AllyWaveManager>();
        Button attackLane = CreateButton(canvas.transform, "Attack Lane", "КРИПИ: АТАКА", KenamUiTheme.DangerDark);
        SetRect(attackLane.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(140f, 118f), new Vector2(220f, 42f));
        attackLane.onClick.AddListener(() => waveManager?.SelectLane(BattleLane.Upper));
        Button defenseLane = CreateButton(canvas.transform, "Defense Lane", "КРИПИ: ЗАХИСТ", KenamUiTheme.Swamp);
        SetRect(defenseLane.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(140f, 70f), new Vector2(220f, 42f));
        defenseLane.onClick.AddListener(() => waveManager?.SelectLane(BattleLane.Lower));

        RectTransform shop = BuildShop(canvas.transform, inventory);
        shop.gameObject.SetActive(false);
        MechanicsHudController controller = canvasObject.AddComponent<MechanicsHudController>();
        controller.Configure(flow, inventory, phase, challenge, shop.gameObject, attackLane, defenseLane);
        shopToggle.onClick.AddListener(controller.ToggleShop);
    }

    // Магазин будується за каталогом предметів, тому дані, рецепти та ціни не дублюються у верстці.
    private static RectTransform BuildShop(Transform parent, HeroInventory inventory)
    {
        RectTransform panel = CreatePanel(parent, "Artifact Shop", KenamUiTheme.PanelRaised);
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280f, 760f));

        TextMeshProUGUI title = CreateText(panel, "Title", "АРСЕНАЛ КЕ́НАМА", 30f, KenamUiTheme.Gold);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-160f, 50f));

        Button close = CreateButton(panel, "Close", "ЗАКРИТИ [B]", KenamUiTheme.DangerDark);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-105f, -38f), new Vector2(160f, 42f));
        close.onClick.AddListener(() => panel.gameObject.SetActive(false));

        ItemShopCategoryController categories = panel.gameObject.AddComponent<ItemShopCategoryController>();
        List<GameObject> pages = new List<GameObject>();
        List<Button> tabs = new List<Button>();
        string[] categoryNames = { "МОБІЛЬНІСТЬ", "АУРИ АРМІЇ", "МАГІЯ", "ФІЗИЧНІ", "БЕЗСМЕРТЯ" };

        for (int index = 0; index < categoryNames.Length; index++)
        {
            int capturedIndex = index;
            Button tab = CreateButton(panel, "Category " + categoryNames[index], categoryNames[index], KenamUiTheme.PanelSoft);
            SetRect(tab.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(155f + index * 245f, -98f), new Vector2(220f, 48f));
            tab.onClick.AddListener(() => categories.Show(capturedIndex));
            tabs.Add(tab);

            RectTransform page = CreatePanel(panel, "Page " + categoryNames[index], KenamUiTheme.WithAlpha(KenamUiTheme.VoidSoft, 0.84f));
            SetRect(page, new Vector2(0.04f, 0.13f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);
            GridLayoutGroup layout = page.gameObject.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.cellSize = new Vector2(350f, 112f);
            layout.spacing = new Vector2(18f, 12f);
            pages.Add(page.gameObject);
        }

        foreach (HeroItemDefinition definition in HeroItemCatalog.All)
        {
            RectTransform page = pages[(int)GetItemCategory(definition.Id)].GetComponent<RectTransform>();
            Button button = CreateButton(
                page,
                definition.Id.ToString(),
                definition.Name,
                definition.Active ? KenamUiTheme.PurpleMuted : KenamUiTheme.Charcoal);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 13.5f;
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 0f), new Vector2(-108f, -16f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            AddItemIcon(button.transform, definition, new Vector2(50f, 0f), new Vector2(74f, 74f));
            button.gameObject.AddComponent<ItemShopButtonController>().Configure(inventory, definition, button, label);
        }

        categories.Configure(pages, tabs);

        Button sell = CreateButton(panel, "Sell Last", "ПРОДАТИ ОСТАННІЙ ПРЕДМЕТ • ПОВЕРНЕННЯ 49%", KenamUiTheme.DangerDark);
        SetRect(sell.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(540f, 52f));
        sell.onClick.AddListener(() =>
        {
            if (inventory.Items.Count > 0) inventory.Sell(inventory.Items[inventory.Items.Count - 1]);
        });
        return panel;
    }

    // Панель показує лише чотири активні слоти; пасивні предмети працюють без окремої кнопки.
    private static void BuildActiveItemBar(Transform parent, HeroInventory inventory)
    {
        RectTransform bar = CreatePanel(parent, "Active Item Bar", KenamUiTheme.Panel);
        SetRect(bar, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-520f, 44f), new Vector2(430f, 68f));

        Button[] buttons = new Button[4];
        TextMeshProUGUI[] labels = new TextMeshProUGUI[4];
        RawImage[] icons = new RawImage[4];
        for (int i = 0; i < buttons.Length; i++)
        {
            int captured = i;
            Button button = CreateButton(bar, "Active Slot " + (i + 1), $"{i + 1}\nПУСТО", KenamUiTheme.PanelSoft);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f + i * 103f, 0f), new Vector2(92f, 54f));
            button.onClick.AddListener(() => inventory.UseActiveSlot(captured));
            buttons[i] = button;
            labels[i] = button.GetComponentInChildren<TextMeshProUGUI>();
            SetRect(labels[i].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(15f, 0f), new Vector2(-34f, -8f));
            labels[i].fontSize = 10f;
            icons[i] = AddItemIcon(button.transform, null, new Vector2(16f, 0f), new Vector2(26f, 26f));
        }

        bar.gameObject.AddComponent<ActiveItemHudController>().Configure(inventory, buttons, labels, icons);
    }

    private static ItemShopCategory GetItemCategory(HeroItemId id)
    {
        return id switch
        {
            HeroItemId.Teleport or HeroItemId.TravelBoots or HeroItemId.GhostScepter or HeroItemId.BlinkDagger or
                HeroItemId.MorbidMask or HeroItemId.MaskOfMadness or HeroItemId.Satanic => ItemShopCategory.Utility,
            HeroItemId.Dominator or HeroItemId.Overlord or HeroItemId.Vladmir or HeroItemId.GreaterVladmir or
                HeroItemId.Drums or HeroItemId.SolarCrest or HeroItemId.Pipe or HeroItemId.Greaves or
                HeroItemId.AssaultCuirass => ItemShopCategory.Army,
            HeroItemId.AeonDisk or HeroItemId.UndyingAura or HeroItemId.DivineRapier or HeroItemId.PassiveBkb or
                HeroItemId.Aegis or HeroItemId.HeartOfTarrasque => ItemShopCategory.Immortality,
            HeroItemId.BattleFury or HeroItemId.Crystalys or HeroItemId.Daedalus => ItemShopCategory.Physical,
            _ => ItemShopCategory.Magic
        };
    }

    private static RectTransform BuildLegacyShop(Transform parent, HeroInventory inventory)
    {
        RectTransform panel = CreatePanel(parent, "Technical Item Shop", KenamUiTheme.PanelRaised);
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 720f));

        TextMeshProUGUI title = CreateText(panel, "Title", "ТЕХНІЧНИЙ МАГАЗИН ПРЕДМЕТІВ", 28f, KenamUiTheme.Gold);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-40f, 50f));

        RectTransform grid = CreatePanel(panel, "Grid", Color.clear);
        SetRect(grid, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        layout.cellSize = new Vector2(190f, 72f);
        layout.spacing = new Vector2(12f, 10f);

        foreach (HeroItemDefinition definition in HeroItemCatalog.All)
        {
            HeroItemDefinition captured = definition;
            Button button = CreateButton(
                grid,
                definition.Id.ToString(),
                $"{definition.Name}\n{definition.Cost}",
                definition.Active ? KenamUiTheme.PurpleMuted : KenamUiTheme.PanelSoft);
            button.onClick.AddListener(() => inventory.Purchase(captured.Id));
        }

        Button sell = CreateButton(panel, "Sell Last", "ПРОДАТИ ОСТАННІЙ ПРЕДМЕТ (49%)", KenamUiTheme.DangerDark);
        SetRect(sell.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(430f, 52f));
        sell.onClick.AddListener(() =>
        {
            if (inventory.Items.Count > 0) inventory.Sell(inventory.Items[inventory.Items.Count - 1]);
        });
        return panel;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.color = color;
        if (color.a > 0.05f) KenamUiTheme.ApplyPanel(image, color, KenamUiTheme.PurpleMuted, 0.26f);
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
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = size;
        KenamUiTheme.ApplyText(text, color, size >= 18f);
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color)
    {
        RectTransform rect = CreatePanel(parent, name, color);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        KenamUiTheme.ApplyButton(button, color, color == KenamUiTheme.DangerDark ? KenamUiTheme.Danger : KenamUiTheme.Gold);
        TextMeshProUGUI text = CreateText(rect, "Label", label, 16f, KenamUiTheme.Text);
        Stretch(text.rectTransform);
        return button;
    }

    private static RawImage AddItemIcon(Transform parent, HeroItemDefinition definition, Vector2 position, Vector2 size)
    {
        RectTransform frame = CreatePanel(parent, "Icon Frame", KenamUiTheme.WithAlpha(KenamUiTheme.Text, 0.92f));
        SetRect(frame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, size);

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(frame, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        Stretch(iconRect);
        iconRect.offsetMin = new Vector2(4f, 4f);
        iconRect.offsetMax = new Vector2(-4f, -4f);
        RawImage icon = iconObject.AddComponent<RawImage>();
        icon.texture = definition != null ? LoadItemIcon(definition) : null;
        icon.color = Color.white;
        return icon;
    }

    public static Texture2D LoadItemIcon(HeroItemDefinition definition)
    {
        return definition != null
            ? Resources.Load<Texture2D>("ItemIcons/" + definition.IconPath)
            : null;
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

public class ItemShopCategoryController : MonoBehaviour
{
    private List<GameObject> pages;
    private List<Button> tabs;

    public void Configure(List<GameObject> targetPages, List<Button> targetTabs)
    {
        pages = targetPages;
        tabs = targetTabs;
        Show(0);
    }

    public void Show(int index)
    {
        if (pages == null || tabs == null) return;
        for (int i = 0; i < pages.Count; i++)
        {
            bool selected = i == index;
            pages[i].SetActive(selected);
            Image image = tabs[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = selected
                    ? KenamUiTheme.PurpleMuted
                    : KenamUiTheme.PanelSoft;
            }
        }
    }
}

public class ItemShopButtonController : MonoBehaviour
{
    private HeroInventory inventory;
    private HeroItemDefinition definition;
    private Button button;
    private TextMeshProUGUI label;
    private Image background;
    private float nextRefresh;

    public void Configure(
        HeroInventory targetInventory,
        HeroItemDefinition targetDefinition,
        Button targetButton,
        TextMeshProUGUI targetLabel)
    {
        inventory = targetInventory;
        definition = targetDefinition;
        button = targetButton;
        label = targetLabel;
        background = targetButton != null ? targetButton.GetComponent<Image>() : null;
        if (button != null) button.onClick.AddListener(Purchase);
        if (inventory != null) inventory.InventoryChanged += Refresh;
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.InventoryChanged -= Refresh;
    }

    private void Purchase()
    {
        inventory?.Purchase(definition.Id);
        Refresh();
    }

    private void Refresh()
    {
        if (inventory == null || definition == null || button == null || label == null) return;
        bool owned = definition.Id != HeroItemId.Aegis && inventory.Has(definition.Id);
        bool unpacking = inventory.IsUnpacking(definition.Id);
        float unpackRemaining = inventory.GetUnpackRemaining(definition.Id);
        int cost = inventory.GetCurrentCost(definition.Id);
        bool affordable = GameManager.Instance != null && GameManager.Instance.currentGold >= cost;
        string recipe = HeroInventory.GetRecipeText(definition);
        button.interactable = !owned && !unpacking;
        label.text = unpacking
            ? $"{definition.Name}\nРОЗПАКОВКА {unpackRemaining:0.0}с"
            : owned
            ? $"{definition.Name}\nПРИДБАНО"
            : $"{definition.Name}\n{cost} золота • {(definition.Active ? "АКТИВНИЙ" : "ПАСИВНИЙ")}" +
              $"\n{HeroItemCatalog.GetEffectText(definition.Id)}" +
              (string.IsNullOrEmpty(recipe) ? string.Empty : $"\nРецепт: {recipe}");
        if (background != null)
        {
            background.color = owned || unpacking
                ? KenamUiTheme.Swamp
                : affordable
                    ? definition.Active ? KenamUiTheme.PurpleMuted : KenamUiTheme.Charcoal
                    : KenamUiTheme.DangerDark;
        }
    }
}

public class ActiveItemHudController : MonoBehaviour
{
    private HeroInventory inventory;
    private Button[] buttons;
    private TextMeshProUGUI[] labels;
    private RawImage[] icons;
    private float nextRefresh;

    public void Configure(
        HeroInventory targetInventory,
        Button[] targetButtons,
        TextMeshProUGUI[] targetLabels,
        RawImage[] targetIcons)
    {
        inventory = targetInventory;
        buttons = targetButtons;
        labels = targetLabels;
        icons = targetIcons;
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        Refresh();
    }

    private void Refresh()
    {
        if (inventory == null || buttons == null || labels == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            bool occupied = i < inventory.ActiveSlots.Count;
            float cooldown = occupied ? inventory.GetCooldownRemaining(i) : 0f;
            buttons[i].interactable = occupied && cooldown <= 0f;
            Image image = buttons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = occupied
                    ? cooldown > 0f ? KenamUiTheme.Charcoal : KenamUiTheme.PurpleMuted
                    : KenamUiTheme.PanelSoft;
            }

            if (!occupied)
            {
                labels[i].text = $"{i + 1}\nПУСТО";
                if (icons != null && i < icons.Length) icons[i].texture = null;
                continue;
            }

            HeroItemDefinition definition = HeroItemCatalog.Get(inventory.ActiveSlots[i]);
            if (definition == null)
            {
                labels[i].text = $"{i + 1}\nПОМИЛКА";
                if (icons != null && i < icons.Length) icons[i].texture = null;
                continue;
            }
            if (icons != null && i < icons.Length) icons[i].texture = GameplayMechanicsBootstrapper.LoadItemIcon(definition);
            labels[i].text = cooldown > 0f
                ? $"{i + 1} • {cooldown:0.0}с\n{definition.Name}"
                : $"{i + 1} • ГОТОВО\n{definition.Name}";
        }
    }
}

public class TrainingMechanicsHudController : MonoBehaviour
{
    private HeroInventory inventory;
    private HeroStats stats;
    private TextMeshProUGUI status;
    private GameObject shop;
    private float nextRefresh;

    public void Configure(
        HeroInventory targetInventory,
        HeroStats targetStats,
        TextMeshProUGUI targetStatus,
        GameObject targetShop)
    {
        inventory = targetInventory;
        stats = targetStats;
        status = targetStatus;
        shop = targetShop;
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) ToggleShop();
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        Refresh();
    }

    public void ToggleShop()
    {
        if (shop != null) shop.SetActive(!shop.activeSelf);
    }

    private void Refresh()
    {
        if (status == null) return;
        int gold = GameManager.Instance != null ? GameManager.Instance.currentGold : 0;
        int items = inventory != null ? inventory.Items.Count : 0;
        int active = inventory != null ? inventory.ActiveSlots.Count : 0;
        float attack = stats != null ? stats.AttackDamage : 0f;
        float spell = stats != null ? stats.SpellPower : 0f;
        status.text =
            $"ПОЛІГОН\n" +
            $"Золото: {gold}\n" +
            $"Предмети: {items} | Активні: {active}/4\n" +
            $"Атака: {attack:0} | Магія: {spell:0}";
    }
}

public class MechanicsHudController : MonoBehaviour
{
    private BattleFlowController flow;
    private HeroInventory inventory;
    private TextMeshProUGUI status;
    private Button challenge;
    private GameObject shop;
    private Button attackLane;
    private Button defenseLane;
    private float nextStatusRefresh;

    public void Configure(
        BattleFlowController targetFlow,
        HeroInventory targetInventory,
        TextMeshProUGUI targetStatus,
        Button targetChallenge,
        GameObject targetShop,
        Button targetAttackLane,
        Button targetDefenseLane)
    {
        flow = targetFlow;
        inventory = targetInventory;
        status = targetStatus;
        challenge = targetChallenge;
        shop = targetShop;
        attackLane = targetAttackLane;
        defenseLane = targetDefenseLane;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) ToggleShop();
        if (Input.GetKeyDown(KeyCode.F5)) CommandAllies(BattleLane.Upper, false);
        if (Input.GetKeyDown(KeyCode.F6)) CommandAllies(BattleLane.Lower, false);
        if (Input.GetKeyDown(KeyCode.F7)) CommandAllies(BattleLane.Upper, true);
        if (status != null && flow != null)
        {
            status.text =
                $"Фаза: {TranslatePhase(flow.Phase)}\n" +
                $"Вежі: {TowerHealth(flow.DefenseFrontTower)} / {TowerHealth(flow.AttackFrontTower)}\n" +
                $"Лють: +{flow.EnemyRageStacks * 10}%  Предм.: {(inventory != null ? inventory.Items.Count : 0)}";
        }
        if (status != null && flow != null && Time.unscaledTime >= nextStatusRefresh)
        {
            nextStatusRefresh = Time.unscaledTime + 0.25f;
            UpdateTechnicalStatus();
        }
        if (challenge != null) challenge.gameObject.SetActive(CampaignProgress.SelectedLevel >= 20);
        if (challenge != null) challenge.interactable = flow != null && flow.CanChallengeDemon();
        if (attackLane != null) attackLane.interactable = true;
        if (defenseLane != null) defenseLane.interactable = true;
    }

    private void UpdateTechnicalStatus()
    {
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        CampaignLevelRule rule = spawner != null && spawner.ActiveLevelRule.Level > 0
            ? spawner.ActiveLevelRule
            : CampaignLevelRules.Get(CampaignProgress.SelectedLevel);
        DemonBossController boss = FindAnyObjectByType<DemonBossController>();
        HeroProgressionController progression = FindAnyObjectByType<HeroProgressionController>();
        HeroStats heroStats = FindAnyObjectByType<HeroStats>();
        EchoSpellbookController echo = FindAnyObjectByType<EchoSpellbookController>();
        CountUnits(out int enemies, out int allies, out int waiting, out int attacking);
        status.text =
            $"Рівень {rule.Level}: {rule.Title}\n" +
            $"Фаза: {TranslatePhase(flow.Phase)} | Лють +{flow.EnemyRageStacks * 10}%\n" +
            $"Юніти: вор. {enemies}, союз. {allies}\n" +
            $"Вежі: {TowerHealth(flow.DefenseFrontTower)} / {TowerHealth(flow.AttackFrontTower)}" +
            (boss != null && boss.IsAlive
                ? $"\nДемон: {Mathf.CeilToInt(boss.CurrentHealth)}/{Mathf.CeilToInt(boss.MaxHealth)}"
                : string.Empty);
    }

    private static void CountUnits(out int enemies, out int allies, out int waiting, out int attacking)
    {
        enemies = 0;
        allies = 0;
        waiting = 0;
        attacking = 0;
        CombatRegistry.RemoveInvalidEntries();
        foreach (EnemyAI enemy in CombatRegistry.ActiveEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            enemies++;
            if (enemy.BrainState == UnitBrainState.Wait) waiting++;
            if (enemy.BrainState == UnitBrainState.Attack) attacking++;
        }
        foreach (AllyController ally in CombatRegistry.ActiveAllies)
        {
            if (ally == null || !ally.IsAlive) continue;
            allies++;
            if (ally.BrainState == UnitBrainState.Wait) waiting++;
            if (ally.BrainState == UnitBrainState.Attack) attacking++;
        }
    }

    public void ToggleShop()
    {
        if (shop != null) shop.SetActive(!shop.activeSelf);
    }

    private static string TranslatePhase(BattlePhase phase)
    {
        return phase switch
        {
            BattlePhase.SeparatedFronts => "Розділені фронти",
            BattlePhase.TotalWar => "Тотальна війна",
            BattlePhase.BossBattle => "Битва з Демоном",
            BattlePhase.Finale => "Злам Петлі",
            BattlePhase.Completed => "Завершено",
            _ => phase.ToString()
        };
    }

    private static string TowerHealth(FrontTower tower)
    {
        return tower != null && tower.IsAlive
            ? $"{Mathf.CeilToInt(tower.CurrentHealth)}/{Mathf.CeilToInt(tower.MaxHealth)}"
            : "знищена";
    }

    private static void CommandAllies(BattleLane lane, bool returnToBase)
    {
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (returnToBase) ally.CommandReturnToBase();
            else ally.CommandMoveToLane(lane);
        }
    }
}
