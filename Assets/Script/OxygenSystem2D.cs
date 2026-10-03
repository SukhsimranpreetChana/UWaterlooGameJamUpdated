using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Attach to the GameSystems object. One shared oxygen tank for both players.
// Hits zero -> "You drifted apart", freeze, R to retry.
[DisallowMultipleComponent]
public sealed class OxygenSystem2D : MonoBehaviour
{
    public static OxygenSystem2D Instance { get; private set; }

    [Header("Oxygen")]
    [SerializeField, Min(1f)] private float maxOxygen = 100f;
    [SerializeField, Min(1f)] private float secondsToEmpty = 150f;

    [Header("Game over")]
    [SerializeField] private Key retryKey = Key.R;

    public float Oxygen { get; private set; }
    public float OxygenFraction => maxOxygen > 0f ? Oxygen / maxOxygen : 0f;
    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Oxygen = maxOxygen;
    }

    private void Update()
    {
        if (IsGameOver)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && retryKey != Key.None
                && keyboard[retryKey].wasPressedThisFrame)
                Retry();
            return;
        }

        Oxygen -= maxOxygen / secondsToEmpty * Time.deltaTime;
        if (Oxygen <= 0f)
        {
            Oxygen = 0f;
            IsGameOver = true;
            Time.timeScale = 0f;
        }
    }

    public void AddOxygen(float amount)
    {
        if (IsGameOver) return;
        Oxygen = Mathf.Min(maxOxygen, Oxygen + amount);
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f; // safety so a reload never stays frozen
    }
}
