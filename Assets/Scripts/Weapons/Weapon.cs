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
        
        // 여러 개일 경우 부채꼴로 발사
        float spreadAngle = 15f; // 투사체 간 각도
        float baseAngle = 0f; // 기본 방향 (오른쪽)
        
        if (totalCount > 1)
        {
            float totalSpread = spreadAngle * (totalCount - 1);
            baseAngle = -totalSpread / 2f + (spreadAngle * index);
        }

        Quaternion rotation = Quaternion.Euler(0f, 0f, baseAngle);

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
