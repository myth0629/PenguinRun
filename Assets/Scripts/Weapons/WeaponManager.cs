using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 무기 관리자
/// - 보유 무기 관리
/// - 무기 추가/레벨업
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("=== 시작 무기 ===")]
    [Tooltip("게임 시작 시 기본으로 장착할 무기")]
    [SerializeField] private WeaponData startingWeapon;

    [Header("=== 무기 풀 ===")]
    [Tooltip("게임에 존재하는 모든 무기 데이터")]
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();

    [Header("=== 제한 ===")]
    [Tooltip("동시에 보유 가능한 최대 무기 수")]
    [SerializeField] private int maxWeaponSlots = 6;

    // 현재 보유 중인 무기들
    private readonly List<Weapon> equippedWeapons = new List<Weapon>();

    // 이벤트
    public event System.Action<Weapon> OnWeaponAdded;
    public event System.Action<Weapon> OnWeaponLevelUp;

    public List<Weapon> EquippedWeapons => equippedWeapons;
    public int MaxWeaponSlots => maxWeaponSlots;
    public bool HasEmptySlot => equippedWeapons.Count < maxWeaponSlots;

    private void Start()
    {
        // 시작 무기 장착
        if (startingWeapon != null)
        {
            AddWeapon(startingWeapon);
        }
    }

    /// <summary>
    /// 새 무기 추가
    /// </summary>
    public bool AddWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return false;

        // 이미 보유 중인 무기인지 확인
        Weapon existingWeapon = GetWeapon(weaponData);
        if (existingWeapon != null)
        {
            // 이미 있으면 레벨업
            return LevelUpWeapon(weaponData);
        }

        // 슬롯 확인
        if (!HasEmptySlot)
        {
            Debug.LogWarning("[WeaponManager] 무기 슬롯이 가득 찼습니다!");
            return false;
        }

        // 새 무기 생성
        GameObject weaponObj = new GameObject($"Weapon_{weaponData.weaponName}");
        weaponObj.transform.SetParent(transform);
        
        Weapon weapon = weaponObj.AddComponent<Weapon>();
        weapon.Initialize(weaponData, transform);
        
        equippedWeapons.Add(weapon);
        OnWeaponAdded?.Invoke(weapon);

        Debug.Log($"[WeaponManager] 무기 추가: {weaponData.weaponName}");
        return true;
    }

    /// <summary>
    /// 무기 레벨업
    /// </summary>
    public bool LevelUpWeapon(WeaponData weaponData)
    {
        Weapon weapon = GetWeapon(weaponData);
        if (weapon == null)
        {
            Debug.LogWarning($"[WeaponManager] {weaponData.weaponName} 무기를 보유하고 있지 않습니다.");
            return false;
        }

        if (weapon.IsMaxLevel)
        {
            Debug.Log($"[WeaponManager] {weaponData.weaponName} 이미 최대 레벨입니다.");
            return false;
        }

        bool success = weapon.LevelUp();
        if (success)
        {
            OnWeaponLevelUp?.Invoke(weapon);
        }
        return success;
    }

    /// <summary>
    /// 특정 무기 데이터로 보유 무기 검색
    /// </summary>
    public Weapon GetWeapon(WeaponData weaponData)
    {
        foreach (var weapon in equippedWeapons)
        {
            if (weapon.Data == weaponData)
            {
                return weapon;
            }
        }
        return null;
    }

    /// <summary>
    /// 레벨업 선택지 생성 (레벨업 UI용)
    /// </summary>
    public List<WeaponData> GetLevelUpChoices(int count = 3)
    {
        List<WeaponData> choices = new List<WeaponData>();
        List<WeaponData> candidates = new List<WeaponData>();

        // 후보 수집: 레벨업 가능한 보유 무기 + 새로 획득 가능한 무기
        foreach (var weapon in equippedWeapons)
        {
            if (!weapon.IsMaxLevel)
            {
                candidates.Add(weapon.Data);
            }
        }

        // 슬롯 여유가 있으면 새 무기도 후보에 추가
        if (HasEmptySlot)
        {
            foreach (var weaponData in allWeapons)
            {
                if (GetWeapon(weaponData) == null)
                {
                    candidates.Add(weaponData);
                }
            }
        }

        // 랜덤 선택
        while (choices.Count < count && candidates.Count > 0)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            choices.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex);
        }

        return choices;
    }

    /// <summary>
    /// 모든 무기 활성화/비활성화
    /// </summary>
    public void SetAllWeaponsActive(bool active)
    {
        foreach (var weapon in equippedWeapons)
        {
            weapon.SetActive(active);
        }
    }

    /// <summary>
    /// 무기 초기화 (게임 리셋용)
    /// </summary>
    public void ResetWeapons()
    {
        foreach (var weapon in equippedWeapons)
        {
            Destroy(weapon.gameObject);
        }
        equippedWeapons.Clear();

        if (startingWeapon != null)
        {
            AddWeapon(startingWeapon);
        }
    }
}
