using UnityEngine;

/// <summary>
/// 공중 유닛 (The Sky Stalker)
/// - 지형 무시하고 플레이어 추적
/// - 사인파를 이용한 출렁이는 움직임
/// </summary>
public class FlyingEnemy : EnemyBase
{
    [Header("=== 공중 유닛 설정 ===")]
    [Tooltip("사인파 출렁임 크기")]
    [SerializeField] private float waveAmplitude = 1f;
    
    [Tooltip("사인파 속도")]
    [SerializeField] private float waveFrequency = 2f;
    
    [Tooltip("추적 부드러움 (높을수록 느리게 방향 전환)")]
    [SerializeField] private float smoothing = 0.5f;

    [Header("=== 정지 거리 ===")]
    [Tooltip("플레이어와 이 거리 이내면 더 이상 접근하지 않음")]
    [SerializeField] private float stopDistance = 0.5f;

    private float waveOffset;
    private Vector2 currentVelocity;

    protected override void Initialize()
    {
        base.Initialize();
        
        // 랜덤 시작 위상 (적들이 동시에 같은 패턴으로 움직이지 않도록)
        waveOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    protected override void Move()
    {
        if (target == null || enemyData == null) return;

        // 플레이어 방향 계산
        Vector2 directionToTarget = (target.position - transform.position);
        float distanceToTarget = directionToTarget.magnitude;

        // 정지 거리 체크
        if (distanceToTarget <= stopDistance)
        {
            // 플레이어와 가까우면 출렁이기만
            ApplyWaveMotion(Vector2.zero);
            return;
        }

        // 정규화된 방향
        Vector2 normalizedDirection = directionToTarget.normalized;

        // 기본 이동 속도
        Vector2 targetVelocity = normalizedDirection * enemyData.moveSpeed;

        // 부드러운 이동 (Smooth Damp)
        Vector2 smoothedVelocity = Vector2.Lerp(currentVelocity, targetVelocity, Time.deltaTime / smoothing);
        currentVelocity = smoothedVelocity;

        // 사인파 출렁임 적용
        ApplyWaveMotion(smoothedVelocity);
    }

    /// <summary>
    /// 사인파 출렁임을 적용한 이동
    /// </summary>
    private void ApplyWaveMotion(Vector2 baseVelocity)
    {
        // 사인파로 위아래 출렁임
        float wave = Mathf.Sin(Time.time * waveFrequency + waveOffset) * waveAmplitude;
        
        // 수직 방향에 파동 추가
        Vector2 waveVector = new Vector2(0f, wave);

        // 최종 속도 적용
        if (rb != null)
        {
            rb.linearVelocity = baseVelocity + waveVector;
        }
        else
        {
            // Rigidbody가 없으면 Transform 직접 이동
            transform.position += (Vector3)(baseVelocity + waveVector) * Time.deltaTime;
        }
    }

    /// <summary>
    /// 특정 위치를 향해 이동 (스폰 시 방향 설정용)
    /// </summary>
    public void SetInitialDirection(Vector2 direction)
    {
        currentVelocity = direction * enemyData.moveSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        // 정지 거리 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // 타겟 방향 시각화
        if (target != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
