using UnityEngine;

public class AllyController : MonoBehaviour
{
    private UnitData unitData;
    
    // Поточні динамічні характеристики юніта в грі
    private float currentHp;
    private float currentMoveSpeed;

    /// <summary>
    /// Метод для ініціалізації юніта даними після спавну
    /// </summary>
    public void Initialize(UnitData data)
    {
        unitData = data;
        
        // Призначаємо стартові стато за даними ScriptableObject
        currentHp = unitData.maxHp;
        currentMoveSpeed = unitData.moveSpeed;

        // Змінюємо назву об'єкта на сцені для зручності
        gameObject.name = unitData.unitName;

        Debug.Log($"{unitData.unitName} успішно активований із HP: {currentHp}");
    }

    void Update()
    {
        // Тут буде ваша логіка руху союзника в стилі Tower Wars:
        // наприклад, рух вперед із швидкістю currentMoveSpeed:
        transform.Translate(Vector3.right * currentMoveSpeed * Time.deltaTime);
    }

    public void TakeDamage(float damage, bool isMagic)
    {
        float finalDamage = damage;

        if (isMagic)
        {
            // Зменшуємо магічну шкоду на відсоток спротиву
            finalDamage *= (1f - unitData.magicResistance);
        }
        else
        {
            // Зменшуємо фізичну шкоду на показник броні
            finalDamage -= unitData.armor;
            if (finalDamage < 1) finalDamage = 1; // Юніт має отримати хоча б 1 урон
        }

        currentHp -= finalDamage;
        Debug.Log($"{unitData.unitName} отримав {finalDamage} урону. Залишилось HP: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{unitData.unitName} загинув!");
        Destroy(gameObject);
    }
}