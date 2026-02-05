using UnityEngine;

/// <summary>
/// 지상 유닛 (The Ground Walker)
/// - 바닥 위를 걸으며 플레이어 추적
/// - 절벽/벽 감지하여 방향 전환
/// </summary>
public class GroundEnemy : EnemyBase
{
    [Header("=== 지상 유닛 설정 ===")]
    [Tooltip("절벽 감지용 레이캐스트 거리")]
    [SerializeField] private float cliffCheckDistance = 1f;
    
    [Tooltip("벽 감지용 레이캐스트 거리")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    
    [Tooltip("바닥 레이어")]
    [SerializeField] private LayerMask groundLayer;
    
    [Tooltip("벽 레이어")]
    [SerializeField] private LayerMask wallLayer;

    [Header("=== 감지 위치 ===")]
    [SerializeField] private Transform cliffCheckPoint;  // 발 앞쪽 위치
    [SerializeField] private Transform wallCheckPoint;   // 몸 앞쪽 위치

    private int moveDirection = -1; // -1: 왼쪽, 1: 오른쪽

    protected override void Initialize()
    {
        base.Initialize();
        
        // 플레이어 방향으로 초기 이동 방향 설정
        if (target != null)
        {
            moveDirection = target.position.x < transform.position.x ? -1 : 1;
        }
    }

    protected override void Move()
    {
        if (enemyData == null) return;

        // 절벽 체크
        bool hasGroundAhead = CheckForGround();
        
        // 벽 체크
        bool hasWallAhead = CheckForWall();

        // 절벽이나 벽이 있으면 방향 전환
        if (!hasGroundAhead || hasWallAhead)
        {
            FlipDirection();
        }

        // 이동
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveDirection * enemyData.moveSpeed;
        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// 발 앞에 바닥이 있는지 확인
    /// </summary>
    private bool CheckForGround()
    {
        Vector2 checkPos;
        
        if (cliffCheckPoint != null)
        {
            checkPos = cliffCheckPoint.position;
        }
        else
        {
            // 기본 위치: 발 앞 대각선 아래
            float offsetX = moveDirection * 0.5f;
            checkPos = (Vector2)transform.position + new Vector2(offsetX, -0.1f);
        }

        // 아래로 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, cliffCheckDistance, groundLayer);
        
        // 디버그 시각화
        Debug.DrawRay(checkPos, Vector2.down * cliffCheckDistance, hit.collider != null ? Color.green : Color.red);
        
        return hit.collider != null;
    }

    /// <summary>
    /// 앞에 벽이 있는지 확인
    /// </summary>
    private bool CheckForWall()
    {
        Vector2 checkPos;
        
        if (wallCheckPoint != null)
        {
            checkPos = wallCheckPoint.position;
        }
        else
        {
            // 기본 위치: 정면
            checkPos = transform.position;
        }

        Vector2 direction = moveDirection > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(checkPos, direction, wallCheckDistance, wallLayer);
        
        // 디버그 시각화
        Debug.DrawRay(checkPos, direction * wallCheckDistance, hit.collider != null ? Color.yellow : Color.blue);
        
        return hit.collider != null;
    }

    /// <summary>
    /// 방향 전환
    /// </summary>
    private void FlipDirection()
    {
        moveDirection *= -1;
        
        // 스프라이트 뒤집기
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDirection;
        transform.localScale = scale;
        
        facingRight = moveDirection > 0;
    }

    protected override void UpdateFacing()
    {
        // 지상 유닛은 이동 방향에 따라 방향 결정 (부모 로직 무시)
    }

    private void OnDrawGizmosSelected()
    {
        // 절벽 체크 시각화
        Gizmos.color = Color.red;
        Vector3 cliffPos = cliffCheckPoint != null 
            ? cliffCheckPoint.position 
            : transform.position + new Vector3(moveDirection * 0.5f, -0.1f, 0f);
        Gizmos.DrawLine(cliffPos, cliffPos + Vector3.down * cliffCheckDistance);

        // 벽 체크 시각화
        Gizmos.color = Color.yellow;
        Vector3 wallPos = wallCheckPoint != null 
            ? wallCheckPoint.position 
            : transform.position;
        Gizmos.DrawLine(wallPos, wallPos + new Vector3(moveDirection * wallCheckDistance, 0f, 0f));
    }
}
