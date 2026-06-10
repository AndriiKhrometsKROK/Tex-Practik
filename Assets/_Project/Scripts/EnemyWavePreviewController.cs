using System.Text;
using TMPro;
using UnityEngine;

public class EnemyWavePreviewController : MonoBehaviour
{
    private EnemySpawner spawner;
    private GameManager gameManager;
    private TextMeshProUGUI previewText;

    public void Configure(EnemySpawner targetSpawner, GameManager manager, TextMeshProUGUI targetText)
    {
        spawner = targetSpawner;
        gameManager = manager;
        previewText = targetText;

        if (gameManager != null)
        {
            gameManager.WaveChanged += HandleWaveChanged;
            gameManager.StateChanged += HandleStateChanged;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (gameManager == null) return;

        gameManager.WaveChanged -= HandleWaveChanged;
        gameManager.StateChanged -= HandleStateChanged;
    }

    private void HandleWaveChanged(int current, int total)
    {
        Refresh();
    }

    private void HandleStateChanged(GameState state)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (previewText == null || spawner == null || spawner.waves == null)
        {
            return;
        }

        int index = spawner.CurrentWaveIndex + 1;
        if (index >= spawner.waves.Length)
        {
            previewText.text = "Це остання хвиля";
            return;
        }

        previewText.text = $"Хвиля {index + 1}\n{BuildDescription(spawner.waves[index])}\nПрава лінія • захист";
    }

    private static string BuildDescription(Wave wave)
    {
        if (wave == null) return "Немає даних про хвилю";

        StringBuilder result = new StringBuilder();
        if (wave.enemies != null && wave.enemies.Length > 0)
        {
            foreach (WaveEnemyConfig enemy in wave.enemies)
            {
                if (enemy == null || enemy.count <= 0) continue;
                if (result.Length > 0) result.Append("  •  ");
                result.Append(Translate(enemy.enemyName));
                result.Append(" ×");
                result.Append(enemy.count);
            }
        }
        else
        {
            result.Append("Випадкові вороги ×");
            result.Append(Mathf.Max(0, wave.enemyCount));
        }

        return result.ToString();
    }

    private static string Translate(string value)
    {
        return value switch
        {
            "Zombie" => "Зомбі",
            "Fighting Dog" => "Бойовий пес",
            "Wizard" => "Чаклун",
            _ => string.IsNullOrWhiteSpace(value) ? "Ворог" : value
        };
    }
}
