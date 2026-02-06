using UnityEngine;

/// <summary>
/// 유도 투사체
/// - 가장 가까운 적을 추적
/// - 부드러운 방향 전환
/// </summary>
public class HomingProjectile : MonoBehaviour
{
    [Header("=== 설정 ===")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private LayerMask targetLayer;
    
    [Header("=== 히트 이펙트 ===")]
    [Tooltip("명중 시 생성할 히트 이펙트 프리팹")]
    [SerializeField] private GameObject hitEffectPrefab;

    [Header("=== 유도 설정 ===")]
    [Tooltip("유도 강도 (높을수록 빠르게 방향 전환)")]
    [SerializeField] private float homingStrength = 5f;
    
    [Tooltip("타겟 감지 반경")]
    [SerializeField] private float detectionRadius = 10f;
    
    [Tooltip("유도 시작 딜레이 (초)")]
    [SerializeField] private float homingDelay = 0.1f;

    [Header("=== 현재 상태 (런타임) ===")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private int pierceCount;
    [SerializeField] private int currentPierceCount;

    private float lifetimeTimer;
    private float homingDelayTimer;
    private Vector3 moveDirection;
    private Transform currentTarget;
    private bool isReturned;

    /// <summary>
    /// 투사체 초기화
    /// </summary>
    public void Initialize(float dmg, float spd, int pierce, float sizeMultiplier = 1f)
    {
        damage = dmg;
        speed = spd;
        pierceCount = pierce;
        currentPierceCount = 0;
        lifetimeTimer = lifetime;
        homingDelayTimer = homingDelay;
        isReturned = false;
        currentTarget = null;

        // 초기 방향 (오른쪽)
        moveDirection = transform.right;

        // 크기 조절
        transform.localScale = Vector3.one * sizeMultiplier;
    }

    private void OnEnable()
    {
        lifetimeTimer = lifetime;
        homingDelayTimer = homingDelay;
        currentPierceCount = 0;
        isReturned = false;
        currentTarget = null;
        moveDirection = transform.right;
    }

    private void Update()
    {
        if (isReturned) return;

        // 유도 딜레이
        if (homingDelayTimer > 0f)
        {
            homingDelayTimer -= Time.deltaTime;
        }
        else
        {
            // 타겟 추적
            UpdateTarget();
            UpdateHoming();
        }

        // 이동
        transform.position += moveDirection * speed * Time.deltaTime;

        // 회전 (이동 방향으로)
        if (moveDirection != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 수명 체크
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 가장 가까운 적 찾기
    /// </summary>
    private void UpdateTarget()
    {
        // 현재 타겟이 유효하면 유지
        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(transform.position, currentTarget.position);
            if (distance <= detectionRadius)
            {
                return;
            }
        }

        // 새 타겟 찾기
        currentTarget = FindClosestTarget();
    }

    private Transform FindClosestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, targetLayer);
        
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!string.IsNullOrEmpty(targetTag) && !hit.CompareTag(targetTag))
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hit.transform;
            }
        }

        return closest;
    }

    /// <summary>
    /// 유도 방향 업데이트
    /// </summary>
    private void UpdateHoming()
    {
        if (currentTarget == null) return;

        // 타겟 방향
        Vector3 targetDirection = (currentTarget.position - transform.position).normalized;

        // 부드러운 방향 전환
        moveDirection = Vector3.Lerp(moveDirection, targetDirection, homingStrength * Time.deltaTime).normalized;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReturned) return;

        // 타겟 체크
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
        {
            return;
        }

        // 레이어 체크
        if (targetLayer != 0 && ((1 << other.gameObject.layer) & targetLayer) == 0)
        {
            return;
        }

        // 데미지 처리
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            
            // 히트 이펙트 생성
            SpawnHitEffect(other.transform.position);
        }

        // 타겟 갱신 (맞은 적 제외)
        if (other.transform == currentTarget)
        {
            currentTarget = null;
        }

        // 관통 처리
        currentPierceCount++;
        if (currentPierceCount > pierceCount)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 히트 이펙트 생성
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab == null) return;
        
        ObjectPool.Instance.Get(hitEffectPrefab, position, Quaternion.identity);
    }

    private void ReturnToPool()
    {
        if (isReturned) return;
        isReturned = true;

        ObjectPool.Instance.Return(gameObject);
    }

    private void OnBecameInvisible()
    {
        // 이미 비활성화 중이면 무시 (풀 반환 중 호출 방지)
        if (!gameObject.activeInHierarchy) return;
        
        ReturnToPool();
    }

    private void OnDrawGizmosSelected()
    {
        // 감지 반경
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 현재 타겟 표시
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
