using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;

    void Update()
    {
        // Летит всегда "вперед" (вправо для спрайта)
        transform.position += transform.right * speed * Time.deltaTime;

        // Самоуничтожение через 3 секунды, чтобы не забивать память
        Destroy(gameObject, 3f);
    }
}
