using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// Attach to the GameObject with the VideoPlayer.
// Plays intro once, then switches to looping title video + shows Start button.
public sealed class OpeningScene2D : MonoBehaviour
{
    [SerializeField] private string gameScene = "MainScene";
    [SerializeField] private VideoClip titleLoopClip;
    [SerializeField] private GameObject startButton;

    private VideoPlayer vp;

    private void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        if (vp != null)
            vp.loopPointReached += OnIntroFinished;

        if (startButton != null)
            startButton.SetActive(false);
    }

    private void OnIntroFinished(VideoPlayer source)
    {
        if (titleLoopClip != null)
        {
            vp.clip = titleLoopClip;
            vp.isLooping = true;
            vp.Play();
        }
        if (startButton != null)
            startButton.SetActive(true);
    }

    public void OnStartButton()
    {
        SceneManager.LoadScene(gameScene);
    }
}
