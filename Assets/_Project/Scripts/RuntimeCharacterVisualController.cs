// Завантажує аркуші персонажів, нарізає кадри та перемикає анімації руху, очікування й атаки.
using UnityEngine;

public enum RuntimeCharacterSkin
{
    Kenam,
    AllySoldier,
    EnemyOrc
}

public class RuntimeCharacterVisualController : MonoBehaviour
{
    private const int FrameSize = 100;

    private SpriteRenderer visual;
    private Sprite[] idleFrames;
    private Sprite[] walkFrames;
    private Sprite[] attackFrames;
    private Vector3 previousPosition;
    private int sortingOrder;

    public void Configure(RuntimeCharacterSkin skin, int targetSortingOrder)
    {
        sortingOrder = targetSortingOrder;
        Texture2D idle = null;
        Texture2D walk = null;
        Texture2D attack = null;
        Color tint = Color.white;

        switch (skin)
        {
            case RuntimeCharacterSkin.EnemyOrc:
                idle = Resources.Load<Texture2D>("Visuals/Characters/OrcIdle");
                walk = Resources.Load<Texture2D>("Visuals/Characters/OrcWalk");
                attack = Resources.Load<Texture2D>("Visuals/Characters/OrcAttack");
                tint = new Color(0.92f, 1f, 0.82f, 1f);
                break;
            case RuntimeCharacterSkin.Kenam:
                idle = Resources.Load<Texture2D>("Visuals/Characters/SoldierIdle");
                walk = Resources.Load<Texture2D>("Visuals/Characters/SoldierWalk");
                attack = Resources.Load<Texture2D>("Visuals/Characters/SoldierAttack");
                tint = new Color(0.72f, 0.58f, 1f, 1f);
                break;
            default:
                idle = Resources.Load<Texture2D>("Visuals/Characters/SoldierIdle");
                walk = Resources.Load<Texture2D>("Visuals/Characters/SoldierWalk");
                attack = Resources.Load<Texture2D>("Visuals/Characters/SoldierAttack");
                break;
        }

        if (idle == null || walk == null || attack == null) return;

        PreparePixelTexture(idle);
        PreparePixelTexture(walk);
        PreparePixelTexture(attack);
        HideLegacyRenderers();
        EnsureRenderer();

        visual.color = tint;
        visual.sortingOrder = sortingOrder;
        idleFrames = Slice(idle);
        walkFrames = Slice(walk);
        attackFrames = Slice(attack);
        visual.sprite = idleFrames[0];
        previousPosition = transform.position;
    }

    public void SetSortingOrder(int targetSortingOrder)
    {
        sortingOrder = targetSortingOrder;
        if (visual != null) visual.sortingOrder = sortingOrder;
    }

    private void LateUpdate()
    {
        if (visual == null || idleFrames == null || walkFrames == null || attackFrames == null) return;

        Vector3 delta = transform.position - previousPosition;
        bool moving = delta.sqrMagnitude > 0.00001f;
        bool attacking = IsAttacking();
        if (Mathf.Abs(delta.x) > 0.001f) visual.flipX = delta.x < 0f;

        Sprite[] frames = attacking ? attackFrames : moving ? walkFrames : idleFrames;
        float speed = attacking ? 12f : moving ? 8f : 4f;
        visual.sprite = frames[Mathf.FloorToInt(Time.time * speed) % frames.Length];
        previousPosition = transform.position;
    }

    private bool IsAttacking()
    {
        EnemyAI enemy = GetComponent<EnemyAI>();
        if (enemy != null) return enemy.BrainState == UnitBrainState.Attack;

        AllyController ally = GetComponent<AllyController>();
        if (ally != null) return ally.BrainState == UnitBrainState.Attack;

        return false;
    }

    private void HideLegacyRenderers()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.gameObject.name == "Runtime Character Visual") continue;
            renderer.enabled = false;
        }
    }

    private void EnsureRenderer()
    {
        if (visual != null) return;

        GameObject visualObject = new GameObject("Runtime Character Visual");
        visualObject.transform.SetParent(transform, false);
        visual = visualObject.AddComponent<SpriteRenderer>();
    }

    private static Sprite[] Slice(Texture2D sheet)
    {
        int count = Mathf.Max(1, sheet.width / FrameSize);
        Sprite[] frames = new Sprite[count];
        float y = Mathf.Max(0f, sheet.height - FrameSize);
        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                sheet,
                new Rect(i * FrameSize, y, FrameSize, FrameSize),
                new Vector2(0.5f, 0.27f),
                72f,
                0,
                SpriteMeshType.FullRect);
        }

        return frames;
    }

    private static void PreparePixelTexture(Texture2D texture)
    {
        if (texture == null) return;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
    }
}

public static class RuntimeCharacterVisuals
{
    public static void Apply(GameObject target, RuntimeCharacterSkin skin, int sortingOrder)
    {
        if (target == null) return;

        RuntimeCharacterVisualController visual = target.GetComponent<RuntimeCharacterVisualController>();
        if (visual == null) visual = target.AddComponent<RuntimeCharacterVisualController>();
        visual.Configure(skin, sortingOrder);
        visual.SetSortingOrder(sortingOrder);
    }
}
