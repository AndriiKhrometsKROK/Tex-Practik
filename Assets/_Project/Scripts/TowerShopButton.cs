// Зв'язує окрему кнопку магазину з TowerData та передає вибрану башту менеджеру будівництва.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerShopButton : MonoBehaviour
{
    [Header("Tower Data")]
    public TowerData towerData;

    [Header("Optional UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;

    private void OnEnable()
    {
        Refresh();
    }

    public void OnBuyButtonClicked()
    {
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.SelectTowerToBuild(towerData);
        }
        else
        {
            Debug.LogError("No TowerPlacementManager found on the scene.");
        }
    }

    public void Refresh()
    {
        if (towerData == null) return;

        if (nameText != null)
        {
            nameText.text = towerData.towerName;
        }

        if (costText != null)
        {
            costText.text = towerData.cost.ToString();
        }

        Sprite sprite = GetTowerSprite(towerData);
        if (iconImage != null && sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
        }
    }

    private static Sprite GetTowerSprite(TowerData data)
    {
        if (data == null || data.towerPrefab == null) return null;

        SpriteRenderer renderer = data.towerPrefab.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }
}
