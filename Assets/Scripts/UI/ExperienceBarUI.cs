using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 경험치 바 UI
/// - 레벨 텍스트 표시
/// - 경험치 바 (Image Fill) 애니메이션
/// - DOTween 사용
/// </summary>
public class ExperienceBarUI : MonoBehaviour
{
    [Header("=== UI 참조 ===")]
    [Tooltip("경험치 바 이미지 (Image Type: Filled)")]
    [SerializeField] private Image expBarFill;
    
    [Tooltip("레벨 텍스트")]
    [SerializeField] private TextMeshProUGUI levelText;
    
    [Tooltip("경험치 텍스트 (선택사항)")]
    [SerializeField] private TextMeshProUGUI expText;

    [Header("=== 플레이어 참조 ===")]
    [Tooltip("PlayerLevel 컴포넌트 (비워두면 자동 검색)")]
    [SerializeField] private PlayerLevel playerLevel;

    [Header("=== 애니메이션 설정 ===")]
    [Tooltip("경험치 바 채우기 애니메이션 시간")]
    [SerializeField] private float fillDuration = 0.3f;
    
    [Tooltip("애니메이션 이징")]
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    [Header("=== 레벨업 연출 ===")]
    [Tooltip("레벨업 시 텍스트 펀치 스케일")]
    [SerializeField] private float levelUpPunchScale = 0.3f;
    
    [Tooltip("레벨업 시 펀치 지속 시간")]
    [SerializeField] private float levelUpPunchDuration = 0.4f;
    
    [Tooltip("레벨업 시 바 플래시 색상")]
    [SerializeField] private Color levelUpFlashColor = Color.white;

    [Header("=== 텍스트 포맷 ===")]
    [Tooltip("레벨 텍스트 포맷 ({0} = 레벨)")]
    [SerializeField] private string levelFormat = "Lv.{0}";
    
    [Tooltip("경험치 텍스트 포맷 ({0} = 현재, {1} = 최대)")]
    [SerializeField] private string expFormat = "{0} / {1}";

    private float currentFillAmount;
    private Color originalFillColor;
    private Tween fillTween;
    private Tween colorTween;

    private void Awake()
    {
        // 원본 색상 저장
        if (expBarFill != null)
        {
            originalFillColor = expBarFill.color;
        }
    }

    private void Start()
    {
        FindPlayerLevel();
        SubscribeEvents();
        InitializeUI();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        KillTweens();
    }

    private void FindPlayerLevel()
    {
        if (playerLevel == null)
        {
            playerLevel = FindFirstObjectByType<PlayerLevel>();
            
            if (playerLevel == null)
            {
                Debug.LogWarning("[ExperienceBarUI] PlayerLevel을 찾을 수 없습니다.");
            }
        }
    }

    private void SubscribeEvents()
    {
        if (playerLevel != null)
        {
            playerLevel.OnLevelChanged += HandleLevelChanged;
            playerLevel.OnLevelUp += HandleLevelUp;
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerLevel != null)
        {
            playerLevel.OnLevelChanged -= HandleLevelChanged;
            playerLevel.OnLevelUp -= HandleLevelUp;
        }
    }

    private void InitializeUI()
    {
        if (playerLevel == null) return;

        // 초기 값 설정 (애니메이션 없이)
        UpdateLevelText(playerLevel.CurrentLevel);
        
        float targetFill = playerLevel.ExpProgress;
        currentFillAmount = targetFill;
        
        if (expBarFill != null)
        {
            expBarFill.fillAmount = targetFill;
        }

        UpdateExpText(playerLevel.CurrentExp, playerLevel.ExpToNextLevel);
    }

    /// <summary>
    /// 레벨/경험치 변경 시 호출
    /// </summary>
    private void HandleLevelChanged(int level, int currentExp, int expToNext)
    {
        UpdateLevelText(level);
        UpdateExpText(currentExp, expToNext);
        
        float targetFill = expToNext > 0 ? (float)currentExp / expToNext : 1f;
        AnimateFillBar(targetFill);
    }

    /// <summary>
    /// 레벨업 시 호출
    /// </summary>
    private void HandleLevelUp(int newLevel)
    {
        // 바 플래시 효과
        FlashExpBar();
        
        // 레벨 텍스트 펀치 효과
        PunchLevelText();
    }

    private void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, level);
        }
    }

    private void UpdateExpText(int current, int max)
    {
        if (expText != null)
        {
            expText.text = string.Format(expFormat, current, max);
        }
    }

    /// <summary>
    /// 경험치 바 채우기 애니메이션
    /// </summary>
    private void AnimateFillBar(float targetFill)
    {
        if (expBarFill == null) return;

        // 기존 트윈 중단
        fillTween?.Kill();

        // DOTween으로 부드럽게 채우기
        fillTween = DOTween.To(
            () => currentFillAmount,
            x => {
                currentFillAmount = x;
                expBarFill.fillAmount = x;
            },
            targetFill,
            fillDuration
        ).SetEase(fillEase);
    }

    /// <summary>
    /// 레벨업 시 바 플래시 효과
    /// </summary>
    private void FlashExpBar()
    {
        if (expBarFill == null) return;

        colorTween?.Kill();

        // 흰색으로 플래시 후 원래 색상으로 복귀
        colorTween = expBarFill.DOColor(levelUpFlashColor, levelUpPunchDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() => expBarFill.color = originalFillColor);
    }

    /// <summary>
    /// 레벨 텍스트 펀치 효과
    /// </summary>
    private void PunchLevelText()
    {
        if (levelText == null) return;

        // 스케일 펀치 효과
        levelText.transform.DOKill();
        levelText.transform.localScale = Vector3.one;
        levelText.transform.DOPunchScale(Vector3.one * levelUpPunchScale, levelUpPunchDuration, 2, 0.5f);
    }

    private void KillTweens()
    {
        fillTween?.Kill();
        colorTween?.Kill();
        
        if (levelText != null)
        {
            levelText.transform.DOKill();
        }
    }

    /// <summary>
    /// UI 강제 새로고침 (PlayerLevel 변경 시)
    /// </summary>
    public void RefreshUI()
    {
        UnsubscribeEvents();
        FindPlayerLevel();
        SubscribeEvents();
        InitializeUI();
    }

    /// <summary>
    /// PlayerLevel 수동 설정
    /// </summary>
    public void SetPlayerLevel(PlayerLevel level)
    {
        UnsubscribeEvents();
        playerLevel = level;
        SubscribeEvents();
        InitializeUI();
    }
}
