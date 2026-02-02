using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [Header("Parallax Scroll")]
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private float speedMultiplier = 0.4f;
    [SerializeField] private float tileWidth = 20f;
    [SerializeField] private Transform[] tiles;
    [SerializeField] private bool autoTileWidth = true;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool useCameraBounds = true;
    [SerializeField] private float recycleBuffer = 0.1f;
    [SerializeField] private float overlap = 0f;

    private float cachedTileWidth;

    private void Reset()
    {
        tiles = new Transform[transform.childCount];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = transform.GetChild(i);
        }
    }

    private void Update()
    {
        if (tiles == null || tiles.Length == 0)
        {
            return;
        }

        if (autoTileWidth && cachedTileWidth <= 0f)
        {
            cachedTileWidth = ResolveTileWidth();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        float baseSpeed = GameSpeedController.Speed > 0f ? GameSpeedController.Speed : scrollSpeed;
        float speed = baseSpeed * speedMultiplier;
        float move = speed * Time.deltaTime;
        float width = autoTileWidth ? cachedTileWidth : tileWidth;
        float camLeft = useCameraBounds && targetCamera != null
            ? targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x
            : -width;
        for (int i = 0; i < tiles.Length; i++)
        {
            Transform tile = tiles[i];
            tile.position += Vector3.left * move;

            float rightEdge = tile.position.x + width * 0.5f;
            if (rightEdge <= camLeft - recycleBuffer)
            {
                float maxRightEdge = GetMaxTileRightEdge(width);
                float newCenterX = maxRightEdge + width * 0.5f - overlap;
                tile.position = new Vector3(newCenterX, tile.position.y, tile.position.z);
            }
        }
    }

    private float GetMaxTileX()
    {
        float maxX = tiles[0].position.x;
        for (int i = 1; i < tiles.Length; i++)
        {
            if (tiles[i].position.x > maxX)
            {
                maxX = tiles[i].position.x;
            }
        }
        return maxX;
    }

    private float GetMaxTileRightEdge(float width)
    {
        float maxRight = tiles[0].position.x + width * 0.5f;
        for (int i = 1; i < tiles.Length; i++)
        {
            float right = tiles[i].position.x + width * 0.5f;
            if (right > maxRight)
            {
                maxRight = right;
            }
        }
        return maxRight;
    }

    private float ResolveTileWidth()
    {
        if (tiles == null || tiles.Length == 0)
        {
            return tileWidth;
        }

        Transform first = tiles[0];
        SpriteRenderer sr = first.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            return sr.bounds.size.x;
        }

        Renderer r = first.GetComponent<Renderer>();
        if (r != null)
        {
            return r.bounds.size.x;
        }

        return tileWidth;
    }
}
