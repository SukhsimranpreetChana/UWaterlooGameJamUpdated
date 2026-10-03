using UnityEngine;

// Add ONCE, to Player 1's physics root. Assign Player 2's Rigidbody2D.
// One joint controls the astronauts. A separate Verlet chain animates the rope.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(DistanceJoint2D), typeof(LineRenderer))]
public sealed class AstronautTether2D : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private Rigidbody2D otherAstronaut;
    [SerializeField, Min(0.1f)] private float maximumLength = 10f;
    [SerializeField] private bool astronautsCollide = true;

    [Header("Tension")]
    [Tooltip("Gentle inward pull, scaled by separation. A whisper up close, firmer when far apart.")]
    [SerializeField, Min(0f)] private float tensionStiffness = 0.2f;

    [Header("Rope appearance")]
    [Tooltip("Assign an unlit material asset. This also keeps its shader in builds.")]
    [SerializeField] private Material ropeMaterial;
    [SerializeField, Min(0.001f)] private float ropeWidth = 0.06f;
    [SerializeField, Range(4, 48)] private int ropePoints = 24;
    [SerializeField] private Color slackColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField] private Color tautColor = new Color(1f, 0.65f, 0.2f, 1f);
    [SerializeField] private int sortingOrder = -1;

    [Header("Rope motion - visual simulation in zero gravity")]
    [Tooltip("Drag per second. Lower values let the rope swing longer.")]
    [SerializeField, Min(0f)] private float motionDamping = 1.2f;
    [Tooltip("Small bending resistance keeps the rope from forming sharp kinks.")]
    [SerializeField, Range(0f, 1f)] private float bendStiffness = 0.08f;
    [SerializeField, Range(4, 32)] private int constraintIterations = 12;
    [SerializeField, Range(1, 4)] private int simulationSubsteps = 2;
    [Tooltip("Reset the visual rope after a large teleport, such as a respawn.")]
    [SerializeField, Min(0.1f)] private float teleportResetDistance = 3f;

    private Rigidbody2D body;
    private DistanceJoint2D joint;
    private LineRenderer rope;
    private Material runtimeMaterial;
    private Vector3[] points;
    private Vector2[] nodes;
    private Vector2[] nodeHistory;
    private Vector2[] previousFrameNodes;
    private Vector2[] bendTargets;
    private Vector2 lastStart;
    private Vector2 lastEnd;
    private Vector2 lastDirection = Vector2.right;
    private float lastSubstepDuration;
    private float simulatedLength;
    private bool simulationReady;
    private bool wasTaut;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    // Convenient for future HUD / effects. This indicates length, not force.
    public float LengthRatio => body != null && otherAstronaut != null
        ? Vector2.Distance(body.position, otherAstronaut.position) / maximumLength
        : 0f;

    private void Awake()
    {
        ValidateSettings();
        body = GetComponent<Rigidbody2D>();
        joint = GetComponent<DistanceJoint2D>();
        rope = GetComponent<LineRenderer>();

        if (!HasValidConnection())
        {
            joint.enabled = false;
            rope.enabled = false;
            Debug.LogError("AstronautTether2D: assign the OTHER player's Rigidbody2D "
                + "to Other Astronaut, then restart Play mode.", this);
            enabled = false;
            return;
        }

        ConfigureJoint();
        ConfigureRope();
    }

    private void ConfigureJoint()
    {
        joint.connectedBody = otherAstronaut;
        joint.autoConfigureConnectedAnchor = false;
        joint.autoConfigureDistance = false;

        // Center anchors avoid adding unwanted turning forces to the controls.
        joint.anchor = Vector2.zero;
        joint.connectedAnchor = Vector2.zero;
        joint.distance = maximumLength;
        joint.maxDistanceOnly = true;
        joint.enableCollision = astronautsCollide;
        joint.breakForce = Mathf.Infinity;
        joint.enabled = true;
    }

    private void ConfigureRope()
    {
        rope.useWorldSpace = true;
        rope.alignment = LineAlignment.View;
        rope.textureMode = LineTextureMode.Stretch;
        rope.numCapVertices = 4;
        rope.numCornerVertices = 3;
        rope.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        rope.startColor = Color.white;
        rope.endColor = Color.white;
        rope.sortingOrder = sortingOrder;

        if (ropeMaterial == null)
        {
            rope.enabled = false;
            Debug.LogWarning("AstronautTether2D: assign an unlit Rope Material "
                + "before Play mode to see the rope. The physics tether is active.", this);
            return;
        }

        // Own a copy so whitening its tint doesn't change the material asset.
        runtimeMaterial = new Material(ropeMaterial);
        runtimeMaterial.name = "Astronaut Rope (Runtime)";
        if (runtimeMaterial.HasProperty(BaseColorId))
            runtimeMaterial.SetColor(BaseColorId, Color.white);
        if (runtimeMaterial.HasProperty(ColorId))
            runtimeMaterial.SetColor(ColorId, Color.white);
        rope.sharedMaterial = runtimeMaterial;
        rope.enabled = true;
        rope.positionCount = 0;
        simulationReady = false;
    }

    private void FixedUpdate()
    {
        if (!HasValidConnection())
        {
            joint.enabled = false;
            rope.enabled = false;
            return;
        }

        // These settings can be tuned in the Inspector during Play mode.
        if (joint.connectedBody != otherAstronaut)
        {
            ConfigureJoint();
            simulationReady = false;
        }
        if (!Mathf.Approximately(joint.distance, maximumLength))
            joint.distance = maximumLength;
        joint.enableCollision = astronautsCollide;
        ApplyTension();
        rope.enabled = runtimeMaterial != null;

        if (runtimeMaterial != null)
            SimulateRope(Time.fixedDeltaTime);
    }

    // A whisper of inward pull, scaled by separation. Keeps the pair cohesive
    // without ever fighting the joint's max-distance enforcement.
    private void ApplyTension()
    {
        Vector2 delta = otherAstronaut.position - body.position;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return;

        Vector2 force = (delta / distance) * (distance * tensionStiffness);
        body.AddForce(force);
        otherAstronaut.AddForce(-force);
    }

    private void LateUpdate()
    {
        if (runtimeMaterial != null && HasValidConnection())
            UpdateRopeVisual();
    }

    private void UpdateRopeVisual()
    {
        EnsureSimulation();

        // Smooth the simulated nodes at the same rate as the player graphics.
        float alpha = Mathf.Clamp01((Time.time - Time.fixedTime)
            / Mathf.Max(0.0001f, Time.fixedDeltaTime));
        Vector3 start = body.transform.position;
        Vector3 end = otherAstronaut.transform.position;
        int last = nodes.Length - 1;
        Vector2 renderedStart = Vector2.Lerp(previousFrameNodes[0], nodes[0], alpha);
        Vector2 renderedEnd = Vector2.Lerp(previousFrameNodes[last], nodes[last], alpha);
        Vector2 startOffset = new Vector2(start.x, start.y) - renderedStart;
        Vector2 endOffset = new Vector2(end.x, end.y) - renderedEnd;

        for (int i = 0; i < ropePoints; i++)
        {
            float t = (float)i / (ropePoints - 1);
            Vector2 position = Vector2.Lerp(previousFrameNodes[i], nodes[i], alpha)
                + Vector2.Lerp(startOffset, endOffset, t);
            // Use the actual player depth, preserving the Z=0 setup.
            points[i] = new Vector3(position.x, position.y,
                Mathf.Lerp(start.z, end.z, t));
        }

        rope.positionCount = ropePoints;
        rope.widthMultiplier = ropeWidth;
        rope.sortingOrder = sortingOrder;
        rope.SetPositions(points);

        float separation = Vector2.Distance(new Vector2(start.x, start.y),
            new Vector2(end.x, end.y));
        float tautness = Mathf.InverseLerp(0.85f, 1f, separation / maximumLength);
        Color color = Color.Lerp(slackColor, tautColor, tautness);
        rope.startColor = color;
        rope.endColor = color;
    }

    private void EnsureSimulation()
    {
        if (!simulationReady || nodes == null || nodes.Length != ropePoints
            || !Mathf.Approximately(simulatedLength, maximumLength))
        {
            ResetRopeSimulation();
        }
    }

    // Can also be called after a future respawn.
    public void ResetRopeSimulation()
    {
        if (body == null || !HasValidConnection())
            return;

        if (nodes == null || nodes.Length != ropePoints)
        {
            nodes = new Vector2[ropePoints];
            nodeHistory = new Vector2[ropePoints];
            previousFrameNodes = new Vector2[ropePoints];
            bendTargets = new Vector2[ropePoints];
            points = new Vector3[ropePoints];
        }

        lastStart = body.position;
        lastEnd = otherAstronaut.position;
        Vector2 delta = lastEnd - lastStart;
        if (delta.sqrMagnitude > 0.000001f)
            lastDirection = delta.normalized;

        Vector2 normal = new Vector2(-lastDirection.y, lastDirection.x);
        float bow = 0.32f * Mathf.Sqrt(Mathf.Max(0f,
            maximumLength * maximumLength - delta.sqrMagnitude));
        lastSubstepDuration = Time.fixedDeltaTime / simulationSubsteps;

        for (int i = 0; i < ropePoints; i++)
        {
            float t = (float)i / (ropePoints - 1);
            // Seed a little irregularity ONCE, rather than an animated sine wave.
            float curve = Mathf.Sin(Mathf.PI * t)
                + 0.18f * Mathf.Sin(3f * Mathf.PI * t);
            nodes[i] = Vector2.Lerp(lastStart, lastEnd, t) + normal * (bow * curve);
            Vector2 velocity = Vector2.Lerp(body.linearVelocity,
                otherAstronaut.linearVelocity, t);
            nodeHistory[i] = nodes[i] - velocity * lastSubstepDuration;
            previousFrameNodes[i] = nodes[i];
        }

        // Pin exactly, including when the initial bow is very small.
        nodes[0] = lastStart;
        nodes[ropePoints - 1] = lastEnd;
        previousFrameNodes[0] = lastStart;
        previousFrameNodes[ropePoints - 1] = lastEnd;
        simulatedLength = maximumLength;
        wasTaut = delta.magnitude >= maximumLength * 0.995f;
        simulationReady = true;
    }

    private void SimulateRope(float dt)
    {
        EnsureSimulation();
        Vector2 start = body.position;
        Vector2 end = otherAstronaut.position;

        if (Vector2.Distance(start, lastStart) > teleportResetDistance
            || Vector2.Distance(end, lastEnd) > teleportResetDistance)
        {
            ResetRopeSimulation();
        }

        for (int i = 0; i < ropePoints; i++)
            previousFrameNodes[i] = nodes[i];

        float subDt = dt / simulationSubsteps;
        float drag = Mathf.Exp(-motionDamping * subDt);

        for (int step = 0; step < simulationSubsteps; step++)
        {
            float fraction = (float)(step + 1) / simulationSubsteps;
            Vector2 pinnedStart = Vector2.Lerp(lastStart, start, fraction);
            Vector2 pinnedEnd = Vector2.Lerp(lastEnd, end, fraction);
            Vector2 span = pinnedEnd - pinnedStart;
            float separation = span.magnitude;
            if (separation > 0.001f)
                lastDirection = span / separation;

            if (separation >= maximumLength * 0.995f)
            {
                // A fully stretched cable has no room for waves or solver jitter.
                for (int i = 0; i < ropePoints; i++)
                {
                    float t = (float)i / (ropePoints - 1);
                    nodes[i] = Vector2.Lerp(pinnedStart, pinnedEnd, t);
                    nodeHistory[i] = nodes[i] - Vector2.Lerp(body.linearVelocity,
                        otherAstronaut.linearVelocity, t) * subDt;
                }
                wasTaut = true;
            }
            else
            {
                if (wasTaut)
                {
                    SeedBuckling(pinnedStart, pinnedEnd);
                    wasTaut = false;
                }

                float timeRatio = subDt / Mathf.Max(0.0001f, lastSubstepDuration);
                for (int i = 1; i < ropePoints - 1; i++)
                {
                    Vector2 current = nodes[i];
                    Vector2 motion = (current - nodeHistory[i]) * timeRatio * drag;
                    nodeHistory[i] = current;
                    nodes[i] = current + motion;
                }

                nodes[0] = pinnedStart;
                nodes[ropePoints - 1] = pinnedEnd;
                ApplyBending(subDt);
                SolveSegments(pinnedStart, pinnedEnd);
            }
            lastSubstepDuration = subDt;
        }

        lastStart = start;
        lastEnd = end;
    }

    private void SeedBuckling(Vector2 start, Vector2 end)
    {
        // Break perfect numerical symmetry when a straight cable becomes slack.
        // Translate history too, so this doesn't inject an artificial velocity.
        Vector2 normal = new Vector2(-lastDirection.y, lastDirection.x);
        float seed = Mathf.Min(0.025f, 0.05f * Mathf.Sqrt(Mathf.Max(0f,
            maximumLength * maximumLength - (end - start).sqrMagnitude)));
        for (int i = 1; i < ropePoints - 1; i++)
        {
            float t = (float)i / (ropePoints - 1);
            Vector2 offset = normal * (seed * Mathf.Sin(Mathf.PI * t)
                * (0.65f * Mathf.Sin(3f * Mathf.PI * t)
                + 0.35f * Mathf.Sin(5f * Mathf.PI * t)));
            nodes[i] += offset;
            nodeHistory[i] += offset;
        }
    }

    private void ApplyBending(float dt)
    {
        float strength = Mathf.Clamp01(bendStiffness * 600f * dt * dt);
        for (int i = 1; i < ropePoints - 1; i++)
            bendTargets[i] = (nodes[i - 1] + nodes[i + 1]) * 0.5f;
        for (int i = 1; i < ropePoints - 1; i++)
            nodes[i] = Vector2.Lerp(nodes[i], bendTargets[i], strength);
    }

    private void SolveSegments(Vector2 start, Vector2 end)
    {
        int last = ropePoints - 1;
        float segmentLength = maximumLength / last;
        for (int iteration = 0; iteration < constraintIterations; iteration++)
        {
            // Alternate directions so neither astronaut gets a solver bias.
            for (int step = 0; step < last; step++)
            {
                int i = iteration % 2 == 0 ? step : last - 1 - step;
                Vector2 delta = nodes[i + 1] - nodes[i];
                float distance = delta.magnitude;
                Vector2 direction = distance > 0.00001f
                    ? delta / distance : lastDirection;
                Vector2 correction = direction * (distance - segmentLength);

                if (i == 0)
                    nodes[i + 1] -= correction;
                else if (i + 1 == last)
                    nodes[i] += correction;
                else
                {
                    nodes[i] += correction * 0.5f;
                    nodes[i + 1] -= correction * 0.5f;
                }
            }
            nodes[0] = start;
            nodes[last] = end;
        }
    }

    private bool HasValidConnection()
    {
        return otherAstronaut != null && otherAstronaut != body;
    }

    private void OnEnable()
    {
        simulationReady = false;
        if (joint != null)
            joint.enabled = HasValidConnection();
        if (rope != null)
            rope.enabled = HasValidConnection() && runtimeMaterial != null;
    }

    private void OnDisable()
    {
        if (joint != null)
            joint.enabled = false;
        if (rope != null)
            rope.enabled = false;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        maximumLength = Mathf.Max(0.1f, maximumLength);
        tensionStiffness = Mathf.Max(0f, tensionStiffness);
        ropeWidth = Mathf.Max(0.001f, ropeWidth);
        ropePoints = Mathf.Clamp(ropePoints, 4, 48);
        motionDamping = Mathf.Max(0f, motionDamping);
        bendStiffness = Mathf.Clamp01(bendStiffness);
        constraintIterations = Mathf.Clamp(constraintIterations, 4, 32);
        simulationSubsteps = Mathf.Clamp(simulationSubsteps, 1, 4);
        teleportResetDistance = Mathf.Max(0.1f, teleportResetDistance);
    }
}
