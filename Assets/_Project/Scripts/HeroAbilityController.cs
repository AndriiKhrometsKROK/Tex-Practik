// Обробляє базові здібності героя, їхню ману, перезарядження, вибір цілі та бойові ефекти.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HeroAbilityController : MonoBehaviour
{
    [SerializeField] private float pulseCooldown = 5f;
    [SerializeField] private float blinkCooldown = 7f;
    [SerializeField] private float recoveryCooldown = 12f;
    [SerializeField] private float pulseDamage = 45f;
    [SerializeField] private float pulseRadius = 2.6f;
    [SerializeField] private float blinkDistance = 3.2f;
    [SerializeField] private float recoveryAmount = 30f;

    private float pulseReadyAt;
    private float blinkReadyAt;
    private float recoveryReadyAt;
    private TextMeshProUGUI pulseText;
    private TextMeshProUGUI blinkText;
    private TextMeshProUGUI recoveryText;

    public void ConfigureUi(TextMeshProUGUI pulse, TextMeshProUGUI blink, TextMeshProUGUI recovery)
    {
        pulseText = pulse;
        blinkText = blink;
        recoveryText = recovery;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) CastPulse();
        if (Input.GetKeyDown(KeyCode.W)) CastBlink();
        if (Input.GetKeyDown(KeyCode.E)) CastRecovery();
        RefreshUi();
    }

    private void CastPulse()
    {
        if (Time.time < pulseReadyAt) return;

        HashSet<EnemyAI> damaged = new HashSet<EnemyAI>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, pulseRadius))
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy != null && damaged.Add(enemy)) enemy.TakeDamage(pulseDamage, true);
        }

        GetComponent<HeroVisualAnimator>()?.PlayAttack();
        pulseReadyAt = Time.time + pulseCooldown;
    }

    private void CastBlink()
    {
        if (Time.time < blinkReadyAt) return;

        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouse - (Vector2)transform.position;
        Vector2 destination = (Vector2)transform.position + Vector2.ClampMagnitude(direction, blinkDistance);
        destination.x = Mathf.Clamp(destination.x, -8.4f, 8.4f);
        destination.y = Mathf.Clamp(destination.y, -4.3f, 4.3f);
        transform.position = destination;
        blinkReadyAt = Time.time + blinkCooldown;
    }

    private void CastRecovery()
    {
        if (Time.time < recoveryReadyAt) return;

        GetComponent<PlayerHealth>()?.Heal(recoveryAmount);
        recoveryReadyAt = Time.time + recoveryCooldown;
    }

    private void RefreshUi()
    {
        RefreshText(pulseText, "Q", "Імпульс", pulseReadyAt);
        RefreshText(blinkText, "W", "Стрибок", blinkReadyAt);
        RefreshText(recoveryText, "E", "Відновлення", recoveryReadyAt);
    }

    private static void RefreshText(TextMeshProUGUI text, string key, string title, float readyAt)
    {
        if (text == null) return;
        float remaining = Mathf.Max(0f, readyAt - Time.time);
        text.text = remaining > 0f ? $"{key}\n{remaining:0.0}" : $"{key}\n{title}";
    }
}
