// Створює список доступних башт у панелі оборонних споруд.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerShopPanel : MonoBehaviour
{
    [SerializeField] private TowerData[] towers;
    [SerializeField] private Button buttonPrefab;

    private void Start()
    {
        BuildButtons();
    }

    public void BuildButtons()
    {
        if (buttonPrefab == null || towers == null) return;

        foreach (TowerData tower in towers)
        {
            if (tower == null) continue;

            Button button = Instantiate(buttonPrefab, transform);
            button.name = tower.towerName + " Button";
            button.onClick.AddListener(() => TowerPlacementManager.Instance.SelectTowerToBuild(tower));

            TowerShopButton shopButton = button.GetComponent<TowerShopButton>();
            if (shopButton != null)
            {
                shopButton.towerData = tower;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{tower.towerName}\n{tower.cost}";
            }

            Image icon = button.transform.Find("Icon")?.GetComponent<Image>();
            Sprite sprite = GetTowerSprite(tower);
            if (icon != null && sprite != null)
            {
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }
        }

        buttonPrefab.gameObject.SetActive(false);
    }

    private static Sprite GetTowerSprite(TowerData tower)
    {
        if (tower == null || tower.towerPrefab == null) return null;

        SpriteRenderer renderer = tower.towerPrefab.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }
}
