using System;
using UnityEngine;

/// <summary>
/// 플레이어 레벨 및 경험치 시스템
/// - 경험치 획득 및 레벨업
/// - 레벨업 시 이벤트 발생
/// </summary>
public class PlayerLevel : MonoBehaviour
{
    [Header("=== 레벨 설정 ===")]
    [Tooltip("시작 레벨")]
    [SerializeField] private int startLevel = 1;
    
    [Tooltip("최대 레벨")]
    [SerializeField] private int maxLevel = 99;

    [Header("=== 경험치 설정 ===")]
    [Tooltip("1레벨에서 2레벨이 되는데 필요한 기본 경험치")]
    [SerializeField] private int baseExpRequired = 100;
    
    [Tooltip("레벨당 필요 경험치 증가율")]
    [SerializeField] private float expGrowthRate = 1.2f;
    
    [Tooltip("레벨업 시 남은 경험치 이월")]
    [SerializeField] private bool carryOverExp = true;

    [Header("=== 현재 상태 (디버그) ===")]
    [SerializeField] private int currentLevel;
    [SerializeField] private int currentExp;
    [SerializeField] private int expToNextLevel;

    // 이벤트
    public event Action<int> OnLevelUp;                     // 레벨업 시 (새 레벨)
    public event Action<int, int> OnExpChanged;             // 경험치 변경 시 (현재, 필요량)
    public event Action<int, int, int> OnLevelChanged;      // 레벨 상태 변경 (레벨, 현재 경험치, 필요 경험치)

    // Properties
    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;
    public int MaxLevel => maxLevel;
    public float ExpProgress => expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 1f;
    public bool IsMaxLevel => currentLevel >= maxLevel;

    private void Awake()
    {
        InitializeLevel();
    }

    private void InitializeLevel()
    {
        currentLevel = startLevel;
        currentExp = 0;
        expToNextLevel = CalculateExpRequired(currentLevel);
        
        OnLevelChanged?.Invoke(currentLevel, currentExp, expToNextLevel);
    }

    /// <summary>
    /// 경험치 획득
    /// </summary>
    public void GainExp(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        currentExp += amount;
        Debug.Log($"[PlayerLevel] 경험치 획득: +{amount} (현재: {currentExp}/{expToNextLevel})");

        // 레벨업 체크
        while (currentExp >= expToNextLevel && !IsMaxLevel)
        {
            LevelUp();
        }

        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnLevelChanged?.Invoke(currentLevel, currentExp, expToNextLevel);
    }

    private void LevelUp()
    {
        if (IsMaxLevel) return;

        // 남은 경험치 계산
        int remainingExp = carryOverExp ? currentExp - expToNextLevel : 0;
        
        currentLevel++;
        currentExp = remainingExp;
        expToNextLevel = CalculateExpRequired(currentLevel);

        Debug.Log($"[PlayerLevel] 레벨업! Lv.{currentLevel} (다음 레벨까지: {expToNextLevel})");
        
        OnLevelUp?.Invoke(currentLevel);
    }

    /// <summary>
    /// 특정 레벨에서 다음 레벨로 가기 위해 필요한 경험치 계산
    /// </summary>
    private int CalculateExpRequired(int level)
    {
        if (level >= maxLevel) return 0;
        
        // 지수 성장: baseExp * growthRate^(level-1)
        return Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expGrowthRate, level - 1));
    }

    /// <summary>
    /// 특정 레벨까지 필요한 총 경험치 계산
    /// </summary>
    public int GetTotalExpForLevel(int targetLevel)
    {
        int total = 0;
        for (int i = 1; i < targetLevel; i++)
        {
            total += CalculateExpRequired(i);
        }
        return total;
    }

    /// <summary>
    /// 강제 레벨업 (치트/테스트용)
    /// </summary>
    public void ForceLevelUp()
    {
        if (!IsMaxLevel)
        {
            currentExp = expToNextLevel;
            LevelUp();
            OnExpChanged?.Invoke(currentExp, expToNextLevel);
            OnLevelChanged?.Invoke(currentLevel, currentExp, expToNextLevel);
        }
    }

    /// <summary>
    /// 레벨 리셋 (게임 재시작 시)
    /// </summary>
    public void ResetLevel()
    {
        InitializeLevel();
    }

    private void OnValidate()
    {
        if (startLevel < 1) startLevel = 1;
        if (maxLevel < startLevel) maxLevel = startLevel;
        if (baseExpRequired < 1) baseExpRequired = 1;
        if (expGrowthRate < 1f) expGrowthRate = 1f;
    }
}
