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
        Debug.Log("Bullet hit " + collision.gameObject.name);
        if (collision.gameObject == shooter) return;

        switch (collision.gameObject.tag)
        {
            case "Enemy":
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null) enemy.Die();
                gameObject.SetActive(false);
                break;
            case "Player":
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null) { player.TakeDamage(1); }
                gameObject.SetActive(false);
                break;
            case "Environment":
                gameObject.SetActive(false);
                break;
        }
    }
    void Update()
    {
        
    }
}
