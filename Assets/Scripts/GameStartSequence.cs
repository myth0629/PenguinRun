using UnityEngine;
using DG.Tweening;

/// <summary>
/// 게임 시작 연출
/// - 맵 정지 상태로 시작
/// - 플레이어가 왼쪽에서 달려와서 등장
/// - 등장 완료 후 게임 시작
/// </summary>
public class GameStartSequence : MonoBehaviour
{
    [Header("=== 플레이어 설정 ===")]
    [Tooltip("플레이어 Transform")]
    [SerializeField] private Transform player;
    
    [Tooltip("플레이어가 도착할 최종 위치")]
    [SerializeField] private Transform targetPosition;
    
    [Tooltip("플레이어 시작 오프셋 (화면 왼쪽 밖)")]
    [SerializeField] private float startOffsetX = -8f;

    [Header("=== 타이밍 설정 ===")]
    [Tooltip("등장 애니메이션 시간")]
    [SerializeField] private float entranceDuration = 1.5f;
    
    [Tooltip("등장 후 게임 시작까지 대기 시간")]
    [SerializeField] private float delayBeforeStart = 0.3f;
    
    [Tooltip("애니메이션 이징")]
    [SerializeField] private Ease entranceEase = Ease.OutQuad;

    [Header("=== 게임 속도 설정 ===")]
    [Tooltip("게임 시작 시 속도 (0에서 이 값으로 증가)")]
    [SerializeField] private float targetGameSpeed = 6.5f;
    
    [Tooltip("속도 증가 시간")]
    [SerializeField] private float speedRampDuration = 0.5f;

    [Header("=== 컴포넌트 참조 ===")]
    [Tooltip("플레이어 움직임 컴포넌트 (시작 전 비활성화)")]
    [SerializeField] private PlayerMovement playerMovement;
    
    [Tooltip("플레이어 애니메이터")]
    [SerializeField] private Animator playerAnimator;

    [Header("=== 디버그 ===")]
    [SerializeField] private bool autoStartOnAwake = true;

    private Vector3 originalPlayerPosition;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    // 이벤트
    public event System.Action OnSequenceStart;
    public event System.Action OnSequenceComplete;

    private void Awake()
    {
        // 원본 위치 저장
        if (player != null)
        {
            originalPlayerPosition = targetPosition != null ? targetPosition.position : player.position;
        }
    }

    private void Start()
    {
        if (autoStartOnAwake)
        {
            PlaySequence();
        }
    }

    /// <summary>
    /// 시작 연출 재생
    /// </summary>
    public void PlaySequence()
    {
        if (isPlaying) return;
        isPlaying = true;

        OnSequenceStart?.Invoke();

        // 1. 게임 속도 0으로 설정 (맵 정지)
        GameSpeedController.SetSpeed(0f);

        // 2. 플레이어 입력 비활성화
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // 3. 플레이어를 시작 위치로 이동
        if (player != null)
        {
            Vector3 startPos = originalPlayerPosition + Vector3.right * startOffsetX;
            player.position = startPos;

            // 4. 달리기 애니메이션 재생
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isGrounded", true);
                playerAnimator.SetBool("isSliding", false);
                playerAnimator.SetBool("isFalling", false);
            }

            // 5. DOTween으로 플레이어 이동
            player.DOMove(originalPlayerPosition, entranceDuration)
                .SetEase(entranceEase)
                .OnComplete(OnEntranceComplete);
        }
        else
        {
            // 플레이어가 없으면 바로 게임 시작
            OnEntranceComplete();
        }

        Debug.Log("[GameStartSequence] 시작 연출 시작");
    }

    private void OnEntranceComplete()
    {
        // 대기 후 게임 시작
        DOVirtual.DelayedCall(delayBeforeStart, StartGame);
    }

    private void StartGame()
    {
        // 1. 플레이어 입력 활성화
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // 2. 게임 속도 점진적 증가
        DOTween.To(
            () => GameSpeedController.Speed,
            x => GameSpeedController.SetSpeed(x),
            targetGameSpeed,
            speedRampDuration
        ).SetEase(Ease.OutQuad);

        isPlaying = false;
        OnSequenceComplete?.Invoke();

        Debug.Log("[GameStartSequence] 게임 시작!");
    }

    /// <summary>
    /// 연출 건너뛰기
    /// </summary>
    public void SkipSequence()
    {
        if (!isPlaying) return;

        // 모든 트윈 중단
        player?.DOKill();
        DOTween.Kill(this);

        // 즉시 게임 시작 상태로
        if (player != null)
        {
            player.position = originalPlayerPosition;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        GameSpeedController.SetSpeed(targetGameSpeed);

        isPlaying = false;
        OnSequenceComplete?.Invoke();

        Debug.Log("[GameStartSequence] 연출 스킵됨");
    }

    /// <summary>
    /// 게임 리셋 시 다시 연출 준비
    /// </summary>
    public void ResetSequence()
    {
        isPlaying = false;
        
        if (player != null)
        {
            player.DOKill();
        }
    }

    private void OnDestroy()
    {
        player?.DOKill();
        DOTween.Kill(this);
    }
}
