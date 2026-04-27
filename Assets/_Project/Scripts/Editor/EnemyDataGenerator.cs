#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class EnemyDataGenerator
{
    // Цей атрибут створює нову кнопку у верхньому меню Unity
    [MenuItem("TD/Згенерувати характеристики ворогів")]
    public static void GenerateData()
    {
        // Перевіряємо, чи існує папка для даних, і створюємо її, якщо ні
        string folderPath = "Assets/_Project/Data";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Створюємо 8 файлів з нашими готовими показниками
        // Параметри: Назва, HP, Швидкість, Золото, Мін. шкода, Макс. шкода, Швидкість атаки, Шкода вежі, Броня, Маг. спротив
        CreateEnemy("Zombie",          90f,  2.5f, 15, 17f, 24f, 1.0f, 1f,   0f, 0.20f);
        CreateEnemy("ZombieNoArms",    50f,  3.0f, 10,  8f, 12f, 1.2f, 0.5f, 0f, 0.10f);
        CreateEnemy("Wizard",          60f,  1.5f, 25, 30f, 45f, 2.0f, 2f,   0f, 0.75f);
        CreateEnemy("WarriorFrog",     130f, 2.0f, 20, 15f, 20f, 0.8f, 1f,   3f, 0.15f);
        CreateEnemy("WizardZombie",    85f,  1.8f, 22, 20f, 30f, 1.5f, 2f,   0f, 0.50f);
        CreateEnemy("ScruffyDog",      35f,  4.5f,  8,  5f, 10f, 0.6f, 1f,   0f, 0.00f);
        CreateEnemy("FightingDog",     75f,  3.8f, 18, 12f, 18f, 0.7f, 1f,   2f, 0.05f);
        CreateEnemy("Knight",          220f, 1.0f, 40, 25f, 35f, 1.8f, 3f,  10f, 0.10f);

        // Зберігаємо зміни
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Успіх! 8 файлів UnitData створено у папці: " + folderPath);
    }

    private static void CreateEnemy(string eName, float hp, float speed, int gold, float minDmg, float maxDmg, float atkRate, float twrDmg, float armor, float magicRes)
    {
        UnitData data = ScriptableObject.CreateInstance<UnitData>();
        
        data.unitName = eName;
        data.maxHp = hp;
        data.moveSpeed = speed;
        data.goldReward = gold;
        data.minDamage = minDmg;
        data.maxDamage = maxDmg;
        data.attackRate = atkRate;
        data.towerDamage = twrDmg;
        data.armor = armor;
        data.magicResistance = magicRes;

        string assetPath = $"Assets/_Project/Data/{eName}_Data.asset";
        AssetDatabase.CreateAsset(data, assetPath);
    }
}
#endif