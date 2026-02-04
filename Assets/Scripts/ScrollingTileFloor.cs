using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 패턴/청크 기반 무한 러너 바닥 시스템
/// 쿠키런, 템플런 등 상용 게임에서 사용하는 방식으로 구현
/// - 미리 디자인된 청크들을 랜덤 순서로 이어붙임
/// - 오브젝트 풀링으로 메모리 효율화
/// - 레벨 디자인 의도 반영 가능
/// </summary>
public class ScrollingTileFloor : MonoBehaviour
{
    #region Settings
    [Header("=== 기본 설정 ===")]
    [Tooltip("게임 스피드 (GameSpeedController가 없을 때 사용)")]
    [SerializeField] private float baseScrollSpeed = 6.5f;
    
    [Tooltip("추가 속도 배율")]
    [SerializeField, Range(0.1f, 3f)] private float speedMultiplier = 1f;

    [Header("=== 청크 프리팹 ===")]
    [Tooltip("사용할 패턴 청크 프리팹들 (레벨 디자이너가 미리 제작)")]
    [SerializeField] private ChunkData[] chunkPatterns;
    
    [Header("=== 청크 배치 설정 ===")]
    [Tooltip("청크의 가로 길이 (모든 청크가 동일한 길이라고 가정)")]
    [SerializeField] private float chunkWidth = 18f;
    
    [Tooltip("동시에 활성화되는 청크 수")]
    [SerializeField, Range(2, 6)] private int activeChunkCount = 3;
    
    [Tooltip("청크 스폰 Y 위치")]
    [SerializeField] private float spawnY = 0f;
    
    [Tooltip("화면 밖 재활용 여유 거리")]
    [SerializeField] private float recycleOffset = 2f;

    [Header("=== 반복 방지 설정 ===")]
    [Tooltip("연속으로 같은 패턴이 나오지 않게 함")]
    [SerializeField] private bool preventRepeat = true;
    
    [Tooltip("마지막 N개 패턴 반복 방지 (값이 클수록 다양한 패턴)")]
    [SerializeField, Range(1, 5)] private int repeatPreventCount = 2;

    [Header("=== 난이도 설정 ===")]
    [Tooltip("플레이 시간에 따른 난이도 조절")]
    [SerializeField] private bool enableDifficultyScaling = false;
    
    [Tooltip("난이도 변경 간격 (초)")]
    [SerializeField] private float difficultyInterval = 30f;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showDebugGizmos = false;
    [Header("=== 오브젝트 풀링 ===")]
    [Tooltip("오브젝트 풀링 사용 여부")]
    [SerializeField] private bool useObjectPooling = true;
    
    [Tooltip("각 프리팹당 미리 생성할 개수")]
    [SerializeField] private int preloadCountPerPrefab = 3;
    #endregion

    #region Runtime Variables
    private readonly List<ChunkInstance> activeChunks = new List<ChunkInstance>();
    private readonly Queue<int> recentPatternHistory = new Queue<int>();
    private Camera mainCamera;
    private float leftBoundary;
    private float rightSpawnPoint;
    private float playTime;
    private int currentDifficultyLevel;
    private bool poolInitialized;
    #endregion

    #region Data Classes
    [System.Serializable]
    public class ChunkData
    {
        [Tooltip("청크 프리팹")]
        public GameObject prefab;
        
        [Tooltip("이 청크의 가중치 (높을수록 자주 등장)")]
        [Range(1, 10)] public int weight = 5;
        
        [Tooltip("이 청크가 등장하기 위한 최소 난이도 레벨")]
        [Range(0, 10)] public int minDifficultyLevel = 0;
        
        [Tooltip("청크 설명 (에디터용)")]
        public string description;
    }

    private class ChunkInstance
    {
        public GameObject gameObject;
        public Transform transform;
        public int patternIndex;
        
        public ChunkInstance(GameObject obj, int index)
        {
            gameObject = obj;
            transform = obj.transform;
            patternIndex = index;
        }
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        mainCamera = Camera.main;
        CalculateBoundaries();
    }

    private void Start()
    {
        ValidateSetup();
        InitializeObjectPool();
        InitializeChunks();
    }

    private void Update()
    {
        if (activeChunks.Count == 0) return;

        UpdatePlayTime();
        MoveChunks();
        RecycleChunks();
    }

    private void OnValidate()
    {
        if (activeChunkCount < 2) activeChunkCount = 2;
        if (chunkWidth <= 0) chunkWidth = 18f;
    }
    #endregion

