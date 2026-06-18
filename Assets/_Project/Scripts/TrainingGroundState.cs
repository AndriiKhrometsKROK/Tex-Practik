// Зберігає між сценами ознаку того, що наступний запуск бойової сцени має відкрити полігон.
using UnityEngine;

public static class TrainingGroundState
{
    private const string ActiveKey = "Kenam.TrainingGround.Active";

    public static bool IsActive => PlayerPrefs.GetInt(ActiveKey, 0) == 1;

    public static void Request()
    {
        PlayerPrefs.SetInt(ActiveKey, 1);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(ActiveKey);
        PlayerPrefs.Save();
    }
}
