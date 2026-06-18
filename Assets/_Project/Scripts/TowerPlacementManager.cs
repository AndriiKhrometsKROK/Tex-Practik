// Керує вибором, попереднім переглядом, розміщенням, продажем і покращенням башт у дозволених слотах.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerPlacementManager : MonoBehaviour
{
    private const float PlacedTowerScale = 0.33f;

    public static TowerPlacementManager Instance { get; private set; }

    [Header("Build Settings")]
    public float gridSize = 1f;
    public LayerMask obstacleLayer;
    [SerializeField, Min(0.1f)] private float buildSlotRadius = 0.4f;

    [Header("Tower Menu")]
    [SerializeField] private float sellRefundMultiplier = 0.6f;
    [SerializeField] private Vector2 towerMenuScreenOffset = new Vector2(0f, 80f);

    [Header("Range Indicator")]
    [SerializeField] private Color buildRangeColor = default;
    [SerializeField] private Color selectedRangeColor = default;

    private TowerData _towerToBuild;
    private GameObject _ghostTower;
    private SpriteRenderer _ghostRenderer;
    private TowerRangeIndicator _rangeIndicator;

    private TowerController _selectedTower;
    private Canvas _towerMenuCanvas;
    private RectTransform _towerMenuPanel;
    private Button _sellButton;
    private Button _upgradeButton;
    private Text _sellButtonText;
    private Text _upgradeButtonText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (buildRangeColor == default) buildRangeColor = KenamUiTheme.WithAlpha(KenamUiTheme.Mint, 0.75f);
        if (selectedRangeColor == default) selectedRangeColor = KenamUiTheme.WithAlpha(KenamUiTheme.Gold, 0.85f);
    }

    // Привид башти не бере участі в бою й не має колайдерів, доки гравець не підтвердить позицію.
    public void SelectTowerToBuild(TowerData towerData)
    {
        CloseTowerMenu();
        _towerToBuild = towerData;

        if (_ghostTower != null) Destroy(_ghostTower);

        if (_towerToBuild != null && _towerToBuild.towerPrefab != null)
        {
            _ghostTower = Instantiate(_towerToBuild.towerPrefab);
            _ghostTower.transform.localScale = Vector3.one * PlacedTowerScale;
            EnsureRangeIndicator();

            if (_ghostTower.TryGetComponent<TowerController>(out var tc)) tc.enabled = false;

            Collider2D[] colliders = _ghostTower.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders) col.enabled = false;

            _ghostRenderer = RuntimeTowerVisuals.ApplyIfKnown(_ghostTower, _towerToBuild);
            if (_ghostRenderer == null) _ghostRenderer = _ghostTower.GetComponent<SpriteRenderer>();
            if (_ghostRenderer == null) _ghostRenderer = _ghostTower.GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (_towerToBuild == null || _ghostTower == null)
        {
            HandleTowerSelectionInput();
            UpdateTowerMenuPosition();
            return;
        }

        if (IsPointerOverUI())
        {
            if (_ghostRenderer != null) _ghostRenderer.enabled = false;
            _rangeIndicator?.Hide();
            return;
        }

        if (_ghostRenderer != null) _ghostRenderer.enabled = true;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 snappedPos = new Vector2(
            Mathf.Round(mousePos.x / gridSize) * gridSize,
            Mathf.Round(mousePos.y / gridSize) * gridSize
        );

        _ghostTower.transform.position = snappedPos;
        ShowRange(snappedPos, _towerToBuild.attackRadius, buildRangeColor);

        bool canBuild = IsInsideBuildZone(snappedPos) &&
            !Physics2D.OverlapCircle(snappedPos, 0.35f, obstacleLayer) &&
            !IsBuildSlotOccupied(snappedPos);

        if (_ghostRenderer != null)
        {
            _ghostRenderer.color = canBuild
                ? KenamUiTheme.WithAlpha(KenamUiTheme.Mint, 0.72f)
                : KenamUiTheme.WithAlpha(KenamUiTheme.Danger, 0.72f);
        }

        if (Input.GetMouseButtonDown(0) && canBuild)
        {
            BuildTower(snappedPos);
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectTower();
        }
    }

    private void BuildTower(Vector2 position)
    {
        if (GameManager.Instance.SpendGold(_towerToBuild.cost))
        {
            GameObject newTower = Instantiate(_towerToBuild.towerPrefab, position, Quaternion.identity);
            newTower.transform.localScale = Vector3.one * PlacedTowerScale;

            if (newTower.TryGetComponent<TowerController>(out var tc))
            {
                tc.data = _towerToBuild;
                tc.RefreshRuntimeVisual();
            }

            Debug.Log($"Tower built: {_towerToBuild.towerName}");
            DeselectTower();
        }
        else
        {
            Debug.LogWarning("Not enough gold to build this tower.");
            GameplayNotificationController.Show("Недостатньо золота для цієї башти");
        }
    }

    // Будувати можна лише біля заздалегідь визначених фундаментів правої захисної лінії.
    private bool IsInsideBuildZone(Vector2 position)
    {
        float[] slotColumns = { 5f, 7f };
        float[] slotRows = { -3f, -1f, 1f, 3f };
        foreach (float x in slotColumns)
        {
            foreach (float y in slotRows)
            {
                if (Vector2.Distance(position, new Vector2(x, y)) <= buildSlotRadius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsBuildSlotOccupied(Vector2 position)
    {
        foreach (TowerController tower in FindObjectsByType<TowerController>(FindObjectsSortMode.None))
        {
            if (tower == null || tower.gameObject == _ghostTower) continue;
            if (Vector2.Distance(tower.transform.position, position) < 0.65f) return true;
        }

        return false;
    }

    private void DeselectTower()
    {
        _towerToBuild = null;
        if (_ghostTower != null)
        {
            Destroy(_ghostTower);
            _ghostTower = null;
        }

        _rangeIndicator?.Hide();
    }

    private void HandleTowerSelectionInput()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTowerMenu();
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerOverUI()) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        TowerController clickedTower = FindTowerAt(mousePos);

        if (clickedTower != null)
        {
            OpenTowerMenu(clickedTower);
        }
        else
        {
            CloseTowerMenu();
        }
    }

    private TowerController FindTowerAt(Vector2 worldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
        foreach (Collider2D hit in hits)
        {
            TowerController tower = hit.GetComponentInParent<TowerController>();
            if (tower != null) return tower;
        }

        return null;
    }

    private void OpenTowerMenu(TowerController tower)
    {
        _selectedTower = tower;
        EnsureTowerMenu();
        _towerMenuPanel.gameObject.SetActive(true);
        RefreshTowerMenu();
        UpdateTowerMenuPosition();
        ShowRange(tower.transform.position, tower.data != null ? tower.data.attackRadius : 0f, selectedRangeColor);
    }

    private void CloseTowerMenu()
    {
        _selectedTower = null;
        if (_towerMenuPanel != null) _towerMenuPanel.gameObject.SetActive(false);
        _rangeIndicator?.Hide();
    }

    private void RefreshTowerMenu()
    {
        if (_selectedTower == null || _selectedTower.data == null) return;
        if (_sellButton == null || _upgradeButton == null || _sellButtonText == null || _upgradeButtonText == null)
        {
            CloseTowerMenu();
            return;
        }

        TowerData currentData = _selectedTower.data;
        int refund = Mathf.RoundToInt(currentData.cost * sellRefundMultiplier);
        int upgradeCost = GetUpgradeCost(currentData);
        bool hasUpgrade = currentData.upgradedTowerData != null;
        bool canPayUpgrade = GameManager.Instance != null && GameManager.Instance.currentGold >= upgradeCost;

        _sellButton.interactable = GameManager.Instance != null;
        _upgradeButton.interactable = hasUpgrade && canPayUpgrade;
        _sellButtonText.text = $"Продати ({refund})";
        _upgradeButtonText.text = hasUpgrade ? $"Покращити ({upgradeCost})" : "Макс. рівень";
    }

    private void SellSelectedTower()
    {
        if (_selectedTower == null || _selectedTower.data == null || GameManager.Instance == null) return;

        int refund = Mathf.RoundToInt(_selectedTower.data.cost * sellRefundMultiplier);
        GameManager.Instance.AddGold(refund);

        GameObject towerObject = _selectedTower.gameObject;
        CloseTowerMenu();
        Destroy(towerObject);
    }

    // Покращення замінює дані башти на наступний рівень, але зберігає сам GameObject і його позицію.
    private void UpgradeSelectedTower()
    {
        if (_selectedTower == null || _selectedTower.data == null) return;

        TowerData currentData = _selectedTower.data;
        TowerData upgradedData = currentData.upgradedTowerData;
        if (upgradedData == null || GameManager.Instance == null) return;

        int upgradeCost = GetUpgradeCost(currentData);
        if (!GameManager.Instance.SpendGold(upgradeCost)) return;

        upgradedData.towerLevel = Mathf.Max(upgradedData.towerLevel, currentData.towerLevel + 1);
        if (upgradedData.archetype == TowerArchetype.Generic) upgradedData.archetype = currentData.archetype;
        if (upgradedData.archetype == TowerArchetype.SentinelPylon)
        {
            upgradedData.damageFamily = DamageFamily.Physical;
            upgradedData.damageModifier = DamageModifier.Piercing;
        }
        else if (upgradedData.archetype == TowerArchetype.LightObelisk)
        {
            upgradedData.damageFamily = DamageFamily.Magical;
            upgradedData.damageModifier = DamageModifier.Light;
            upgradedData.isMagic = true;
        }
        else if (upgradedData.archetype == TowerArchetype.DistortionPrism)
        {
            upgradedData.damageFamily = upgradedData.towerLevel >= 3 ? DamageFamily.Chaos : DamageFamily.Hybrid;
            upgradedData.damageModifier = upgradedData.towerLevel >= 3 ? DamageModifier.Chaos : DamageModifier.Mixed;
            if (upgradedData.towerLevel >= 2) upgradedData.slowFactor = 0.85f;
        }

        _selectedTower.data = upgradedData;
        ApplyUpgradedTowerVisuals(_selectedTower, upgradedData);
        _selectedTower.RefreshRuntimeVisual();
        RefreshTowerMenu();
        ShowRange(_selectedTower.transform.position, upgradedData.attackRadius, selectedRangeColor);
    }

    private int GetUpgradeCost(TowerData currentData)
    {
        if (currentData == null || currentData.upgradedTowerData == null) return 0;
        if (currentData.upgradeCost > 0) return currentData.upgradeCost;

        return Mathf.Max(0, currentData.upgradedTowerData.cost - currentData.cost);
    }

    private void ApplyUpgradedTowerVisuals(TowerController tower, TowerData upgradedData)
    {
        if (tower == null || upgradedData == null || upgradedData.towerPrefab == null) return;
        if (RuntimeTowerVisuals.IsArcherTower(tower.gameObject, upgradedData)) return;

        SpriteRenderer sourceRenderer = upgradedData.towerPrefab.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer targetRenderer = tower.GetComponentInChildren<SpriteRenderer>();
        if (sourceRenderer != null && targetRenderer != null)
        {
            targetRenderer.sprite = sourceRenderer.sprite;
            targetRenderer.color = sourceRenderer.color;
        }
    }

    private void EnsureTowerMenu()
    {
        if (_towerMenuPanel != null) return;

        _towerMenuCanvas = FindAnyObjectByType<Canvas>();
        if (_towerMenuCanvas == null)
        {
            GameObject canvasObject = new GameObject("Tower Menu Canvas");
            _towerMenuCanvas = canvasObject.AddComponent<Canvas>();
            _towerMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else if (_towerMenuCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            _towerMenuCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObject = new GameObject("Tower Action Menu");
        panelObject.transform.SetParent(_towerMenuCanvas.transform, false);

        _towerMenuPanel = panelObject.AddComponent<RectTransform>();
        _towerMenuPanel.sizeDelta = new Vector2(180f, 86f);
        _towerMenuPanel.pivot = new Vector2(0.5f, 0f);

        Image panelImage = panelObject.AddComponent<Image>();
        KenamUiTheme.ApplyPanel(panelImage, KenamUiTheme.PanelRaised, KenamUiTheme.Gold);

        VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        _sellButton = CreateMenuButton("Sell Button", panelObject.transform, SellSelectedTower, out _sellButtonText);
        _upgradeButton = CreateMenuButton("Upgrade Button", panelObject.transform, UpgradeSelectedTower, out _upgradeButtonText);
        _towerMenuPanel.gameObject.SetActive(false);
    }

    private Button CreateMenuButton(string objectName, Transform parent, UnityEngine.Events.UnityAction onClick, out Text label)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = KenamUiTheme.PanelSoft;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        KenamUiTheme.ApplyButton(button, KenamUiTheme.PanelSoft, KenamUiTheme.Purple);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObject.AddComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.color = KenamUiTheme.Text;
        label.fontSize = 15;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 15;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return button;
    }

    private void UpdateTowerMenuPosition()
    {
        if (_towerMenuPanel == null || !_towerMenuPanel.gameObject.activeSelf || _selectedTower == null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector2 screenPosition = mainCamera.WorldToScreenPoint(_selectedTower.transform.position);
        screenPosition += towerMenuScreenOffset;

        RectTransform canvasRect = (RectTransform)_towerMenuCanvas.transform;
        Camera uiCamera = _towerMenuCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _towerMenuCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
        {
            _towerMenuPanel.anchoredPosition = localPoint;
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void EnsureRangeIndicator()
    {
        if (_rangeIndicator != null) return;

        GameObject indicatorObject = new GameObject("Tower Range Indicator");
        _rangeIndicator = indicatorObject.AddComponent<TowerRangeIndicator>();
    }

    private void ShowRange(Vector3 position, float radius, Color color)
    {
        EnsureRangeIndicator();
        _rangeIndicator.Show(position, radius, color);
    }
}
