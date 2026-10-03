using UnityEngine;

public sealed class RockSpawner2D : MonoBehaviour
{
    [Header("Prefabs (picks one at random)")]
    [SerializeField] private GameObject[] rockPrefabs;

    [Header("Activation")]
    [SerializeField] private float activationY = 50f;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 0.6f;
    [SerializeField] private float minInterval = 0.3f;
    [SerializeField] private float rampTime = 120f;
    [SerializeField] private int maxAlive = 40;
    [SerializeField] private float spawnAboveTop = 8f;
    [SerializeField] private float spawnBandHeight = 22f;

    [Header("Spacing")]
    [SerializeField] private float minSpawnDistance = 4f;
    [SerializeField] private float minPlayerDistance = 6f;
    [SerializeField] private int spawnAttempts = 20;

    [Header("Random Size (bigger)")]
    [SerializeField] private float minScale = 3f;
    [SerializeField] private float maxScale = 6f;

    [Header("Random Mass")]
    [SerializeField] private float minMass = 7f;
    [SerializeField] private float maxMass = 14f;

    private Camera mainCam;
    private AstronautMovement2D playerOne;
    private AstronautMovement2D playerTwo;
    private float timer;
    private float elapsed;
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

        elapsed += Time.deltaTime;
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        float t = Mathf.Clamp01(elapsed / rampTime);
        timer = Mathf.Lerp(spawnInterval, minInterval, t);

        if (rockPrefabs == null || rockPrefabs.Length == 0 || mainCam == null) return;

        var rocks = FindObjectsByType<Rock2D>(FindObjectsSortMode.None);
        if (rocks.Length >= maxAlive) return;

        float topY = mainCam.transform.position.y + mainCam.orthographicSize;
        float halfW = mainCam.orthographicSize * mainCam.aspect;

        Vector2 pos = Vector2.zero;
        bool foundSpot = false;
        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            float x = mainCam.transform.position.x + Random.Range(-halfW * 0.9f, halfW * 0.9f);
            float y = topY + Random.Range(spawnAboveTop, spawnAboveTop + spawnBandHeight);
            Vector2 candidate = new Vector2(x, y);

            bool tooClose = false;
            foreach (var r in rocks)
            {
                if (r == null) continue;
                if (Vector2.Distance(candidate, (Vector2)r.transform.position) < minSpawnDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose && playerOne != null &&
                Vector2.Distance(candidate, (Vector2)playerOne.transform.position) < minPlayerDistance)
                tooClose = true;
            if (!tooClose && playerTwo != null &&
                Vector2.Distance(candidate, (Vector2)playerTwo.transform.position) < minPlayerDistance)
                tooClose = true;

            if (!tooClose)
            {
                pos = candidate;
                foundSpot = true;
                break;
            }
        }

        if (!foundSpot) return;

        GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
        if (prefab == null) return;

        Quaternion rot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        GameObject rock = Instantiate(prefab, pos, rot);

        float s = Random.Range(minScale, maxScale);
        rock.transform.localScale = new Vector3(s, s, 1f);

        Rigidbody2D rb = rock.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.mass = Random.Range(minMass, maxMass) * s;
    }
}
