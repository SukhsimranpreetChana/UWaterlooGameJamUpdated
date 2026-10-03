using UnityEngine;

// Heavy floating rock. Stays put (no gravity) until a player shoves it.
// Big mass = barely budges. Despawns below camera or after lifetime.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class Rock2D : MonoBehaviour
{
    [SerializeField] private float lifetime = 30f;
    [SerializeField] private float despawnBelowCamera = 6f;

    private Camera mainCam;
    private float age;

    private void Awake()
    {
        mainCam = Camera.main;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (mainCam != null)
        {
            float bottomY = mainCam.transform.position.y - mainCam.orthographicSize;
            if (transform.position.y < bottomY - despawnBelowCamera)
                Destroy(gameObject);
        }
    }
}
