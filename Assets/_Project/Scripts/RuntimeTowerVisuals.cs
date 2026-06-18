// Підмінює стандартний вигляд відомих башт на підготовлені рантайм-спрайти та іконки.
using System;
using UnityEngine;

public static class RuntimeTowerVisuals
{
    private const float TowerPpu = 64f;
    private const string ArcherVisualName = "Runtime Archer Tower Visual";

    public static string GetVisualKey(GameObject owner, TowerData data)
    {
        if (data == null) return string.Empty;
        return (owner != null ? owner.name : string.Empty) + "|" + data.towerName + "|" + data.towerLevel;
    }

    public static bool IsArcherTower(GameObject owner, TowerData data)
    {
        string ownerName = owner != null ? owner.name : string.Empty;
        string towerName = data != null ? data.towerName : string.Empty;
        string combined = ownerName + " " + towerName;

        if (combined.IndexOf("Archer", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (combined.IndexOf("archer", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (combined.IndexOf("стр", StringComparison.OrdinalIgnoreCase) >= 0) return true;

        return data != null &&
            data.archetype == TowerArchetype.SentinelPylon &&
            data.damageModifier == DamageModifier.Piercing;
    }

    public static SpriteRenderer ApplyIfKnown(GameObject owner, TowerData data)
    {
        if (owner == null || data == null || !IsArcherTower(owner, data)) return null;

        Sprite sprite = GetArcherTowerSprite(data);
        if (sprite == null) return null;

        Transform visual = owner.transform.Find(ArcherVisualName);
        if (visual == null)
        {
            GameObject visualObject = new GameObject(ArcherVisualName);
            visualObject.transform.SetParent(owner.transform, false);
            visual = visualObject.transform;
        }

        visual.localPosition = new Vector3(0f, -0.16f, 0f);
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one * 0.82f;

        SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
        if (visualRenderer == null) visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = sprite;
        visualRenderer.color = Color.white;
        visualRenderer.sortingOrder = 8;
        visualRenderer.enabled = true;

        foreach (SpriteRenderer renderer in owner.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer != visualRenderer) renderer.enabled = false;
        }

        return visualRenderer;
    }

    public static Sprite GetIconSprite(TowerData data)
    {
        return data != null && IsArcherTower(null, data) ? GetArcherTowerSprite(data) : null;
    }

    private static Sprite GetArcherTowerSprite(TowerData data)
    {
        int level = Mathf.Clamp(data != null ? data.towerLevel : 1, 1, 7);
        Texture2D texture = RuntimeSpriteAssetMap.LoadTexture("Visuals/Towers/Archer/Upgrade" + level);
        if (texture == null) texture = RuntimeSpriteAssetMap.LoadTexture("Visuals/Towers/Archer/Idle" + level);
        if (texture == null) texture = RuntimeSpriteAssetMap.LoadTexture("Visuals/Towers/Archer/Idle1");
        if (texture == null) return null;

        float frameWidth = Mathf.Min(70f, texture.width);
        return RuntimeSpriteAssetMap.SpriteFromTopLeft(
            texture,
            0f,
            0f,
            frameWidth,
            texture.height,
            TowerPpu,
            RuntimeSpriteAssetMap.BottomCenter);
    }
}
