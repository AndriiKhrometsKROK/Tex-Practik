using System.Collections;
using UnityEngine;

public class SpellEffect : MonoBehaviour
{
    public float damage = 50f;
    public float radius = 2f;
    public float lifetime = 3f;
    public bool isAOE = false;

    private Coroutine _returnCoroutine;

    void OnEnable()
    {
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
        }

        if (isAOE)
        {
            ApplyAOE();
            _returnCoroutine = StartCoroutine(ReturnAfterDelay(0.5f));
        }
        else
        {
            _returnCoroutine = StartCoroutine(ReturnAfterDelay(lifetime));
        }
    }

    private void OnDisable()
    {
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
    }

    void ApplyAOE()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, true);
                continue;
            }

            hit.GetComponentInParent<EnemyCastle>()?.TakeDamage(damage);
        }
    }

    void Update()
    {
        if (!isAOE)
        {
            transform.position += transform.right * 10f * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isAOE) return;

        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, true);
            ObjectPoolManager.Return(gameObject);
            return;
        }

        EnemyCastle castle = other.GetComponentInParent<EnemyCastle>();
        if (castle != null)
        {
            castle.TakeDamage(damage);
            ObjectPoolManager.Return(gameObject);
        }
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _returnCoroutine = null;
        ObjectPoolManager.Return(gameObject);
    }
}
