// Відображає в інтерфейсі чергу союзних юнітів, які вийдуть із наступною хвилею.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AllyQueueHudController : MonoBehaviour
{
    private AllyWaveManager waveManager;
    private IncomeManager incomeManager;
    private TextMeshProUGUI queueText;
    private TextMeshProUGUI laneText;
    private Button upperLaneButton;
    private Button lowerLaneButton;
    private readonly List<Button> purchaseButtons = new List<Button>();

    public void Configure(
        AllyWaveManager manager,
        TextMeshProUGUI targetQueueText,
        TextMeshProUGUI targetLaneText,
        Button targetUpperLaneButton,
        Button targetLowerLaneButton,
        IEnumerable<Button> buttons)
    {
        waveManager = manager;
        incomeManager = IncomeManager.Instance != null
            ? IncomeManager.Instance
            : FindAnyObjectByType<IncomeManager>();
        queueText = targetQueueText;
        laneText = targetLaneText;
        upperLaneButton = targetUpperLaneButton;
        lowerLaneButton = targetLowerLaneButton;

        purchaseButtons.Clear();
        if (buttons != null) purchaseButtons.AddRange(buttons);

        if (waveManager != null) waveManager.QueueChanged += Refresh;
        if (incomeManager != null) incomeManager.EssenceChanged += HandleEssenceChanged;
        Refresh();
    }

    private void Update()
    {
        RefreshButtonStates();
    }

    private void OnDestroy()
    {
        if (waveManager != null) waveManager.QueueChanged -= Refresh;
        if (incomeManager != null) incomeManager.EssenceChanged -= HandleEssenceChanged;
    }

    private void HandleEssenceChanged(int value)
    {
        RefreshButtonStates();
    }

    private void Refresh()
    {
        if (queueText != null)
        {
            int count = waveManager != null ? waveManager.QueuedUnits.Count : 0;
            queueText.text = count == 0 ? "Черга порожня" : $"До атаки готові: {count}";
        }

        if (laneText != null && waveManager != null)
        {
            laneText.text = waveManager.SelectedLane == BattleLane.Upper
                ? "ЛІВА ЛІНІЯ • АТАКА"
                : "ПРАВА ЛІНІЯ • ЗАХИСТ";
        }

        if (waveManager != null)
        {
            if (upperLaneButton != null) upperLaneButton.interactable = waveManager.SelectedLane != BattleLane.Upper;
            if (lowerLaneButton != null) lowerLaneButton.interactable = waveManager.SelectedLane != BattleLane.Lower;
        }

        RefreshButtonStates();
    }

    private void RefreshButtonStates()
    {
        if (waveManager == null || incomeManager == null) return;

        for (int i = 0; i < purchaseButtons.Count; i++)
        {
            Button button = purchaseButtons[i];
            if (button == null || i >= waveManager.AvailableUnits.Count) continue;

            button.interactable = incomeManager.currentEssence >= waveManager.AvailableUnits[i].essenceCost;
        }
    }
}
