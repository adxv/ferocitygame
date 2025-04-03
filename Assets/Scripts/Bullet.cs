using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 30f;
    public float lifeDuration = 2f;
    private Rigidbody2D rb;
    private GameObject shooter; // track who fired the bullet

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

        switch (collision.gameObject.tag)
        {
            case "Enemy":
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    // Record hit only if player shot a LIVE enemy
                    if (isPlayerShooter && ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.RecordHit();
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
