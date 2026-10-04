using UnityEngine;

public sealed class BackgroundTile2D : MonoBehaviour
{
    [SerializeField] private float tileHeight = 30f;

    private Camera mainCam;

    private void Awake() { mainCam = Camera.main; }

    private void LateUpdate()
    {
        if (mainCam == null) return;
        // If tile is fully below camera, jump it to the top
        float camBottom = mainCam.transform.position.y - mainCam.orthographicSize;
        if (transform.position.y + tileHeight / 2f < camBottom)
            transform.position += new Vector3(0f, tileHeight * 3f, 0f);
    }
}
