using UnityEngine;

public class StairsTeleporter : MonoBehaviour
{
    public Transform teleportDestination;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Get the player's rigidbody if it exists
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            
            // Teleport the player to the destination
            collision.transform.position = teleportDestination.position;
            
            // If the player has a rigidbody, reset its velocity
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
            }
            
            // Optionally play a sound effect
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}