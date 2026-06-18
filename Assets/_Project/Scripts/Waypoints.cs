// Зберігає сумісний зі старими сценами список контрольних точок маршруту.
using UnityEngine;

public class Waypoints : MonoBehaviour
{
    // Статичний масив точок, щоб будь-який ворог мав до нього швидкий доступ
    public static Transform[] points;

    void Awake()
    {
        // Збираємо всі дочірні об'єкти (точки маршруту) в масив
        points = new Transform[transform.childCount];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = transform.GetChild(i);
        }
    }
}
