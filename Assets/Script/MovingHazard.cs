using UnityEngine;

// Attach to a hazard with Rigidbody2D + Collider2D (e.g., Flying Rock).
// Slides back and forth along a direction. Kinematic, so players collide
// with it but can't push it off course.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MovingHazard2D : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Direction of travel (normalized automatically).")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;
    [Tooltip("How far it goes before turning around.")]
    [SerializeField] private float moveDistance = 6f;
    [Tooltip("Speed in units per second.")]
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D body;
    private Vector2 startPos;
    private float traveled;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        if (moveDirection.sqrMagnitude < 0.0001f)
            moveDirection = Vector2.right;
        moveDirection.Normalize();

        startPos = body.position;
        traveled = 0f;
    }

    private void FixedUpdate()
    {
        traveled += moveSpeed * Time.fixedDeltaTime;
        float t = Mathf.PingPong(traveled, moveDistance);
        body.MovePosition(startPos + moveDirection * t);
    }
}
