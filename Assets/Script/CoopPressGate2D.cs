using UnityEngine;

// Opens when both physical buttons are pressed simultaneously.
public sealed class CoopPressGate2D : MonoBehaviour
{
    [Header("Gate")]
    [SerializeField] private Collider2D gateBlocker;
    [SerializeField] private SlidingGate2D slidingGate;

    [Header("Buttons")]
    [SerializeField] private PressButton2D buttonA;
    [SerializeField] private PressButton2D buttonB;

    private bool isOpen;

    private void Update()
    {
        if (isOpen) return;
        if (buttonA != null && buttonB != null
            && buttonA.IsPressed && buttonB.IsPressed)
            OpenGate();
    }

    private void OpenGate()
    {
        isOpen = true;
        if (gateBlocker != null) gateBlocker.enabled = false;
        if (slidingGate != null) slidingGate.Open();
    }
}
