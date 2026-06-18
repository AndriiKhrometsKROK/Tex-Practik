// Створює та обслуговує автоматичні ворожі вежі, які захищають верхню частину карти.
using UnityEngine;

public class EnemyAutoTower : MonoBehaviour
{
    private const string RootName = "Enemy Scripted Towers";

    [SerializeField, Min(0.2f)] private float attackRadius = 2.65f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.45f;
    [SerializeField, Min(1f)] private float damage = 18f;

    private float nextAttackTime;

    public static void EnsureScriptedTowers()
    {
        if (GameObject.Find(RootName) != null) return;

        GameObject root = new GameObject(RootName);
        Vector2[] positions =
        {
            new Vector2(-5.85f, 2.72f),
            new Vector2(-0.85f, 2.95f),
            new Vector2(0.85f, 2.95f),
            new Vector2(5.85f, 2.72f)
        };

        foreach (Vector2 position in positions)
        {
            Create(root.transform, position);
        }
    }

    private static void Create(Transform parent, Vector2 position)
    {
        Sprite white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        Sprite pillar = RuntimeSpriteAssetMap.LoadSprite("Visuals/Curated/props/stone_pillar", 32f, RuntimeSpriteAssetMap.BottomCenter);

        GameObject towerObject = new GameObject("Enemy Auto Tower");
        towerObject.transform.SetParent(parent, false);
        towerObject.transform.localPosition = position;

        SpriteRenderer baseRenderer = towerObject.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = pillar != null ? pillar : white;
        baseRenderer.color = pillar != null ? new Color(0.94f, 0.82f, 0.9f, 1f) : KenamUiTheme.DangerDark;
        baseRenderer.sortingOrder = 7;
        towerObject.transform.localScale = pillar != null ? Vector3.one * 0.24f : new Vector3(0.21f, 0.37f, 1f);

        GameObject core = new GameObject("Enemy Tower Core");
        core.transform.SetParent(towerObject.transform, false);
        core.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        core.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
        SpriteRenderer coreRenderer = core.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = white;
        coreRenderer.color = KenamUiTheme.Purple;
        coreRenderer.sortingOrder = 8;

        towerObject.AddComponent<EnemyAutoTower>();
    }

    private void Update()
    {
        if (Time.time < nextAttackTime) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        AllyController ally = FindNearestAlly();
        if (ally != null)
        {
            FireAt(ally);
            return;
        }

        PlayerHealth player = FindNearestPlayer();
        if (player != null)
        {
            FireAt(player);
        }
    }

    private AllyController FindNearestAlly()
    {
        AllyController nearest = null;
        float nearestDistance = attackRadius * attackRadius;
        CombatRegistry.RemoveInvalidEntries();
        foreach (AllyController ally in CombatRegistry.ActiveAllies)
        {
            if (ally == null || !ally.IsAlive) continue;
            float distance = ((Vector2)ally.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance > nearestDistance) continue;
            nearestDistance = distance;
            nearest = ally;
        }

        return nearest;
    }

    private PlayerHealth FindNearestPlayer()
    {
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player == null || player.IsFallen) return null;
        return Vector2.Distance(player.transform.position, transform.position) <= attackRadius ? player : null;
    }

    private void FireAt(AllyController target)
    {
        target.TakeDamage(new DamagePacket(damage, DamageFamily.Magical, DamageModifier.Dark, gameObject, false, true));
        nextAttackTime = Time.time + attackCooldown;
        GameAudioController.PlaySfx(GameSfxCue.Magic, 0.22f);
    }

    private void FireAt(PlayerHealth target)
    {
        target.TakeDamage(new DamagePacket(damage * 0.8f, DamageFamily.Magical, DamageModifier.Dark, gameObject, false, true));
        nextAttackTime = Time.time + attackCooldown;
        GameAudioController.PlaySfx(GameSfxCue.Magic, 0.22f);
    }
}
