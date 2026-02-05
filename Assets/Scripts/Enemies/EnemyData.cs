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
    
    public enum EnemyType { Ground, Flying }
    public EnemyType enemyType;

    [Header("=== 스탯 ===")]
    public float maxHealth = 10f;
    public float moveSpeed = 3f;
    public float damage = 1f;
    
    [Header("=== 보상 ===")]
    public int expReward = 5;
    
    [Header("=== 이펙트 ===")]
    public GameObject deathEffect;
    public AudioClip deathSound;
}
