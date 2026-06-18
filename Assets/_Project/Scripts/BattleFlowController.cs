// Керує фазами бою, двома фронтами, переходом до фіналу та станом головних веж.
using System;
using System.Collections;
using UnityEngine;

public enum BattlePhase
{
    SeparatedFronts,
    TotalWar,
    BossBattle,
    Finale,
    Completed
}

public class BattleFlowController : MonoBehaviour
{
    public static BattleFlowController Instance { get; private set; }

    public event Action<BattlePhase> PhaseChanged;
    public event Action<int> RageChanged;

    [SerializeField, Min(0f)] private float ragePerMissedCycle = 0.1f;

    public BattlePhase Phase { get; private set; } = BattlePhase.SeparatedFronts;
    public int EnemyRageStacks { get; private set; }
    public FrontTower DefenseFrontTower { get; private set; }
    public FrontTower AttackFrontTower { get; private set; }
    public bool IsTotalWar => Phase != BattlePhase.SeparatedFronts;
    public float EnemyRageMultiplier => 1f + EnemyRageStacks * ragePerMissedCycle;
    public float AllyPowerMultiplier { get; private set; } = 1f;

    private GameManager gameManager;
    private EnemySpawner enemySpawner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Configure(GameManager manager, EnemySpawner spawner)
    {
        gameManager = manager;
        enemySpawner = spawner;
        EnsureFrontTowers();
        if (CampaignProgress.ConsumePendingDemonChallenge())
        {
            StartCoroutine(ChallengeWhenReady());
        }
    }

    public void HandleWaveStarted()
    {
        if (Phase != BattlePhase.SeparatedFronts) return;

        bool hasAttackers = false;
        AllyWaveManager waveManager = FindAnyObjectByType<AllyWaveManager>();
        if (waveManager != null)
        {
            foreach (QueuedAlly queued in waveManager.QueuedUnits)
            {
                if (queued.Lane == BattleLane.Upper)
                {
                    hasAttackers = true;
                    break;
                }
            }
        }
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (ally != null && ally.IsAlive && ally.Lane == BattleLane.Upper)
            {
                hasAttackers = true;
                break;
            }
        }

