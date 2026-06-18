// Збирає тайловий вигляд хаба, бойової карти й полігону з підготовлених спрайтів та декору.
using UnityEngine;

public static class RuntimeBattlefieldVisuals
{
    private const float TilePpu = 32f;
    private const float PropPpu = 64f;

    public static bool TryBuildTrainingGround(Transform parent)
    {
        Texture2D spring = RuntimeSpriteAssetMap.LoadTexture("Visuals/Farm/TilesetSpring");

        Sprite white = CreateWhiteSprite();
        Sprite grass = LoadCuratedTile("grass/grass_center") ?? (spring != null ? RuntimeSpriteAssetMap.TileFromGrid(spring, 9, 2, 16, TilePpu) : null);
        Sprite road = LoadCuratedTile("road/road_center") ?? (spring != null ? RuntimeSpriteAssetMap.TileFromGrid(spring, 9, 10, 16, TilePpu) : null);
        Sprite stoneTile = LoadCuratedTile("stone/stone_slab_cross");
        if (grass == null) return false;

        CreateWorldPart(parent, "Training Void Frame", white, Vector2.zero, new Vector2(18f, 11f), KenamUiTheme.Void, -32);
        CreateTiledPart(parent, "Training Grass", grass, Vector2.zero, new Vector2(15.8f, 9.4f), KenamUiTheme.WithAlpha(Color.white, 0.92f), -28);
        CreateWorldPart(parent, "Training Calm Shade", white, Vector2.zero, new Vector2(9.2f, 5.4f), KenamUiTheme.WithAlpha(KenamUiTheme.Swamp, 0.16f), -27);

        if (stoneTile != null)
        {
            CreateFoundation(parent, stoneTile, white, Vector2.zero, new Vector2(6.2f, 3.35f), "Training Arena");
            CreateFoundation(parent, stoneTile, white, new Vector2(0f, -3.05f), new Vector2(3.6f, 0.8f), "Training Entry");
        }
        else if (road != null)
        {
            CreateTiledPart(parent, "Training Arena Road", road, Vector2.zero, new Vector2(6.2f, 3.35f), KenamUiTheme.WithAlpha(Color.white, 0.9f), -21);
        }

        AddTrainingDecor(parent);
        return true;
    }

