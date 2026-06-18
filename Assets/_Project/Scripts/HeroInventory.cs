// Описує каталог предметів, інвентар героя, рецепти, активні здібності та всі предметні модифікатори бою.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HeroItemId
{
    Teleport,
    TravelBoots,
    GhostScepter,
    BlinkDagger,
    MorbidMask,
    MaskOfMadness,
    Satanic,
    Dominator,
    Overlord,
    Vladmir,
    GreaterVladmir,
    Drums,
    SolarCrest,
    Pipe,
    Greaves,
    VoodooMask,
    Bloodstone,
    Veil,
    ArcaneDaedalus,
    BattleFury,
    Gleipnir,
    Octarine,
    Refresher,
    MageSlayer,
    GreaterMageSlayer,
    AeonDisk,
    UndyingAura,
    DivineRapier,
    PassiveBkb,
    Aegis,
    Crystalys,
    Daedalus,
    AssaultCuirass,
    HeartOfTarrasque
}

public sealed class HeroItemDefinition
{
    public HeroItemId Id;
    public string Name;
    public int Cost;
    public bool Active;
    public float Cooldown;
    public string IconPath;
    public HeroItemId[] Recipe;

    public HeroItemDefinition(
        HeroItemId id,
        string name,
        int cost,
        bool active = false,
        float cooldown = 0f,
        string iconPath = null,
        params HeroItemId[] recipe)
    {
        Id = id;
        Name = name;
        Cost = HeroItemCatalog.GetBalancedCost(cost);
        Active = active;
        Cooldown = cooldown;
        IconPath = iconPath ?? id.ToString();
        Recipe = recipe ?? Array.Empty<HeroItemId>();
    }
}

