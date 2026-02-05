using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 무기 슬롯 UI
/// </summary>
public class WeaponSlotUI : MonoBehaviour
{
    [Header("=== UI 요소 ===")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject newBadge; // "NEW" 표시

    private System.Action onSelect;

    public void Setup(WeaponData data, int level, bool isOwned, System.Action selectCallback)
    {
        onSelect = selectCallback;

        // 아이콘
        if (iconImage != null && data.weaponIcon != null)
        {
            iconImage.sprite = data.weaponIcon;
        }

        // 이름
        if (nameText != null)
        {
            nameText.text = data.weaponName;
        }

        // 레벨
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }

        // 설명
        if (descriptionText != null)
        {
            var levelData = data.GetLevelData(level);
            descriptionText.text = levelData.description;
        }

        // NEW 뱃지
        if (newBadge != null)
        {
            newBadge.SetActive(!isOwned);
        }

        // 버튼 이벤트
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke());
        }
    }
}
