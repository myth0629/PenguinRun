using UnityEngine;

/// <summary>
/// 적 베이스 클래스
/// - 체력, 데미지, 사망 처리
/// - IDamageable 구현
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("=== 적 데이터 ===")]
    [SerializeField] protected EnemyData enemyData;

    [Header("=== 타겟 ===")]
    [SerializeField] protected Transform target;
    [SerializeField] protected string playerTag = "Player";

    [Header("=== 현재 상태 ===")]
    [SerializeField] protected float currentHealth;
    
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected bool isDead;
    protected bool facingRight = true;

    // 이벤트
    public event System.Action<EnemyBase> OnDeath;

    public EnemyData Data => enemyData;
    public bool IsDead => isDead;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        Initialize();
    }

    protected virtual void OnEnable()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
        }
        isDead = false;

        // 플레이어 찾기
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead || target == null) return;

        Move();
        UpdateFacing();
    }

    /// <summary>
    /// 이동 로직 (하위 클래스에서 구현)
    /// </summary>
    protected abstract void Move();

    /// <summary>
    /// 방향 전환
    /// </summary>
    protected virtual void UpdateFacing()
    {
        if (target == null) return;

        bool shouldFaceRight = target.position.x > transform.position.x;
        
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
            transform.localScale = scale;
        }
    }

    /// <summary>
    /// 데미지 받기 (IDamageable)
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        
        // 피격 효과
        OnHit();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual void OnHit()
    {
        // 피격 애니메이션이나 효과
        if (animator != null)
        {
            animator.SetTrigger("hit");
        }

        // 깜빡임 효과
        StartCoroutine(FlashWhite());
    }

    private System.Collections.IEnumerator FlashWhite()
    {
        if (spriteRenderer != null)
        {
            //spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        // 경험치 보상
        if (target != null && enemyData != null)
        {
            PlayerLevel playerLevel = target.GetComponent<PlayerLevel>();
            if (playerLevel != null)
            {
                playerLevel.GainExp(enemyData.expReward);
            }
        }

        // 사망 이펙트
        if (enemyData != null && enemyData.deathEffect != null)
        {
            Instantiate(enemyData.deathEffect, transform.position, Quaternion.identity);
        }

        // 사망 사운드
        if (enemyData != null && enemyData.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(enemyData.deathSound, transform.position);
        }

        OnDeath?.Invoke(this);

        // 풀에 반환
        ObjectPool.Instance.Return(gameObject);
    }

    /// <summary>
    /// 플레이어와 충돌 시 데미지
    /// </summary>
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null && enemyData != null)
            {
                playerHealth.TakeDamage((int)enemyData.damage);
            }
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && enemyData != null)
            {
                playerHealth.TakeDamage((int)enemyData.damage);
            }
        }
    }
}
