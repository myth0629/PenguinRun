using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스포너
/// - 화면 오른쪽 밖에서 적 생성
/// - 시간/거리에 따른 난이도 증가
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("=== 스폰 설정 ===")]
    [SerializeField] private List<SpawnData> spawnPool = new List<SpawnData>();
    
    [Tooltip("스폰 쿨다운 (초)")]
    [SerializeField] private float spawnCooldown = 2f;
    
    [Tooltip("카메라 오른쪽 밖 오프셋")]
    [SerializeField] private float spawnOffsetX = 2f;

    [Header("=== 스폰 위치 ===")]
    [Tooltip("지상 유닛 스폰 Y 위치")]
    [SerializeField] private float groundSpawnY = -2f;
    
    [Tooltip("공중 유닛 스폰 Y 범위")]
    [SerializeField] private Vector2 flyingSpawnYRange = new Vector2(0f, 3f);

    [Header("=== 난이도 ===")]
    [Tooltip("시간에 따른 스폰 쿨다운 감소율")]
    [SerializeField] private float cooldownDecreaseRate = 0.01f;
    
    [Tooltip("최소 스폰 쿨다운")]
    [SerializeField] private float minCooldown = 0.5f;

    [Header("=== 참조 ===")]
    [SerializeField] private Camera mainCamera;

    [System.Serializable]
    public struct SpawnData
    {
        public GameObject enemyPrefab;
        public EnemyData enemyData;
        [Range(0f, 100f)]
        public float spawnWeight; // 스폰 확률 가중치
    }

    private float spawnTimer;
    private float totalWeight;
    private float elapsedTime;
    private bool isSpawning = true;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 총 가중치 계산
        CalculateTotalWeight();
    }

    private void Update()
    {
        if (!isSpawning || spawnPool.Count == 0) return;

        elapsedTime += Time.deltaTime;
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            
            // 시간에 따른 쿨다운 감소
            float currentCooldown = Mathf.Max(minCooldown, spawnCooldown - elapsedTime * cooldownDecreaseRate);
            spawnTimer = currentCooldown;
        }
    }

    private void CalculateTotalWeight()
    {
        totalWeight = 0f;
        foreach (var data in spawnPool)
        {
            totalWeight += data.spawnWeight;
        }
    }

    private void SpawnEnemy()
    {
        // 가중치 기반 랜덤 선택
        SpawnData selectedData = SelectRandomEnemy();
        if (selectedData.enemyPrefab == null) return;

        // 스폰 위치 계산
        Vector3 spawnPos = GetSpawnPosition(selectedData.enemyData);

        // 풀에서 가져오기
        GameObject enemy = ObjectPool.Instance.Get(selectedData.enemyPrefab, spawnPos, Quaternion.identity);

        // 공중 유닛 초기 방향 설정
        if (selectedData.enemyData != null && selectedData.enemyData.enemyType == EnemyData.EnemyType.Flying)
        {
            FlyingEnemy flyingEnemy = enemy.GetComponent<FlyingEnemy>();
            if (flyingEnemy != null)
            {
                flyingEnemy.SetInitialDirection(Vector2.left);
            }
        }
    }

    private SpawnData SelectRandomEnemy()
    {
        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var data in spawnPool)
        {
            cumulative += data.spawnWeight;
            if (random <= cumulative)
            {
                return data;
            }
        }

        return spawnPool[0];
    }

    private Vector3 GetSpawnPosition(EnemyData data)
    {
        // 화면 오른쪽 밖 X 위치
        float spawnX = mainCamera.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x + spawnOffsetX;

        float spawnY;
        if (data != null && data.enemyType == EnemyData.EnemyType.Flying)
        {
            // 공중 유닛: 랜덤 Y
            spawnY = Random.Range(flyingSpawnYRange.x, flyingSpawnYRange.y);
        }
        else
        {
            // 지상 유닛: 고정 Y
            spawnY = groundSpawnY;
        }

        return new Vector3(spawnX, spawnY, 0f);
    }

    /// <summary>
    /// 스폰 시작/정지
    /// </summary>
    public void SetSpawning(bool enabled)
    {
        isSpawning = enabled;
    }

    /// <summary>
    /// 스포너 리셋
    /// </summary>
    public void ResetSpawner()
    {
        elapsedTime = 0f;
        spawnTimer = spawnCooldown;
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) return;

        float spawnX = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x + spawnOffsetX;

        // 지상 스폰 위치
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(spawnX, groundSpawnY, 0f), 0.5f);

        // 공중 스폰 범위
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(spawnX, flyingSpawnYRange.x, 0f),
            new Vector3(spawnX, flyingSpawnYRange.y, 0f)
        );
    }
}
