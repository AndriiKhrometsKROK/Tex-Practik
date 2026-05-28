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
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                enemy.GetComponent<EnemyAI>()?.TakeDamage(damage);
                Debug.Log("Magic hit: " + enemy.name);
            }
        }
    }

    void Update()
    {
        if (!isAOE)
        {
            transform.position += transform.right * 10f * Time.deltaTime;
        }
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _returnCoroutine = null;
        ObjectPoolManager.Return(gameObject);
    }
}