// Керує пересуванням Кенама в хабі та взаємодією з найближчими будівлями.
using UnityEngine;
using UnityEngine.EventSystems;

public class HubHeroController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
    [SerializeField] private Vector2 bounds = new Vector2(8.5f, 4.3f);

    private Vector2 target;
    public bool IsMoving { get; private set; }

    private void Start()
    {
        target = transform.position;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            target = camera.ScreenToWorldPoint(Input.mousePosition);
            target.x = Mathf.Clamp(target.x, -bounds.x, bounds.x);
            target.y = Mathf.Clamp(target.y, -bounds.y, bounds.y);
            IsMoving = true;
        }

        if (!IsMoving) return;

        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, target) < 0.05f) IsMoving = false;
    }
}