public static class HeroItemCatalog
{
    private const float EconomyPriceMultiplier = 0.35f;
    public static readonly HeroItemDefinition[] All =
    {
        new HeroItemDefinition(HeroItemId.Teleport, "Якір переходу", 500, true, 30f, "TransitionAnchor"),
        new HeroItemDefinition(HeroItemId.TravelBoots, "Черевики розлому", 1800, true, 20f, "RiftwalkBoots", HeroItemId.Teleport),
        new HeroItemDefinition(HeroItemId.GhostScepter, "Примарний жезл", 1200, true, 25f, "GhostRod"),
        new HeroItemDefinition(HeroItemId.BlinkDagger, "Клинок розриву", 1600, true, 30f, "RiftBlade"),
        new HeroItemDefinition(HeroItemId.MorbidMask, "Кровопивна личина", 700, true, 20f, "BlooddrinkerMask"),
        new HeroItemDefinition(HeroItemId.MaskOfMadness, "Личина люті", 1800, false, 0f, "RageMask", HeroItemId.MorbidMask),
        new HeroItemDefinition(HeroItemId.Satanic, "Кривавий договір", 5200, false, 0f, "CrimsonPact", HeroItemId.MaskOfMadness),
        new HeroItemDefinition(HeroItemId.Dominator, "Шолом вожака", 2200, false, 0f, "WarleaderHelm"),
        new HeroItemDefinition(HeroItemId.Overlord, "Корона владаря", 5000, false, 0f, "OverlordCrown", HeroItemId.Dominator),
        new HeroItemDefinition(HeroItemId.Vladmir, "Кривава хоругва", 2400, false, 0f, "BloodBanner"),
        new HeroItemDefinition(HeroItemId.GreaterVladmir, "Хоругва пожирача", 5800, false, 0f, "DevourerBanner", HeroItemId.Vladmir, HeroItemId.Drums),
        new HeroItemDefinition(HeroItemId.Drums, "Маршові барабани", 2100, false, 0f, "MarchDrums"),
        new HeroItemDefinition(HeroItemId.SolarCrest, "Сонячний оберіг", 3000, true, 22f, "SunWard"),
        new HeroItemDefinition(HeroItemId.Pipe, "Покров прозріння", 3600, true, 30f, "InsightVeil"),
        new HeroItemDefinition(HeroItemId.Greaves, "Клятвені наголінники", 4200, true, 35f, "OathGreaves"),
        new HeroItemDefinition(HeroItemId.VoodooMask, "Відьомська личина", 1100, false, 0f, "WitchMask"),
        new HeroItemDefinition(HeroItemId.Bloodstone, "Серцевий рубін", 4600, false, 0f, "HeartRuby", HeroItemId.VoodooMask),
        new HeroItemDefinition(HeroItemId.Veil, "Вуаль розладу", 2500, false, 0f, "DiscordVeil"),
        new HeroItemDefinition(HeroItemId.ArcaneDaedalus, "Рунний крит", 4800, false, 0f, "RunicCritical"),
        new HeroItemDefinition(HeroItemId.BattleFury, "Сікач хвиль", 1400, false, 0f, "WaveCleaver"),
        new HeroItemDefinition(HeroItemId.Gleipnir, "Ланцюги привидів", 2800, true, 22f, "GhostChains"),
        new HeroItemDefinition(HeroItemId.Octarine, "Вузол часу", 5000, false, 0f, "TimeKnot"),
        new HeroItemDefinition(HeroItemId.Refresher, "Сфера оновлення", 5400, true, 90f, "RenewalOrb"),
        new HeroItemDefinition(HeroItemId.MageSlayer, "Антимагічний клинок", 3000, false, 0f, "MagebaneBlade"),
        new HeroItemDefinition(HeroItemId.GreaterMageSlayer, "Клинок тиші", 6000, false, 0f, "SilenceBlade", HeroItemId.MageSlayer),
        new HeroItemDefinition(HeroItemId.AeonDisk, "Диск миті", 3800, false, 0f, "MomentDisk"),
        new HeroItemDefinition(HeroItemId.UndyingAura, "Обіт невмирущих", 7200, false, 0f, "UndyingOath", HeroItemId.AeonDisk),
        new HeroItemDefinition(HeroItemId.DivineRapier, "Зоряна рапіра", 8000, false, 0f, "StarRapier"),
        new HeroItemDefinition(HeroItemId.PassiveBkb, "Чорна присяга", 5600, false, 0f, "BlackOath"),
        new HeroItemDefinition(HeroItemId.Aegis, "Егіда петлі", 3000, false, 0f, "LoopAegis"),
        new HeroItemDefinition(HeroItemId.Crystalys, "Кристал розсічення", 2200, false, 0f, "CuttingShard"),
        new HeroItemDefinition(HeroItemId.Daedalus, "Корона криту", 5200, false, 0f, "CriticalCrown", HeroItemId.Crystalys),
        new HeroItemDefinition(HeroItemId.AssaultCuirass, "Кераса командора", 5600, false, 0f, "CommanderCuirass"),
        new HeroItemDefinition(HeroItemId.HeartOfTarrasque, "Серце велета", 5400, true, 0f, "GiantHeart")
    };

    public static HeroItemDefinition Get(HeroItemId id)
    {
        return Array.Find(All, item => item.Id == id);
    }

    public static int GetBalancedCost(int originalCost)
    {
        return Mathf.Max(100, Mathf.RoundToInt(originalCost * EconomyPriceMultiplier / 25f) * 25);
    }

