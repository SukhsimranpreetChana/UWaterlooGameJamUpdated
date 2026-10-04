using UnityEngine;

public sealed class EndingTrigger2D : MonoBehaviour
{
    [SerializeField] private string winScene = "Ending";
    private int playersInside;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var astro = other.GetComponent<AstronautMovement2D>();
        if (astro == null && other.transform.parent != null)
            astro = other.transform.parent.GetComponent<AstronautMovement2D>();
        if (astro == null) return;

        playersInside++;
        if (playersInside >= 2)
            UnityEngine.SceneManagement.SceneManager.LoadScene(winScene);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var astro = other.GetComponent<AstronautMovement2D>();
        if (astro == null && other.transform.parent != null)
            astro = other.transform.parent.GetComponent<AstronautMovement2D>();
        if (astro != null) playersInside = Mathf.Max(0, playersInside - 1);
    }
}
