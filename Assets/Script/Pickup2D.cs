using UnityEngine;

// Add to an empty GameObject to make a pickup. Set the Type in the Inspector.
// Uses the prefab's own SpriteRenderer (no procedural override).
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