        if (!hasAttackers)
        {
            EnemyRageStacks++;
            RageChanged?.Invoke(EnemyRageStacks);
            GameplayNotificationController.Show(
                $"Ворожа вежа накопичує лють: +{EnemyRageStacks * 10}% сили ворогам");
        }
    }

    public float GetEnemySpawnMultiplier()
    {
        return Phase == BattlePhase.SeparatedFronts ? EnemyRageMultiplier : 1f;
    }

    // До зламу бар'єра атака та захист ізольовані; ворог спочатку б'є фронтову вежу, а не головний замок.
    public bool HandleEnemyReachedDefense(UnitData data, float damageMultiplier = 1f)
    {
        if (Phase != BattlePhase.SeparatedFronts || DefenseFrontTower == null || !DefenseFrontTower.IsAlive)
        {
            return false;
        }

        float damage = (data != null ? Mathf.Max(1f, data.towerDamage) : 1f) *
            Mathf.Max(0.1f, damageMultiplier);
        DefenseFrontTower.TakeDamage(damage);
        return true;
    }

    public void NotifyFrontTowerDestroyed(FrontTower tower)
    {
        if (tower == null || Phase == BattlePhase.Completed) return;

        if (tower.Side == FrontTowerSide.Defense && Phase == BattlePhase.SeparatedFronts)
        {
            ApplyFrontLossPenalty();
            EnterTotalWar("Оборонну вежу знищено. Сила героя та союзників знижена на 25%.");
        }
        else if (tower.Side == FrontTowerSide.Attack)
        {
            SetPhase(BattlePhase.Completed);
            GameplayNotificationController.Show("Ворожу вежу знищено. Цикл прорвано.");
            gameManager?.SetVictory();
        }
    }

    // Після втрати фронту прибираємо просторові бар'єри й дозволяємо юнітам вільно переходити між лініями.
    public void EnterTotalWar(string reason = null)
    {
        if (Phase != BattlePhase.SeparatedFronts) return;

        SetPhase(BattlePhase.TotalWar);
        BreakBarriers();

        if (!string.IsNullOrWhiteSpace(reason)) GameplayNotificationController.Show(reason);
    }

    public bool CanChallengeDemon()
    {
        return CampaignProgress.SelectedLevel >= 20 &&
            gameManager != null &&
            gameManager.CurrentState == GameState.WaitingForNextWave &&
            Phase != BattlePhase.BossBattle &&
            Phase != BattlePhase.Completed;
    }

    public void ChallengeDemon()
    {
        if (!CanChallengeDemon())
        {
            GameplayNotificationController.Show("Виклик доступний між хвилями, починаючи з рівня 20.");
            return;
        }

        StartBossBattle(GetDemonPowerForLevel(CampaignProgress.SelectedLevel), false);
    }

    public bool TryTriggerForcedFinale()
    {
        if (CampaignProgress.SelectedLevel != CampaignProgress.FinalLevel ||
            Phase == BattlePhase.BossBattle ||
            Phase == BattlePhase.Finale ||
            Phase == BattlePhase.Completed)
        {
            return false;
        }

        SetPhase(BattlePhase.Finale);
        GameplayNotificationController.Show(
            "Ке́нам усвідомив штучність петлі. Правила зламано.");

        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        player?.RestoreToFull();
        FindAnyObjectByType<HeroStats>()?.UnlockGreyMana();
        StartBossBattle(0.2f, true);
        return true;
    }

    public void BossDefeated()
    {
        bool isFinale = Phase == BattlePhase.Finale ||
            CampaignProgress.SelectedLevel == CampaignProgress.FinalLevel;
        SetPhase(BattlePhase.Completed);
        CampaignProgress.CompleteAct2ByDemonChallenge(CampaignProgress.SelectedLevel);
        if (isFinale)
        {
            FinalSequenceController.Play(() => gameManager?.SetVictory());
        }
        else
        {
            gameManager?.SetVictory();
        }
    }

    public static float GetDemonPowerForLevel(int level)
    {
        if (level < 20) return 1f;
        if (level >= CampaignProgress.FinalLevel) return 0.2f;
        return Mathf.Clamp(1f - (level - 20) * 0.1f, 0.4f, 1f);
    }

    // Сила демона залежить від рівня; примусовий фінал додатково змінює сценарій завершення кампанії.
    private void StartBossBattle(float powerMultiplier, bool forcedFinale)
    {
        enemySpawner?.StopAllSpawning();
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            enemy.RemoveWithoutReward();
        }

        EnterTotalWar();
        BreakBarriers();
        SetPhase(forcedFinale ? BattlePhase.Finale : BattlePhase.BossBattle);
        DemonBossController.Spawn(powerMultiplier);
        gameManager?.BeginBossBattle();
        GameplayNotificationController.Show(
            $"Демон матеріалізувався. Сила: {Mathf.RoundToInt(powerMultiplier * 100f)}%");
    }

    private void EnsureFrontTowers()
    {
        FrontTower[] existing = FindObjectsByType<FrontTower>(FindObjectsSortMode.None);
        foreach (FrontTower tower in existing)
        {
            if (tower.Side == FrontTowerSide.Defense) DefenseFrontTower = tower;
            else AttackFrontTower = tower;
        }

        if (DefenseFrontTower == null)
        {
            DefenseFrontTower = FrontTower.Create(
                "Defense Front Tower",
                FrontTowerSide.Defense,
                new Vector2(BattleLaneUtility.DefenseX, -3.85f),
                KenamUiTheme.Mint);
        }

        if (AttackFrontTower == null)
        {
            EnemyCastle castle = FindAnyObjectByType<EnemyCastle>();
            Vector2 position = castle != null
                ? (Vector2)castle.transform.position
                : new Vector2(BattleLaneUtility.AttackX, 3.85f);
            AttackFrontTower = FrontTower.Create(
                "Attack Front Tower",
                FrontTowerSide.Attack,
                position,
                KenamUiTheme.Danger);
            if (castle != null) castle.ConfigureAsFrontTower(AttackFrontTower);
        }
    }

    private void ApplyFrontLossPenalty()
    {
        AllyPowerMultiplier = 0.75f;
        FindAnyObjectByType<HeroStats>()?.ApplyFrontPenalty();
        foreach (AllyController ally in FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            ally.ApplyPermanentMultiplier(0.75f);
        }
    }

    private void SetPhase(BattlePhase phase)
    {
        if (Phase == phase) return;
        Phase = phase;
        PhaseChanged?.Invoke(Phase);
    }

    private void BreakBarriers()
    {
        foreach (Barrier barrier in FindObjectsByType<Barrier>(FindObjectsSortMode.None))
        {
            if (barrier != null) Destroy(barrier.gameObject);
        }
    }

    private IEnumerator ChallengeWhenReady()
    {
        while (gameManager != null && gameManager.CurrentState != GameState.WaitingForNextWave)
        {
            yield return null;
        }
        ChallengeDemon();
    }
}

