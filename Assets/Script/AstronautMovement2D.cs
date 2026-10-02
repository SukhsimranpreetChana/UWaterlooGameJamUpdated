using UnityEngine;
using UnityEngine.InputSystem;

// Unity 6 + Input System. Attach once to EACH player's physics root.
// The root's local +Y axis is forward. Put graphics and animation on children.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class AstronautMovement2D : MonoBehaviour
{
    [Header("Keyboard controls")]
    [SerializeField] private Key rotateLeftKey = Key.A;
    [SerializeField] private Key rotateRightKey = Key.D;
    [SerializeField] private Key boostKey = Key.W;

    [Header("Jetpack")]
    [SerializeField, Min(0.01f)] private float thrustAcceleration = 8f;
    [SerializeField, Min(0.01f)] private float maximumSpeed = 6f;
    [SerializeField, Min(0f)] private float thrustRiseTime = 0.18f;
    [SerializeField, Min(0f)] private float thrustFallTime = 0.10f;

    [Header("Rotation")]
    [SerializeField, Min(0.01f)] private float turnSpeed = 240f;
    [SerializeField, Min(0f)] private float turnRampTime = 0.08f;

    [Header("Idle drift")]
    [Tooltip("Starting WORLD direction. Turning does not redirect existing drift.")]
    [SerializeField] private Vector2 initialDriftDirection = Vector2.up;
    [SerializeField, Min(0f)] private float initialDriftSpeed = 0.6f;
    [SerializeField, Min(0f)] private float idleDriftSpeed = 0.35f;
    [SerializeField, Min(0.01f)] private float idleDriftRecovery = 2f;

    private Rigidbody2D body;
    private Vector2 lastTravelDirection;
    private float currentTurnSpeed;

    // Read these from animation / particles / audio scripts later.
    public float ThrustAmount { get; private set; } // 0..1, includes ramp-down.
    public float TurnInput { get; private set; }    // +1 left, -1 right.
    public bool BoostHeld { get; private set; }
    public bool IsThrusting => ThrustAmount > 0.001f;
    public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
    public float Speed => Velocity.magnitude;

    private void Awake()
    {
        ValidateSettings();
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        lastTravelDirection = initialDriftDirection.sqrMagnitude > 0.000001f
            ? initialDriftDirection.normalized
            : Vector2.up;
    }

    private void Start()
    {
        // Only initialize once; re-enabling this component won't restart a drift.
        if (body.linearVelocity.sqrMagnitude < 0.000001f)
        {
            body.linearVelocity = lastTravelDirection
                * Mathf.Max(initialDriftSpeed, idleDriftSpeed);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !Application.isFocused)
        {
            ClearInput();
            return;
        }

        float left = IsKeyHeld(keyboard, rotateLeftKey) ? 1f : 0f;
        float right = IsKeyHeld(keyboard, rotateRightKey) ? 1f : 0f;
        TurnInput = left - right;
        BoostHeld = IsKeyHeld(keyboard, boostKey);
    }

    private void FixedUpdate()
    {
        if (!body.simulated)
            return;

        float dt = Time.fixedDeltaTime;

        // Smooth facing control without turning the existing velocity vector.
        float targetTurnSpeed = TurnInput * turnSpeed;
        currentTurnSpeed = turnRampTime > 0f
            ? Mathf.MoveTowards(currentTurnSpeed, targetTurnSpeed,
                turnSpeed / turnRampTime * dt)
            : targetTurnSpeed;
        body.angularVelocity = currentTurnSpeed;

        float targetThrust = BoostHeld ? 1f : 0f;
        float rampTime = BoostHeld ? thrustRiseTime : thrustFallTime;
        ThrustAmount = rampTime > 0f
            ? Mathf.MoveTowards(ThrustAmount, targetThrust, dt / rampTime)
            : targetThrust;

        Vector2 velocity = body.linearVelocity;
        RememberTravelDirection(velocity);

        // Restore a gentle drift only AFTER the jetpack has stopped firing.
        // Applying a minimum speed during braking would prevent reversal.
        if (!BoostHeld && !IsThrusting && velocity.magnitude < idleDriftSpeed)
        {
            float recoveredSpeed = Mathf.MoveTowards(velocity.magnitude,
                idleDriftSpeed, idleDriftRecovery * dt);
            velocity = lastTravelDirection * recoveredSpeed;
            body.linearVelocity = velocity;
        }

        // Use the PHYSICS rotation, rather than the interpolated graphics pose.
        float angle = body.rotation * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));

        Vector2 targetVelocity = velocity
            + forward * (thrustAcceleration * ThrustAmount * dt);
        targetVelocity = Vector2.ClampMagnitude(targetVelocity, maximumSpeed);

        // Impulse = mass * change in velocity. The dt above makes this
        // continuous acceleration, independent of render frame rate.
        // Add to existing motion so collisions and the tether can affect it.
        Vector2 changeInVelocity = targetVelocity - velocity;
        if (changeInVelocity.sqrMagnitude > 0f)
            body.AddForce(changeInVelocity * body.mass, ForceMode2D.Impulse);

        RememberTravelDirection(targetVelocity);
    }

    // Call after a future respawn teleport. Zero velocity gently resumes idle
    // drift on the next physics step. Freeze body.simulated when actually docking.
    public void ResetMotion(Vector2 worldVelocity)
    {
        ClearInput();
        ThrustAmount = 0f;
        currentTurnSpeed = 0f;
        body.angularVelocity = 0f;
        body.linearVelocity = Vector2.ClampMagnitude(worldVelocity, maximumSpeed);
        RememberTravelDirection(body.linearVelocity);
    }

    private void RememberTravelDirection(Vector2 velocity)
    {
        if (velocity.sqrMagnitude > 0.000001f)
            lastTravelDirection = velocity.normalized;
    }

    private static bool IsKeyHeld(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].isPressed;
    }

    private void ClearInput()
    {
        TurnInput = 0f;
        BoostHeld = false;
    }

    private void OnDisable()
    {
        ClearInput();
        ThrustAmount = 0f;
        currentTurnSpeed = 0f;
        if (body != null)
            body.angularVelocity = 0f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ClearInput();
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        thrustAcceleration = Mathf.Max(0.01f, thrustAcceleration);
        maximumSpeed = Mathf.Max(0.01f, maximumSpeed);
        turnSpeed = Mathf.Max(0.01f, turnSpeed);
        thrustRiseTime = Mathf.Max(0f, thrustRiseTime);
        thrustFallTime = Mathf.Max(0f, thrustFallTime);
        turnRampTime = Mathf.Max(0f, turnRampTime);
        idleDriftSpeed = Mathf.Clamp(idleDriftSpeed, 0f, maximumSpeed);
        initialDriftSpeed = Mathf.Clamp(initialDriftSpeed, 0f, maximumSpeed);
        idleDriftRecovery = Mathf.Max(0.01f, idleDriftRecovery);
    }
}
