using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// When a spike cuts the tether: 5s countdown with warning text.
// Players must get within reconnectDistance to repair.
// Timeout -> loads the drift-apart ending scene.
[DisallowMultipleComponent]
public sealed class RopeCutter2D : MonoBehaviour
{
    public static RopeCutter2D Instance { get; private set; }

    [Header("Reconnect")]
    [SerializeField] private float reconnectTime = 3f;
    [SerializeField] private float reconnectDistance = 3.5f;
    [SerializeField] private string deathScene = "DeathScene";

    public bool IsCut { get; private set; }

    private MonoBehaviour tetherScript;
    private DistanceJoint2D tetherJoint;
    private LineRenderer ropeRenderer;
    private AstronautMovement2D playerOne;
    private AstronautMovement2D playerTwo;

    private float countdown;
    private GameObject warningGo;
    private Text warningText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        GameObject p1 = GameObject.Find("Player1");
        if (p1 == null) p1 = GameObject.Find("Player 1");
        if (p1 != null)
        {
            foreach (var mb in p1.GetComponents<MonoBehaviour>())
                if (mb.GetType().Name == "AstronautTether2D") { tetherScript = mb; break; }
            tetherJoint = p1.GetComponent<DistanceJoint2D>();
            ropeRenderer = p1.GetComponentInChildren<LineRenderer>();
        }

        var movers = FindObjectsByType<AstronautMovement2D>(FindObjectsSortMode.None);
        System.Array.Sort(movers, (a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.Ordinal));
        if (movers.Length > 0) playerOne = movers[0];
        if (movers.Length > 1) playerTwo = movers[1];

        BuildWarning();
    }

    private void BuildWarning()
    {
        GameObject canvasGo = new GameObject("RopeWarning Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        warningGo = new GameObject("Warning");
        warningGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = warningGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 220f);
        rt.sizeDelta = new Vector2(1400f, 160f);

        warningText = warningGo.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) warningText.font = font;
        warningText.fontSize = 52;
        warningText.alignment = TextAnchor.MiddleCenter;
        warningText.color = new Color(1f, 0.3f, 0.3f);
        warningGo.SetActive(false);
    }

    public void CutRope()
    {
        if (IsCut) return;
        IsCut = true;
        countdown = reconnectTime;
        if (tetherScript != null) tetherScript.enabled = false;
        if (tetherJoint != null) tetherJoint.enabled = false;
        if (ropeRenderer != null) ropeRenderer.enabled = false;
        if (warningGo != null) warningGo.SetActive(true);
    }

    private void Update()
    {
        if (!IsCut) return;

        if (playerOne != null && playerTwo != null)
        {
            float d = Vector2.Distance(playerOne.transform.position, playerTwo.transform.position);
            if (d <= reconnectDistance) { RepairRope(); return; }
        }

        countdown -= Time.deltaTime;
        if (warningText != null)
        {
            int s = Mathf.CeilToInt(Mathf.Max(0f, countdown));
            warningText.text = $"drifting apart in {s} seconds...\nreconnect with the other player!";
        }

        if (countdown <= 0f)
            SceneManager.LoadScene(deathScene);
    }

    private void RepairRope()
    {
        if (!IsCut) return;
        IsCut = false;
        if (warningGo != null) warningGo.SetActive(false);
        if (tetherScript != null) tetherScript.enabled = true;
        if (tetherJoint != null) tetherJoint.enabled = true;
        if (ropeRenderer != null) ropeRenderer.enabled = true;
    }
}
