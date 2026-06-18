// Перемикає фонову музику відповідно до фази матчу, небезпеки для замку та появи боса.
using UnityEngine;

public class DynamicAudioController : MonoBehaviour
{
    [SerializeField, Range(0.05f, 0.75f)] private float criticalHealthThreshold = 0.35f;

    private GameManager gameManager;
    private BattleFlowController flow;
    private BattlePhase currentPhase = BattlePhase.SeparatedFronts;

    public void Configure(GameManager manager, BattleFlowController battleFlow)
    {
        gameManager = manager;
        flow = battleFlow;
        if (gameManager != null) gameManager.StateChanged += HandleStateChanged;
        if (gameManager != null) gameManager.BaseHealthChanged += HandleBaseHealthChanged;
        if (flow != null) flow.PhaseChanged += HandlePhaseChanged;
        currentPhase = flow != null ? flow.Phase : BattlePhase.SeparatedFronts;
        HandleStateChanged(gameManager != null ? gameManager.CurrentState : GameState.Preparing);
    }

    private void OnDestroy()
    {
        if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
        if (gameManager != null) gameManager.BaseHealthChanged -= HandleBaseHealthChanged;
        if (flow != null) flow.PhaseChanged -= HandlePhaseChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.WaveInProgress:
                GameAudioController.PlayMusic(GetCombatTrack());
                break;
            case GameState.Preparing:
            case GameState.WaitingForNextWave:
                GameAudioController.PlayMusic(GameMusicTrack.Preparation);
                break;
            case GameState.Victory:
                break;
            case GameState.Defeat:
                GameAudioController.PlayMusic(GameMusicTrack.Critical);
                break;
        }
    }

    private void HandlePhaseChanged(BattlePhase phase)
    {
        currentPhase = phase;
        if (gameManager != null && gameManager.CurrentState == GameState.WaveInProgress)
            GameAudioController.PlayMusic(GetCombatTrack());
    }

    private void HandleBaseHealthChanged(float current, float maximum)
    {
        if (gameManager == null || gameManager.CurrentState != GameState.WaveInProgress) return;
        if (maximum <= 0f) return;
        if (current / maximum <= criticalHealthThreshold) GameAudioController.PlayMusic(GameMusicTrack.Critical);
    }

    private GameMusicTrack GetCombatTrack()
    {
        if (currentPhase == BattlePhase.Finale) return GameMusicTrack.Finale;
        if (currentPhase == BattlePhase.BossBattle) return GameMusicTrack.Boss;
        if (gameManager != null &&
            gameManager.maxBaseHealth > 0f &&
            gameManager.currentBaseHealth / gameManager.maxBaseHealth <= criticalHealthThreshold)
        {
            return GameMusicTrack.Critical;
        }

        return CampaignProgress.SelectedLevel >= 20
            ? GameMusicTrack.LateHardcore
            : GameMusicTrack.Battle;
    }
}
