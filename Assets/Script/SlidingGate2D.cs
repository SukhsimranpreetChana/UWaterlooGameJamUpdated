using UnityEngine;
using System.Collections;

// Two-piece sliding door. Call Open() to slide LGate left, RGate right.
public sealed class SlidingGate2D : MonoBehaviour
{
    [SerializeField] private Transform lGate;
    [SerializeField] private Transform rGate;
    [SerializeField] private float slideDistance = 3f;
    [SerializeField] private float slideDuration = 0.8f;

    private bool isOpen;

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        StartCoroutine(Slide());
    }

    private System.Collections.IEnumerator Slide()
    {
        Vector3 lStart = lGate.position;
        Vector3 rStart = rGate.position;
        Vector3 lEnd = lStart + Vector3.left * slideDistance;
        Vector3 rEnd = rStart + Vector3.right * slideDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float e = t * t * (3f - 2f * t); // smoothstep
            lGate.position = Vector3.Lerp(lStart, lEnd, e);
            rGate.position = Vector3.Lerp(rStart, rEnd, e);
            yield return null;
        }
    }
}
