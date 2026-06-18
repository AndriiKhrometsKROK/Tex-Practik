// Керує здоров'ям героя, лікуванням, смертю та предметними ефектами виживання.
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider hpSlider;
    private HeroStats stats;
    private HeroInventory inventory;
    private bool isFallen;

    public bool IsFallen => isFallen;

    private void Start()
    {
        stats = GetComponent<HeroStats>() ?? gameObject.AddComponent<HeroStats>();
        inventory = GetComponent<HeroInventory>() ?? gameObject.AddComponent<HeroInventory>();
        currentHealth = maxHealth;
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = currentHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(new DamagePacket(amount, DamageFamily.Physical, DamageModifier.Default));
    }

    public float TakeDamage(DamagePacket packet)
    {
        if (packet.Amount <= 0f || currentHealth <= 0f || isFallen) return 0f;
        stats = stats != null ? stats : GetComponent<HeroStats>();
        inventory = inventory != null ? inventory : GetComponent<HeroInventory>();

        if (stats != null)
        {
            if (stats.Invulnerable || stats.ConsumeSpiritWard()) return 0f;
            if (stats.PhysicalImmune && packet.Family == DamageFamily.Physical) return 0f;
            if (stats.ObserveAndCheckAdaptation(packet)) return 0f;
            packet = stats.AbsorbMagicBarrier(packet);
            if (packet.Amount <= 0f) return 0f;
        }

        if (inventory != null) packet = inventory.SuppressEnemySpell(packet, transform.position);
        float armor = stats != null ? stats.Armor : 0f;
        float magicResistance = stats != null ? stats.MagicResistance : 0f;
        if (inventory != null && inventory.Has(HeroItemId.MageSlayer)) magicResistance += 0.18f;
        if (inventory != null && inventory.Has(HeroItemId.GreaterMageSlayer)) magicResistance += 0.32f;
        float finalDamage = CombatResolver.Resolve(packet, armor, magicResistance);
        currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0f, maxHealth);

        if (hpSlider != null)
        {
            hpSlider.value = currentHealth;
        }

        inventory?.NotifyDamageTaken(maxHealth > 0f ? currentHealth / maxHealth : 0f, packet);
        if (currentHealth <= 0f)
        {
            if (inventory != null && inventory.ConsumeAegis())
            {
                ReviveFromAegis();
                return finalDamage;
            }

            inventory?.HandleHeroDeath();
            EnterFallenState();
        }

        return finalDamage;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f || isFallen) return;
        inventory = inventory != null ? inventory : GetComponent<HeroInventory>();
        if (inventory != null && inventory.TrySplitHeal(amount, out float splitAmount))
        {
            amount = splitAmount;
        }
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        if (hpSlider != null) hpSlider.value = currentHealth;
    }

    public void RestoreToFull()
    {
        currentHealth = maxHealth;
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = currentHealth;
        }
    }

    public void ApplyPermanentGrowth(float maxHealthMultiplier)
    {
        maxHealth *= Mathf.Max(1f, maxHealthMultiplier);
        RestoreToFull();
    }

    public void ReviveFromAegis()
    {
        ExitFallenState();
        maxHealth *= 1.2f;
        RestoreToFull();
        GameplayNotificationController.Show("Егіда повернула Ке́нама до бою.");
    }

    private void EnterFallenState()
    {
        if (isFallen) return;

        isFallen = true;
        currentHealth = 0f;
        if (hpSlider != null) hpSlider.value = 0f;

        SetHeroControl(false);
        SetHeroVisibility(false);

        Camera camera = Camera.main;
        if (camera != null)
        {
            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow != null) follow.target = null;

            FreeCameraController freeCamera = camera.GetComponent<FreeCameraController>();
            if (freeCamera == null) freeCamera = camera.gameObject.AddComponent<FreeCameraController>();
            freeCamera.enabled = true;
        }

        GameplayNotificationController.Show("Ке́нам вибитий з бою. Купіть Егіду, щоб повернути героя.");
        Debug.Log("Player fell. The battle continues without direct hero control.");
    }

    private void ExitFallenState()
    {
        if (!isFallen) return;

        isFallen = false;
        SetHeroControl(true);
        SetHeroVisibility(true);

        Camera camera = Camera.main;
        if (camera != null)
        {
            FreeCameraController freeCamera = camera.GetComponent<FreeCameraController>();
            if (freeCamera != null) freeCamera.enabled = false;

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow != null) follow.target = transform;
        }
    }

    private void SetHeroControl(bool enabled)
    {
        SetComponentEnabled<HeroController>(enabled);
        SetComponentEnabled<HeroBasicAttack>(enabled);
        SetComponentEnabled<EchoSpellbookController>(enabled);
        SetComponentEnabled<HeroVisualAnimator>(enabled);

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = enabled;
        }
    }

    private void SetHeroVisibility(bool visible)
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.enabled = visible;
        }
    }

    private void SetComponentEnabled<T>(bool enabled) where T : Behaviour
    {
        T component = GetComponent<T>();
        if (component != null) component.enabled = enabled;
    }
}