    public static string GetEffectText(HeroItemId id)
    {
        return id switch
        {
            HeroItemId.Teleport => "Перехід до протилежного фронту після підготовки.",
            HeroItemId.TravelBoots => "Швидший перехід між фронтами; покращує Якір.",
            HeroItemId.GhostScepter => "Коротка невразливість до фізичної шкоди.",
            HeroItemId.BlinkDagger => "Миттєвий ривок у напрямку курсора.",
            HeroItemId.MorbidMask => "Активно посилює вампіризм; пасивно лікує від ударів.",
            HeroItemId.MaskOfMadness => "Висока швидкість атак і посилений вампіризм.",
            HeroItemId.Satanic => "Велика фізична сила та надпотужне лікування від ударів.",
            HeroItemId.Dominator => "Посилює здоров'я і шкоду союзної армії.",
            HeroItemId.Overlord => "Значно посилює союзників і їхню витривалість.",
            HeroItemId.Vladmir => "Аура шкоди та вампіризму для союзників.",
            HeroItemId.GreaterVladmir => "Розширена аура шкоди, швидкості та вампіризму.",
            HeroItemId.Drums => "Аура швидкості руху і атаки армії.",
            HeroItemId.SolarCrest => "Тимчасово посилює обраного союзника.",
            HeroItemId.Pipe => "Накладає на армію щит від магічної шкоди.",
            HeroItemId.Greaves => "Лікує героя, союзників і оборонну споруду.",
            HeroItemId.VoodooMask => "Лікує героя частиною завданої магічної шкоди.",
            HeroItemId.Bloodstone => "Сильний магічний вампіризм з активним підсиленням.",
            HeroItemId.Veil => "Вороги поруч отримують більше магічної шкоди.",
            HeroItemId.ArcaneDaedalus => "Посилює закляття; кожне четверте критує.",
            HeroItemId.BattleFury => "Фізичні удари розсікають ворогів поруч.",
            HeroItemId.Gleipnir => "Обплутує групу ворогів і знижує їхній опір.",
            HeroItemId.Octarine => "Скорочує відновлення здібностей і предметів на 25%.",
            HeroItemId.Refresher => "Негайно скидає відновлення активних предметів.",
            HeroItemId.MageSlayer => "Знижує отриману магічну шкоду і силу ворожої магії.",
            HeroItemId.GreaterMageSlayer => "Посилений захист від магії та довше ослаблення ворогів.",
            HeroItemId.AeonDisk => "Рятує героя при критично низькому здоров'ї.",
            HeroItemId.UndyingAura => "Не дозволяє союзникам поруч померти, поки живий герой.",
            HeroItemId.DivineRapier => "Різко збільшує фізичну шкоду, але губиться при падінні.",
            HeroItemId.PassiveBkb => "Автоматично дає захист від магії на порогах здоров'я.",
            HeroItemId.Aegis => "Одноразово повертає героя до бою після падіння.",
            HeroItemId.Crystalys => "Дає шанс на потужний фізичний критичний удар.",
            HeroItemId.Daedalus => "Підвищений шанс і множник критичної шкоди.",
            HeroItemId.AssaultCuirass => "Броня герою й союзникам, ослаблення броні ворогів.",
            HeroItemId.HeartOfTarrasque => "Великий запас здоров'я, регенерація і зв'язок із захисником.",
            _ => string.Empty
        };
    }
}

public readonly struct HeroAttackResult
{
    public readonly float Damage;
    public readonly bool Critical;

    public HeroAttackResult(float damage, bool critical)
    {
        Damage = damage;
        Critical = critical;
    }
}

public class HeroInventory : MonoBehaviour
{
    private const float UnpackDuration = 30f;

    public event Action InventoryChanged;

    private readonly List<HeroItemId> items = new List<HeroItemId>();
    private readonly List<HeroItemId> activeSlots = new List<HeroItemId>();
    private readonly Dictionary<HeroItemId, float> readyAt = new Dictionary<HeroItemId, float>();
    private readonly Dictionary<HeroItemId, float> unpackingReadyAt = new Dictionary<HeroItemId, float>();

    private HeroStats stats;
    private PlayerHealth health;
    private float auraTickAt;
    private float heartTickAt;
    private float lastDamageTakenAt = -999f;
    private float lifestealMultiplier = 1f;
    private float lifestealBuffUntil;
    private int aegisPurchases;
    private int lastBkbThreshold = 100;
    private int passiveBkbProcsLeft = 8;
    private float aeonReadyAt;
    private AllyController heartLinkedAlly;
    private FrontTower heartLinkedTower;

    public IReadOnlyList<HeroItemId> Items => items;
    public IReadOnlyList<HeroItemId> ActiveSlots => activeSlots;
    public float CooldownMultiplier => Has(HeroItemId.Octarine) ? 0.75f : 1f;
    public float PhysicalDamageMultiplier =>
        (Has(HeroItemId.DivineRapier) ? 2.75f : 1f) *
        (Has(HeroItemId.Satanic) ? 1.45f : 1f);
    public float SpellDamageMultiplier => Has(HeroItemId.ArcaneDaedalus) ? 1.15f : 1f;
    public float PhysicalLifesteal => GetBasePhysicalLifesteal() * lifestealMultiplier;
    public float SpellLifesteal => (Has(HeroItemId.Bloodstone) ? 0.32f : Has(HeroItemId.VoodooMask) ? 0.14f : 0f) * lifestealMultiplier;
    public bool HasBattleFury => Has(HeroItemId.BattleFury);
    public bool HasVeil => Has(HeroItemId.Veil);
    public bool HasUndyingAura => Has(HeroItemId.UndyingAura);
    public int AegisPurchases => aegisPurchases;
    public bool IsHeartLinked => heartLinkedAlly != null || heartLinkedTower != null;

