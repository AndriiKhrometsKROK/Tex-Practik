using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TinySwordsVisualSetup
{
    private const string CatalogPath = "Assets/Resources/VisualAssetCatalog.asset";
    private const string TinyRoot = "Assets/_Project/art/Tiny Swords/Tiny Swords (Update 010)/";

    static TinySwordsVisualSetup()
    {
        EditorApplication.delayCall += Configure;
    }

    private static void Configure()
    {
        VisualAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<VisualAssetCatalog>(CatalogPath);
        if (catalog == null) return;

        catalog.tinyGround = LoadTexture("Terrain/Ground/Tilemap_Flat.png");
        catalog.tinyWater = LoadTexture("Terrain/Water/Water.png");
        catalog.tinyTree = LoadTexture("Resources/Trees/Tree.png");
        catalog.tinyAllyCastle = LoadTexture("Factions/Knights/Buildings/Castle/Castle_Blue.png");
        catalog.tinyEnemyCastle = LoadTexture("Factions/Knights/Buildings/Castle/Castle_Red.png");

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static Texture2D LoadTexture(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TinyRoot + relativePath);
    }
}
