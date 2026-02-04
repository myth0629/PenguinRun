using UnityEngine;

/// <summary>
/// 데드존 (낭떠러지) 스크립트
/// - 플레이어가 이 영역에 닿으면 데미지를 입고 리스폰 위치로 복귀
/// </summary>
public class DeadZone : MonoBehaviour
{
    [Header("=== 데드존 설정 ===")]
    [Tooltip("플레이어가 받을 데미지")]
    [SerializeField] private int damageAmount = 1;
    
    [Tooltip("리스폰 위치 (비워두면 플레이어 시작 위치 사용)")]
    [SerializeField] private Transform respawnPoint;
    
    [Tooltip("플레이어 태그")]
    [SerializeField] private string playerTag = "Player";

    [Header("=== 리스폰 효과 ===")]
    [Tooltip("리스폰 시 잠시 멈춤 시간 (초)")]
    [SerializeField] private float freezeDuration = 0.2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            HandlePlayerFall(other.gameObject);
        }
    }

    /// <summary>
    /// 플레이어 낙하 처리
    /// </summary>
    private void HandlePlayerFall(GameObject player)
    {
        // 플레이어 체력 컴포넌트 가져오기
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[DeadZone] PlayerHealth 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        // 이미 무적 상태이거나 사망한 경우 무시
        if (playerHealth.IsInvincible || playerHealth.IsDead)
        {
            // 리스폰만 처리 (데미지 없이)
            RespawnPlayer(player);
            return;
        }

        // 데미지 처리
        playerHealth.TakeDamage(damageAmount);

        // 사망하지 않았다면 리스폰
        if (!playerHealth.IsDead)
        {
            RespawnPlayer(player);
        }
    }

    /// <summary>
    /// 플레이어를 리스폰 위치로 이동
    /// </summary>
    private void RespawnPlayer(GameObject player)
    {
        // Rigidbody2D가 있다면 속도 초기화
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 리스폰 위치로 이동
        Vector3 respawnPosition = GetRespawnPosition();
        player.transform.position = respawnPosition;

        Debug.Log($"[DeadZone] 플레이어 리스폰: {respawnPosition}");
    }

    /// <summary>
    /// 리스폰 위치 반환
    /// </summary>
    private Vector3 GetRespawnPosition()
    {
        if (respawnPoint != null)
        {
            return respawnPoint.position;
        }

        // respawnPoint가 없으면 기본 위치 (0, 2, 0) 반환
        Debug.LogWarning("[DeadZone] 리스폰 포인트가 설정되지 않았습니다. 기본 위치를 사용합니다.");
        return new Vector3(0f, 2f, 0f);
    }

    private void OnDrawGizmos()
    {
        // 에디터에서 데드존 영역 시각화 (빨간색 반투명)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Vector3 center = transform.position + (Vector3)boxCollider.offset;
            Vector3 size = boxCollider.size;
            Gizmos.DrawCube(center, size);
        }
        
        // 리스폰 포인트 시각화 (녹색)
        if (respawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, respawnPoint.position);
        }
    }
}
