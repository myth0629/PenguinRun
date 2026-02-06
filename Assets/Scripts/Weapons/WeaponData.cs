using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 데이터 ScriptableObject
/// - 무기별 기본 정보 및 레벨업 데이터 관리
/// </summary>
[CreateAssetMenu(fileName = "New Weapon", menuName = "ScriptableObjects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("=== 기본 정보 ===")]
    public string weaponName;
    public Sprite weaponIcon;
    
    [TextArea(2, 4)]
    public string weaponDescription;

    [Header("=== 투사체 ===")]
    public GameObject projectilePrefab;
    
    [Header("=== 발사 패턴 ===")]
    [Tooltip("부채꼴(샷건) 발사 사용 여부")]
    public bool useSpreadFire = false;
    
    [Tooltip("총 퍼짐 각도 (예: 60도면 -30~+30 범위로 발사)")]
    public float spreadAngle = 60f;

    [Header("=== 레벨별 데이터 ===")]
    public List<LevelData> levelUpstream;

    [System.Serializable]
    public struct LevelData
    {
        [Tooltip("데미지")]
        public float damage;
        
        [Tooltip("쿨다운 (초)")]
        public float cooldown;
        
        [Tooltip("투사체 속도")]
        public float speed;
        
        [Tooltip("한 번에 발사되는 투사체 개수")]
        public int projectileCount;
        
        [Tooltip("투사체 크기 배율")]
        public float sizeMultiplier;
        
        [Tooltip("관통 횟수 (0 = 관통 없음)")]
        public int pierceCount;
        
        [TextArea(1, 2)]
        [Tooltip("스킬 선택창에 표시할 설명")]
        public string description;
    }

    /// <summary>
    /// 특정 레벨의 데이터 반환 (1-indexed)
    /// </summary>
    public LevelData GetLevelData(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levelUpstream.Count - 1);
        return levelUpstream[index];
    }

    /// <summary>
    /// 최대 레벨
    /// </summary>
    public int MaxLevel => levelUpstream.Count;
}
