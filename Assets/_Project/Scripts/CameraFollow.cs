using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;        // Наш КеномАрч
    public float smoothTime = 0.3f; // Время "догона" (плавность)
    public Vector2 deadZone = new Vector2(2f, 2f); // Размер окна

    private Vector3 _velocity = Vector3.zero;

    void LateUpdate()
    { // LateUpdate используется для камер, чтобы игрок успел переместиться
        if (target == null) return;

        Vector3 targetPos = transform.position;
        Vector3 delta = target.position - transform.position;

        // Проверяем, вышел ли игрок за границы окна по X и Y
        if (Mathf.Abs(delta.x) > deadZone.x)
            targetPos.x = target.position.x - (Mathf.Sign(delta.x) * deadZone.x);

        if (Mathf.Abs(delta.y) > deadZone.y)
            targetPos.y = target.position.y - (Mathf.Sign(delta.y) * deadZone.y);

        // Z камеры всегда должна быть -10, иначе она ничего не увидит
        targetPos.z = -10f;

        // Плавное движение (интерполяция)
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
    }
}
