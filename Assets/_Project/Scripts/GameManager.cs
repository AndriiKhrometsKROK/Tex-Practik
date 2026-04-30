using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Це і є "номер телефону" нашого банку. 
    // Будь-який скрипт зможе написати GameManager.Instance, щоб звернутися сюди.
    public static GameManager Instance { get; private set; }

    [Header("Економіка")]
    public int currentGold = 100; // Твій стартовий капітал

    // Awake спрацьовує найпершим, навіть раніше за Start
    private void Awake()
    {
        // Перевіряємо, чи ми єдиний банк у грі
        if (Instance == null)
        {
            Instance = this; // Тепер ми - головний банк
        }
        else
        {
            Destroy(gameObject); // Якщо вже є інший банк, цей видаляємо
        }
    }

    // Функція, яку викликатимуть вороги, коли помирають
    public void AddGold(int amount)
    {
        currentGold += amount;
        // Debug.Log виводить текст у вікно Console в Unity, щоб ти бачив, що все працює
        Debug.Log("Гроші додано! Тепер у вас: " + currentGold);
    }

    // Функція для купівлі веж
    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            Debug.Log("Купівля успішна! Залишилося: " + currentGold);
            return true; // Кажемо: "Так, гроші є, купуй"
        }
        else
        {
            Debug.Log("Грошей не вистачає!");
            return false; // Кажемо: "Ні, замало золота"
        }
    }
}