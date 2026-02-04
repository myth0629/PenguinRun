using UnityEngine;
using System;
using System.Collections.Generic;

public class ScrollingBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ParallaxLayer[] layers;
    
    [Header("Global Settings")]
    [SerializeField] private float baseScrollSpeed = 5f;
    [SerializeField] private bool useGameSpeedController = true;
    
    [Header("Auto Tile Duplication")]
    [Tooltip("화면을 채우기 위해 타일을 자동으로 복제")]
    [SerializeField] private bool autoFillScreen = true;
    [Tooltip("최소 타일 개수 (화면을 완전히 채우기 위해 최소 2개 권장)")]
    [SerializeField] private int minTileCount = 2;

    /// <summary>
    /// 각 레이어의 패럴랙스 설정
    /// </summary>
    [Serializable]
    public class ParallaxLayer
    {
        [Tooltip("레이어 이름 (디버그용)")]
        public string layerName;
        
        [Tooltip("이 레이어에 포함된 타일들")]
        public Transform[] tiles;
        
        [Tooltip("속도 배율 (0 = 정지, 0.5 = 절반 속도, 1 = 기본 속도)")]
        [Range(0f, 2f)]
        public float speedMultiplier = 1f;
        
        [Tooltip("타일 너비 자동 계산")]
        public bool autoTileWidth = true;
        
        [Tooltip("수동 타일 너비 설정")]
        public float tileWidth = 20f;
        
        [Tooltip("타일 간 오버랩")]
        public float overlap = 0f;
        
        [Tooltip("재활용 버퍼")]
        public float recycleBuffer = 0.1f;
        
        // 캐시된 타일 너비
        [NonSerialized] public float cachedTileWidth;
    }

    private void Reset()
    {
        // 자식 오브젝트들을 기반으로 레이어 자동 설정
        AutoSetupLayers();
    }

    /// <summary>
    /// 하이어라키 구조를 기반으로 레이어 자동 설정
    /// </summary>
    [ContextMenu("Auto Setup Layers")]
    private void AutoSetupLayers()
    {
        int childCount = transform.childCount;
        layers = new ParallaxLayer[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            layers[i] = new ParallaxLayer
            {
                layerName = child.name,
                tiles = GetAllChildTransforms(child),
                // 뒤에 있을수록 느리게 (인덱스 기반 자동 배율)
                speedMultiplier = Mathf.Lerp(0.1f, 1f, (float)i / Mathf.Max(1, childCount - 1)),
                autoTileWidth = true,
                overlap = 0f,
                recycleBuffer = 0.1f
            };
        }
    }

    private Transform[] GetAllChildTransforms(Transform parent)
    {
        // 자식이 있으면 자식들 반환, 없으면 부모 자체 반환
        if (parent.childCount > 0)
        {
            Transform[] children = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                children[i] = parent.GetChild(i);
            }
            return children;
        }
        else
        {
            return new Transform[] { parent };
        }
    }

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // 각 레이어의 타일 너비 캐싱
        CacheTileWidths();
        
        // 화면을 채우기 위해 타일 자동 복제
        if (autoFillScreen)
        {
            DuplicateTilesToFillScreen();
        }
    }

    /// <summary>
    /// 카메라 뷰포트를 채우기 위해 필요한 만큼 타일을 복제
    /// </summary>
    private void DuplicateTilesToFillScreen()
    {
        if (layers == null || targetCamera == null) return;

        // 카메라 뷰포트 너비 계산
        float camWidth = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x 
                       - targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x;

        for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            var layer = layers[layerIndex];
            if (layer.tiles == null || layer.tiles.Length == 0) continue;

            float tileWidth = layer.autoTileWidth ? layer.cachedTileWidth : layer.tileWidth;
            if (tileWidth <= 0f) continue;

            // 화면을 채우는데 필요한 타일 개수 계산 (여유분 포함)
            int requiredTiles = Mathf.CeilToInt(camWidth / tileWidth) + 1;
            requiredTiles = Mathf.Max(requiredTiles, minTileCount);

            int currentTileCount = layer.tiles.Length;
            if (currentTileCount >= requiredTiles) continue;

            // 복제할 개수
            int tilesToCreate = requiredTiles - currentTileCount;
            
            // 원본 타일 (첫 번째 타일 기준으로 복제)
            Transform originalTile = layer.tiles[0];
            Transform parent = originalTile.parent;
            
            List<Transform> newTilesList = new List<Transform>(layer.tiles);

            // 가장 오른쪽 타일의 위치 찾기
            float rightMostX = GetMaxTileRightEdge(layer.tiles, tileWidth);

            for (int i = 0; i < tilesToCreate; i++)
            {
                // 타일 복제
                GameObject duplicatedObj = Instantiate(originalTile.gameObject, parent);
                duplicatedObj.name = originalTile.name + "_Copy" + (i + 1);
                
                // 오른쪽에 차례로 배치
                float newX = rightMostX + tileWidth * 0.5f - layer.overlap;
                duplicatedObj.transform.position = new Vector3(
                    newX,
                    originalTile.position.y,
                    originalTile.position.z
                );
                
                newTilesList.Add(duplicatedObj.transform);
                rightMostX = newX + tileWidth * 0.5f;
            }

            // 레이어의 타일 배열 업데이트
            layer.tiles = newTilesList.ToArray();
            
            Debug.Log($"[ScrollingBackground] '{layer.layerName}' 레이어: {tilesToCreate}개 타일 복제됨 (총 {layer.tiles.Length}개)");
        }
    }

    private void CacheTileWidths()
    {
        if (layers == null) return;

        foreach (var layer in layers)
        {
            if (layer.autoTileWidth && layer.tiles != null && layer.tiles.Length > 0)
            {
                layer.cachedTileWidth = ResolveTileWidth(layer.tiles[0], layer.tileWidth);
            }
            else
            {
                layer.cachedTileWidth = layer.tileWidth;
            }
        }
    }

    private void Update()
    {
        if (layers == null || layers.Length == 0) return;
        if (targetCamera == null) return;

        // GameSpeedController가 설정되어 있으면 그 값 사용 (0 포함)
        float baseSpeed = useGameSpeedController 
            ? GameSpeedController.Speed 
            : baseScrollSpeed;

        float camLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x;

        // 각 레이어별로 개별 스크롤 처리
        foreach (var layer in layers)
        {
            if (layer.tiles == null || layer.tiles.Length == 0) continue;

            float layerSpeed = baseSpeed * layer.speedMultiplier;
            float move = layerSpeed * Time.deltaTime;
            float width = layer.autoTileWidth ? layer.cachedTileWidth : layer.tileWidth;

            // 레이어 내 모든 타일 이동
            foreach (var tile in layer.tiles)
            {
                if (tile == null) continue;
                
                tile.position += Vector3.left * move;

                // 화면 왼쪽을 벗어나면 오른쪽 끝으로 재배치
                float rightEdge = tile.position.x + width * 0.5f;
                if (rightEdge <= camLeft - layer.recycleBuffer)
                {
                    float maxRightEdge = GetMaxTileRightEdge(layer.tiles, width);
                    float newCenterX = maxRightEdge + width * 0.5f - layer.overlap;
                    tile.position = new Vector3(newCenterX, tile.position.y, tile.position.z);
                }
            }
        }
    }

    private float GetMaxTileRightEdge(Transform[] tiles, float width)
    {
        if (tiles == null || tiles.Length == 0) return 0f;

        float maxRight = float.MinValue;
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            float right = tile.position.x + width * 0.5f;
            if (right > maxRight)
            {
                maxRight = right;
            }
        }
        return maxRight;
    }

    private float ResolveTileWidth(Transform tile, float fallback)
    {
        if (tile == null) return fallback;

        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return sr.bounds.size.x;
        }

        Renderer r = tile.GetComponent<Renderer>();
        if (r != null)
        {
            return r.bounds.size.x;
        }

        return fallback;
    }

    /// <summary>
    /// 특정 레이어의 속도 배율을 런타임에 변경
    /// </summary>
    public void SetLayerSpeedMultiplier(int layerIndex, float multiplier)
    {
        if (layers != null && layerIndex >= 0 && layerIndex < layers.Length)
        {
            layers[layerIndex].speedMultiplier = multiplier;
        }
    }

    /// <summary>
    /// 레이어 이름으로 속도 배율 변경
    /// </summary>
    public void SetLayerSpeedMultiplier(string layerName, float multiplier)
    {
        if (layers == null) return;
        foreach (var layer in layers)
        {
            if (layer.layerName == layerName)
            {
                layer.speedMultiplier = multiplier;
                break;
            }
        }
    }
}
