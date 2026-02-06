using UnityEngine;

/// <summary>
/// 적 데이터 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Enemy", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("=== 기본 정보 ===")]
    public string enemyName;
    public Sprite enemySprite;
    
    public enum EnemyType { Ground, Flying, Boss }
    public EnemyType enemyType;

    [Header("=== 스탯 ===")]
    public float maxHealth = 10f;
    public float moveSpeed = 3f;
    public float damage = 1f;
    
    [Header("=== 보스 전용 ===")]
    [Tooltip("보스 공격 쿨다운")]
    public float attackCooldown = 2f;
    [Tooltip("보스 투사체 데미지")]
    public float projectileDamage = 1f;
    [Tooltip("보스 투사체 속도")]
    public float projectileSpeed = 5f;
    
    [Header("=== 보상 ===")]
    public int expReward = 5;
    
    [Header("=== 이펙트 ===")]
    public GameObject deathEffect;
    public AudioClip deathSound;
}

