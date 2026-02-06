using UnityEngine;

/// <summary>
/// 히트 이펙트
/// - Animator Controller로 애니메이션 재생
/// - 애니메이션 완료 후 풀에 반환
/// </summary>
public class HitEffect : MonoBehaviour
{
    [Header("=== 설정 ===")]
    [Tooltip("이펙트 수명 (초) - 애니메이션 이벤트가 없을 경우 이 시간 후 자동 반환")]
    [SerializeField] private float lifetime = 0.5f;

    private float lifetimeTimer;
    private bool isReturned;

    private void OnEnable()
    {
        isReturned = false;
        lifetimeTimer = lifetime;
    }

    private void Update()
    {
        if (isReturned) return;

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출 - 풀에 반환
    /// </summary>
    public void OnAnimationComplete()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturned) return;
        isReturned = true;

        ObjectPool.Instance.Return(gameObject);
    }
}
