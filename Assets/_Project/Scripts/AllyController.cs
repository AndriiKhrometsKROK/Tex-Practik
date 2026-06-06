using System.Collections;
using UnityEngine;

public class AllyController : MonoBehaviour
{
    private UnitData unitData;
    
    // Поточні динамічні характеристики юніта в грі
    private float currentHp;
    private float currentMoveSpeed;
    private Coroutine barrierWaitCoroutine;

    public UnitMovementState CurrentMovementState { get; private set; } = UnitMovementState.Moving;

    /// <summary>
    /// Метод для ініціалізації юніта даними після спавну
    /// </summary>
    public void Initialize(UnitData data)
    {
        unitData = data;
        
        // Призначаємо стартові стато за даними ScriptableObject
        currentHp = unitData.maxHp;
        currentMoveSpeed = unitData.moveSpeed;
        CurrentMovementState = UnitMovementState.Moving;

        // Змінюємо назву об'єкта на сцені для зручності
        gameObject.name = unitData.unitName;

        Debug.Log($"{unitData.unitName} успішно активований із HP: {currentHp}");
    }

    void Update()
    {
        // Тут буде ваша логіка руху союзника в стилі Tower Wars:
        // наприклад, рух вперед із швидкістю currentMoveSpeed:
        if (CurrentMovementState == UnitMovementState.Wait) return;

        transform.Translate(Vector3.right * currentMoveSpeed * Time.deltaTime);
    }

    private void OnDisable()
    {
        if (barrierWaitCoroutine != null)
        {
            StopCoroutine(barrierWaitCoroutine);
            barrierWaitCoroutine = null;
        }

        CurrentMovementState = UnitMovementState.Moving;
    }

    public void WaitAtBarrier(float duration)
    {
        if (duration <= 0f) return;
        if (barrierWaitCoroutine != null) return;

        barrierWaitCoroutine = StartCoroutine(BarrierWaitRoutine(duration));
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

    private IEnumerator BarrierWaitRoutine(float duration)
    {
        CurrentMovementState = UnitMovementState.Wait;
        yield return new WaitForSeconds(duration);

        CurrentMovementState = UnitMovementState.Moving;
        barrierWaitCoroutine = null;
    }

    private void Die()
    {
        Debug.Log($"{unitData.unitName} загинув!");
        Destroy(gameObject);
    }
}
