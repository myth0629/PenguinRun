using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 체력을 단계별 스프라이트로 표시하는 UI
/// 체력에 따라 해당하는 스프라이트로 교체됩니다.
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("=== 참조 ===")]
    [Tooltip("PlayerHealth 컴포넌트 (비워두면 자동 탐색)")]
    [SerializeField] private PlayerHealth playerHealth;
    
    [Tooltip("체력바를 표시할 Image 컴포넌트")]
    [SerializeField] private Image healthBarImage;

    [Header("=== 체력 스프라이트 ===")]
    [Tooltip("체력 단계별 스프라이트 배열 (인덱스 0 = 체력 0, 인덱스 1 = 체력 1, ...)")]
    [SerializeField] private Sprite[] healthSprites;

    private void Start()
    {
        // PlayerHealth 자동 탐색
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("[HealthUI] PlayerHealth를 찾을 수 없습니다!");
            return;
        }

        if (healthBarImage == null)
        {
            Debug.LogError("[HealthUI] Health Bar Image가 설정되지 않았습니다!");
            return;
        }

        // 이벤트 구독
        playerHealth.OnHealthChanged += UpdateHealthUI;

        // 초기 UI 설정
        UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }

    /// <summary>
    /// 체력 변경 시 스프라이트 교체
    /// </summary>
    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthBarImage == null || healthSprites == null || healthSprites.Length == 0)
        {
            return;
        }

        // 현재 체력에 해당하는 스프라이트 인덱스
        // 배열: [0]=체력0, [1]=체력1, [2]=체력2, ...
        int spriteIndex = Mathf.Clamp(currentHealth, 0, healthSprites.Length - 1);
        
        if (healthSprites[spriteIndex] != null)
        {
            healthBarImage.sprite = healthSprites[spriteIndex];
        }
    }

    /// <summary>
    /// 수동으로 UI 새로고침
    /// </summary>
    public void RefreshUI()
    {
        if (playerHealth != null)
        {
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }
}
