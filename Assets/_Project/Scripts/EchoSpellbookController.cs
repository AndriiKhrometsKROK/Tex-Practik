// Збирає комбінації трьох відлунь і виконує відповідні заклинання Кенама.
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class EchoSpellbookController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float invokeCooldown = 3f;
    [SerializeField, Min(0f)] private float manaCost = 35f;
    [SerializeField, Min(0.1f)] private float spellRadius = 3.2f;

    private readonly Queue<char> echoes = new Queue<char>(3);
    private readonly Dictionary<string, float> spellReadyAt = new Dictionary<string, float>();
    private HeroStats stats;
    private HeroInventory inventory;
    private TextMeshProUGUI echoText;
    private TextMeshProUGUI spellText;
    private int castIndex;
    private int darkCastIndex;
    private string greyManaFocus;
    private int greyFocusMastery;

    public string CurrentEchoes => new string(echoes.ToArray());
    public string GreyManaFocus => greyManaFocus;
    public float GreyManaFocusMultiplier => string.IsNullOrEmpty(greyManaFocus)
        ? 1f
        : 1.35f + greyFocusMastery * 0.05f;

    private void Awake()
    {
        stats = GetComponent<HeroStats>() ?? gameObject.AddComponent<HeroStats>();
        inventory = GetComponent<HeroInventory>() ?? gameObject.AddComponent<HeroInventory>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) AddEcho('й');
        if (Input.GetKeyDown(KeyCode.W)) AddEcho('ц');
        if (Input.GetKeyDown(KeyCode.E)) AddEcho('у');
        if (Input.GetKeyDown(KeyCode.R)) Invoke();
        if (Input.GetKeyDown(KeyCode.G)) FocusGreyMana();
    }

    public void ConfigureUi(TextMeshProUGUI targetEchoText, TextMeshProUGUI targetSpellText)
    {
        echoText = targetEchoText;
        spellText = targetSpellText;
        RefreshUi("Зберіть три Еха: Q / W / E, виклик: R");
    }

    public void ResetCooldowns()
    {
        spellReadyAt.Clear();
        RefreshUi("Перезарядки Еха скинуто.");
    }

    // Зберігаємо лише три останні символи: саме вони утворюють ключ заклинання.
    private void AddEcho(char echo)
    {
        if (echoes.Count >= 3) echoes.Dequeue();
        echoes.Enqueue(echo);
        RefreshUi("R — створити заклинання");
    }

    // Перед кастом нормалізуємо комбінацію, перевіряємо ману й кулдаун, а потім делегуємо конкретному заклинанню.
    private void Invoke()
    {
        if (stats.EchoSilenced)
        {
            GameplayNotificationController.Show("Ехо Духів тимчасово заблоковане.");
            return;
        }
        if (echoes.Count < 3)
        {
            GameplayNotificationController.Show("Потрібні три Еха.");
            return;
        }

        string key = GetCanonicalKey();
        if (spellReadyAt.TryGetValue(key, out float readyAt) && Time.time < readyAt)
        {
            GameplayNotificationController.Show($"Ехо відновиться через {readyAt - Time.time:0.0} с");
            return;
        }
        if (!stats.SpendMana(manaCost))
        {
            GameplayNotificationController.Show("Недостатньо мани.");
            return;
        }

        castIndex++;
        if (key.Contains("у")) darkCastIndex++;
        bool darkProc = key.Contains("у") && darkCastIndex % 3 == 0;
        float power = stats.SpellPower * inventory.SpellDamageMultiplier * (darkProc ? 2f : 1f);
        if (stats.GreyManaUnlocked && key == greyManaFocus)
        {
            power *= GreyManaFocusMultiplier;
        }
        power = inventory.RollDeterministicMagicCritical(power, castIndex);
        bool cast = CastSpell(key, power);
        if (!cast)
        {
            stats.RestoreMana(manaCost);
            return;
        }

        GameAudioController.PlaySfx(GameSfxCue.Magic, 0.75f);
        spellReadyAt[key] = darkProc
            ? Time.time
            : Time.time + invokeCooldown * inventory.CooldownMultiplier;
        if (stats.GreyManaUnlocked && key == greyManaFocus)
        {
            greyFocusMastery = Mathf.Min(5, greyFocusMastery + 1);
        }
        echoes.Clear();
        RefreshUi(darkProc ? "Темне Ехо: подвійний ефект і миттєва перезарядка." : GetSpellName(key));
    }

    private void FocusGreyMana()
    {
        if (!stats.GreyManaUnlocked)
        {
            GameplayNotificationController.Show("Демон блокує Серу ману.");
            return;
        }
        if (echoes.Count < 3)
        {
            GameplayNotificationController.Show(
                $"Сіра мана імітує {stats.MimickedFamily}. Зберіть три Еха, щоб обрати посилене заклинання.");
            return;
        }

        greyManaFocus = GetCanonicalKey();
        greyFocusMastery = 0;
        GameAudioController.PlaySfx(GameSfxCue.MysteryReveal, 0.75f);
        GameplayNotificationController.Show(
            $"Сіра мана сфокусована на «{GetSpellName(greyManaFocus)}». Сила: +35%.");
        RefreshUi($"Фокус Сірої мани: {GetSpellName(greyManaFocus)}");
    }

    // Таблиця диспетчеризації відокремлює комбінації від реалізації ефектів і спрощує додавання нових заклинань.
    private bool CastSpell(string key, float power)
    {
        switch (key)
        {
            case "ййй":
                return CastGhostBarrage(power);
            case "ййц":
                CastBattleCry();
                return true;
            case "ййу":
                CastWallOfFallen();
                return true;
            case "ццц":
                CastGrace(power);
                return true;
            case "йцц":
                StartCoroutine(CastLightBonds(power));
                return true;
            case "ццу":
                stats.GrantSpiritWard();
                BuffNearbyAllies(1.25f, 1.15f, 8f);
                return true;
            case "ууу":
                CastShadowExplosion(power);
                return true;
            case "йуу":
                CastHereticSpirit(power);
                return true;
            case "цуу":
                CastCleansingRay(power);
                return true;
            case "йцу":
                return CastLoopOfOblivion(power);
            default:
                GameplayNotificationController.Show("Ця комбінація Еха не існує.");
                return false;
        }
    }

    private bool CastGhostBarrage(float power)
    {
        EnemyAI target = FindTargetNearCursor();
        TrainingDummyController dummy = FindDummyNearCursor();
        DemonBossController boss = FindAnyObjectByType<DemonBossController>();
        if (target == null && dummy == null && boss == null)
        {
            GameplayNotificationController.Show("Немає цілі для Примарного Шквалу.");
            return false;
        }

        float total = 0f;
        for (int i = 0; i < 5; i++)
        {
            DamagePacket packet = new DamagePacket(
                power * 0.34f,
                DamageFamily.Physical,
                DamageModifier.Piercing,
                gameObject,
                i == 4,
                true);
            if (target != null)
            {
                total += target.TakeDamage(packet);
            }
            else if (dummy != null)
            {
                float dealt = dummy.TakeDamage(packet.Amount, false);
                DamageNumberController.Show(dummy.transform.position, dealt, false);
                total += dealt;
            }
            else
            {
                boss.TakeDamage(packet);
                total += packet.Amount;
            }
        }
        inventory.HealFromSpellDamage(total);
        return true;
    }

    private void CastBattleCry()
    {
        BuffNearbyAllies(1.45f, 1.2f, 10f);
        foreach (TowerController tower in FindObjectsByType<TowerController>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, tower.transform.position) <= spellRadius + 2f)
                tower.ApplyTemporaryBuff(1.45f, 1.25f, 10f);
        }
    }

    private void CastWallOfFallen()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2 position = new Vector2(BattleLaneUtility.DefenseX - 1.5f + i, transform.position.y + 1.2f);
            CreateSpiritAlly("Полеглий воїн", position, 0f, 140f + CampaignProgress.SelectedLevel * 8f, 12f);
        }
    }

    private void CastGrace(float power)
    {
        float healPercent = Mathf.Clamp(0.18f + power / 1000f, 0.18f, 0.42f);
        PlayerHealth player = GetComponent<PlayerHealth>();
        if (player != null) player.Heal(player.maxHealth * healPercent);

        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, ally.transform.position) <= spellRadius)
                ally.Heal(ally.MaxHealth * healPercent);
        }
        BattleFlowController.Instance?.DefenseFrontTower?.Heal(450f * healPercent);
    }

    private IEnumerator CastLightBonds(float power)
    {
        AllyController target = FindNearestAlly();
        if (target == null) yield break;
        for (int i = 0; i < 6 && target != null && target.IsAlive; i++)
        {
            target.Heal(power * 0.28f);
            yield return new WaitForSeconds(1f);
        }
    }

    private void CastShadowExplosion(float power)
    {
        float dealt = DamageEnemiesInRadius(
            transform.position,
            spellRadius,
            new DamagePacket(power * 2.1f, DamageFamily.Pure, DamageModifier.Pure, gameObject, true, true));
        inventory.HealFromSpellDamage(dealt);
    }

    private void CastHereticSpirit(float power)
    {
        CreateSpiritAlly("Дух-Єретик", (Vector2)transform.position + Vector2.right, 0f, 180f, power * 0.45f);
    }

    private void CastCleansingRay(float power)
    {
        Camera camera = Camera.main;
        Vector2 direction = camera != null
            ? ((Vector2)camera.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized
            : Vector2.up;
        float dealt = 0f;
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (DistanceToRay(enemy.transform.position, transform.position, direction, 7f) <= 0.75f)
            {
                dealt += enemy.TakeDamage(new DamagePacket(
                    power * 1.25f,
                    DamageFamily.Pure,
                    DamageModifier.Pure,
                    gameObject,
                    true,
                    true));
            }
        }
        TrainingDummyController dummy = FindAnyObjectByType<TrainingDummyController>();
        if (dummy != null && DistanceToRay(dummy.transform.position, transform.position, direction, 7f) <= 0.95f)
        {
            float dummyDamage = power * 1.25f;
            dealt += dummy.TakeDamage(dummyDamage, false);
            DamageNumberController.Show(dummy.transform.position, dummyDamage, false);
        }
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (DistanceToRay(ally.transform.position, transform.position, direction, 7f) <= 0.75f)
                ally.Heal(power * 0.8f);
        }
        inventory.HealFromSpellDamage(dealt);
    }

    // Петля Небуття є сюжетним заклинанням і блокується демоном, доки не виконано умови фіналу.
    private bool CastLoopOfOblivion(float power)
    {
        if (BattleFlowController.Instance == null ||
            BattleFlowController.Instance.Phase != BattlePhase.Finale)
        {
            GameplayNotificationController.Show("Демон заблокував заклинання «Петля Небуття».");
            return false;
        }

        stats.GrantSpiritWard();
        DamageEnemiesInRadius(
            transform.position,
            8f,
            new DamagePacket(power * 2.5f, DamageFamily.Chaos, DamageModifier.Chaos, gameObject, true, true));
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
            ally.ForceAttack();
        return true;
    }

    private float DamageEnemiesInRadius(Vector2 center, float radius, DamagePacket packet)
    {
        float dealt = 0f;
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(center, enemy.transform.position) <= radius)
                dealt += enemy.TakeDamage(packet);
        }
        DemonBossController boss = FindAnyObjectByType<DemonBossController>();
        if (boss != null && Vector2.Distance(center, boss.transform.position) <= radius)
        {
            boss.TakeDamage(packet);
            dealt += packet.Amount;
        }
        TrainingDummyController dummy = FindAnyObjectByType<TrainingDummyController>();
        if (dummy != null && Vector2.Distance(center, dummy.transform.position) <= radius)
        {
            float dummyDamage = dummy.TakeDamage(packet.Amount, false);
            DamageNumberController.Show(dummy.transform.position, dummyDamage, false);
            dealt += dummyDamage;
        }
        return dealt;
    }

    private void BuffNearbyAllies(float damageMultiplier, float speedMultiplier, float duration)
    {
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, ally.transform.position) <= spellRadius + 2f)
                ally.ApplyTemporaryBuff(damageMultiplier, speedMultiplier, duration);
        }
    }

    private EnemyAI FindTargetNearCursor()
    {
        Camera camera = Camera.main;
        if (camera == null) return null;
        Vector2 cursor = camera.ScreenToWorldPoint(Input.mousePosition);
        EnemyAI nearest = null;
        float distance = 2.2f;
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            float candidate = Vector2.Distance(cursor, enemy.transform.position);
            if (candidate < distance)
            {
                distance = candidate;
                nearest = enemy;
            }
        }
        return nearest;
    }

    private TrainingDummyController FindDummyNearCursor()
    {
        Camera camera = Camera.main;
        if (camera == null) return null;
        TrainingDummyController dummy = FindAnyObjectByType<TrainingDummyController>();
        if (dummy == null || !dummy.IsAlive) return null;
        Vector2 cursor = camera.ScreenToWorldPoint(Input.mousePosition);
        return Vector2.Distance(cursor, dummy.transform.position) <= 1.8f
            ? dummy
            : null;
    }

    private AllyController FindNearestAlly()
    {
        AllyController nearest = null;
        float distance = float.MaxValue;
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            float candidate = Vector2.Distance(transform.position, ally.transform.position);
            if (candidate < distance)
            {
                distance = candidate;
                nearest = ally;
            }
        }
        return nearest;
    }

    private static AllyController CreateSpiritAlly(string name, Vector2 position, float speed, float health, float damage)
    {
        GameObject spirit = new GameObject(name);
        spirit.transform.position = position;
        SpriteRenderer renderer = spirit.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        renderer.color = KenamUiTheme.WithAlpha(KenamUiTheme.Purple, 0.75f);
        renderer.sortingOrder = 18;
        spirit.transform.localScale = new Vector3(0.55f, 0.85f, 1f);
        spirit.AddComponent<BoxCollider2D>().isTrigger = true;

        UnitData data = ScriptableObject.CreateInstance<UnitData>();
        data.unitName = name;
        data.maxHp = health;
        data.moveSpeed = speed;
        data.minDamage = damage;
        data.maxDamage = damage;
        data.attackRate = 1.1f;
        data.towerDamage = damage * 0.2f;
        data.armor = 4f;
        AllyController controller = spirit.AddComponent<AllyController>();
        controller.Initialize(data);
        controller.SetCustomLane(position.x, BattleLane.Lower);
        if (BattleFlowController.Instance != null)
            controller.ApplyPermanentMultiplier(BattleFlowController.Instance.AllyPowerMultiplier);
        return controller;
    }

    private string GetCanonicalKey()
    {
        int q = 0;
        int w = 0;
        int e = 0;
        foreach (char echo in echoes)
        {
            if (echo == 'й') q++;
            else if (echo == 'ц') w++;
            else if (echo == 'у') e++;
        }
        return new string('й', q) + new string('ц', w) + new string('у', e);
    }

    private void RefreshUi(string message)
    {
        if (echoText != null)
        {
            StringBuilder value = new StringBuilder();
            foreach (char echo in echoes)
            {
                if (value.Length > 0) value.Append("  ");
                value.Append(echo);
            }
            echoText.text = value.Length == 0 ? "—  —  —" : value.ToString();
        }
        if (spellText != null) spellText.text = message;
    }

    private static string GetSpellName(string key)
    {
        return key switch
        {
            "ййй" => "Примарний Шквал",
            "ййц" => "Бойовий Клич",
            "ййу" => "Стіна Полеглих",
            "ццц" => "Благодать",
            "йцц" => "Узи Світла",
            "ццу" => "Оберіг Духа",
            "ууу" => "Тіньовий Вибух",
            "йуу" => "Дух-Єретик",
            "цуу" => "Промінь Очищення",
            "йцу" => "Петля Небуття",
            _ => key
        };
    }

    private static float DistanceToRay(Vector2 point, Vector2 origin, Vector2 direction, float length)
    {
        Vector2 offset = point - origin;
        float projection = Mathf.Clamp(Vector2.Dot(offset, direction), 0f, length);
        return Vector2.Distance(point, origin + direction * projection);
    }
}
