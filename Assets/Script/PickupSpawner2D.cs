using UnityEngine;

// Spawns O2 and Fuel pickups with minimum spacing so they don't clump.
public sealed class PickupSpawner2D : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] o2Prefabs;
    [SerializeField] private GameObject[] fuelPrefabs;

    [Header("Activation")]
    [SerializeField] private float activationY = 35f;

    [Header("O2")]
    [SerializeField] private float o2Interval = 8f;

    [Header("Fuel")]
    [SerializeField] private float fuelInterval = 6f;

    [Header("Spawn Area")]
    [SerializeField] private float spawnAboveTop = -4f;
    [SerializeField] private float spawnBandHeight = 16f;

    [Header("Spacing")]
    [SerializeField] private float minSpawnDistance = 3f;
    [SerializeField] private int spawnAttempts = 10;

    private Camera mainCam;
    private AstronautMovement2D playerOne;
    private AstronautMovement2D playerTwo;
    private float o2Timer;
    private float fuelTimer;
    private bool active;

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
            if (playerOne == null || playerTwo == null) return;
            float midY = (playerOne.transform.position.y + playerTwo.transform.position.y) * 0.5f;
            if (midY <= activationY) return;
            active = true;
        }

        o2Timer -= Time.deltaTime;
        if (o2Timer <= 0f)
        {
            o2Timer = o2Interval;
            TrySpawn(o2Prefabs);
        }

        fuelTimer -= Time.deltaTime;
        if (fuelTimer <= 0f)
        {
            fuelTimer = fuelInterval;
            TrySpawn(fuelPrefabs);
        }
    }

    private void TrySpawn(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0 || mainCam == null) return;

        var pickups = FindObjectsByType<Pickup2D>(FindObjectsSortMode.None);

        float topY = mainCam.transform.position.y + mainCam.orthographicSize;
        float halfW = mainCam.orthographicSize * mainCam.aspect;

        Vector2 pos = Vector2.zero;
        bool foundSpot = false;
        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            float x = mainCam.transform.position.x + Random.Range(-halfW * 0.85f, halfW * 0.85f);
            float y = topY + Random.Range(spawnAboveTop, spawnAboveTop + spawnBandHeight);
            Vector2 candidate = new Vector2(x, y);

            bool tooClose = false;
            foreach (var p in pickups)
            {
                if (p == null) continue;
                if (Vector2.Distance(candidate, (Vector2)p.transform.position) < minSpawnDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                pos = candidate;
                foundSpot = true;
                break;
            }
        }

        if (!foundSpot) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab != null)
            Instantiate(prefab, pos, Quaternion.identity);
    }
}
