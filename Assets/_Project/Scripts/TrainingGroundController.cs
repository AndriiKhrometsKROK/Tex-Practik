// Налаштовує полігон: нескінченні ресурси, тренувальну ціль, підказки та повернення до хаба.
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingGroundController : MonoBehaviour
{
    private const int InfiniteGold = 999999;
    private const int InfiniteEssence = 999999;

    private GameManager gameManager;
    private IncomeManager incomeManager;

    public void Configure(GameManager manager)
    {
        gameManager = manager;
    }

    private void Start()
    {
        gameManager = gameManager != null ? gameManager : GameManager.Instance;
        incomeManager = IncomeManager.Instance != null ? IncomeManager.Instance : FindAnyObjectByType<IncomeManager>();

        gameManager?.enemySpawner?.StopAllSpawning();
        gameManager?.enemySpawner?.RefreshWaveButton();

        CreateTrainingDummy();
        CreateTrainingBanner();
        RefillResources();
        GameAudioController.PlayMusic(GameMusicTrack.Preparation, 0f);
        GameplayNotificationController.Show("Полігон активний: ресурси безмежні, мішень невразлива.");
    }

    private void Update()
    {
        RefillResources();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToHub();
        }
    }

    public static void ReturnToHub()
    {
        TrainingGroundState.Clear();
        SceneManager.LoadScene("SampleScene");
    }

    private void RefillResources()
    {
        if (gameManager != null && gameManager.currentGold < InfiniteGold)
        {
            gameManager.SetGoldForTesting(InfiniteGold);
        }

        if (incomeManager != null)
        {
            if (incomeManager.currentEssence < InfiniteEssence)
                incomeManager.SetEssenceForTesting(InfiniteEssence);
            if (incomeManager.GoldPerSecond < 999)
                incomeManager.SetGoldPerSecondForTesting(999);
        }
    }

    private void CreateTrainingDummy()
    {
        if (FindAnyObjectByType<TrainingDummyController>() != null) return;

        Sprite white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        GameObject dummy = new GameObject("Training Dummy");
        dummy.transform.position = new Vector3(0f, 1.2f, 0f);

        SpriteRenderer body = dummy.AddComponent<SpriteRenderer>();
        body.sprite = white;
        body.color = KenamUiTheme.WithAlpha(KenamUiTheme.Danger, 0.9f);
        body.sortingOrder = 12;
        dummy.transform.localScale = Vector3.one * 2.2f;
        RuntimeCharacterVisuals.Apply(dummy, RuntimeCharacterSkin.EnemyOrc, 12);

        CircleCollider2D collider = dummy.AddComponent<CircleCollider2D>();
        collider.radius = 0.65f;
        collider.isTrigger = true;

        TrainingDummyController controller = dummy.AddComponent<TrainingDummyController>();

        GameObject labelObject = new GameObject("Training Dummy Label");
        labelObject.transform.SetParent(dummy.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.fontSize = 2.1f;
        label.fontStyle = FontStyles.Bold;
        label.color = KenamUiTheme.Text;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(5.6f, 2.2f);
        label.renderer.sortingOrder = 20;
        controller.Configure(label);
    }

    private void CreateTrainingBanner()
    {
        GameObject bannerObject = new GameObject("Training Ground Banner");
        bannerObject.transform.position = new Vector3(0f, 4.45f, 0f);
        TextMeshPro banner = bannerObject.AddComponent<TextMeshPro>();
        banner.text = "ПОЛІГОН: нескінченне золото й есенція • ЛКМ по мішені • ESC повертає у хаб";
        banner.fontSize = 2.3f;
        banner.fontStyle = FontStyles.Bold;
        banner.color = KenamUiTheme.Gold;
        banner.alignment = TextAlignmentOptions.Center;
        banner.rectTransform.sizeDelta = new Vector2(14f, 0.8f);
        banner.renderer.sortingOrder = 50;
    }
}
