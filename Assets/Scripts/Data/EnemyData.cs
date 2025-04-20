using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public float forgetTime = 3f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float rotationSpeed = 5f;

    // public Sprite defaultSprite;
    public Sprite deathSprite;

    public float health = 100f;
}