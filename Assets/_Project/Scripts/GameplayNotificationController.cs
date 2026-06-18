// Показує короткі системні повідомлення: нестачу ресурсів, результати покупок і важливі події бою.
using System.Collections;
using TMPro;
using UnityEngine;

public class GameplayNotificationController : MonoBehaviour
{
    public static GameplayNotificationController Instance { get; private set; }

    private TextMeshProUGUI messageText;
    private Coroutine hideCoroutine;

    public void Configure(TextMeshProUGUI targetText)
    {
        Instance = this;
        messageText = targetText;
        gameObject.SetActive(false);
    }

    public static void Show(string message)
    {
        if (Instance == null || string.IsNullOrWhiteSpace(message)) return;
        Instance.ShowInternal(message);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void ShowInternal(string message)
    {
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        messageText.text = message;
        gameObject.SetActive(true);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(2.2f);
        hideCoroutine = null;
        gameObject.SetActive(false);
    }
}
