using UnityEngine;

public class StairsTeleporter : MonoBehaviour //unused script
{
    public Transform teleportDestination;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            collision.transform.position = teleportDestination.position; //tp
            
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
            }
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}