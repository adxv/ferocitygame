using UnityEngine;

public class GunPickup : MonoBehaviour
{
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) // Tag your ground as "Ground"
        {
            rb.angularVelocity = 0f; // stop spinning
        }
    }
}