param(
    [string]$SlicesRoot = "AssetSlices",
    [string]$OutputRoot = "Assets/Resources/Visuals/Curated"
)

$ErrorActionPreference = "Stop"

function New-CleanDirectory {
    param([string]$Path)
    if (Test-Path $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Add-Map {
    param(
        [System.Collections.Generic.List[object]]$List,
        [string]$Group,
        [int]$Index,
        [string]$Category,
        [string]$Name,
        [string]$Description = ""
    )

    $List.Add([PSCustomObject]@{
        Group = $Group
        Index = $Index
        Category = $Category
        Name = $Name
        Description = $Description
    })
}

$map = New-Object System.Collections.Generic.List[object]

Add-Map $map "ancient_ruins_props" 1 "props" "yellow_tree" "Yellow tree"
Add-Map $map "ancient_ruins_props" 2 "props" "yellow_tree_dark" "Darker yellow tree"
Add-Map $map "ancient_ruins_props" 3 "props" "small_palm_tree" "Small palm tree"
Add-Map $map "ancient_ruins_props" 5 "props" "stone_small_01" "Stone"
Add-Map $map "ancient_ruins_props" 6 "props" "stone_medium_01" "Medium stone"
Add-Map $map "ancient_ruins_props" 7 "props" "stone_small_02" "Small stone"
Add-Map $map "ancient_ruins_props" 8 "props" "stone_large_01" "Large stone"
Add-Map $map "ancient_ruins_props" 9 "props" "stone_pillar" "Stone pillar"
Add-Map $map "ancient_ruins_props" 10 "props" "stone_large_02" "Bigger stone"
Add-Map $map "ancient_ruins_props" 18 "props" "artificial_stone_pond" "Artificial stone pond"

Add-Map $map "farm_fence_16px" 5 "fence" "fence_connect_right" "Fence with a connection on the right"
Add-Map $map "farm_fence_16px" 6 "fence" "fence_connect_left_right" "Fence with connections on the right and left"
Add-Map $map "farm_fence_16px" 7 "fence" "fence_connect_left" "Fence with a connection on the left"
Add-Map $map "farm_fence_16px" 10 "fence" "fence_post" "Fence without connections"

Add-Map $map "farm_house_objects" 0 "buildings" "house_back" "House facing backwards"
Add-Map $map "farm_house_objects" 1 "buildings" "house_front_with_parts" "House facing forwards with extra parts"

Add-Map $map "farm_maple_objects" 0 "trees" "maple_full_grown" "Full-grown tree"
Add-Map $map "farm_maple_objects" 1 "trees" "maple_young_tree" "Young tree"
Add-Map $map "farm_maple_objects" 2 "trees" "maple_sprout" "Tree sprout"
Add-Map $map "farm_maple_objects" 3 "trees" "maple_stump" "Stump"
Add-Map $map "farm_maple_objects" 4 "terrain" "dirt_pile" "Pile of dirt"

Add-Map $map "terrain_stone_16px_debug" 0 "stone" "stone_corner_top_left" "Stone corner top left"
Add-Map $map "terrain_stone_16px_debug" 1 "stone" "stone_corner_top_right" "Stone corner top right"
Add-Map $map "terrain_stone_16px_debug" 5 "stone" "stone_corner_bottom_left" "Stone corner bottom left"
Add-Map $map "terrain_stone_16px_debug" 6 "stone" "stone_corner_bottom_right" "Stone corner bottom right"
Add-Map $map "terrain_stone_16px_debug" 10 "stone" "stone_slab_circle" "Stone slab with circular pattern"
Add-Map $map "terrain_stone_16px_debug" 15 "stone" "stone_slab_cross" "Stone slab with cross pattern"

Add-Map $map "tileset_spring_16px" 8 "grass" "grass_corner_top_left" "Grass corner top left"
Add-Map $map "tileset_spring_16px" 10 "grass" "grass_border_top" "Grass top border"
Add-Map $map "tileset_spring_16px" 11 "grass" "grass_corner_top_right" "Grass corner top right"
Add-Map $map "tileset_spring_16px" 20 "grass" "grass_border_left" "Grass left border"
Add-Map $map "tileset_spring_16px" 32 "grass" "grass_center" "Just grass"
Add-Map $map "tileset_spring_16px" 34 "grass" "grass_border_right" "Grass right border"
Add-Map $map "tileset_spring_16px" 43 "grass" "grass_corner_bottom_left" "Grass corner bottom left"
Add-Map $map "tileset_spring_16px" 44 "grass" "grass_border_bottom" "Grass bottom border"
Add-Map $map "tileset_spring_16px" 46 "grass" "grass_corner_bottom_right" "Grass corner bottom right"

Add-Map $map "tileset_spring_16px" 101 "road" "road_corner_top_left" "Road corner upper left"
Add-Map $map "tileset_spring_16px" 103 "road" "road_border_top" "Road top boundary"
Add-Map $map "tileset_spring_16px" 104 "road" "road_corner_top_right" "Road corner upper right"
Add-Map $map "tileset_spring_16px" 113 "road" "road_border_left" "Road left boundary"
Add-Map $map "tileset_spring_16px" 125 "road" "road_center" "Road only"
Add-Map $map "tileset_spring_16px" 127 "road" "road_border_right" "Road right boundary"
Add-Map $map "tileset_spring_16px" 136 "road" "road_corner_bottom_left" "Road corner lower left"
Add-Map $map "tileset_spring_16px" 137 "road" "road_border_bottom" "Road bottom boundary"
Add-Map $map "tileset_spring_16px" 139 "road" "road_corner_bottom_right" "Road corner lower right"

$uiIconNames = @(
    "play", "close", "settings_gear", "knight_helmet_round", "info", "back", "pause", "check",
    "potion_red", "potion_blue", "potion_green", "gold_shop_bag", "crystal_bag", "medicine", "barrel_square", "bread_square",
    "sword_square", "axe_square", "lion_shield_square", "shield", "armor_glove", "crossbow", "helmet_square", "chainmail_square",
    "glowing_crown", "crossed_swords", "magic_staff", "red_assassin", "orthodox_cross", "medal", "tree_icon", "paper_square",
    "compass", "spyglass", "map_icon", "gold_flag", "paper_with_sword", "mirror", "enchanted_mirror", "city",
    "military_camp", "forge", "farm", "mine", "kingdom", "tower", "port", "small_stone_tower",
    "chat", "mail", "contacts", "dragon", "cup", "network", "gift", "gold_transfer",
    "effect_fire", "effect_ice", "effect_lightning", "effect_earth", "effect_healing", "skip", "effect_defense", "invoke_like"
)

for ($i = 0; $i -lt $uiIconNames.Count; $i++) {
    Add-Map $map "ui_icons1_32px" $i "ui_icons" $uiIconNames[$i] ""
}

$mainMenuNames = @(
    "play", "campaign", "new_campaign", "load_game",
    "multiplayer", "armory", "troops", "kingdom",
    "map", "quests", "shop", "settings",
    "exit", "fight", "settings_text", "start"
)

for ($i = 0; $i -lt $mainMenuNames.Count; $i++) {
    Add-Map $map "ui_main_menu_64x32" $i "ui_main_menu" $mainMenuNames[$i] ""
}

New-CleanDirectory $OutputRoot

$copied = New-Object System.Collections.Generic.List[object]
$missing = New-Object System.Collections.Generic.List[object]

foreach ($entry in $map) {
    $groupDir = Join-Path $SlicesRoot $entry.Group
    $pattern = "{0:D3}__*.png" -f $entry.Index
    $source = Get-ChildItem -LiteralPath $groupDir -Filter $pattern -File -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($source -eq $null) {
        $missing.Add($entry)
        continue
    }

    $categoryDir = Join-Path $OutputRoot $entry.Category
    New-Item -ItemType Directory -Force -Path $categoryDir | Out-Null
    $destination = Join-Path $categoryDir ($entry.Name + ".png")
    Copy-Item -LiteralPath $source.FullName -Destination $destination -Force

    $copied.Add([PSCustomObject]@{
        Group = $entry.Group
        Index = $entry.Index
        Category = $entry.Category
        Name = $entry.Name
        Source = $source.FullName
        Output = $destination
        Description = $entry.Description
    })
}

$manifestPath = Join-Path $OutputRoot "curated_manifest.csv"
$copied | Export-Csv -Path $manifestPath -NoTypeInformation -Encoding UTF8

if ($missing.Count -gt 0) {
    $missingPath = Join-Path $OutputRoot "missing_manifest.csv"
    $missing | Export-Csv -Path $missingPath -NoTypeInformation -Encoding UTF8
    Write-Warning "Missing $($missing.Count) mapped slices. See $missingPath"
}

Write-Host "Copied $($copied.Count) curated sprites to $OutputRoot"
Write-Host "Manifest: $manifestPath"