    // Повертаємо false, якщо ключових тайлів немає: тоді PresentationBootstrapper використає простий резервний вигляд.
    public static bool TryBuildFarmBattlefield(Transform parent)
    {
        Texture2D spring = RuntimeSpriteAssetMap.LoadTexture("Visuals/Farm/TilesetSpring");

        Sprite white = CreateWhiteSprite();
        Sprite grass = LoadCuratedTile("grass/grass_center") ?? (spring != null ? RuntimeSpriteAssetMap.TileFromGrid(spring, 9, 2, 16, TilePpu) : null);
        Sprite road = LoadCuratedTile("road/road_center") ?? (spring != null ? RuntimeSpriteAssetMap.TileFromGrid(spring, 9, 10, 16, TilePpu) : null);
        if (grass == null || road == null) return false;
        Sprite stoneTile = LoadCuratedTile("stone/stone_slab_cross");

        CreateWorldPart(parent, "Battle Void Frame", white, Vector2.zero, new Vector2(30f, 16f), KenamUiTheme.Void, -32);
        CreateTiledPart(parent, "Battle Main Grass", grass, Vector2.zero, new Vector2(26f, 14.1f), KenamUiTheme.WithAlpha(Color.white, 0.86f), -28);
        CreateWorldPart(parent, "Battle Central Moss", white, new Vector2(0f, 0.05f), new Vector2(10.4f, 11.3f), KenamUiTheme.WithAlpha(KenamUiTheme.Swamp, 0.2f), -27);

        CreateBattleRoad(parent, road, white, "Enemy Gate Walkway", new Vector2(0f, 4.18f), new Vector2(6.5f, 0.95f), 0f);
        CreateFoundation(parent, stoneTile, white, new Vector2(0f, -4.28f), new Vector2(7.4f, 1.55f), "Ally Castle Foundation");
        CreateFoundation(parent, stoneTile, white, new Vector2(-2.35f, 4.42f), new Vector2(2.3f, 0.9f), "Attack Gate Foundation");
        CreateFoundation(parent, stoneTile, white, new Vector2(2.35f, 4.42f), new Vector2(2.3f, 0.9f), "Defense Gate Foundation");

        CreateBattlePath(parent, road, white, "Attack Road", BattleLaneUtility.GetPath(BattleLane.Upper));
        CreateBattlePath(parent, road, white, "Defense Road", BattleLaneUtility.GetPath(BattleLane.Lower));

        CreateWorldPart(parent, "Attack Front Line", white, new Vector2(-5.25f, 0.08f), new Vector2(0.1f, 9.15f), KenamUiTheme.WithAlpha(KenamUiTheme.Danger, 0.72f), -16);
        CreateWorldPart(parent, "Defense Front Line", white, new Vector2(5.25f, 0.08f), new Vector2(0.1f, 9.15f), KenamUiTheme.WithAlpha(KenamUiTheme.Mint, 0.72f), -16);
        CreateWorldPart(parent, "Enemy Mana Stain Left", white, new Vector2(-2.35f, 4.12f), new Vector2(1.65f, 0.28f), KenamUiTheme.WithAlpha(KenamUiTheme.Purple, 0.36f), -15);
        CreateWorldPart(parent, "Enemy Mana Stain Right", white, new Vector2(2.35f, 4.12f), new Vector2(1.65f, 0.28f), KenamUiTheme.WithAlpha(KenamUiTheme.Purple, 0.36f), -15);

        CreateWorldPart(parent, "Defense Build Field Shadow", white, new Vector2(6.2f, -0.1f), new Vector2(3.95f, 9.25f), KenamUiTheme.WithAlpha(KenamUiTheme.Void, 0.38f), -24);
        CreateTiledPart(parent, "Defense Build Field", grass, new Vector2(6.2f, -0.1f), new Vector2(3.62f, 8.9f), KenamUiTheme.WithAlpha(KenamUiTheme.Swamp, 0.58f), -23);

        for (float y = -3f; y <= 3f; y += 2f)
        {
            CreateBuildPad(parent, stoneTile, white, new Vector2(5f, y));
            CreateBuildPad(parent, stoneTile, white, new Vector2(7f, y));
        }

        AddBattlefieldDecor(parent);
        return true;
    }

    private static void CreateBattleRoad(Transform parent, Sprite road, Sprite white, string name, Vector2 position, Vector2 size, float rotation)
    {
        GameObject shadow = CreateWorldPart(parent, name + " Edge Shadow", white, position + new Vector2(0.08f, -0.08f), size + new Vector2(0.36f, 0.36f), KenamUiTheme.WithAlpha(KenamUiTheme.Void, 0.36f), -22);
        GameObject roadObject = CreateTiledPart(parent, name, road, position, size, KenamUiTheme.WithAlpha(Color.white, 0.96f), -21);
        shadow.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        roadObject.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
    }

    // Кожен відрізок дороги з'єднує дві точки маршруту, а квадратні вузли приховують шви на поворотах.
    private static void CreateBattlePath(Transform parent, Sprite road, Sprite white, string name, Vector2[] points)
    {
        if (points == null || points.Length < 2) return;

        const float roadWidth = 1.08f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 from = points[i];
            Vector2 to = points[i + 1];
            Vector2 delta = to - from;
            float length = delta.magnitude + 0.26f;
            float rotation = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
            CreateBattleRoad(parent, road, white, name + " " + (i + 1), (from + to) * 0.5f, new Vector2(roadWidth, length), rotation);
        }