    private void Awake()
    {
        stats = GetComponent<HeroStats>() ?? gameObject.AddComponent<HeroStats>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (Time.time >= lifestealBuffUntil) lifestealMultiplier = 1f;
        CompleteUnpacking();
        ApplyHeartRegeneration();

        if (Input.GetKeyDown(KeyCode.Alpha1)) UseActiveSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) UseActiveSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) UseActiveSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) UseActiveSlot(3);

        if (Time.time >= auraTickAt)
        {
            auraTickAt = Time.time + 0.5f;
            ApplyAuras();
        }
    }

    public bool Has(HeroItemId id)
    {
        return items.Contains(id);
    }

    public bool IsUnpacking(HeroItemId id)
    {
        return unpackingReadyAt.ContainsKey(id);
    }

    public float GetUnpackRemaining(HeroItemId id)
    {
        return unpackingReadyAt.TryGetValue(id, out float readyTime)
            ? Mathf.Max(0f, readyTime - Time.time)
            : 0f;
    }

    // Купівля перевіряє золото, рецепт, вільні активні слоти та час розпакування предмета.
    public bool Purchase(HeroItemId id)
    {
        HeroItemDefinition definition = HeroItemCatalog.Get(id);
        if (definition == null || GameManager.Instance == null) return false;
        if (IsUnpacking(id))
        {
            GameplayNotificationController.Show("Цей артефакт уже розпаковується.");
            return false;
        }
        if (id != HeroItemId.Aegis && Has(id))
        {
            GameplayNotificationController.Show("Цей артефакт уже є в інвентарі.");
            return false;
        }

        if (!HasRecipeComponents(definition))
        {
            GameplayNotificationController.Show($"Потрібні компоненти: {GetRecipeText(definition)}");
            return false;
        }

        if (GetProjectedActiveSlotCount(definition) > 4)
        {
            GameplayNotificationController.Show("Усі чотири активні слоти зайняті.");
            return false;
        }

        int cost = GetCurrentCost(id);
        if (!GameManager.Instance.SpendGold(cost)) return false;

        ConsumeRecipeComponents(definition);
        if (id == HeroItemId.Aegis) aegisPurchases++;
        unpackingReadyAt[id] = Time.time + UnpackDuration;
        InventoryChanged?.Invoke();
        GameplayNotificationController.Show($"Доставлено: {definition.Name}. Розпаковка {UnpackDuration:0} с.");
        return true;
    }

    public bool Sell(HeroItemId id)
    {
        if (!items.Remove(id) || GameManager.Instance == null) return false;
        activeSlots.Remove(id);
        HeroItemDefinition definition = HeroItemCatalog.Get(id);
        GameManager.Instance.AddGold(Mathf.FloorToInt(definition.Cost * 0.49f));
        InventoryChanged?.Invoke();
        return true;
    }

    public bool ConsumeAegis()
    {
        if (!items.Remove(HeroItemId.Aegis)) return false;
        activeSlots.Remove(HeroItemId.Aegis);
        stats.AddPermanentPower(1.2f);
        passiveBkbProcsLeft = 8;
        lastBkbThreshold = 100;
        InventoryChanged?.Invoke();
        return true;
    }

    public int GetCurrentCost(HeroItemId id)
    {
        HeroItemDefinition definition = HeroItemCatalog.Get(id);
        if (definition == null) return 0;
        int baseCost = id == HeroItemId.Aegis
            ? Mathf.RoundToInt(definition.Cost * Mathf.Pow(1.8f, aegisPurchases))
            : definition.Cost;
        if (definition.Recipe.Length == 0) return baseCost;

        int componentCost = 0;
        foreach (HeroItemId component in definition.Recipe)
        {
            if (Has(component))
            {
                HeroItemDefinition componentDefinition = HeroItemCatalog.Get(component);
                if (componentDefinition != null) componentCost += componentDefinition.Cost;
            }
        }
        return Mathf.Max(1, baseCost - componentCost);
    }

    public float GetCooldownRemaining(int slot)
    {
        if (slot < 0 || slot >= activeSlots.Count) return 0f;
        return readyAt.TryGetValue(activeSlots[slot], out float time)
            ? Mathf.Max(0f, time - Time.time)
            : 0f;
    }

    private void CompleteUnpacking()
    {
        if (unpackingReadyAt.Count == 0) return;

        List<HeroItemId> completed = null;
        foreach (KeyValuePair<HeroItemId, float> entry in unpackingReadyAt)
        {
            if (Time.time < entry.Value) continue;
            completed ??= new List<HeroItemId>();
            completed.Add(entry.Key);
        }

        if (completed == null) return;
        foreach (HeroItemId id in completed)
        {
            unpackingReadyAt.Remove(id);
            items.Add(id);
            HeroItemDefinition definition = HeroItemCatalog.Get(id);
            if (definition != null && definition.Active) activeSlots.Add(id);
            if (id == HeroItemId.PassiveBkb)
            {
                passiveBkbProcsLeft = 8;
                lastBkbThreshold = health != null && health.maxHealth > 0f
                    ? Mathf.FloorToInt(health.currentHealth / health.maxHealth * 100f)
                    : 100;
            }
            if (id == HeroItemId.Aegis && health != null && health.IsFallen && ConsumeAegis())
            {
                health.ReviveFromAegis();
                continue;
            }
            GameplayNotificationController.Show($"Готово до використання: {definition?.Name ?? id.ToString()}");
        }

        InventoryChanged?.Invoke();
    }

    // Рецепт збирається лише з уже придбаних компонентів і не створює їх автоматично.
    private bool HasRecipeComponents(HeroItemDefinition definition)
    {
        if (definition == null || definition.Recipe.Length == 0) return true;
        foreach (HeroItemId component in definition.Recipe)
        {
            if (!Has(component)) return false;
        }
        return true;
    }

    private void ConsumeRecipeComponents(HeroItemDefinition definition)
    {
        if (definition == null || definition.Recipe.Length == 0) return;
        foreach (HeroItemId component in definition.Recipe)
        {
            items.Remove(component);
            activeSlots.Remove(component);
            readyAt.Remove(component);
        }
    }

    private int GetProjectedActiveSlotCount(HeroItemDefinition definition)
    {
        if (definition == null) return activeSlots.Count;
        int count = activeSlots.Count;
        foreach (HeroItemId component in definition.Recipe)
        {
            HeroItemDefinition componentDefinition = HeroItemCatalog.Get(component);
            if (componentDefinition != null && componentDefinition.Active && activeSlots.Contains(component)) count--;
        }

        foreach (KeyValuePair<HeroItemId, float> pending in unpackingReadyAt)
        {
            HeroItemDefinition pendingDefinition = HeroItemCatalog.Get(pending.Key);
            if (pendingDefinition != null && pendingDefinition.Active) count++;
        }

        if (definition.Active) count++;
        return count;
    }

    public static string GetRecipeText(HeroItemDefinition definition)
    {
        if (definition == null || definition.Recipe.Length == 0) return string.Empty;
        List<string> names = new List<string>();
        foreach (HeroItemId component in definition.Recipe)
        {
            HeroItemDefinition componentDefinition = HeroItemCatalog.Get(component);
            names.Add(componentDefinition != null ? componentDefinition.Name : component.ToString());
        }
        return string.Join(" + ", names);
    }

    public DamagePacket SuppressEnemySpell(DamagePacket packet, Vector2 receiverPosition)
    {
        if (!packet.IsSpell || Vector2.Distance(transform.position, receiverPosition) > 5.5f) return packet;
        float multiplier = Has(HeroItemId.GreaterMageSlayer)
            ? 0.5f
            : Has(HeroItemId.MageSlayer)
                ? 0.72f
                : 1f;
        return multiplier < 1f ? packet.WithAmount(packet.Amount * multiplier) : packet;
    }

    public void HandleHeroDeath()
    {
        if (!items.Remove(HeroItemId.DivineRapier)) return;

        InventoryChanged?.Invoke();
        GameplayNotificationController.Show("Зоряна рапіра розсипалась після смерті Ке́нама.");
    }

    public void NotifyDamageTaken(float healthPercent, DamagePacket packet)
    {
        lastDamageTakenAt = Time.time;

        if (Has(HeroItemId.PassiveBkb) && passiveBkbProcsLeft > 0)
        {
            int healthPercentInt = Mathf.Clamp(Mathf.FloorToInt(healthPercent * 100f), 0, 100);
            int threshold = Mathf.FloorToInt(healthPercentInt / 10f) * 10;
            if (threshold < lastBkbThreshold && threshold >= 20)
            {
                lastBkbThreshold = threshold;
                passiveBkbProcsLeft--;
                int lostHealthSteps = Mathf.FloorToInt((100f - healthPercentInt) / 10f);
                stats.GrantBkb(Mathf.Min(4f + lostHealthSteps, 9f));
            }
        }

        if (Has(HeroItemId.AeonDisk) && healthPercent <= 0.2f && Time.time >= aeonReadyAt)
        {
            aeonReadyAt = Time.time + 60f;
            stats.GrantInvulnerability(3.5f);
            foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
            {
                if (Vector2.Distance(transform.position, ally.transform.position) <= 5.5f)
                    ally.GrantInvulnerability(3.5f);
            }
        }
    }

    public void HealFromPhysicalDamage(float dealtDamage)
    {
        health?.Heal(dealtDamage * PhysicalLifesteal);
    }

    public void HealFromSpellDamage(float dealtDamage)
    {
        health?.Heal(dealtDamage * SpellLifesteal);
    }

    public bool TrySplitHeal(float amount, out float heroAmount)
    {
        heroAmount = amount;
        if (amount <= 0f || !Has(HeroItemId.HeartOfTarrasque)) return false;

        bool linkedAllyAlive = heartLinkedAlly != null && heartLinkedAlly.IsAlive;
        bool linkedTowerAlive = heartLinkedTower != null && heartLinkedTower.IsAlive;
        if (!linkedAllyAlive && !linkedTowerAlive)
        {
            heartLinkedAlly = null;
            heartLinkedTower = null;
            return false;
        }

        heroAmount = amount * 0.5f;
        float linkedAmount = amount - heroAmount;
        if (linkedAllyAlive) heartLinkedAlly.Heal(linkedAmount);
        else if (linkedTowerAlive) heartLinkedTower.Heal(linkedAmount);
        return true;
    }

    // В одному місці визначаємо крит, колір числа шкоди та підсумковий фізичний удар героя.
    public HeroAttackResult RollHeroAttack(float baseDamage)
    {
        float multiplier = PhysicalDamageMultiplier;
        bool critical = false;
        if (Has(HeroItemId.Daedalus))
        {
            critical = UnityEngine.Random.value < 0.34f;
            if (critical) multiplier *= 2.25f;
        }
        else if (Has(HeroItemId.Crystalys))
        {
            critical = UnityEngine.Random.value < 0.25f;
            if (critical) multiplier *= 1.6f;
        }

        return new HeroAttackResult(baseDamage * multiplier, critical);
    }

    public float RollDeterministicMagicCritical(float amount, int castIndex)
    {
        if (!Has(HeroItemId.ArcaneDaedalus) || castIndex % 4 != 0) return amount;
        return amount * 2f;
    }

    public void ResetActiveCooldowns()
    {
        readyAt.Clear();
    }

    public bool UseActiveSlot(int slot)
    {
        if (slot < 0 || slot >= activeSlots.Count) return false;
        HeroItemId id = activeSlots[slot];
        HeroItemDefinition definition = HeroItemCatalog.Get(id);
        if (definition == null) return false;
        if (readyAt.TryGetValue(id, out float time) && Time.time < time)
        {
            GameplayNotificationController.Show($"Перезарядка: {time - Time.time:0.0} с");
            return false;
        }

        if (!Activate(id)) return false;
        GameAudioController.PlaySfx(GameSfxCue.Magic, 0.55f);
        readyAt[id] = Time.time + definition.Cooldown * CooldownMultiplier;
        InventoryChanged?.Invoke();
        return true;
    }

    // Активні предмети виконують ефект тут, а спільна логіка слотів і кулдаунів лишається в UseActiveSlot.
    private bool Activate(HeroItemId id)
    {
        switch (id)
        {
            case HeroItemId.Teleport:
            case HeroItemId.TravelBoots:
                StartCoroutine(TeleportRoutine(id == HeroItemId.TravelBoots ? 0.8f : 1.5f));
                return true;
            case HeroItemId.GhostScepter:
                stats.GrantPhysicalImmunity(5f);
                return true;
            case HeroItemId.BlinkDagger:
                BlinkToCursor(8.5f);
                return true;
            case HeroItemId.MorbidMask:
                lifestealMultiplier = 2f;
                lifestealBuffUntil = Time.time + 5f;
                return true;
            case HeroItemId.MaskOfMadness:
                return false;
            case HeroItemId.Satanic:
                return false;
            case HeroItemId.SolarCrest:
                BuffNearbyAllies(1.35f, 1.3f, 8f);
                DebuffNearbyEnemies(0.28f, 8f, 0f);
                return true;
            case HeroItemId.Pipe:
                GrantNearbyMagicBarriers();
                return true;
            case HeroItemId.Greaves:
                HealNearby(80f);
                return true;
            case HeroItemId.Bloodstone:
                return false;
            case HeroItemId.Gleipnir:
                DebuffNearbyEnemies(0.18f, 5f, stats.SpellPower * 0.7f);
                return true;
            case HeroItemId.Refresher:
                ResetActiveCooldowns();
                GetComponent<EchoSpellbookController>()?.ResetCooldowns();
                return true;
            case HeroItemId.HeartOfTarrasque:
                ToggleHeartLink();
                return true;
            default:
                return false;
        }
    }

    private IEnumerator TeleportRoutine(float castTime)
    {
        yield return new WaitForSeconds(castTime);

        Camera camera = Camera.main;
        Vector2 cursor = camera != null
            ? (Vector2)camera.ScreenToWorldPoint(Input.mousePosition)
            : (Vector2)transform.position;
        Transform target = null;
        float nearest = float.MaxValue;
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            float distance = Vector2.Distance(cursor, ally.transform.position);
            if (distance < nearest)
            {
                nearest = distance;
                target = ally.transform;
            }
        }

        FrontTower tower = BattleFlowController.Instance?.DefenseFrontTower;
        if (tower != null && Vector2.Distance(cursor, tower.transform.position) < nearest)
        {
            target = tower.transform;
        }

        if (target != null) transform.position = target.position;
    }

    private void BlinkToCursor(float distance)
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector2 mouse = camera.ScreenToWorldPoint(Input.mousePosition);
        transform.position = (Vector2)transform.position +
            Vector2.ClampMagnitude(mouse - (Vector2)transform.position, distance);
    }

    // Аури оновлюються короткими імпульсами, щоб союзники, які входять у радіус, одразу отримували актуальний ефект.
    private void ApplyAuras()
    {
        float damageMultiplier = 1f;
        float flatDamageBonus = 0f;
        float armorBonus = 0f;
        float speedMultiplier = 1f;
        float lifesteal = 0f;

        if (Has(HeroItemId.Dominator))
        {
            damageMultiplier += stats.AttackDamage * 0.002f;
            armorBonus += stats.Armor * 0.2f;
        }
        if (Has(HeroItemId.Overlord))
        {
            damageMultiplier += stats.AttackDamage * 0.004f;
            armorBonus += stats.Armor * 0.35f;
        }
        if (Has(HeroItemId.Vladmir))
        {
            damageMultiplier += 0.12f;
            armorBonus += 3f;
            lifesteal += 0.15f;
        }
        if (Has(HeroItemId.GreaterVladmir))
        {
            damageMultiplier += 0.2f;
            armorBonus += 5f;
            lifesteal += 1.6f;
        }
        if (Has(HeroItemId.Drums)) speedMultiplier += 0.2f;
        if (Has(HeroItemId.AssaultCuirass))
        {
            flatDamageBonus += stats.AttackDamage * 0.1f;
            armorBonus += stats.Armor * 0.1f;
            speedMultiplier += Mathf.Max(0f, stats.AttackSpeedMultiplier - 1f) * 0.1f;
        }

        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, ally.transform.position) <= 5.5f)
            {
                ally.ApplyAura(damageMultiplier, flatDamageBonus, armorBonus, speedMultiplier, lifesteal, HasUndyingAura);
            }
        }
    }

    private void BuffNearbyAllies(float damageMultiplier, float speedMultiplier, float duration)
    {
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, ally.transform.position) <= 5.5f)
                ally.ApplyTemporaryBuff(damageMultiplier, speedMultiplier, duration);
        }
    }

    private void GrantNearbyMagicBarriers()
    {
        stats.GrantMagicBarrier(140f);
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, ally.transform.position) <= 5.5f)
                ally.GrantMagicBarrier(140f);
        }
    }

    private void HealNearby(float amount)
    {
        health?.Heal(amount);
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, ally.transform.position) <= 5.5f)
                ally.Heal(amount);
        }
    }

    private void DebuffNearbyEnemies(float resistanceReduction, float duration, float damage)
    {
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, enemy.transform.position) <= 5.5f)
            {
                if (damage > 0f) enemy.ApplyRoot(3f);
                enemy.ApplyItemResistanceDebuff(resistanceReduction, duration);
                if (damage > 0f)
                {
                    float dealt = enemy.TakeDamage(new DamagePacket(
                        damage,
                        DamageFamily.Magical,
                        DamageModifier.Mixed,
                        gameObject,
                        false,
                        true));
                    HealFromSpellDamage(dealt);
                }
            }
        }
    }

    private float GetBasePhysicalLifesteal()
    {
        if (Has(HeroItemId.Satanic)) return 1.2f;
        if (Has(HeroItemId.MaskOfMadness)) return 0.67f;
        if (Has(HeroItemId.MorbidMask)) return 0.09f;
        return 0f;
    }

    private void ApplyHeartRegeneration()
    {
        if (!Has(HeroItemId.HeartOfTarrasque) || health == null || health.currentHealth <= 0f) return;
        if (Time.time < heartTickAt) return;

        float elapsedSinceDamage = Mathf.Max(0f, Time.time - lastDamageTakenAt);
        float calmFactor = Mathf.Clamp01(elapsedSinceDamage / 8f);
        float regenPerSecond = health.maxHealth * Mathf.Lerp(0.004f, 0.035f, calmFactor);
        float tickInterval = 0.5f;
        heartTickAt = Time.time + tickInterval;
        health.Heal(regenPerSecond * tickInterval);
    }

    // Зв'язок Тараски ділить подальше лікування між героєм і вибраним союзним об'єктом порівну.
    private void ToggleHeartLink()
    {
        if (heartLinkedAlly != null || heartLinkedTower != null)
        {
            heartLinkedAlly = null;
            heartLinkedTower = null;
            GameplayNotificationController.Show("Зв'язок Серця велета розірвано.");
            return;
        }

        Camera camera = Camera.main;
        if (camera == null) return;

        Vector2 cursor = camera.ScreenToWorldPoint(Input.mousePosition);
        AllyController nearestAlly = null;
        float nearestDistance = 1.8f;
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (ally == null || !ally.IsAlive) continue;
            float distance = Vector2.Distance(cursor, ally.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestAlly = ally;
            }
        }

        FrontTower frontTower = BattleFlowController.Instance?.DefenseFrontTower;
        if (nearestAlly != null)
        {
            heartLinkedAlly = nearestAlly;
            GameplayNotificationController.Show("Серце велета ділить лікування з союзником.");
            return;
        }

        if (frontTower != null && frontTower.IsAlive && Vector2.Distance(cursor, frontTower.transform.position) <= 2.2f)
        {
            heartLinkedTower = frontTower;
            GameplayNotificationController.Show("Серце велета ділить лікування з оборонною спорудою.");
            return;
        }

        GameplayNotificationController.Show("Наведіть курсор на союзника або оборонну споруду.");
    }
}
