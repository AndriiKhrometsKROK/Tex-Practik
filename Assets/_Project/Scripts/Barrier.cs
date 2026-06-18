// Реалізує просторовий бар'єр між фронтами та тимчасово зупиняє юнітів, які його перетинають.
using UnityEngine;

public enum UnitMovementState
{
    Moving,
    Wait
}

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Barrier : MonoBehaviour
{
    [SerializeField] private float waitDuration = 15f;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ConfigureRigidbody();
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ConfigureRigidbody();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsKenomArch(other)) return;

        EnemyAI enemy = GetComponentFromCollider<EnemyAI>(other);
        if (enemy != null)
        {
            enemy.WaitAtBarrier(waitDuration);
            return;
        }

        AllyController ally = GetComponentFromCollider<AllyController>(other);
        if (ally != null)
        {
            ally.WaitAtBarrier(waitDuration);
        }
    }

    private static bool IsKenomArch(Collider2D other)
    {
        return GetComponentFromCollider<HeroController>(other) != null ||
            GetComponentFromCollider<PlayerMovement>(other) != null;
    }

    private static T GetComponentFromCollider<T>(Collider2D other) where T : Component
    {
        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.TryGetComponent(out T rigidbodyComponent))
        {
            return rigidbodyComponent;
        }

        return other.GetComponentInParent<T>();
    }

    private void ConfigureRigidbody()
    {
        Rigidbody2D barrierRigidbody = GetComponent<Rigidbody2D>();
        barrierRigidbody.bodyType = RigidbodyType2D.Kinematic;
        barrierRigidbody.gravityScale = 0f;
        barrierRigidbody.simulated = true;
    }
}
