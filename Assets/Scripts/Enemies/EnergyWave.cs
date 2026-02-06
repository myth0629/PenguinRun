using UnityEngine;

/// <summary>
/// 에너지파 (보스 레이저)
/// - 애니메이션 이벤트로 콜라이더 ON/OFF
/// - 플레이어 충돌 시 데미지
/// - 애니메이션 완료 후 풀 반환
/// </summary>
public class EnergyWave : MonoBehaviour
{
    [Header("=== 설정 ===")]
    [SerializeField] private float damage = 2f;
    [SerializeField] private string playerTag = "Player";
    
    private Collider2D col;
    private bool isReturned;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        isReturned = false;
        
        // 시작 시 콜라이더 비활성화 (차징 상태)
        if (col != null)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// 콜라이더 활성화 - 애니메이션 이벤트에서 호출 (발사 시)
    /// </summary>
    public void EnableCollider()
    {
        if (col != null)
        {
            col.enabled = true;
        }
    }

    /// <summary>
    /// 콜라이더 비활성화 - 애니메이션 이벤트에서 호출
    /// </summary>
    public void DisableCollider()
    {
        if (col != null)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// 풀에 반환 - 애니메이션 이벤트에서 호출 (애니메이션 종료 시)
    /// </summary>
    public void ReturnToPool()
    {
        if (isReturned) return;
        isReturned = true;
        
        // 부모 해제 (BossEnemy 손에서 분리)
        transform.SetParent(null);
        
        ObjectPool.Instance.Return(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage((int)damage);
        }
    }
}
