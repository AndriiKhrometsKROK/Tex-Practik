// Дозволяє вільно пересувати та масштабувати камеру на великому полі бою.
using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [SerializeField, Min(1f)] private float moveSpeed = 8f;
    [SerializeField, Min(0.1f)] private float zoomSpeed = 1.8f;
    [SerializeField, Min(2f)] private float minZoom = 4f;
    [SerializeField, Min(4f)] private float maxZoom = 14f;

    private Camera controlledCamera;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) movement.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) movement.x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) movement.y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) movement.y += 1f;

        if (movement.sqrMagnitude > 0f)
        {
            transform.position += movement.normalized * moveSpeed * Time.unscaledDeltaTime;
        }

        if (controlledCamera == null) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            controlledCamera.orthographicSize = Mathf.Clamp(
                controlledCamera.orthographicSize - scroll * zoomSpeed,
                minZoom,
                maxZoom);
        }
    }
}
