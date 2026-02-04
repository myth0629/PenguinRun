using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범용 오브젝트 풀 시스템
/// - 재사용 가능한 오브젝트들을 미리 생성해두고 관리
/// - GC 부하 감소, 메모리 효율 개선
/// </summary>
public class ObjectPool : MonoBehaviour
{
    #region Singleton
    private static ObjectPool instance;
    public static ObjectPool Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ObjectPool");
                instance = go.AddComponent<ObjectPool>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    #endregion

    /// <summary>
    /// 프리팹별 풀 데이터
    /// </summary>
    private class PoolData
    {
        public GameObject prefab;
        public Transform container;
        public Queue<GameObject> available = new Queue<GameObject>();
        public HashSet<GameObject> inUse = new HashSet<GameObject>();
    }

    private readonly Dictionary<GameObject, PoolData> pools = new Dictionary<GameObject, PoolData>();
    private readonly Dictionary<GameObject, PoolData> instanceToPool = new Dictionary<GameObject, PoolData>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// 풀 초기화 및 미리 생성
    /// </summary>
    /// <param name="prefab">풀링할 프리팹</param>
    /// <param name="initialCount">미리 생성할 개수</param>
    public void InitializePool(GameObject prefab, int initialCount)
    {
        if (prefab == null) return;
        
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        PoolData pool = pools[prefab];
        
        // 미리 생성
        for (int i = 0; i < initialCount; i++)
        {
            GameObject obj = CreateNewInstance(pool);
            obj.SetActive(false);
            pool.available.Enqueue(obj);
        }

        Debug.Log($"[ObjectPool] '{prefab.name}' 풀 초기화: {initialCount}개 생성");
    }

    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        PoolData pool = pools[prefab];
        GameObject obj;

        if (pool.available.Count > 0)
        {
            obj = pool.available.Dequeue();
            
            // null 체크 (씬 전환 등으로 파괴된 경우)
            if (obj == null)
            {
                obj = CreateNewInstance(pool);
            }
        }
        else
        {
            obj = CreateNewInstance(pool);
        }

        // 위치 및 부모 설정
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        pool.inUse.Add(obj);
        
        return obj;
    }

    /// <summary>
    /// 오브젝트를 풀에 반환
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        if (!instanceToPool.TryGetValue(obj, out PoolData pool))
        {
            // 풀에서 관리하지 않는 오브젝트면 그냥 파괴
            Destroy(obj);
            return;
        }

        pool.inUse.Remove(obj);
        
        // 비활성화 후 컨테이너로 이동
        obj.SetActive(false);
        obj.transform.SetParent(pool.container);
        
        pool.available.Enqueue(obj);
    }

    /// <summary>
    /// 특정 프리팹의 모든 활성 오브젝트 반환
    /// </summary>
    public void ReturnAll(GameObject prefab)
    {
        if (prefab == null || !pools.ContainsKey(prefab)) return;

        PoolData pool = pools[prefab];
        
        // HashSet을 복사해서 순회 (수정 중 순회 방지)
        var inUseList = new List<GameObject>(pool.inUse);
        
        foreach (var obj in inUseList)
        {
            if (obj != null)
            {
                Return(obj);
            }
        }
    }

    /// <summary>
    /// 모든 풀 초기화 (게임 재시작 시)
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var kvp in pools)
        {
            PoolData pool = kvp.Value;
            
            // 사용 중인 오브젝트 파괴
            foreach (var obj in pool.inUse)
            {
                if (obj != null) Destroy(obj);
            }
            
            // 대기 중인 오브젝트 파괴
            while (pool.available.Count > 0)
            {
                var obj = pool.available.Dequeue();
                if (obj != null) Destroy(obj);
            }
            
            // 컨테이너 파괴
            if (pool.container != null)
            {
                Destroy(pool.container.gameObject);
            }
        }

        pools.Clear();
        instanceToPool.Clear();
    }

    /// <summary>
    /// 풀 상태 정보
    /// </summary>
    public (int available, int inUse) GetPoolStatus(GameObject prefab)
    {
        if (prefab == null || !pools.ContainsKey(prefab))
        {
            return (0, 0);
        }

        PoolData pool = pools[prefab];
        return (pool.available.Count, pool.inUse.Count);
    }

    #region Private Methods
    private void CreatePool(GameObject prefab)
    {
        PoolData pool = new PoolData
        {
            prefab = prefab
        };

        // 컨테이너 생성
        GameObject containerObj = new GameObject($"Pool_{prefab.name}");
        containerObj.transform.SetParent(transform);
        pool.container = containerObj.transform;

        pools[prefab] = pool;
    }

    private GameObject CreateNewInstance(PoolData pool)
    {
        GameObject obj = Instantiate(pool.prefab, pool.container);
        instanceToPool[obj] = pool;
        return obj;
    }
    #endregion
}
