using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ThrustSfx2D : MonoBehaviour
{
    [SerializeField] private Key thrustKey = Key.W;
    [SerializeField] private AudioClip thrustClip;
    [SerializeField] private float volume = 0.4f;
    [SerializeField] private float fadeTime = 0.08f;

    private AudioSource audioSource;
    private float targetVolume;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = thrustClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        bool thrusting = kb != null && kb[thrustKey].isPressed;

        targetVolume = thrusting ? volume : 0f;

        if (thrusting && !audioSource.isPlaying)
            audioSource.Play();

        audioSource.volume = Mathf.MoveTowards(
            audioSource.volume, targetVolume, (volume / fadeTime) * Time.deltaTime);

        if (!thrusting && audioSource.isPlaying && audioSource.volume <= 0.001f)
            audioSource.Stop();
    }
}
