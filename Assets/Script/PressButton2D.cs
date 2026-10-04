using UnityEngine;

// Physical button. IsPressed = true while any astronaut touches it.
[RequireComponent(typeof(Collider2D))]
public sealed class PressButton2D : MonoBehaviour
{
    public bool IsPressed { get; private set; }

    [SerializeField] private Color idleColor = Color.red;
    [SerializeField] private Color pressedColor = Color.green;

    private SpriteRenderer sr;
    private int touchCount;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = idleColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<AstronautMovement2D>() == null) return;
        touchCount++;
        SetPressed(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<AstronautMovement2D>() == null) return;
        touchCount = Mathf.Max(0, touchCount - 1);
        if (touchCount == 0) SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        IsPressed = pressed;
        if (sr != null) sr.color = pressed ? pressedColor : idleColor;
    }
}
