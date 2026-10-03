using UnityEngine;

// Attach to the orthographic Main Camera. Assign both PLAYER ROOTS.
// The default camera follows upward travel through a vertical corridor.
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class TwoPlayerCamera2D : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private Transform playerOne;
    [SerializeField] private Transform playerTwo;

    [Header("Smooth following")]
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.3f;
    [SerializeField, Min(0f)] private float upwardLookAhead = 2f;
    [SerializeField] private float cameraZ = -10f;

    [Header("Vertical corridor - match the INNER edges of your walls")]
    [SerializeField] private bool centerOnCorridor = true;
    [SerializeField] private float leftBoundaryX = -9f;
    [SerializeField] private float rightBoundaryX = 9f;

    [Header("Smooth zoom")]
    [SerializeField, Min(0.1f)] private float minimumSize = 7f;
    [SerializeField, Min(0f)] private float padding = 1.2f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.4f;

    [Header("Fixed zoom")]
    [SerializeField] private bool lockZoom = true;
    [SerializeField, Min(0.1f)] private float fixedSize = 9f;

    private Camera view;
    private Vector3 followVelocity;
    private float zoomVelocity;
    private bool initialized;

    private void Awake()
    {
        ValidateSettings();
        view = GetComponent<Camera>();
        view.orthographic = true;
    }

    private void OnEnable()
    {
        initialized = false;
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    private void LateUpdate()
    {
        if (playerOne == null || playerTwo == null)
            return;

        // Start at the correct position instead of flying in from the origin.
        if (!initialized)
        {
            SnapToPlayers();
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector3 target = GetTargetPosition();

        // Read interpolated transforms after physics, not Rigidbody2D.position.
        transform.position = Vector3.SmoothDamp(transform.position, target,
            ref followVelocity, followSmoothTime, Mathf.Infinity, dt);

        float targetSize = lockZoom ? fixedSize : GetTargetSize(target);
        view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize,
            targetSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, dt);
    }

    // Optional: call after a respawn to reset the camera's smoothing.
    public void SnapToPlayers()
    {
        if (playerOne == null || playerTwo == null)
            return;

        if (view == null)
            view = GetComponent<Camera>();

        Vector3 target = GetTargetPosition();
        view.orthographic = true;
        transform.position = target;
        view.orthographicSize = lockZoom ? fixedSize : GetTargetSize(target);
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
        initialized = true;
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 midpoint = (playerOne.position + playerTwo.position) * 0.5f;

        // Keeping X fixed prevents side-to-side camera wobble in this level.
        if (centerOnCorridor)
            midpoint.x = (leftBoundaryX + rightBoundaryX) * 0.5f;

        midpoint.y += upwardLookAhead;
        midpoint.z = cameraZ;
        return midpoint;
    }

    private float GetTargetSize(Vector3 target)
    {
        float halfWidth;
        if (centerOnCorridor)
        {
            // Fit the full corridor, so normal sideways movement doesn't zoom.
            halfWidth = (rightBoundaryX - leftBoundaryX) * 0.5f + padding;
        }
        else
        {
            halfWidth = Mathf.Max(Mathf.Abs(playerOne.position.x - target.x),
                Mathf.Abs(playerTwo.position.x - target.x)) + padding;
        }

        float halfHeight = Mathf.Max(Mathf.Abs(playerOne.position.y - target.y),
            Mathf.Abs(playerTwo.position.y - target.y)) + padding;

        return Mathf.Max(minimumSize, Mathf.Max(halfHeight,
            halfWidth / Mathf.Max(0.01f, view.aspect)));
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
        zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime);
        minimumSize = Mathf.Max(0.1f, minimumSize);
        padding = Mathf.Max(0f, padding);
        upwardLookAhead = Mathf.Max(0f, upwardLookAhead);
        fixedSize = Mathf.Max(0.1f, fixedSize);
        rightBoundaryX = Mathf.Max(leftBoundaryX + 0.1f, rightBoundaryX);
    }
}
