using System;
using System.Collections;
using UnityEngine;

public class IncomeManager : MonoBehaviour
{
    public static IncomeManager Instance { get; private set; }

    public event Action<int> EssenceChanged;
    public event Action<int> GoldPerSecondChanged;
    public event Action<int> PassiveGoldAdded;

    [Header("Passive Gold")]
    [Min(0)] public int GoldPerSecond = 1;
    [Min(0.1f)] public float incomeInterval = 10f;

    [Header("Essence")]
    [Min(0)] public int currentEssence = 100;

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    private Coroutine _incomeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        ResolveGameManager();
    }

    private void Start()
    {
        ResolveGameManager();
        EssenceChanged?.Invoke(currentEssence);
        GoldPerSecondChanged?.Invoke(GoldPerSecond);
        StartIncome();
    }

    private void OnDisable()
    {
        StopIncome();
    }

    public void StartIncome()
    {
        if (_incomeCoroutine != null) return;

        _incomeCoroutine = StartCoroutine(PassiveIncomeRoutine());
    }

    public void StopIncome()
    {
        if (_incomeCoroutine == null) return;

        StopCoroutine(_incomeCoroutine);
        _incomeCoroutine = null;
    }

    public void AddEssence(int amount)
    {
        if (amount <= 0) return;

        currentEssence += amount;
        EssenceChanged?.Invoke(currentEssence);
    }

    public bool SpendEssence(int amount)
    {
        if (amount < 0) return false;

        if (currentEssence < amount)
        {
            Debug.Log("Not enough essence.");
            return false;
        }

        currentEssence -= amount;
        EssenceChanged?.Invoke(currentEssence);
        return true;
    }

    public void IncreaseGoldPerSecond(int amount)
    {
        if (amount <= 0) return;

        GoldPerSecond += amount;
        GoldPerSecondChanged?.Invoke(GoldPerSecond);
    }

    public bool SpendEssenceForCreepIncome(int essenceCost, int goldPerSecondIncrease)
    {
        if (!SpendEssence(essenceCost)) return false;

        IncreaseGoldPerSecond(goldPerSecondIncrease);
        return true;
    }

    private IEnumerator PassiveIncomeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, incomeInterval));
            AddPassiveGold();
        }
    }

    private void AddPassiveGold()
    {
        if (GoldPerSecond <= 0) return;

        ResolveGameManager();
        if (gameManager == null)
        {
            Debug.LogWarning("IncomeManager cannot add passive gold because GameManager was not found.");
            return;
        }

        int goldAmount = Mathf.RoundToInt(GoldPerSecond * Mathf.Max(0.1f, incomeInterval));
        if (goldAmount <= 0) return;

        gameManager.AddGold(goldAmount);
        PassiveGoldAdded?.Invoke(goldAmount);
    }

    private void ResolveGameManager()
    {
        if (gameManager != null) return;

        gameManager = GameManager.Instance != null
            ? GameManager.Instance
            : FindAnyObjectByType<GameManager>();
    }
}
