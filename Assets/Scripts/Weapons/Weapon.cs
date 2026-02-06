using UnityEngine;

/// <summary>
/// 개별 무기 인스턴스
/// - WeaponData를 기반으로 실제 공격 수행
/// - 쿨다운 관리 및 투사체 발사
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("=== 무기 데이터 ===")]
    [SerializeField] private WeaponData weaponData;
    
    [Header("=== 상태 ===")]
    [SerializeField] private int currentLevel = 1;

    private float cooldownTimer;
    private Transform owner;
    private bool isActive = true;

    // Properties
    public WeaponData Data => weaponData;
    public int CurrentLevel => currentLevel;
    public bool IsMaxLevel => currentLevel >= weaponData.MaxLevel;
    public WeaponData.LevelData CurrentLevelData => weaponData.GetLevelData(currentLevel);

    public void Initialize(WeaponData data, Transform ownerTransform)
    {
        weaponData = data;
        owner = ownerTransform;
        currentLevel = 1;
        cooldownTimer = 0f;
        isActive = true;
    }

    private void Update()
    {
        if (!isActive || weaponData == null) return;

        // 쿨다운 감소
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
        else
        {
            // 쿨다운 완료 시 공격
            Attack();
            cooldownTimer = CurrentLevelData.cooldown;
        }
    }

    /// <summary>
    /// 공격 수행
    /// </summary>
    protected virtual void Attack()
    {
        if (weaponData.projectilePrefab == null) return;

        var levelData = CurrentLevelData;
        int count = levelData.projectileCount;

        // 투사체 발사
        for (int i = 0; i < count; i++)
        {
            SpawnProjectile(i, count, levelData);
        }
    }

    /// <summary>
    /// 투사체 생성
    /// </summary>
    protected virtual void SpawnProjectile(int index, int totalCount, WeaponData.LevelData levelData)
    {
        // 기본: 오른쪽으로 발사 (러너 게임 특성)
        Vector3 spawnPos = owner != null ? owner.position : transform.position;
        
        float angle = 0f;
        
        // 부채꼴(샷건) 발사
        if (weaponData.useSpreadFire && totalCount > 1)
        {
            // 총 퍼짐 각도를 탄 수-1로 나눠서 균등 분배
            // 예: 60도, 3발 → -30, 0, +30
            // 예: 60도, 6발 → -30, -18, -6, +6, +18, +30
            float halfSpread = weaponData.spreadAngle / 2f;
            float step = weaponData.spreadAngle / (totalCount - 1);
            angle = -halfSpread + (step * index);
        }
        else if (totalCount > 1)
        {
            // 기존 방식: 고정 간격
            float spreadAngle = 15f;
            float totalSpread = spreadAngle * (totalCount - 1);
            angle = -totalSpread / 2f + (spreadAngle * index);
        }

        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // 풀링 사용
        GameObject projectileObj = ObjectPool.Instance.Get(
            weaponData.projectilePrefab, 
            spawnPos, 
            rotation
        );

        // 투사체 초기화 (Projectile 또는 HomingProjectile)
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(levelData.damage, levelData.speed, levelData.pierceCount, levelData.sizeMultiplier);
        }
        else
        {
            // HomingProjectile 체크
            HomingProjectile homingProjectile = projectileObj.GetComponent<HomingProjectile>();
            if (homingProjectile != null)
            {
                homingProjectile.Initialize(levelData.damage, levelData.speed, levelData.pierceCount, levelData.sizeMultiplier);
            }
        }
    }

    /// <summary>
    /// 레벨업
    /// </summary>
    public bool LevelUp()
    {
        if (IsMaxLevel) return false;

        currentLevel++;
        Debug.Log($"[Weapon] {weaponData.weaponName} 레벨업! Lv.{currentLevel}");
        return true;
    }

    /// <summary>
    /// 무기 활성화/비활성화
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
    }
}
