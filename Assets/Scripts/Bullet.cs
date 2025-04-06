using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    public float speed = 30f;
    public float lifeDuration = 2f;
    private Rigidbody2D rb;
    private GameObject shooter; // track who fired the bullet
    
    // Shotgun pellet properties
    [HideInInspector] public bool isShotgunPellet = false;
    [HideInInspector] public bool hasRecordedHit = false;
    
    // Event that fires when this bullet hits an enemy
    public event Action OnEnemyHit;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;
        Destroy(gameObject, lifeDuration);
    }

    // Public method to set the shooter when spawned
    public void SetShooter(GameObject shooterObj)
    {
        shooter = shooterObj;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Debug.Log("Bullet hit " + collision.gameObject.name + " fired by " + (shooter != null ? shooter.name : "Unknown"));
        if (collision.gameObject == shooter) return; // Don't collide with the entity that shot it

        // Check if the shooter is the player before recording a hit
        bool isPlayerShooter = shooter != null && shooter.CompareTag("Player");
        bool isEnemyShooter = shooter != null && shooter.CompareTag("Enemy");

        switch (collision.gameObject.tag)
        {
            case "Enemy":
                // Prevent enemies from killing each other
                if (isEnemyShooter)
                {
                    // If an enemy shot this bullet and hit another enemy, just destroy the bullet without damage
                    Destroy(gameObject);
                    return;
                }
                
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    // Record hit only if player shot a LIVE enemy
                    if (isPlayerShooter && ScoreManager.Instance != null)
                    {
                        // For shotgun pellets, only record one hit per shotgun blast
                        if (!isShotgunPellet || !hasRecordedHit)
                        {
                            ScoreManager.Instance.RecordHit();
                            // Notify that this pellet hit an enemy
                            OnEnemyHit?.Invoke();
                        }
                    }
                    enemy.Die(); // Call Die() only if it's alive
                }
                // Regardless of hit, destroy the bullet
                Destroy(gameObject);
                break;
            case "Player":
                // Apply damage/effect to the player
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null) { player.TakeDamage(1); }
                 Destroy(gameObject); // Destroy bullet after hitting player
                break;
            case "Environment":
                 // Optionally record a miss if the player shot it (handled by accuracy calculation)
                 Destroy(gameObject); // Destroy bullet after hitting environment
                break;
            default:
                 // Destroy bullet if it hits anything else unaccounted for
                Destroy(gameObject);
                break;
        }
    }
    void Update()
    {
        
    }
}
