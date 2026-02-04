using UnityEngine;

/// <summary>
/// 청크가 풀에서 재활용될 때 자식 오브젝트들을 리셋
/// 청크 프리팹의 루트에 부착
/// </summary>
public class ChunkResetter : MonoBehaviour
{
    [Header("=== 리셋 설정 ===")]
    [Tooltip("활성화 시 자식 픽업 아이템들 리셋")]
    [SerializeField] private bool resetPickupsOnEnable = true;
    
    [Tooltip("활성화 시 자식 오브젝트들 모두 활성화")]
    [SerializeField] private bool enableAllChildrenOnEnable = true;

    // 캐시된 자식 픽업들
    private ExperiencePickup[] cachedPickups;
    private GameObject[] cachedChildren;
    private Vector3[] cachedChildPositions;
    private Quaternion[] cachedChildRotations;

    private void Awake()
    {
        CacheChildren();
    }

    private void OnEnable()
    {
        ResetChunk();
    }

    /// <summary>
    /// 자식 오브젝트들 캐싱
    /// </summary>
    private void CacheChildren()
    {
        // 경험치 픽업 캐싱
        cachedPickups = GetComponentsInChildren<ExperiencePickup>(true);
        
        // 모든 자식 오브젝트 캐싱 (1단계 깊이만)
        int childCount = transform.childCount;
        cachedChildren = new GameObject[childCount];
        cachedChildPositions = new Vector3[childCount];
        cachedChildRotations = new Quaternion[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            cachedChildren[i] = child.gameObject;
            cachedChildPositions[i] = child.localPosition;
            cachedChildRotations[i] = child.localRotation;
        }
    }

    /// <summary>
    /// 청크 리셋 - 모든 자식 오브젝트 복원
    /// </summary>
    public void ResetChunk()
    {
        if (enableAllChildrenOnEnable)
        {
            ResetAllChildren();
        }
        
        if (resetPickupsOnEnable)
        {
            ResetPickups();
        }
    }

    private void ResetAllChildren()
    {
        if (cachedChildren == null) return;

        for (int i = 0; i < cachedChildren.Length; i++)
        {
            if (cachedChildren[i] != null)
            {
                // 활성화
                cachedChildren[i].SetActive(true);
                
                // 위치/회전 복원
                cachedChildren[i].transform.localPosition = cachedChildPositions[i];
                cachedChildren[i].transform.localRotation = cachedChildRotations[i];
            }
        }
    }

    private void ResetPickups()
    {
        if (cachedPickups == null) return;

        foreach (var pickup in cachedPickups)
        {
            if (pickup != null)
            {
                pickup.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 런타임에 자식이 변경된 경우 수동 갱신
    /// </summary>
    public void RefreshCache()
    {
        CacheChildren();
    }
}
