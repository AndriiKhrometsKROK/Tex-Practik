// Реалізує MOBA-керування героєм мишею та рух до вибраної точки карти.
using UnityEngine;

public class HeroController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField] private Vector2 movementBounds = new Vector2(8.4f, 4.3f);

    private Vector2 targetPosition;
    public bool IsMoving { get; private set; }

    private void Start()
    {
        targetPosition = transform.position;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 position = transform.position;
            position.x = Mathf.Abs(position.x - BattleLaneUtility.AttackX) <
                Mathf.Abs(position.x - BattleLaneUtility.DefenseX)
                ? BattleLaneUtility.DefenseX
                : BattleLaneUtility.AttackX;
            transform.position = position;
            targetPosition = position;
            IsMoving = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPosition.x = Mathf.Clamp(targetPosition.x, -movementBounds.x, movementBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, -movementBounds.y, movementBounds.y);
            IsMoving = true;
        }

        if (!IsMoving) return;

        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, targetPosition) < 0.05f) IsMoving = false;
    }
}
