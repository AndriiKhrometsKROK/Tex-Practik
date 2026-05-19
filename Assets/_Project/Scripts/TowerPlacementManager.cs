using UnityEngine;
using UnityEngine.EventSystems; // Для перевірки наведення на UI (кнопки)

public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    [Header("Налаштування будівництва")]
    public float gridSize = 1f; // Розмір клітинки сітки
    public LayerMask obstacleLayer; // Шар об'єктів, на яких НЕ можна будувати (дорога, інші вежі)

    private TowerData _towerToBuild;
    private GameObject _ghostTower;
    private SpriteRenderer _ghostRenderer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Цю функцію буде викликати кнопка магазину
    public void SelectTowerToBuild(TowerData towerData)
    {
        _towerToBuild = towerData;
        
        // Видаляємо старий фантом, якщо ми передумали і вибрали іншу вежу
        if (_ghostTower != null) Destroy(_ghostTower);

        if (_towerToBuild != null && _towerToBuild.towerPrefab != null)
        {
            // Створюємо "фантом" обраної вежі
            _ghostTower = Instantiate(_towerToBuild.towerPrefab);
            
            // Вимикаємо логіку на фантомі, щоб він не стріляв під час будівництва
            if (_ghostTower.TryGetComponent<TowerController>(out var tc)) tc.enabled = false;
            
            // Вимикаємо коллайдери, щоб фантом не заважав ворогам
            Collider2D[] colliders = _ghostTower.GetComponentsInChildren<Collider2D>();
            foreach(var col in colliders) col.enabled = false;

            // Шукаємо малюнок вежі, щоб зробити його напівпрозорим
            _ghostRenderer = _ghostTower.GetComponent<SpriteRenderer>();
            if (_ghostRenderer == null) _ghostRenderer = _ghostTower.GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        // Якщо вежа не вибрана, нічого не робимо
        if (_towerToBuild == null || _ghostTower == null) return;

        // Якщо миша знаходиться над UI (наприклад, кнопкою магазину) - ховаємо фантом
        if (EventSystem.current.IsPointerOverGameObject())
        {
            _ghostRenderer.enabled = false;
            return;
        }
        _ghostRenderer.enabled = true;

        // Отримуємо позицію миші в ігровому світі
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Прив'язуємо позицію до рівної сітки
        Vector2 snappedPos = new Vector2(
            Mathf.Round(mousePos.x / gridSize) * gridSize,
            Mathf.Round(mousePos.y / gridSize) * gridSize
        );

        _ghostTower.transform.position = snappedPos;

        // Перевіряємо, чи місце вільне (шукаємо коллайдери на шарі Obstacle)
        bool canBuild = !Physics2D.OverlapCircle(snappedPos, 0.2f, obstacleLayer);

        // Фарбуємо фантом у зелений (можна будувати) або червоний (зайнято)
        if (canBuild)
            _ghostRenderer.color = new Color(0.5f, 1f, 0.5f, 0.7f); // Зеленуватий прозорий
        else
            _ghostRenderer.color = new Color(1f, 0.3f, 0.3f, 0.7f); // Червонуватий прозорий

        // ЛІВИЙ клік миші - побудувати
        if (Input.GetMouseButtonDown(0) && canBuild)
        {
            BuildTower(snappedPos);
        }

        // ПРАВИЙ клік миші (або Escape) - відмінити будівництво
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectTower();
        }
    }

    private void BuildTower(Vector2 position)
    {
        // Пробуємо списати золото через наш GameManager
        if (GameManager.Instance.SpendGold(_towerToBuild.cost))
        {
            // Створюємо СПРАВЖНЮ вежу
            GameObject newTower = Instantiate(_towerToBuild.towerPrefab, position, Quaternion.identity);
            
            // Передаємо вежі її дані
            if (newTower.TryGetComponent<TowerController>(out var tc))
            {
                tc.data = _towerToBuild;
            }

            Debug.Log($"Побудовано вежу: {_towerToBuild.towerName}");
            
            // Скидаємо вибір (або можна закоментувати цей рядок, щоб будувати кілька веж підряд)
            DeselectTower(); 
        }
        else
        {
            // Можна додати виклик UI анімації "Недостатньо грошей"
            Debug.LogWarning("Недостатньо золота для будівництва!");
        }
    }

    private void DeselectTower()
    {
        _towerToBuild = null;
        if (_ghostTower != null)
        {
            Destroy(_ghostTower);
            _ghostTower = null;
        }
    }
}