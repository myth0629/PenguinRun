using UnityEngine;

/// <summary>
/// 보스 몬스터
/// - 스폰 후 목표 X 위치로 이동
/// - Y축으로만 패트롤하며 원거리 공격
/// - 투사체 공격 + 에너지파 범위 공격
/// </summary>
public class BossEnemy : EnemyBase
{
    [Header("=== 보스 설정 ===")]
    [Tooltip("목표 X 위치 (스폰 후 이동할 위치)")]
    [SerializeField] private float targetXPosition = 5f;
    
    [Tooltip("Y축 이동 범위")]
    [SerializeField] private Vector2 yMoveRange = new Vector2(-2f, 2f);
    
    [Tooltip("Y축 이동 속도")]
    [SerializeField] private float yMoveSpeed = 2f;

    [Header("=== 투사체 공격 ===")]
    [Tooltip("투사체 프리팹")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Tooltip("발사 위치 오프셋")]
    [SerializeField] private Vector2 fireOffset = new Vector2(-0.5f, 0f);

    [Header("=== 에너지파 공격 ===")]
    [Tooltip("에너지파 사용 여부")]
    [SerializeField] private bool useEnergyWave = true;
    
    [Tooltip("에너지파 공격 간격")]
    [SerializeField] private float energyWaveCooldown = 5f;
    
    [Tooltip("에너지파 프리팹 (콜라이더 포함)")]
    [SerializeField] private GameObject energyWavePrefab;
    
    [Tooltip("에너지파 생성 위치 (손 Transform)")]
    [SerializeField] private Transform energyWaveSpawnPoint;
    
    [Tooltip("에너지파 생성 오프셋")]
    [SerializeField] private Vector2 energyWaveOffset = new Vector2(-5f, 0f);

    // 상태
    private enum BossState { MovingToPosition, Patrolling }
    private BossState currentState = BossState.MovingToPosition;
    
    private float attackTimer;
    private float energyWaveTimer;
    private int yDirection = 1; // 1: 위, -1: 아래
    private bool reachedTargetPosition;
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        currentState = BossState.MovingToPosition;
        reachedTargetPosition = false;
        attackTimer = 0f;
        energyWaveTimer = energyWaveCooldown;
        
        // 초기 Y 방향 랜덤
        yDirection = Random.value > 0.5f ? 1 : -1;
    }

    protected override void Move()
    {
        if (enemyData == null) return;

        switch (currentState)
        {
            case BossState.MovingToPosition:
                MoveToTargetPosition();
                break;
            case BossState.Patrolling:
                PatrolYAxis();
                HandleAttacks();
                break;
        }
    }

    /// <summary>
    /// 목표 X 위치로 이동
    /// </summary>
    private void MoveToTargetPosition()
    {
        Vector3 currentPos = transform.position;
        
        // 목표 위치에 도달했는지 체크
        if (Mathf.Abs(currentPos.x - targetXPosition) < 0.1f)
        {
            // 도달 - 패트롤 모드로 전환
            currentState = BossState.Patrolling;
            reachedTargetPosition = true;
            
            // X 위치 고정
            transform.position = new Vector3(targetXPosition, currentPos.y, currentPos.z);
            return;
        }

        // 목표 위치로 이동
        float direction = targetXPosition < currentPos.x ? -1f : 1f;
        Vector3 movement = new Vector3(direction * enemyData.moveSpeed * Time.deltaTime, 0f, 0f);
        
        if (rb != null)
        {
            rb.MovePosition(currentPos + movement);
        }
        else
        {
            transform.position += movement;
        }
    }

    /// <summary>
    /// Y축 패트롤
    /// </summary>
    private void PatrolYAxis()
    {
        Vector3 currentPos = transform.position;
        
        // Y 범위 체크 및 방향 전환
        if (currentPos.y >= yMoveRange.y)
        {
            yDirection = -1;
        }
        else if (currentPos.y <= yMoveRange.x)
        {
            yDirection = 1;
        }

        // Y축 이동
        Vector3 movement = new Vector3(0f, yDirection * yMoveSpeed * Time.deltaTime, 0f);
        
        if (rb != null)
        {
            rb.MovePosition(currentPos + movement);
        }
        else
        {
            transform.position += movement;
        }
    }

    /// <summary>
    /// 공격 처리
    /// </summary>
    private void HandleAttacks()
    {
        if (target == null || enemyData == null) return;

        // 투사체 공격
        if (projectilePrefab != null)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                FireProjectile();
                attackTimer = enemyData.attackCooldown;
            }
        }

        // 에너지파 공격 (애니메이션 트리거)
        if (useEnergyWave)
        {
            energyWaveTimer -= Time.deltaTime;
            if (energyWaveTimer <= 0f)
            {
                // 애니메이션 트리거 - 실제 데미지는 애니메이션 이벤트에서 ExecuteEnergyWave() 호출
                if (anim != null)
                {
                    anim.SetTrigger("shoot");
                }
                energyWaveTimer = energyWaveCooldown;
            }
        }
    }

    /// <summary>
    /// 투사체 발사
    /// </summary>
    private void FireProjectile()
    {
        // 발사 위치
        Vector3 firePos = transform.position + (Vector3)fireOffset;
        
        // 플레이어 방향
        Vector3 direction = (target.position - firePos).normalized;
        
        // 투사체 생성
        GameObject projectile = ObjectPool.Instance.Get(projectilePrefab, firePos, Quaternion.identity);
        
        if (projectile != null)
        {
            EnemyProjectile enemyProj = projectile.GetComponent<EnemyProjectile>();
            if (enemyProj != null)
            {
                enemyProj.Initialize(
                    enemyData.projectileDamage,
                    enemyData.projectileSpeed,
                    direction
                );
            }
        }
    }

    /// <summary>
    /// 에너지파 발동 - 애니메이션 이벤트에서 호출
    /// </summary>
    public void ExecuteEnergyWave()
    {
        if (energyWavePrefab == null) return;
        
        // 손 위치가 설정되어 있으면 손 위치에서 생성하고 자식으로 설정
        Vector3 spawnPos = energyWaveSpawnPoint != null 
            ? energyWaveSpawnPoint.position 
            : transform.position;
        
        GameObject wave = ObjectPool.Instance.Get(energyWavePrefab, spawnPos, Quaternion.identity);
        
        // 에너지파를 손(또는 보스)의 자식으로 설정하여 Y축 이동을 따라감
        if (wave != null)
        {
            wave.transform.SetParent(energyWaveSpawnPoint != null ? energyWaveSpawnPoint : transform);
        }
    }

    /// <summary>
    /// 보스는 항상 왼쪽(플레이어 방향)을 바라봄
    /// </summary>
    protected override void UpdateFacing()
    {
        // 보스는 항상 왼쪽을 바라봄
        if (facingRight)
        {
            facingRight = false;
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 목표 X 위치
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(targetXPosition, yMoveRange.x, 0f),
            new Vector3(targetXPosition, yMoveRange.y, 0f)
        );
        
        // Y 이동 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            new Vector3(targetXPosition, (yMoveRange.x + yMoveRange.y) / 2f, 0f),
            new Vector3(0.5f, yMoveRange.y - yMoveRange.x, 0f)
        );

        // 발사 위치
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + (Vector3)fireOffset, 0.2f);

        // 에너지파 생성 위치
        if (useEnergyWave)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // 주황색
            Vector3 spawnPos = transform.position + (Vector3)energyWaveOffset;
            Gizmos.DrawWireSphere(spawnPos, 0.3f);
        }
    }
}

