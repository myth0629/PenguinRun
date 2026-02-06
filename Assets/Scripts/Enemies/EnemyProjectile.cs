using UnityEngine;

/// <summary>
/// 적 투사체
/// - 플레이어를 타겟으로 함
/// - 직선 이동 후 충돌 시 데미지
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("=== 설정 ===")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private string targetTag = "Player";

    [Header("=== 현재 상태 (런타임) ===")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;

    private float lifetimeTimer;
    private Vector3 moveDirection;
    private bool isReturned;

    /// <summary>
    /// 투사체 초기화
    /// </summary>
    public void Initialize(float dmg, float spd, Vector3 direction)
    {
        damage = dmg;
        speed = spd;
        moveDirection = direction.normalized;
        lifetimeTimer = lifetime;
        isReturned = false;

        // 이동 방향으로 회전
        if (moveDirection != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnEnable()
    {
        lifetimeTimer = lifetime;
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

        // 플레이어 체크
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
        {
            return;
        }

        // 플레이어 데미지 처리
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage((int)damage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturned) return;
        isReturned = true;

        ObjectPool.Instance.Return(gameObject);
    }

    private void OnBecameInvisible()
    {
        if (!gameObject.activeInHierarchy) return;
        ReturnToPool();
    }
}
