using UnityEngine;
using UnityEngine.UI; // ƒл€ работы с UI

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider hpSlider; // —юда закинем наш ползунок из »ерархии

    void Start()
    {
        currentHealth = maxHealth;
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = currentHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth; // «ащита от оверхила

        if (hpSlider != null) hpSlider.value = currentHealth;

        if (currentHealth <= 0)
        {
            Debug.Log(" еномјрч погиб!");
            Destroy(gameObject);
        }
    }
}