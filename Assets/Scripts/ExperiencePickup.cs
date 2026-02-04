using UnityEngine;

/// <summary>
/// 경험치 아이템 픽업
/// - 플레이어와 닿으면 경험치 획득 후 사라짐
/// - 오브젝트 풀링 지원
/// </summary>
public class ExperiencePickup : MonoBehaviour
{
    [Header("=== 경험치 설정 ===")]
    [Tooltip("획득 시 주는 경험치량")]
    [SerializeField] private int expAmount = 10;
    
    [Tooltip("플레이어 태그")]
    [SerializeField] private string playerTag = "Player";

    [Header("=== 연출 설정 ===")]
    [Tooltip("획득 시 사운드")]
    [SerializeField] private AudioClip pickupSound;
    
    [Tooltip("획득 시 파티클 이펙트")]
    [SerializeField] private GameObject pickupEffect;

    [Header("=== 풀링 설정 ===")]
    [Tooltip("오브젝트 풀링 사용 (true: 풀에 반환, false: Destroy)")]
    [SerializeField] private bool usePooling = true;
    
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
    [SerializeField] private float rotationSpeed = 90f;

    private Vector3 startPosition;
    private float floatOffset;
    private bool isCollected;

    private void Start()
    {
        startPosition = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f); // 랜덤 시작 위상
    }

    private void OnEnable()
    {
        // 풀에서 재활성화될 때 상태 초기화
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

        // 경험치 부여
        PlayerLevel playerLevel = player.GetComponent<PlayerLevel>();
        if (playerLevel != null)
        {
            playerLevel.GainExp(expAmount);
        }
        else
        {
            Debug.LogWarning("[ExperiencePickup] PlayerLevel 컴포넌트를 찾을 수 없습니다.");
        }

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

        // 오브젝트 제거 처리
        if (isPartOfChunk)
        {
            // 청크 내부 오브젝트: 비활성화만 (청크 재활용 시 ChunkResetter가 다시 활성화)
            gameObject.SetActive(false);
        }
        else if (usePooling)
        {
            // 독립 오브젝트 + 풀링: 풀에 반환
            ObjectPool.Instance.Return(gameObject);
        }
        else
        {
            // 풀링 미사용: 파괴
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 경험치량 설정 (스폰 시 동적 설정용)
    /// </summary>
    public void SetExpAmount(int amount)
    {
        expAmount = Mathf.Max(1, amount);
    }

    /// <summary>
    /// 현재 경험치량 반환
    /// </summary>
    public int GetExpAmount()
    {
        return expAmount;
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 떠다니는 범위 표시
        if (enableFloating)
        {
            Gizmos.color = Color.yellow;
            Vector3 pos = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawLine(pos + Vector3.up * floatHeight, pos - Vector3.up * floatHeight);
        }
    }
}
