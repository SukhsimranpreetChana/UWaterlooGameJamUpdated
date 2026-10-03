using UnityEngine;

// Add to an empty GameObject to make a pickup. Set the Type in the Inspector.
// Builds its own trigger + visual in code. Cyan ring = oxygen, orange = fuel.
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class Pickup2D : MonoBehaviour
{
    public enum PickupType { Oxygen, Fuel }

    [Header("Pickup")]
    [SerializeField] private PickupType type = PickupType.Oxygen;
    [SerializeField, Min(0f)] private float oxygenAmount = 25f;
    [SerializeField, Min(0f)] private float fuelAmount = 40f;

    [Header("Float")]
    [SerializeField, Min(0f)] private float bobAmplitude = 0.35f;
    [SerializeField, Min(0f)] private float bobSpeed = 2f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip collectSound;

    private Vector3 basePosition;
    private float bobPhase;

    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;

        basePosition = transform.position;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        BuildVisual();
    }

    private void BuildVisual()
    {
        // Soft ring texture, generated at runtime.
        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) / 32f;
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) * 3.2f);
                float core = Mathf.Clamp01(1f - d * 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(ring, core * 0.9f)));
            }
        }
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 32f);

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -1;
        sr.color = type == PickupType.Oxygen
            ? new Color(0.35f, 0.9f, 1f)
            : new Color(1f, 0.65f, 0.25f);

        transform.localScale = Vector3.one * 0.5f;
    }

    private void Update()
    {
        transform.position = basePosition
            + Vector3.up * Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;
        transform.Rotate(0f, 0f, 40f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var astro = other.GetComponent<AstronautMovement2D>();
        if (astro == null)
            return;

        if (type == PickupType.Oxygen)
        {
            if (OxygenSystem2D.Instance != null)
                OxygenSystem2D.Instance.AddOxygen(oxygenAmount);
        }
        else
        {
            astro.AddFuel(fuelAmount);
        }

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        Destroy(gameObject);
    }
}
