using UnityEngine;

// Attach to an empty "Walls" object. Assign the camera and both wall bodies.
// Walls sit exactly on the camera's left/right screen edges on ANY aspect
// ratio, follow it vertically forever, and are bouncy.
[DisallowMultipleComponent]
public sealed class CameraWalls2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Rigidbody2D leftWall;
    [SerializeField] private Rigidbody2D rightWall;

    [Header("Corridor")]
    [Tooltip("On: walls sit exactly at the screen edges. Off: uses Half Corridor Width.")]
    [SerializeField] private bool fitToScreenEdges = true;
    [SerializeField, Min(0.1f)] private float halfCorridorWidth = 9f;
    [SerializeField, Min(0f)] private float verticalMargin = 6f;

    [Header("Bounce")]
    [Tooltip("Wall bounciness. Unity averages this with the player's material (0), so 1 gives 0.5 effective.")]
    [SerializeField, Range(0f, 1f)] private float wallBounciness = 1f;

    private PhysicsMaterial2D bouncyMaterial;

    private void Awake()
    {
        ValidateSettings();
        if (targetCamera == null)
            targetCamera = Camera.main;

        bouncyMaterial = new PhysicsMaterial2D("Wall Bounce (Runtime)")
        {
            bounciness = wallBounciness,
            friction = 0f
        };

        ConfigureWall(leftWall);
        ConfigureWall(rightWall);
    }

    private void ConfigureWall(Rigidbody2D wall)
    {
        if (wall == null)
            return;

        wall.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D col = wall.GetComponent<BoxCollider2D>();
        if (col != null)
            col.sharedMaterial = bouncyMaterial;
    }

    private float GetEdgeDistance()
    {
        if (fitToScreenEdges && targetCamera != null)
            return targetCamera.orthographicSize * targetCamera.aspect;
        return halfCorridorWidth;
    }

    private void SizeWall(Rigidbody2D wall, float viewHeight)
    {
        BoxCollider2D col = wall.GetComponent<BoxCollider2D>();
        if (col != null)
            col.size = new Vector2(1f, viewHeight + verticalMargin * 2f);
    }

    private void FixedUpdate()
    {
        if (targetCamera == null || leftWall == null || rightWall == null)
            return;

        Vector2 camPos = targetCamera.transform.position;
        float edge = GetEdgeDistance();

        // Wall is 1 wide: extra 0.5 puts its INNER face exactly on the screen edge.
        leftWall.MovePosition(new Vector2(camPos.x - edge - 0.5f, camPos.y));
        rightWall.MovePosition(new Vector2(camPos.x + edge + 0.5f, camPos.y));

        float viewHeight = targetCamera.orthographicSize * 2f;
        SizeWall(leftWall, viewHeight);
        SizeWall(rightWall, viewHeight);
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        halfCorridorWidth = Mathf.Max(0.1f, halfCorridorWidth);
        verticalMargin = Mathf.Max(0f, verticalMargin);
        wallBounciness = Mathf.Clamp01(wallBounciness);
    }
}
