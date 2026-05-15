using UnityEngine;
using UnityEditor;
using System.IO;

public class AllyDataGenerator
{
    [MenuItem("Tools/Generate Ally Unit Data")]
    public static void GenerateAllies()
    {
        // Перевіряємо чи існує папка, якщо ні - створюємо
        string folderPath = "Assets/_Project/Data/Allies";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parentFolder = "Assets/_Project/Data";
            if (!AssetDatabase.IsValidFolder(parentFolder)) AssetDatabase.CreateFolder("Assets/_Project", "Data");
            AssetDatabase.CreateFolder(parentFolder, "Allies");
        }

        // Створюємо персонажів
        CreateUnitData(folderPath, "Peasant", "Селянин", 50f, 1.5f, 0, 3f, 6f, 1.2f, 1f, 0f, 0f);
        CreateUnitData(folderPath, "Samurai", "Самурай", 110f, 1.8f, 0, 15f, 22f, 0.8f, 3f, 2f, 0.1f);
        CreateUnitData(folderPath, "Knight", "Лицар", 150f, 1.2f, 0, 10f, 18f, 1.5f, 4f, 5f, 0.1f);
        CreateUnitData(folderPath, "Alchemist", "Алхімік", 80f, 1.1f, 0, 8f, 25f, 2.0f, 2f, 1f, 0.6f);
        CreateUnitData(folderPath, "Armored_Knight", "Лицар в броні", 250f, 0.8f, 0, 12f, 16f, 2.0f, 5f, 12f, 0.2f);
        CreateUnitData(folderPath, "Spear_Knight", "Лицар із списом", 130f, 1.1f, 0, 12f, 35f, 1.8f, 6f, 4f, 0.1f);
        CreateUnitData(folderPath, "Monk_Bell", "Монах-свічка з дзвоном", 120f, 1.0f, 0, 5f, 10f, 1.5f, 1f, 2f, 0.75f);
        CreateUnitData(folderPath, "Monk_Hoodless", "Монах-свічка без каптура", 90f, 1.3f, 0, 14f, 24f, 1.2f, 2f, 1f, 0.4f);
        CreateUnitData(folderPath, "CuteDog", "Милий песик", 9999f, 10f, 0, 9999f, 9999f, 0.1f, 9999f, 999f, 1f);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Усі файли позитивних персонажів успішно згенеровано в папці: " + folderPath);
    }

    private static void CreateUnitData(string path, string fileName, string unitName, float hp, float speed, int gold, float minDmg, float maxDmg, float rate, float towerDmg, float armor, float magicRes)
    {
        string fullPath = $"{path}/{fileName}_Data.asset";
        
        // Якщо файл вже існує, не перезаписуємо його, щоб не стерти майбутні зміни
        if (AssetDatabase.LoadAssetAtPath<UnitData>(fullPath) != null)
        {
            Debug.Log($"Файл {fileName}_Data вже існує. Пропускаємо.");
            return;
        }

        UnitData asset = ScriptableObject.CreateInstance<UnitData>();
        
        asset.unitName = unitName;
        asset.maxHp = hp;
        asset.moveSpeed = speed;
        asset.goldReward = gold;
        
        asset.minDamage = minDmg;
        asset.maxDamage = maxDmg;
        asset.attackRate = rate;
        asset.towerDamage = towerDmg;
        
        asset.armor = armor;
        asset.magicResistance = magicRes;

        AssetDatabase.CreateAsset(asset, fullPath);
    }
}