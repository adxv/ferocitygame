using UnityEngine;

public class Bullet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float speed = 30f;
    public float lifeDuration = 2f;
    private Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;
        Destroy(gameObject, lifeDuration);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Bullet hit " + collision.gameObject.name);
        switch (collision.gameObject.tag)
        {
            case "Enemy":
            Destroy(collision.gameObject);
            Destroy(gameObject);
            break;

            case "Player":
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(1);
            }
            Destroy(gameObject);
            break;

            case "Wall":
            Destroy(gameObject);
            break;
            
            default:
            break;
        }
        Destroy(gameObject);
    }
    void Update()
    {
        
    }
}
