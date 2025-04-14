using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Movement")]
    public float forgetTime = 3f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float rotationSpeed = 5f;

    [Header("Appearance")]
    // Removed: public Sprite defaultSprite;
    public Sprite deathSprite;

    [Header("Stats")]
    public float health = 100f;
}