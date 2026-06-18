// Централізований каталог посилань на основні візуальні ассети, які використовує рантайм-збірка сцен.
using UnityEngine;

[CreateAssetMenu(fileName = "VisualAssetCatalog", menuName = "Echoes of the Void/Visual Asset Catalog")]
public class VisualAssetCatalog : ScriptableObject
{
    public Sprite allyCastle;
    public Sprite enemyCastle;
    public GameObject heroPrefab;
    public Texture2D tinyGround;
    public Texture2D tinyWater;
    public Texture2D tinyTree;
    public Texture2D tinyAllyCastle;
    public Texture2D tinyEnemyCastle;
    public Texture2D tinyHeroSheet;
    public Texture2D tinyLibrary;
    public Texture2D tinyDemonTower;
}
