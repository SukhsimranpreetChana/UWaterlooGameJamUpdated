using UnityEngine;
using UnityEngine.UI;

// Attach to the GameSystems object next to OxygenSystem2D.
// Builds the full HUD in code: P1 fuel top-left, O2 top-center, P2 fuel top-right.
// Bars shrink by resizing their RectTransform (bulletproof, no Image.Type dependency).
[DisallowMultipleComponent]
public sealed class Hud2D : MonoBehaviour
{
    [Header("References (optional - auto-finds if empty)")]
    [SerializeField] private OxygenSystem2D oxygenSystem;
    [SerializeField] private AstronautMovement2D playerOne;
    [SerializeField] private AstronautMovement2D playerTwo;

    [Header("Style")]
    [SerializeField] private Color oxygenColor = new Color(0.35f, 0.9f, 1f);
    [SerializeField] private Color fuelColorA = new Color(1f, 0.75f, 0.3f);
    [SerializeField] private Color fuelColorB = new Color(0.7f, 0.95f, 0.4f);

    private Image oxygenFillImage;
    private RectTransform oxygenFillRect;
    private RectTransform fuelFillRectA;
    private RectTransform fuelFillRectB;
    private GameObject gameOverPanel;

    private void Awake()
    {
        if (oxygenSystem == null)
            oxygenSystem = OxygenSystem2D.Instance;
        if (oxygenSystem == null)
            oxygenSystem = FindFirstObjectByType<OxygenSystem2D>();

        if (playerOne == null)
            playerOne = FindPlayer("Player1", "Player 1");
        if (playerTwo == null)
            playerTwo = FindPlayer("Player2", "Player 2");

        BuildHud();
    }

    private AstronautMovement2D FindPlayer(params string[] names)
    {
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
            {
                var m = go.GetComponent<AstronautMovement2D>();
                if (m != null) return m;
            }
        }
        return null;
    }

    private void BuildHud()
    {
        GameObject canvasGo = new GameObject("HUD Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // P1 fuel: top-left. Shared O2: top-center. P2 fuel: top-right.
        fuelFillRectA = MakeBar(canvas.transform,
            new Vector2(0f, 1f), new Vector2(130f, -64f),
            new Vector2(220f, 18f), "P1 FUEL", fuelColorA);
        oxygenFillRect = MakeBar(canvas.transform,
            new Vector2(0.5f, 1f), new Vector2(0f, -64f),
            new Vector2(300f, 20f), "O2", oxygenColor);
        fuelFillRectB = MakeBar(canvas.transform,
            new Vector2(1f, 1f), new Vector2(-130f, -64f),
            new Vector2(220f, 18f), "P2 FUEL", fuelColorB);

        BuildGameOverPanel(canvas.transform);
    }

    // Returns the Fill's RectTransform. Bar shrinks by moving anchorMax.x.
    private RectTransform MakeBar(Transform parent, Vector2 anchor, Vector2 anchoredPos,
        Vector2 size, string labelText, Color fillColor)
    {
        GameObject bgGo = new GameObject(labelText + " Bar");
        bgGo.transform.SetParent(parent, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = anchor;
        bgRt.anchorMax = anchor;
        bgRt.anchoredPosition = anchoredPos;
        bgRt.sizeDelta = size;
        Image bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        Image fill = fillGo.AddComponent<Image>();
        fill.color = fillColor;
        if (labelText == "O2")
            oxygenFillImage = fill;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(bgGo.transform, false);
            RectTransform labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 1f);
            labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, 36f);
            labelRt.sizeDelta = new Vector2(300f, 56f);

            Text label = labelGo.AddComponent<Text>();
            label.font = font;
            label.text = labelText;
            label.fontSize = 42;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        return fillRt;
    }

    private void BuildGameOverPanel(Transform parent)
    {
        gameOverPanel = new GameObject("GameOver Panel");
        gameOverPanel.transform.SetParent(parent, false);
        RectTransform pRt = gameOverPanel.AddComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        Image dim = gameOverPanel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(gameOverPanel.transform, false);
            RectTransform tRt = textGo.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.5f);
            tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(900f, 220f);
            Text t = textGo.AddComponent<Text>();
            t.font = font;
            t.text = "YOU DRIFTED APART\n\nPress R to retry";
            t.fontSize = 77;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
        }

        gameOverPanel.SetActive(false);
    }

    private static void SetBar(RectTransform fillRect, float fraction)
    {
        if (fillRect == null) return;
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    private void Update()
    {
        if (oxygenSystem != null)
        {
            float f = oxygenSystem.OxygenFraction;
            SetBar(oxygenFillRect, f);
            if (oxygenFillImage != null)
                oxygenFillImage.color = f < 0.25f
                    ? Color.Lerp(Color.red, oxygenColor, Mathf.PingPong(Time.unscaledTime * 4f, 1f))
                    : oxygenColor;
        }

        if (playerOne != null)
            SetBar(fuelFillRectA, playerOne.FuelFraction);
        if (playerTwo != null)
            SetBar(fuelFillRectB, playerTwo.FuelFraction);

        bool over = oxygenSystem != null && oxygenSystem.IsGameOver;
        if (gameOverPanel != null && gameOverPanel.activeSelf != over)
            gameOverPanel.SetActive(over);
    }
}
