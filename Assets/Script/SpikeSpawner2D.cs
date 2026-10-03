using UnityEngine;

// Spawns spike clones just above the camera once players leave Phase0.
// Assign the spike prefab in the Inspector.
[DisallowMultipleComponent]
public sealed class SpikeSpawner2D : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject spikePrefab;

    [Header("Activation")]
    [SerializeField] private float activationY = 35f;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private float minInterval = 1.5f;
    [SerializeField] private float rampDuration = 120f;
    [SerializeField] private int maxAlive = 5;
    [SerializeField] private float spawnAboveTop = 2f;

    private Camera mainCam;
    private AstronautMovement2D playerOne;
    private AstronautMovement2D playerTwo;
    private bool active;
    private float timer = 1f;
    private float elapsed;

    private void Awake()
    {
        mainCam = Camera.main;
        var movers = FindObjectsByType<AstronautMovement2D>(FindObjectsSortMode.None);
        System.Array.Sort(movers, (a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.Ordinal));
        if (movers.Length > 0) playerOne = movers[0];
        if (movers.Length > 1) playerTwo = movers[1];
    }

    private void Update()
    {
        if (!active)
        {
            float midY = float.MinValue;
            if (playerOne != null && playerTwo != null)
                midY = (playerOne.transform.position.y + playerTwo.transform.position.y) * 0.5f;
            else if (playerOne != null) midY = playerOne.transform.position.y;
            else if (playerTwo != null) midY = playerTwo.transform.position.y;
            if (midY <= activationY) return;
            active = true;
        }

        elapsed += Time.deltaTime;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Mathf.Lerp(spawnInterval, minInterval, Mathf.Clamp01(elapsed / rampDuration));

        if (spikePrefab == null || mainCam == null) return;
        if (FindObjectsByType<Spike2D>(FindObjectsSortMode.None).Length >= maxAlive) return;

        float topY = mainCam.transform.position.y + mainCam.orthographicSize;
        float halfW = mainCam.orthographicSize * mainCam.aspect;
        float x = mainCam.transform.position.x + Random.Range(-halfW * 0.9f, halfW * 0.9f);
        Instantiate(spikePrefab, new Vector2(x, topY + spawnAboveTop), Quaternion.identity);
    }
}
