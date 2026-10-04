using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// DBD-style co-op skill check gate.
// Both players stand in zones -> dial appears with spinning needle.
// Both press (E + Enter) when needle is in the highlighted zone. 3 hits opens gate.
public sealed class CoopSkillGate2D : MonoBehaviour
{
    [Header("Gate")]
    [SerializeField] private Collider2D gateBlocker;
    [SerializeField] private GameObject gateVisual;

    [Header("Zones (triggers)")]
    [SerializeField] private Collider2D zoneOne;
    [SerializeField] private Collider2D zoneTwo;

    [Header("Keys")]
    [SerializeField] private Key playerOneKey = Key.E;
    [SerializeField] private Key playerTwoKey = Key.Enter;

    [Header("Skill Check")]
    [SerializeField] private int requiredSuccesses = 3;
    [SerializeField] private float needleSpeed = 200f;
    [SerializeField] private float zoneSize = 70f;
    [SerializeField] private float pressWindow = 0.35f;

    private AstronautMovement2D playerOne;
    private AstronautMovement2D playerTwo;

    private Canvas canvas;
    private RectTransform needleRect;
    private RectTransform zoneRect;
    private Text countText;

    private float needleAngle;
    private float zoneCenter;
    private int successCount;
    private float p1Time = -999f;
    private float p2Time = -999f;
    private bool isOpen;
    private bool showingDial;

    private void Awake()
    {
        var movers = FindObjectsByType<AstronautMovement2D>(FindObjectsSortMode.None);
        System.Array.Sort(movers, (a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.Ordinal));
        if (movers.Length > 0) playerOne = movers[0];
        if (movers.Length > 1) playerTwo = movers[1];

        BuildDialUI();
    }

    private void BuildDialUI()
    {
        GameObject cObj = new GameObject("SkillCheckCanvas");
        canvas = cObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        // Dial background (dark circle)
        GameObject bgObj = new GameObject("DialBG");
        bgObj.transform.SetParent(cObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.sprite = Sprite.Create(MakeCircleTex(256, new Color(0.1f, 0.1f, 0.1f, 0.85f), 0f, 128f),
            new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        bg.rectTransform.sizeDelta = new Vector2(300, 300);
        bg.rectTransform.anchoredPosition = Vector2.zero;

        // Success zone (green wedge)
        GameObject zObj = new GameObject("Zone");
        zObj.transform.SetParent(cObj.transform, false);
        Image zImg = zObj.AddComponent<Image>();
        zImg.sprite = Sprite.Create(MakeCircleTex(256, new Color(0.2f, 0.9f, 0.3f, 0.9f), 0f, 128f),
            new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        zImg.type = Image.Type.Filled;
        zImg.fillMethod = Image.FillMethod.Radial360;
        zImg.fillOrigin = (int)Image.Origin360.Top;
        zImg.fillAmount = zoneSize / 360f;
        zoneRect = zImg.rectTransform;
        zoneRect.sizeDelta = new Vector2(280, 280);
        zoneRect.anchoredPosition = Vector2.zero;

        // Needle (white line from center)
        GameObject nObj = new GameObject("Needle");
        nObj.transform.SetParent(cObj.transform, false);
        Image nImg = nObj.AddComponent<Image>();
        nImg.color = Color.white;
        Texture2D white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
        nImg.sprite = Sprite.Create(white, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f));
        needleRect = nImg.rectTransform;
        needleRect.sizeDelta = new Vector2(6, 140);
        needleRect.anchoredPosition = Vector2.zero;
        needleRect.pivot = new Vector2(0.5f, 0f);

        // Count text
        GameObject tObj = new GameObject("CountText");
        tObj.transform.SetParent(cObj.transform, false);
        countText = tObj.AddComponent<Text>();
        countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countText.fontSize = 36;
        countText.alignment = TextAnchor.MiddleCenter;
        countText.color = Color.white;
        RectTransform trt = countText.rectTransform;
        trt.sizeDelta = new Vector2(300, 60);
        trt.anchoredPosition = new Vector2(0, -200);
        countText.text = $"0 / {requiredSuccesses}";

        canvas.enabled = false;
    }

    private Texture2D MakeCircleTex(int size, Color col, float innerR, float outerR)
    {
        Texture2D tex = new Texture2D(size, size);
        Vector2 c = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, (d >= innerR && d <= outerR) ? col : Color.clear);
            }
        tex.Apply();
        return tex;
    }

    private void Update()
    {
        if (isOpen) return;

        bool p1In = IsInZone(playerOne, zoneOne);
        bool p2In = IsInZone(playerTwo, zoneTwo);

        if (p1In && p2In && !showingDial)
        {
            showingDial = true;
            canvas.enabled = true;
            RandomizeZone();
        }
        else if ((!p1In || !p2In) && showingDial)
        {
            showingDial = false;
            canvas.enabled = false;
        }

        if (!showingDial) return;

        needleAngle += needleSpeed * Time.deltaTime;
        if (needleAngle >= 360f) needleAngle -= 360f;
        needleRect.rotation = Quaternion.Euler(0, 0, -needleAngle);

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[playerOneKey].wasPressedThisFrame) p1Time = Time.time;
        if (kb[playerTwoKey].wasPressedThisFrame) p2Time = Time.time;

        if (p1Time > -900f && p2Time > -900f && Mathf.Abs(p1Time - p2Time) <= pressWindow)
        {
            if (IsNeedleInZone())
            {
                successCount++;
                countText.text = $"{successCount} / {requiredSuccesses}";
                if (successCount >= requiredSuccesses)
                {
                    isOpen = true;
                    canvas.enabled = false;
                    if (gateBlocker != null) gateBlocker.enabled = false;
                    if (gateVisual != null) gateVisual.SetActive(false);
                    if (zoneOne != null) zoneOne.gameObject.SetActive(false);
                    if (zoneTwo != null) zoneTwo.gameObject.SetActive(false);

                }
                else RandomizeZone();
            }
            p1Time = -999f;
            p2Time = -999f;
        }
    }

    private bool IsInZone(AstronautMovement2D p, Collider2D z)
    {
        return p != null && z != null && z.OverlapPoint(p.transform.position);
    }

    private bool IsNeedleInZone()
    {
        return Mathf.Abs(Mathf.DeltaAngle(needleAngle, zoneCenter)) <= zoneSize / 2f;
    }

    private void RandomizeZone()
    {
        zoneCenter = Random.Range(0f, 360f);
        zoneRect.rotation = Quaternion.Euler(0, 0, -zoneCenter + zoneSize / 2f);
    }
}
