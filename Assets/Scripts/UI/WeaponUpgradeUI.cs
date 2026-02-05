using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 무기 업그레이드 UI
/// - 레벨업 시 게임 일시정지
/// - 무기 선택지 표시
/// - 선택 시 획득/업그레이드
/// </summary>
public class WeaponUpgradeUI : MonoBehaviour
{
    [Header("=== UI 참조 ===")]
    [SerializeField] private GameObject panel;
    [SerializeField] private List<WeaponSlotUI> weaponSlots;

    [Header("=== 참조 ===")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private PlayerLevel playerLevel;

    [Header("=== 선택지 수 ===")]
    [SerializeField] private int choiceCount = 3;

    private List<WeaponData> currentChoices = new List<WeaponData>();
    private bool isShowing;

    private void Start()
    {
        // 패널 초기 비활성화
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // PlayerLevel 이벤트 연결
        if (playerLevel != null)
        {
            playerLevel.OnLevelUp += OnPlayerLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (playerLevel != null)
        {
            playerLevel.OnLevelUp -= OnPlayerLevelUp;
        }
    }

    /// <summary>
    /// 레벨업 시 호출
    /// </summary>
    private void OnPlayerLevelUp(int newLevel)
    {
        ShowUpgradePanel();
    }

    /// <summary>
    /// 업그레이드 패널 표시
    /// </summary>
    public void ShowUpgradePanel()
    {
        if (isShowing || weaponManager == null) return;
        isShowing = true;

        // 게임 일시정지
        Time.timeScale = 0f;

        // 선택지 가져오기
        currentChoices = weaponManager.GetLevelUpChoices(choiceCount);

        // 슬롯 업데이트
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (i < currentChoices.Count)
            {
                weaponSlots[i].gameObject.SetActive(true);
                SetupSlot(weaponSlots[i], currentChoices[i], i);
            }
            else
            {
                weaponSlots[i].gameObject.SetActive(false);
            }
        }

        // 패널 표시
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    /// <summary>
    /// 슬롯 설정
    /// </summary>
    private void SetupSlot(WeaponSlotUI slot, WeaponData weaponData, int index)
    {
        // 현재 보유 중인지 확인
        Weapon existingWeapon = weaponManager.GetWeapon(weaponData);
        bool isOwned = existingWeapon != null;
        int displayLevel = isOwned ? existingWeapon.CurrentLevel + 1 : 1;

        // 슬롯 UI 설정
        slot.Setup(
            weaponData,
            displayLevel,
            isOwned,
            () => OnSlotSelected(index)
        );
    }

    /// <summary>
    /// 슬롯 선택 시
    /// </summary>
    private void OnSlotSelected(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;

        WeaponData selectedWeapon = currentChoices[index];

        // 무기 추가 또는 레벨업
        weaponManager.AddWeapon(selectedWeapon);

        // 패널 숨기기
        HideUpgradePanel();
    }

    /// <summary>
    /// 패널 숨기기
    /// </summary>
    public void HideUpgradePanel()
    {
        isShowing = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        // 게임 재개
        Time.timeScale = 1f;
    }
}
