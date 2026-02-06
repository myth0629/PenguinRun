using UnityEngine;

/// <summary>
/// 투사체 베이스 클래스
/// - 이동, 충돌, 데미지 처리
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("=== 설정 ===")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private string targetTag = "Enemy";
    
    [Header("=== 히트 이펙트 ===")]
    [Tooltip("명중 시 생성할 히트 이펙트 프리팹")]
    [SerializeField] private GameObject hitEffectPrefab;

    [Header("=== 현재 상태 (런타임) ===")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private int pierceCount;
    [SerializeField] private int currentPierceCount;

    private float lifetimeTimer;
    private Vector3 moveDirection;
    private bool isReturned; // 중복 반환 방지 플래그

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
        isReturned = false;

        // 이동 방향 설정 (오른쪽 기본)
        moveDirection = transform.right;

        // 크기 조절
        transform.localScale = Vector3.one * sizeMultiplier;
    }

    private void OnEnable()
    {
        lifetimeTimer = lifetime;
        currentPierceCount = 0;
        isReturned = false;
    }

    private void Update()
    {
        if (isReturned) return;

        // 이동
        transform.position += moveDirection * speed * Time.deltaTime;

        // 수명 체크
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            ReturnToPool();
        }
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
        // if (effect != null)
        // {
        //     HitEffect hitEffect = effect.GetComponent<HitEffect>();
        //     if (hitEffect != null)
        //     {
        //         hitEffect.Initialize(position);
        //     }
        //     else
        //     {
        //         effect.transform.position = position;
        //     }
        // }
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
        
        // 화면 밖으로 나가면 풀에 반환
        ReturnToPool();
    }
}

/// <summary>
/// 데미지를 받을 수 있는 오브젝트 인터페이스
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
