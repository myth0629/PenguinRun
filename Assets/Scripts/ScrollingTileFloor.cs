using UnityEngine;

public class ScrollingTileFloor : MonoBehaviour
{
    [Header("Tile Movement")]
    [SerializeField] private float scrollSpeed = 6.5f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float tileWidth = 18f;
    [SerializeField] private Transform[] tiles;

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

        float baseSpeed = GameSpeedController.Speed > 0f ? GameSpeedController.Speed : scrollSpeed;
        float speed = baseSpeed * speedMultiplier;
        float move = speed * Time.deltaTime;
        for (int i = 0; i < tiles.Length; i++)
        {
            Transform tile = tiles[i];
            tile.position += Vector3.left * move;

            if (tile.position.x <= -tileWidth)
            {
                float maxX = GetMaxTileX();
                tile.position = new Vector3(maxX + tileWidth, tile.position.y, tile.position.z);
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
}
