// Автоматично зберігає прогрес кампанії у важливі моменти та під час виходу зі сцени.
using UnityEngine;

public class GameAutosaveController : MonoBehaviour
{
    [SerializeField, Min(5f)] private float interval = 20f;
    private float nextSaveTime;

    private void Start()
    {
        nextSaveTime = Time.unscaledTime + interval;
        CampaignProgress.SaveAutosave();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSaveTime) return;
        CampaignProgress.SaveAutosave();
        nextSaveTime = Time.unscaledTime + interval;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) CampaignProgress.SaveAutosave();
    }

    private void OnApplicationQuit()
    {
        CampaignProgress.SaveAutosave();
    }
}
