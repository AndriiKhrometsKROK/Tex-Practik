// Перемикає анімації очікування, руху й атаки героя та додає магічне підсвічування.
using UnityEngine;

public class HeroVisualAnimator : MonoBehaviour
{
    private const int CellSize = 192;
    private const int Columns = 6;
    private const int TinyCellSize = 100;

    private SpriteRenderer visual;
    private SpriteRenderer aura;
    private Sprite[] idleFrames;
    private Sprite[] moveFrames;
    private Sprite[] attackFrames;
    private Vector3 previousPosition;
    private float attackUntil;

    public void Configure(Texture2D sheet)
    {
        if (visual != null) return;

        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.enabled = false;
        }

        GameObject visualObject = new GameObject("Animated Hero Visual");
        visualObject.transform.SetParent(transform, false);
        visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sortingOrder = 20;

        if (TryConfigureTinyRpgHero())
        {
            previousPosition = transform.position;
            return;
        }

        if (sheet == null)
        {
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer != visual) renderer.enabled = true;
            }
            Destroy(visualObject);
            visual = null;
            return;
        }

        idleFrames = SliceRow(sheet, 0);
        moveFrames = SliceRow(sheet, 1);
        attackFrames = SliceRow(sheet, 2);
        visual.sprite = idleFrames[0];
        previousPosition = transform.position;
    }

    public void PlayAttack(float duration = 0.55f)
    {
        attackUntil = Time.time + Mathf.Max(0.1f, duration);
    }

    private void LateUpdate()
    {
        if (visual == null) return;

        Vector3 delta = transform.position - previousPosition;
        bool moving = delta.sqrMagnitude > 0.00001f;
        bool attacking = Time.time < attackUntil;
        if (Mathf.Abs(delta.x) > 0.001f) visual.flipX = delta.x < 0f;

        Sprite[] frames = attacking ? attackFrames : moving ? moveFrames : idleFrames;
        float speed = attacking ? 12f : moving ? 8f : 4f;
        visual.sprite = frames[Mathf.FloorToInt(Time.time * speed) % frames.Length];
        if (aura != null)
        {
            float pulse = 0.9f + Mathf.Sin(Time.time * 5f) * 0.08f;
            aura.transform.localScale = new Vector3(0.95f * pulse, 0.32f * pulse, 1f);
        }
        previousPosition = transform.position;
    }

    private bool TryConfigureTinyRpgHero()
    {
        Texture2D idle = Resources.Load<Texture2D>("Visuals/Characters/SoldierIdle");
        Texture2D walk = Resources.Load<Texture2D>("Visuals/Characters/SoldierWalk");
        Texture2D attack = Resources.Load<Texture2D>("Visuals/Characters/SoldierAttack");
        if (idle == null || walk == null || attack == null) return false;

        PreparePixelTexture(idle);
        PreparePixelTexture(walk);
        PreparePixelTexture(attack);
        visual.color = new Color(0.72f, 0.58f, 1f, 1f);
        idleFrames = SliceHorizontal(idle, TinyCellSize, TinyCellSize, 72f, new Vector2(0.5f, 0.27f));
        moveFrames = SliceHorizontal(walk, TinyCellSize, TinyCellSize, 72f, new Vector2(0.5f, 0.27f));
        attackFrames = SliceHorizontal(attack, TinyCellSize, TinyCellSize, 72f, new Vector2(0.5f, 0.27f));
        visual.sprite = idleFrames[0];

        GameObject auraObject = new GameObject("Kenam Arcane Glow");
        auraObject.transform.SetParent(transform, false);
        auraObject.transform.localPosition = new Vector3(0f, -0.43f, 0f);
        aura = auraObject.AddComponent<SpriteRenderer>();
        aura.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        aura.color = KenamUiTheme.WithAlpha(KenamUiTheme.Purple, 0.22f);
        aura.sortingOrder = 18;
        aura.transform.localScale = new Vector3(0.95f, 0.32f, 1f);
        return true;
    }

    private static Sprite[] SliceRow(Texture2D sheet, int rowFromTop)
    {
        Sprite[] frames = new Sprite[Columns];
        float y = sheet.height - (rowFromTop + 1) * CellSize;
        for (int i = 0; i < Columns; i++)
        {
            frames[i] = Sprite.Create(
                sheet,
                new Rect(i * CellSize, y, CellSize, CellSize),
                new Vector2(0.5f, 0.27f),
                100f,
                0,
                SpriteMeshType.FullRect);
        }

        return frames;
    }

    private static Sprite[] SliceHorizontal(Texture2D sheet, int frameWidth, int frameHeight, float pixelsPerUnit, Vector2 pivot)
    {
        int count = Mathf.Max(1, sheet.width / frameWidth);
        Sprite[] frames = new Sprite[count];
        float y = Mathf.Max(0f, sheet.height - frameHeight);
        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                sheet,
                new Rect(i * frameWidth, y, frameWidth, frameHeight),
                pivot,
                pixelsPerUnit,
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