public enum FrontTowerSide
{
    Defense,
    Attack
}

public class FrontTower : MonoBehaviour
{
    public event Action<float, float> HealthChanged;

    public FrontTowerSide Side { get; private set; }
    public float MaxHealth { get; private set; } = 450f;
    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0f;

    public static FrontTower Create(string name, FrontTowerSide side, Vector2 position, Color color)
    {
        GameObject towerObject = new GameObject(name);
        towerObject.transform.position = position;
        FrontTower tower = towerObject.AddComponent<FrontTower>();
        tower.Side = side;
        tower.CurrentHealth = tower.MaxHealth;

        SpriteRenderer renderer = towerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCastleSprite() ?? Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        renderer.color = renderer.sprite.texture == Texture2D.whiteTexture
            ? color
            : side == FrontTowerSide.Attack
                ? new Color(1f, 0.84f, 0.84f, 1f)
                : Color.white;
        renderer.sortingOrder = 6;
        towerObject.transform.localScale = renderer.sprite.texture == Texture2D.whiteTexture
            ? new Vector3(1.15f, 1.55f, 1f)
            : Vector3.one * 0.68f;

        // The defense objective uses the player's existing castle model.
        renderer.enabled = side != FrontTowerSide.Defense;

        BoxCollider2D collider = towerObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.25f, 2.5f);
        collider.offset = new Vector2(0f, 0.55f);
        collider.isTrigger = true;
        collider.enabled = side != FrontTowerSide.Defense;
        return tower;
    }

    private static Sprite CreateCastleSprite()
    {
        Texture2D props = RuntimeSpriteAssetMap.LoadTexture("Visuals/Props/AtlasProps");
        Sprite citadel = RuntimeSpriteAssetMap.SpriteFromTopLeft(
            props,
            359f,
            194f,
            144f,
            189f,
            64f,
            RuntimeSpriteAssetMap.BottomCenter);
        if (citadel != null) return citadel;

        VisualAssetCatalog assets = Resources.Load<VisualAssetCatalog>("VisualAssetCatalog");
        if (assets != null && assets.tinyAllyCastle != null)
        {
            RuntimeSpriteAssetMap.PreparePixelTexture(assets.tinyAllyCastle);
            return RuntimeSpriteAssetMap.FullSprite(assets.tinyAllyCastle, 100f, RuntimeSpriteAssetMap.BottomCenter);
        }

        return null;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0f)
        {
            BattleFlowController.Instance?.NotifyFrontTowerDestroyed(this);
            gameObject.SetActive(false);
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
