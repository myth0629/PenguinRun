using System;
using UnityEngine;

/// <summary>
/// 플레이어 체력 시스템
/// - 칸 단위 체력 (하트 등)
/// - 장애물 충돌 시 데미지
/// - 무적 시간 (피격 후 일시적 무적)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("=== 체력 설정 ===")]
    [Tooltip("최대 체력 (하트 개수)")]
    [SerializeField, Range(1, 10)] private int maxHealth = 3;
    
    [Tooltip("현재 체력")]
    [SerializeField] private int currentHealth;

    [Header("=== 피격 설정 ===")]
    [Tooltip("장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayer;
    
    [Tooltip("피격 시 무적 시간 (초)")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    
    [Tooltip("무적 중 깜빡임 간격")]
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("=== 참조 ===")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D bodyCollider;

    // 이벤트
    public event Action<int, int> OnHealthChanged;  // (현재체력, 최대체력)
    public event Action OnPlayerDeath;
    public event Action OnPlayerDamaged;

    // 상태
    private bool isInvincible;
    private float invincibilityTimer;
    private float blinkTimer;
    private bool isBlinkVisible = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;
    public bool IsDead => currentHealth <= 0;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        UpdateInvincibility();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsObstacle(other.gameObject))
        {
            TakeDamage(1);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsObstacle(collision.gameObject))
        {
            TakeDamage(1);
        }
    }

    private bool IsObstacle(GameObject obj)
    {
        return ((1 << obj.layer) & obstacleLayer) != 0;
    }

    /// <summary>
    /// 데미지를 받음
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isInvincible || IsDead) return;
        if (damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerDamaged?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartInvincibility();
        }
    }

    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 체력 완전 회복
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 게임 리셋
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isInvincible = false;
        SetSpriteVisible(true);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] 플레이어 사망!");
        OnPlayerDeath?.Invoke();
        // 추가 사망 처리 (게임 오버 UI 등)
    }

    private void StartInvincibility()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
        blinkTimer = 0f;
        isBlinkVisible = true;
    }

    private void UpdateInvincibility()
    {
        if (!isInvincible) return;

        invincibilityTimer -= Time.deltaTime;

        // 깜빡임 효과
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            isBlinkVisible = !isBlinkVisible;
            SetSpriteVisible(isBlinkVisible);
        }

        // 무적 종료
        if (invincibilityTimer <= 0f)
        {
            isInvincible = false;
            SetSpriteVisible(true);
        }
    }

    private void SetSpriteVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = visible ? 1f : 0.3f;
            spriteRenderer.color = c;
        }
    }

    private void OnValidate()
    {
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        if (currentHealth < 0)
            currentHealth = 0;
    }
}