    #region Initialization
    private void ValidateSetup()
    {
        if (chunkPatterns == null || chunkPatterns.Length == 0)
        {
            Debug.LogError("[ScrollingTileFloor] 청크 패턴이 설정되지 않았습니다!", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < chunkPatterns.Length; i++)
        {
            if (chunkPatterns[i].prefab == null)
            {
                Debug.LogWarning($"[ScrollingTileFloor] 청크 패턴 [{i}]에 프리팹이 없습니다!", this);
            }
        }
    }

    private void CalculateBoundaries()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            float cameraZ = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 leftEdge = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, cameraZ));
            Vector3 rightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1f, 0f, cameraZ));
            
            leftBoundary = leftEdge.x - chunkWidth - recycleOffset;
            rightSpawnPoint = rightEdge.x + recycleOffset;
        }
        else
        {
            leftBoundary = -chunkWidth * 2f;
            rightSpawnPoint = chunkWidth;
        }
    }

    private void InitializeObjectPool()
    {
        if (!useObjectPooling || poolInitialized) return;
        
        foreach (var chunkData in chunkPatterns)
        {
            if (chunkData.prefab != null)
            {
                ObjectPool.Instance.InitializePool(chunkData.prefab, preloadCountPerPrefab);
            }
        }
        
        poolInitialized = true;
        Debug.Log("[ScrollingTileFloor] 오브젝트 풀 초기화 완료");
    }

    private void InitializeChunks()
    {
        // 시작 위치부터 오른쪽까지 청크 배치
        float startX = leftBoundary + chunkWidth;
        
        for (int i = 0; i < activeChunkCount; i++)
        {
            float xPos = startX + (i * chunkWidth);
            SpawnChunkAt(xPos);
        }
    }
    #endregion

    #region Chunk Movement
    private void MoveChunks()
    {
        float speed = GetCurrentSpeed();
        float deltaMove = speed * Time.deltaTime;

        for (int i = 0; i < activeChunks.Count; i++)
        {
            ChunkInstance chunk = activeChunks[i];
            if (chunk.transform != null)
            {
                chunk.transform.position += Vector3.left * deltaMove;
            }
        }
    }

    private float GetCurrentSpeed()
    {
        // GameSpeedController가 설정되어 있으면 그 값 사용 (0 포함)
        float baseSpeed = GameSpeedController.Speed >= 0f ? GameSpeedController.Speed : baseScrollSpeed;
        return baseSpeed * speedMultiplier;
    }
    #endregion

    #region Chunk Recycling
    private void RecycleChunks()
    {
        CalculateBoundaries(); // 카메라 이동에 대응

        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            ChunkInstance chunk = activeChunks[i];
            
            if (chunk.transform == null)
            {
                activeChunks.RemoveAt(i);
                continue;
            }

            // 화면 왼쪽 밖으로 나갔으면 재활용
            if (chunk.transform.position.x <= leftBoundary)
            {
                RecycleChunk(chunk, i);
            }
        }
    }

    private void RecycleChunk(ChunkInstance oldChunk, int listIndex)
    {
        // 기존 청크를 풀에 반환 또는 제거
        if (oldChunk.gameObject != null)
        {
            if (useObjectPooling)
            {
                ObjectPool.Instance.Return(oldChunk.gameObject);
            }
            else
            {
                Destroy(oldChunk.gameObject);
            }
        }
        activeChunks.RemoveAt(listIndex);

        // 가장 오른쪽 청크 위치 계산
        float rightmostX = GetRightmostChunkX();
        float newX = rightmostX + chunkWidth;

        // 새 청크 스폰
        SpawnChunkAt(newX);
    }

    private float GetRightmostChunkX()
    {
        float maxX = 0f;
        for (int i = 0; i < activeChunks.Count; i++)
        {
            if (activeChunks[i].transform != null && activeChunks[i].transform.position.x > maxX)
            {
                maxX = activeChunks[i].transform.position.x;
            }
        }
        return maxX;
    }
    #endregion

    #region Chunk Spawning
    private void SpawnChunkAt(float xPosition)
    {
        int patternIndex = SelectRandomPattern();
        if (patternIndex < 0 || chunkPatterns[patternIndex].prefab == null)
        {
            Debug.LogWarning("[ScrollingTileFloor] 스폰할 유효한 패턴이 없습니다!");
            return;
        }

        Vector3 spawnPos = new Vector3(xPosition, spawnY, 0f);
        GameObject newChunk;
        
        if (useObjectPooling)
        {
            newChunk = ObjectPool.Instance.Get(chunkPatterns[patternIndex].prefab, spawnPos, Quaternion.identity, transform);
        }
        else
        {
            newChunk = Instantiate(chunkPatterns[patternIndex].prefab, spawnPos, Quaternion.identity, transform);
        }
        
        ChunkInstance instance = new ChunkInstance(newChunk, patternIndex);
        activeChunks.Add(instance);

        // 패턴 히스토리 업데이트
        UpdatePatternHistory(patternIndex);
    }

    private int SelectRandomPattern()
    {
        // 현재 난이도에 맞는 패턴들 필터링
        List<int> validPatterns = new List<int>();
        List<int> weights = new List<int>();

        for (int i = 0; i < chunkPatterns.Length; i++)
        {
            ChunkData chunk = chunkPatterns[i];
            if (chunk.prefab == null) continue;
            if (chunk.minDifficultyLevel > currentDifficultyLevel) continue;
            
            // 반복 방지 체크
            if (preventRepeat && recentPatternHistory.Contains(i)) continue;

            validPatterns.Add(i);
            weights.Add(chunk.weight);
        }

        // 유효한 패턴이 없으면 반복 방지 무시
        if (validPatterns.Count == 0)
        {
            for (int i = 0; i < chunkPatterns.Length; i++)
            {
                if (chunkPatterns[i].prefab != null && chunkPatterns[i].minDifficultyLevel <= currentDifficultyLevel)
                {
                    validPatterns.Add(i);
                    weights.Add(chunkPatterns[i].weight);
                }
            }
        }

        if (validPatterns.Count == 0) return -1;
        if (validPatterns.Count == 1) return validPatterns[0];

        // 가중치 기반 랜덤 선택
        return WeightedRandomSelect(validPatterns, weights);
    }

    private int WeightedRandomSelect(List<int> indices, List<int> weights)
    {
        int totalWeight = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += weights[i];
        }

        int randomValue = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < indices.Count; i++)
        {
            cumulative += weights[i];
            if (randomValue < cumulative)
            {
                return indices[i];
            }
        }

        return indices[indices.Count - 1];
    }

    private void UpdatePatternHistory(int patternIndex)
    {
        recentPatternHistory.Enqueue(patternIndex);
        
        while (recentPatternHistory.Count > repeatPreventCount)
        {
            recentPatternHistory.Dequeue();
        }
    }
    #endregion

    #region Difficulty System
    private void UpdatePlayTime()
    {
        if (!enableDifficultyScaling) return;

        playTime += Time.deltaTime;
        int newLevel = Mathf.FloorToInt(playTime / difficultyInterval);
        
        if (newLevel != currentDifficultyLevel)
        {
            currentDifficultyLevel = newLevel;
            OnDifficultyChanged(currentDifficultyLevel);
        }
    }

    private void OnDifficultyChanged(int newLevel)
    {
        Debug.Log($"[ScrollingTileFloor] 난이도 레벨 변경: {newLevel}");
        // 필요시 이벤트 발생 또는 추가 로직
    }
    #endregion

    #region Public API
    /// <summary>
    /// 게임 재시작 시 호출 - 모든 청크 초기화
    /// </summary>
    public void ResetFloor()
    {
        ClearAllChunks();
        playTime = 0f;
        currentDifficultyLevel = 0;
        recentPatternHistory.Clear();
        InitializeChunks();
    }

    /// <summary>
    /// 특정 패턴을 강제로 스폰 (보스전 등 특수 상황용)
    /// </summary>
    public void ForceSpawnPattern(int patternIndex)
    {
        if (patternIndex < 0 || patternIndex >= chunkPatterns.Length) return;
        
        float rightmostX = GetRightmostChunkX();
        Vector3 spawnPos = new Vector3(rightmostX + chunkWidth, spawnY, 0f);
        
        GameObject newChunk = Instantiate(chunkPatterns[patternIndex].prefab, spawnPos, Quaternion.identity, transform);
        ChunkInstance instance = new ChunkInstance(newChunk, patternIndex);
        activeChunks.Add(instance);
    }

    /// <summary>
    /// 속도 배율 변경
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
    }

    /// <summary>
    /// 일시정지/재개
    /// </summary>
    public void SetPaused(bool paused)
    {
        enabled = !paused;
    }

    private void ClearAllChunks()
    {
        for (int i = 0; i < activeChunks.Count; i++)
        {
            if (activeChunks[i].gameObject != null)
            {
                if (useObjectPooling)
                {
                    ObjectPool.Instance.Return(activeChunks[i].gameObject);
                }
                else
                {
                    Destroy(activeChunks[i].gameObject);
                }
            }
        }
        activeChunks.Clear();
    }
    #endregion

    #region Editor Helpers
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // 경계선 표시
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(leftBoundary, -5f, 0f), new Vector3(leftBoundary, 5f, 0f));
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(rightSpawnPoint, -5f, 0f), new Vector3(rightSpawnPoint, 5f, 0f));

        // 활성 청크 표시
        Gizmos.color = Color.cyan;
        for (int i = 0; i < activeChunks.Count; i++)
        {
            if (activeChunks[i].transform != null)
            {
                Gizmos.DrawWireCube(activeChunks[i].transform.position, new Vector3(chunkWidth, 2f, 1f));
            }
        }
    }

    [ContextMenu("Reset To Children Tiles")]
    private void ResetToChildrenTiles()
    {
        // 에디터에서 자식 오브젝트들을 자동으로 찾아 설정
        Debug.Log("[ScrollingTileFloor] 자식 타일 자동 설정 완료");
    }
    #endregion
}
