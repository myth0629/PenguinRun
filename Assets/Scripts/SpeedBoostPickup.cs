using UnityEngine;
using System.Collections;

/// <summary>
/// 가속 아이템 픽업
/// - 플레이어와 닿으면 일정 시간 동안 게임 속도 증가
/// - 청크 내부 오브젝트 지원
/// </summary>
public class SpeedBoostPickup : MonoBehaviour
{
    [Header("=== 가속 설정 ===")]
    [Tooltip("속도 증가 배율")]
    [SerializeField] private float speedMultiplier = 1.5f;
    
    [Tooltip("가속 지속 시간 (초)")]
    [SerializeField] private float boostDuration = 3f;
    
    [Tooltip("플레이어 태그")]
    [SerializeField] private string playerTag = "Player";

    [Header("=== 연출 설정 ===")]
    [Tooltip("획득 시 사운드")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("획득 시 스프라이트 이펙트")]
    [SerializeField] private GameObject pickupSpriteEffect;
    
    [Tooltip("획득 시 파티클 이펙트")]
    [SerializeField] private GameObject pickupEffect;

    [Header("=== 풀링 설정 ===")]
    [Tooltip("청크 내부 오브젝트인 경우 (true: SetActive(false)로 처리)")]
    [SerializeField] private bool isPartOfChunk = true;

    [Header("=== 애니메이션 ===")]
    [Tooltip("위아래로 둥둥 떠다니는 효과")]
    [SerializeField] private bool enableFloating = true;
    
    [Tooltip("떠다니는 속도")]
    [SerializeField] private float floatSpeed = 2f;
    
    [Tooltip("떠다니는 높이")]
    [SerializeField] private float floatHeight = 0.2f;

    [Tooltip("회전 효과")]
    [SerializeField] private bool enableRotation = true;
    
    [Tooltip("회전 속도")]
    [SerializeField] private float rotationSpeed = 120f;

    private Vector3 startPosition;
    private float floatOffset;
    private bool isCollected;

    // 정적 변수로 현재 부스트 상태 관리
    private static Coroutine activeBoostCoroutine;
    private static float originalSpeed;
    private static bool isBoosting;

    public static bool IsBoosting => isBoosting;

    private void Start()
    {
        startPosition = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        isCollected = false;
        startPosition = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (isCollected) return;

        // 떠다니는 애니메이션
        if (enableFloating)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + floatOffset) * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // 회전 애니메이션
        if (enableRotation)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag(playerTag))
        {
            Collect(other.gameObject);
        }
    }

    private void Collect(GameObject player)
    {
        isCollected = true;

        // 가속 적용
        ApplySpeedBoost(player);

        // 사운드 재생
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // 이펙트 생성
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // 스프라이트 이펙트 생성
        if (pickupSpriteEffect != null)
        {
            pickupSpriteEffect.SetActive(true);
        }
    }

    private void ApplySpeedBoost(GameObject player)
    {
        // 플레이어에서 코루틴 실행 (이 오브젝트는 비활성화되므로)
        MonoBehaviour playerMono = player.GetComponent<MonoBehaviour>();
        if (playerMono == null) return;

        // 플레이어 dash 애니메이션 트리거
        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("dash");
        }

        // 기존 부스트가 있으면 중단하고 새로 시작
        if (activeBoostCoroutine != null)
        {
            playerMono.StopCoroutine(activeBoostCoroutine);
            // 속도 복원 없이 새 부스트로 덮어씌움
        }
        else
        {
            // 처음 부스트 시작할 때만 원본 속도 저장
            originalSpeed = GameSpeedController.Speed;
        }

        activeBoostCoroutine = playerMono.StartCoroutine(SpeedBoostCoroutine(boostDuration, speedMultiplier));
    }

    private static IEnumerator SpeedBoostCoroutine(float duration, float multiplier)
    {
        isBoosting = true;
        
        // 속도 증가
        float boostedSpeed = originalSpeed * multiplier;
        GameSpeedController.SetSpeed(boostedSpeed);
        
        Debug.Log($"[SpeedBoost] 가속 시작! 속도: {originalSpeed} → {boostedSpeed} ({duration}초)");

        yield return new WaitForSeconds(duration);

        // 속도 복원
        GameSpeedController.SetSpeed(originalSpeed);
        
        Debug.Log($"[SpeedBoost] 가속 종료. 속도: {originalSpeed}");
        
        isBoosting = false;
        activeBoostCoroutine = null;
    }

    /// <summary>
    /// 부스트 강제 종료 (게임 리셋 시 호출)
    /// </summary>
    public static void ForceEndBoost()
    {
        if (isBoosting)
        {
            GameSpeedController.SetSpeed(originalSpeed);
            isBoosting = false;
            activeBoostCoroutine = null;
        }
    }
}