        for (int i = 0; i < points.Length; i++)
        {
            CreateBattleRoad(parent, road, white, name + " Turn " + i, points[i], new Vector2(roadWidth + 0.18f, roadWidth + 0.18f), 0f);
        }
    }

    private static void AddBattlefieldDecor(Transform parent)
    {
        Sprite tree = LoadCuratedObject("trees/maple_full_grown");
        Sprite youngTree = LoadCuratedObject("trees/maple_young_tree");
        Sprite stump = LoadCuratedObject("trees/maple_stump");
        Sprite yellowTree = LoadCuratedObject("props/yellow_tree");
        Sprite yellowTreeDark = LoadCuratedObject("props/yellow_tree_dark");
        Sprite stoneSmall = LoadCuratedObject("props/stone_small_01");
        Sprite stoneSmallAlt = LoadCuratedObject("props/stone_small_02");
        Sprite stoneMedium = LoadCuratedObject("props/stone_medium_01");
        Sprite stoneLarge = LoadCuratedObject("props/stone_large_01");
        Sprite pillar = LoadCuratedObject("props/stone_pillar");

        CreateSpritePart(parent, "Battle Tree L1", tree, new Vector2(-9.6f, 4.25f), Vector2.one * 0.86f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Battle Tree L2", youngTree, new Vector2(-10.0f, 2.55f), Vector2.one * 0.88f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Battle Tree L3", yellowTreeDark, new Vector2(-9.25f, 0.72f), Vector2.one * 0.58f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Battle Tree L4", tree, new Vector2(-10.05f, -1.35f), Vector2.one * 0.8f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Battle Tree L5", youngTree, new Vector2(-8.95f, -3.72f), Vector2.one * 0.88f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Battle Tree L6", yellowTree, new Vector2(-7.55f, 3.55f), Vector2.one * 0.6f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Battle Tree L7", tree, new Vector2(-11.35f, 3.2f), Vector2.one * 0.76f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -12);
        CreateSpritePart(parent, "Battle Tree L8", youngTree, new Vector2(-11.1f, 0.95f), Vector2.one * 0.84f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Battle Tree L9", yellowTreeDark, new Vector2(-10.85f, -3.1f), Vector2.one * 0.56f, KenamUiTheme.WithAlpha(Color.white, 0.76f), -12);

        CreateSpritePart(parent, "Battle Tree R1", yellowTree, new Vector2(9.55f, 4.2f), Vector2.one * 0.6f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Battle Tree R2", tree, new Vector2(10.05f, 2.3f), Vector2.one * 0.82f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Battle Tree R3", youngTree, new Vector2(9.0f, 0.35f), Vector2.one * 0.88f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Battle Tree R4", yellowTreeDark, new Vector2(10.0f, -1.65f), Vector2.one * 0.58f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Battle Tree R5", tree, new Vector2(8.85f, -3.85f), Vector2.one * 0.8f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Battle Tree R6", youngTree, new Vector2(7.72f, 3.48f), Vector2.one * 0.84f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -12);
        CreateSpritePart(parent, "Battle Tree R7", tree, new Vector2(11.2f, 3.1f), Vector2.one * 0.76f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -12);
        CreateSpritePart(parent, "Battle Tree R8", youngTree, new Vector2(11.15f, 0.95f), Vector2.one * 0.84f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Battle Tree R9", yellowTree, new Vector2(10.95f, -3.05f), Vector2.one * 0.58f, KenamUiTheme.WithAlpha(Color.white, 0.76f), -12);

        CreateSpritePart(parent, "Battle Stump Left", stump, new Vector2(-6.75f, -4.35f), Vector2.one * 0.9f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -11);
        CreateSpritePart(parent, "Battle Stump Right", stump, new Vector2(6.85f, -4.2f), Vector2.one * 0.9f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -11);
        CreateSpritePart(parent, "Battle Stone 1", stoneSmall, new Vector2(-6.45f, 2.25f), Vector2.one * 0.9f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Battle Stone 2", stoneSmallAlt, new Vector2(6.25f, 2.55f), Vector2.one * 0.9f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Battle Stone 3", stoneMedium, new Vector2(-1.3f, 3.75f), Vector2.one * 0.82f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Battle Stone 4", stoneLarge, new Vector2(1.35f, -3.55f), Vector2.one * 0.68f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Battle Stone 5", stoneMedium, new Vector2(-6.95f, 0.65f), Vector2.one * 0.7f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Battle Stone 6", stoneSmallAlt, new Vector2(6.75f, -0.35f), Vector2.one * 0.8f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Battle Stone 7", stoneSmall, new Vector2(-0.1f, -2.9f), Vector2.one * 0.62f, KenamUiTheme.WithAlpha(Color.white, 0.76f), -10);
        CreateSpritePart(parent, "Battle Stone 8", stoneLarge, new Vector2(0.25f, 2.25f), Vector2.one * 0.58f, KenamUiTheme.WithAlpha(Color.white, 0.74f), -10);
        CreateSpritePart(parent, "Battle Pillar Left", pillar, new Vector2(-5.55f, 3.55f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Battle Pillar Right", pillar, new Vector2(5.55f, 3.55f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Battle Pillar Broken", pillar, new Vector2(0f, 4.55f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Battle Pillar Low Left", pillar, new Vector2(-5.95f, -2.85f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.76f), -10);
        CreateSpritePart(parent, "Battle Pillar Low Right", pillar, new Vector2(5.95f, -2.85f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.76f), -10);
    }

    private static void AddTrainingDecor(Transform parent)
    {
        Sprite tree = LoadCuratedObject("trees/maple_full_grown");
        Sprite youngTree = LoadCuratedObject("trees/maple_young_tree");
        Sprite stump = LoadCuratedObject("trees/maple_stump");
        Sprite stoneSmall = LoadCuratedObject("props/stone_small_01");
        Sprite stoneSmallAlt = LoadCuratedObject("props/stone_small_02");
        Sprite stoneMedium = LoadCuratedObject("props/stone_medium_01");
        Sprite stoneLarge = LoadCuratedObject("props/stone_large_01");
        Sprite pillar = LoadCuratedObject("props/stone_pillar");

        CreateSpritePart(parent, "Training Tree Left Top", tree, new Vector2(-6.7f, 3.55f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Training Tree Left Mid", youngTree, new Vector2(-7.2f, 0.8f), Vector2.one * 0.74f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -12);
        CreateSpritePart(parent, "Training Tree Left Bottom", tree, new Vector2(-6.45f, -3.35f), Vector2.one * 0.68f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -12);
        CreateSpritePart(parent, "Training Tree Right Top", youngTree, new Vector2(6.7f, 3.45f), Vector2.one * 0.76f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -12);
        CreateSpritePart(parent, "Training Tree Right Mid", tree, new Vector2(7.1f, 0.45f), Vector2.one * 0.7f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Training Tree Right Bottom", youngTree, new Vector2(6.55f, -3.3f), Vector2.one * 0.74f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -12);

        CreateSpritePart(parent, "Training Stump", stump, new Vector2(-3.55f, -3.6f), Vector2.one * 0.82f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -11);
        CreateSpritePart(parent, "Training Stone 1", stoneSmall, new Vector2(-4.2f, 2.4f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Training Stone 2", stoneSmallAlt, new Vector2(4.15f, 2.25f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Training Stone 3", stoneMedium, new Vector2(-4.85f, -1.7f), Vector2.one * 0.74f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Training Stone 4", stoneLarge, new Vector2(4.7f, -1.95f), Vector2.one * 0.62f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Training Pillar Left", pillar, new Vector2(-2.55f, 2.05f), Vector2.one * 0.68f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Training Pillar Right", pillar, new Vector2(2.55f, 2.05f), Vector2.one * 0.68f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
    }

    public static bool TryBuildFarmHub(Transform parent)
    {
        Texture2D spring = RuntimeSpriteAssetMap.LoadTexture("Visuals/Farm/TilesetSpring");

        Sprite white = CreateWhiteSprite();
        Sprite grass = LoadCuratedTile("grass/grass_center") ?? (spring != null ? RuntimeSpriteAssetMap.TileFromGrid(spring, 9, 2, 16, TilePpu) : null);
        Sprite road = LoadCuratedTile("road/road_center") ?? (spring != null ? RuntimeSpriteAssetMap.TileFromGrid(spring, 9, 10, 16, TilePpu) : null);
        if (grass == null || road == null) return false;

        Sprite stoneTile = LoadCuratedTile("stone/stone_slab_cross");

        CreateWorldPart(parent, "Hub Void Frame", white, Vector2.zero, new Vector2(23.5f, 12f), KenamUiTheme.Void, -30);
        CreateTiledPart(parent, "Hub Main Grass", grass, Vector2.zero, new Vector2(21.4f, 11.2f), KenamUiTheme.WithAlpha(Color.white, 0.93f), -24);
        CreateWorldPart(parent, "Hub Inner Shade", white, Vector2.zero, new Vector2(7.1f, 4.9f), KenamUiTheme.WithAlpha(KenamUiTheme.Swamp, 0.14f), -23);

        CreateFoundation(parent, stoneTile, white, new Vector2(-5.8f, 1.08f), new Vector2(2.75f, 1.2f), "Library Foundation");
        CreateFoundation(parent, stoneTile, white, new Vector2(5.8f, 1.08f), new Vector2(2.75f, 1.2f), "Demon Foundation");
        CreateFoundation(parent, stoneTile, white, new Vector2(0f, 3.08f), new Vector2(3f, 1.1f), "Level Gate Foundation");
        CreateFoundation(parent, stoneTile, white, new Vector2(0f, -3.18f), new Vector2(2.8f, 1.15f), "Training Foundation");

        CreateHubRoad(parent, road, new Vector2(0f, 1.8f), new Vector2(1f, 3.85f), "Road To Level Gate");
        CreateHubRoad(parent, road, new Vector2(-3.05f, 0.62f), new Vector2(5.1f, 1f), "Road To Library");
        CreateHubRoad(parent, road, new Vector2(3.05f, 0.62f), new Vector2(5.1f, 1f), "Road To Demon");
        CreateHubRoad(parent, road, new Vector2(0f, -1.62f), new Vector2(1f, 3.65f), "Road To Training");
        CreateHubRoad(parent, road, new Vector2(0f, 0.62f), new Vector2(1.25f, 1.25f), "Central Road Crossing");

        AddHubDecor(parent);
        return true;
    }

    private static void CreateHubRoad(Transform parent, Sprite road, Vector2 position, Vector2 size, string name)
    {
        CreateTiledPart(parent, name, road, position, size, KenamUiTheme.WithAlpha(Color.white, 0.96f), -20);
    }

    private static void CreateFoundation(Transform parent, Sprite stoneTile, Sprite white, Vector2 position, Vector2 size, string name)
    {
        CreateWorldPart(parent, name + " Shadow", white, position + new Vector2(0.08f, -0.08f), size + new Vector2(0.18f, 0.18f), KenamUiTheme.WithAlpha(KenamUiTheme.Void, 0.28f), -22);
        CreateTiledPart(parent, name, stoneTile, position, size, KenamUiTheme.WithAlpha(Color.white, 0.88f), -21);
    }

    private static void AddHubDecor(Transform parent)
    {
        Sprite tree = LoadCuratedObject("trees/maple_full_grown");
        Sprite youngTree = LoadCuratedObject("trees/maple_young_tree");
        Sprite stump = LoadCuratedObject("trees/maple_stump");
        Sprite stoneSmall = LoadCuratedObject("props/stone_small_01");
        Sprite stoneSmallAlt = LoadCuratedObject("props/stone_small_02");
        Sprite stoneMedium = LoadCuratedObject("props/stone_medium_01");
        Sprite stoneLarge = LoadCuratedObject("props/stone_large_01");
        Sprite pillar = LoadCuratedObject("props/stone_pillar");
        Sprite fenceLeft = LoadCuratedTile("fence/fence_connect_right");
        Sprite fenceSpan = LoadCuratedTile("fence/fence_connect_left_right");
        Sprite fenceRight = LoadCuratedTile("fence/fence_connect_left");

        CreateSpritePart(parent, "Hub Tree Left Top", tree, new Vector2(-8.55f, 3.65f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Hub Tree Left Bottom", youngTree, new Vector2(-8.45f, -3.6f), Vector2.one * 0.82f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Hub Tree Right Top", youngTree, new Vector2(8.55f, 3.62f), Vector2.one * 0.82f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Hub Tree Right Bottom", tree, new Vector2(8.45f, -3.65f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -12);
        CreateSpritePart(parent, "Hub Stump", stump, new Vector2(-4.95f, -3.85f), Vector2.one * 0.85f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -11);
        CreateSpritePart(parent, "Hub Stone Small", stoneSmall, new Vector2(4.95f, -3.8f), Vector2.one * 0.85f, KenamUiTheme.WithAlpha(Color.white, 0.85f), -10);
        CreateSpritePart(parent, "Hub Stone Medium", stoneMedium, new Vector2(2.65f, 3.42f), Vector2.one * 0.86f, KenamUiTheme.WithAlpha(Color.white, 0.85f), -10);
        CreateSpritePart(parent, "Hub Stone Small Alt", stoneSmallAlt, new Vector2(-2.9f, 3.45f), Vector2.one * 0.9f, KenamUiTheme.WithAlpha(Color.white, 0.85f), -10);
        CreateSpritePart(parent, "Hub Stone Large", stoneLarge, new Vector2(3.85f, -4.2f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Young Tree Mid Left", youngTree, new Vector2(-8.7f, 0.8f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Hub Young Tree Mid Right", youngTree, new Vector2(8.75f, 0.65f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Hub Tree Left Center", youngTree, new Vector2(-6.95f, -0.85f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.76f), -12);
        CreateSpritePart(parent, "Hub Tree Right Center", tree, new Vector2(6.95f, -0.95f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.76f), -12);
        CreateSpritePart(parent, "Hub Grove Left A", tree, new Vector2(-9.25f, 1.9f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Hub Grove Left B", youngTree, new Vector2(-7.55f, 2.65f), Vector2.one * 0.7f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Hub Grove Right A", tree, new Vector2(9.2f, 1.75f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Hub Grove Right B", youngTree, new Vector2(7.55f, 2.72f), Vector2.one * 0.7f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -12);
        CreateSpritePart(parent, "Hub Grove Bottom A", youngTree, new Vector2(-7.25f, -2.15f), Vector2.one * 0.68f, KenamUiTheme.WithAlpha(Color.white, 0.77f), -12);
        CreateSpritePart(parent, "Hub Grove Bottom B", tree, new Vector2(7.35f, -2.2f), Vector2.one * 0.62f, KenamUiTheme.WithAlpha(Color.white, 0.77f), -12);
        CreateSpritePart(parent, "Hub Stone Near Library", stoneSmall, new Vector2(-4.12f, 2.58f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Stone Near Demon", stoneSmallAlt, new Vector2(4.1f, 2.6f), Vector2.one * 0.78f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Stone Near Training", stoneMedium, new Vector2(-1.65f, -4.18f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Stone Library Path", stoneSmallAlt, new Vector2(-3.35f, 0.02f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Stone Demon Path", stoneSmall, new Vector2(3.35f, 0.02f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Stone Gate Path", stoneMedium, new Vector2(-1.1f, 2.15f), Vector2.one * 0.58f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -10);
        CreateSpritePart(parent, "Hub Stone Training Path", stoneLarge, new Vector2(1.1f, -2.18f), Vector2.one * 0.54f, KenamUiTheme.WithAlpha(Color.white, 0.8f), -10);
        CreateSpritePart(parent, "Hub Pillar Left", pillar, new Vector2(-2.05f, 2.35f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Pillar Right", pillar, new Vector2(2.05f, 2.35f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -10);
        CreateSpritePart(parent, "Hub Pillar Training", pillar, new Vector2(1.55f, -3.96f), Vector2.one * 0.66f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Hub Pillar Library", pillar, new Vector2(-7.25f, 0.36f), Vector2.one * 0.62f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);
        CreateSpritePart(parent, "Hub Pillar Demon", pillar, new Vector2(7.25f, 0.36f), Vector2.one * 0.62f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -10);

        CreateFenceLine(parent, fenceLeft, fenceSpan, fenceRight, new Vector2(-9.0f, 2.45f), 5);
        CreateFenceLine(parent, fenceLeft, fenceSpan, fenceRight, new Vector2(6.85f, -3.95f), 5);
    }

    private static void AddFarmDecor(Transform parent)
    {
        Texture2D chest = RuntimeSpriteAssetMap.LoadTexture("Visuals/Farm/Chest");

        Sprite bigMaple = LoadCuratedObject("trees/maple_full_grown");
        Sprite youngMaple = LoadCuratedObject("trees/maple_young_tree");
        Sprite stump = LoadCuratedObject("trees/maple_stump");
        CreateSpritePart(parent, "Maple Tree Big", bigMaple, new Vector2(-8.35f, 3.3f), Vector2.one * 0.92f, Color.white, -12);
        CreateSpritePart(parent, "Maple Tree", youngMaple, new Vector2(-6.15f, 1.35f), Vector2.one * 0.95f, Color.white, -12);
        CreateSpritePart(parent, "Maple Tree Big", bigMaple, new Vector2(8.35f, 3.25f), Vector2.one * 0.92f, Color.white, -12);
        CreateSpritePart(parent, "Maple Tree", youngMaple, new Vector2(6.25f, -4.05f), Vector2.one * 0.95f, Color.white, -12);
        CreateSpritePart(parent, "Old Stump", stump, new Vector2(-6.8f, -4.05f), Vector2.one * 0.95f, Color.white, -11);

        Sprite houseBack = LoadCuratedObject("buildings/house_back");
        Sprite houseFront = LoadCuratedObject("buildings/house_front_with_parts");
        CreateSpritePart(parent, "Left House", houseFront, new Vector2(-8.35f, -4.18f), Vector2.one * 0.76f, KenamUiTheme.WithAlpha(Color.white, 0.82f), -11);
        CreateSpritePart(parent, "Right House", houseBack, new Vector2(8.45f, 4.05f), Vector2.one * 0.72f, KenamUiTheme.WithAlpha(Color.white, 0.78f), -11);

        Sprite fenceLeft = LoadCuratedTile("fence/fence_connect_right");
        Sprite fenceSpan = LoadCuratedTile("fence/fence_connect_left_right");
        Sprite fenceRight = LoadCuratedTile("fence/fence_connect_left");
        CreateFenceLine(parent, fenceLeft, fenceSpan, fenceRight, new Vector2(-8.3f, 2.35f), 5);
        CreateFenceLine(parent, fenceLeft, fenceSpan, fenceRight, new Vector2(6.65f, -3.62f), 5);

        if (chest != null)
        {
            Sprite chestSprite = RuntimeSpriteAssetMap.FullSprite(chest, TilePpu, RuntimeSpriteAssetMap.BottomCenter);
            CreateSpritePart(parent, "Supply Chest", chestSprite, new Vector2(4.35f, -3.95f), Vector2.one * 0.75f, Color.white, -10);
        }
    }

    private static void AddAtlasProps(Transform parent, bool hub)
    {
        Sprite yellowTree = LoadCuratedObject("props/yellow_tree");
        Sprite yellowTreeDark = LoadCuratedObject("props/yellow_tree_dark");
        Sprite palm = LoadCuratedObject("props/small_palm_tree");
        Sprite stoneSmall = LoadCuratedObject("props/stone_small_01");
        Sprite stoneLarge = LoadCuratedObject("props/stone_large_01");
        Sprite pillar = LoadCuratedObject("props/stone_pillar");
        Sprite pond = LoadCuratedObject("props/artificial_stone_pond");

        CreateSpritePart(parent, "Yellow Tree", yellowTree, hub ? new Vector2(6.5f, 2.1f) : new Vector2(-7.9f, -1.4f), Vector2.one * 0.72f, Color.white, -9);
        CreateSpritePart(parent, "Yellow Tree Dark", yellowTreeDark, hub ? new Vector2(7.85f, -2.25f) : new Vector2(7.95f, 1.65f), Vector2.one * 0.68f, Color.white, -9);
        CreateSpritePart(parent, "Palm Tree", palm, hub ? new Vector2(-6.7f, 2.95f) : new Vector2(-8.55f, 0.2f), Vector2.one * 0.82f, Color.white, -9);
        CreateSpritePart(parent, "Stone Small", stoneSmall, hub ? new Vector2(4.25f, -3.55f) : new Vector2(-4.85f, -4.25f), Vector2.one * 1.05f, Color.white, -8);
        CreateSpritePart(parent, "Stone Large", stoneLarge, hub ? new Vector2(-4.3f, -3.65f) : new Vector2(7.92f, 3.85f), Vector2.one * 0.95f, Color.white, -8);
        CreateSpritePart(parent, "Stone Pillar", pillar, hub ? new Vector2(1.45f, 2.25f) : new Vector2(1.55f, -3.6f), Vector2.one * 0.8f, Color.white, -9);
        CreateSpritePart(parent, "Stone Pond", pond, hub ? new Vector2(-7.8f, -2.8f) : new Vector2(5.85f, -4f), Vector2.one * 0.62f, Color.white, -9);
    }

    private static void AddVoidCrystals(Transform parent, Sprite white)
    {
        Vector2[] crystals =
        {
            new Vector2(-8.9f, 0.1f), new Vector2(-6.7f, -4.45f), new Vector2(8.95f, -0.55f), new Vector2(6.9f, 4.55f)
        };

        foreach (Vector2 crystal in crystals)
        {
            GameObject glow = CreateWorldPart(parent, "Void Crystal Glow", white, crystal, new Vector2(0.36f, 0.36f), KenamUiTheme.WithAlpha(KenamUiTheme.Purple, 0.28f), -16);
            glow.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject core = CreateWorldPart(parent, "Void Crystal Core", white, crystal, new Vector2(0.14f, 0.32f), KenamUiTheme.Mint, -15);
            core.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
    }

    private static void CreateRoad(Transform parent, Sprite edge, Sprite road, Vector2 position, Vector2 size, float rotation)
    {
        GameObject edgeObject = CreateTiledPart(parent, "Road Edge", edge, position, size + new Vector2(0.28f, 0.28f), KenamUiTheme.WithAlpha(Color.white, 0.84f), -20);
        GameObject roadObject = CreateTiledPart(parent, "Road", road, position, size, KenamUiTheme.WithAlpha(Color.white, 0.95f), -19);
        edgeObject.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        roadObject.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static void CreateBuildPad(Transform parent, Sprite padSprite, Sprite white, Vector2 position)
    {
        GameObject shadow = CreateWorldPart(parent, "Build Pad Shadow", white, position + new Vector2(0.08f, -0.08f), new Vector2(0.74f, 0.74f), KenamUiTheme.WithAlpha(KenamUiTheme.Void, 0.52f), -14);
        shadow.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        if (padSprite != null)
        {
            CreateSpritePart(parent, "Stone Build Pad", padSprite, position, Vector2.one * 1.25f, KenamUiTheme.WithAlpha(Color.white, 0.72f), -13);
            return;
        }

        GameObject pad = CreateWorldPart(parent, "Build Pad", white, position, new Vector2(0.58f, 0.58f), KenamUiTheme.WithAlpha(KenamUiTheme.Mint, 0.32f), -13);
        pad.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void CreateFenceLine(Transform parent, Sprite left, Sprite span, Sprite right, Vector2 start, int spans)
    {
        if (left == null || span == null || right == null) return;

        float step = 0.5f;
        CreateSpritePart(parent, "Fence Left", left, start, Vector2.one, Color.white, -10);
        for (int i = 1; i <= spans; i++)
        {
            CreateSpritePart(parent, "Fence Span", span, start + new Vector2(i * step, 0f), Vector2.one, Color.white, -10);
        }
        CreateSpritePart(parent, "Fence Right", right, start + new Vector2((spans + 1) * step, 0f), Vector2.one, Color.white, -10);
    }

    private static Sprite LoadCuratedTile(string relativePath)
    {
        return RuntimeSpriteAssetMap.LoadSprite("Visuals/Curated/" + relativePath, TilePpu, RuntimeSpriteAssetMap.Center);
    }

    private static Sprite LoadCuratedObject(string relativePath)
    {
        return RuntimeSpriteAssetMap.LoadSprite("Visuals/Curated/" + relativePath, TilePpu, RuntimeSpriteAssetMap.BottomCenter);
    }

    // Tiled-режим повторює тайл без розтягування пікселів і підходить для трави, дороги та фундаментів.
    private static GameObject CreateTiledPart(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        if (sprite == null) return null;

        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;
        renderer.sortingOrder = sortingOrder;
        return part;
    }

    private static GameObject CreateWorldPart(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 scale, Color color, int sortingOrder)
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

    private static GameObject CreateSpritePart(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 scale, Color color, int sortingOrder)
    {
        if (sprite == null) return null;

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

    private static Sprite CreateWhiteSprite()
    {
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
