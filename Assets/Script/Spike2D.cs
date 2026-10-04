using UnityEngine;

// Unstoppable spike. Nudges rocks aside (slightly) and passes through.
// Player hit = knockback. Rope graze = CUT.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class Spike2D : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float flySpeed = 4f;
    [SerializeField] private float lifetime = 12f;

    [Header("Hit")]
    [SerializeField] private float knockbackForce = 7f;
    [SerializeField] private float rockShoveForce = 10f;
    [SerializeField] private float ropeCutRadius = 0.6f;

    private Rigidbody2D body;
    private AstronautMovement2D playerOne;
    private AstronautMovement2D playerTwo;
    private Vector2 flyDir;
    private float age;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        GetComponent<Collider2D>().isTrigger = true;

        var movers = FindObjectsByType<AstronautMovement2D>(FindObjectsSortMode.None);
        System.Array.Sort(movers, (a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.Ordinal));
        if (movers.Length > 0) playerOne = movers[0];
        if (movers.Length > 1) playerTwo = movers[1];

        Vector2 target = GetPlayersMidpoint();
        Vector2 toTarget = target - (Vector2)transform.position;
        flyDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.down;

        // Face the direction of travel
        float angle = Mathf.Atan2(flyDir.y, flyDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= lifetime) { Destroy(gameObject); return; }

        body.MovePosition(body.position + flyDir * flySpeed * Time.fixedDeltaTime);

        if (RopeCutter2D.Instance != null && !RopeCutter2D.Instance.IsCut
            && playerOne != null && playerTwo != null)
        {
            float d = DistancePointToSegment(body.position,
                playerOne.transform.position, playerTwo.transform.position);
            if (d < ropeCutRadius)
            {
                RopeCutter2D.Instance.CutRope();
                Destroy(gameObject);
            }
        }
    }

    private Vector2 GetPlayersMidpoint()
    {
        bool hasOne = playerOne != null, hasTwo = playerTwo != null;
        if (hasOne && hasTwo)
            return (playerOne.transform.position + playerTwo.transform.position) * 0.5f;
        if (hasOne) return playerOne.transform.position;
        if (hasTwo) return playerTwo.transform.position;
        return body.position;
    }

    private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector2.Distance(p, a + ab * t);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rock2D rock = other.GetComponent<Rock2D>();
        if (rock != null)
        {
            Rigidbody2D rockRb = rock.GetComponent<Rigidbody2D>();
            if (rockRb != null)
                rockRb.AddForce(flyDir * rockShoveForce, ForceMode2D.Impulse);
            return;
        }

        AstronautMovement2D astro = other.GetComponent<AstronautMovement2D>();
        if (astro == null && other.transform.parent != null)
            astro = other.transform.parent.GetComponent<AstronautMovement2D>();
        if (astro == null) return;

        Rigidbody2D playerBody = astro.GetComponent<Rigidbody2D>();
        if (playerBody != null)
        {
            Vector2 knockDir = playerBody.position - body.position;
            if (knockDir.sqrMagnitude < 0.0001f) knockDir = Vector2.up;
            playerBody.AddForce(knockDir.normalized * knockbackForce, ForceMode2D.Impulse);
        }
        Destroy(gameObject);
    }
}
