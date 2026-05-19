using UnityEngine;

public class TowerShopButton : MonoBehaviour
{
    [Header("Дані вежі для цієї кнопки")]
    public TowerData towerData;

    public void OnBuyButtonClicked()
    {
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.SelectTowerToBuild(towerData);
        }
        else
        {
            Debug.LogError("На сцені немає об'єкта з TowerPlacementManager!");
        }
    }
}