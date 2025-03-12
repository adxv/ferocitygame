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

    void OnTriggerEnter2D(Collider2D hitObject)
    {
        Debug.Log("Bullet hit " + hitObject.name);
        if(hitObject.CompareTag("Enemy"))
        {
            Destroy(hitObject.gameObject);
        }
        Destroy(gameObject);
    }
    void Update()
    {
        
    }
}
