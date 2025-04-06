using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float maxOpenAngle = 90f;     // Maximum door opening angle
    public float pushForce = 50f;        // Dramatically increased force
    public float doorMass = 1f;          // Even lighter door
    public float doorDamping = 3f;       // How quickly the door slows down
    
    private float currentAngularVelocity = 0f;
    private Quaternion initialRotation;
    private float currentRelativeAngle = 0f;
    
    // For debugging
    private bool hasBeenPushed = false;
    
    private void Start()
    {
        initialRotation = transform.rotation;
        Debug.Log("Door initialized at " + transform.position + " with initial rotation " + initialRotation.eulerAngles);
    }

    private void Update()
    {
        // Apply damping to slow door movement over time
        if (Mathf.Abs(currentAngularVelocity) > 0.01f)
        {
            // Update current angle
            currentRelativeAngle += currentAngularVelocity * Time.deltaTime;
            
            // Clamp angle to limits
            currentRelativeAngle = Mathf.Clamp(currentRelativeAngle, -maxOpenAngle, maxOpenAngle);
            
            // Apply rotation directly
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, currentRelativeAngle);
            
            // Apply damping after movement
            currentAngularVelocity *= (1f - Time.deltaTime * doorDamping);
            
            // Debug to track rotation
            if (hasBeenPushed)
            {
                Debug.Log("Door angle: " + currentRelativeAngle + ", current rotation: " + transform.rotation.eulerAngles);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for player or enemy and apply initial push
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            ApplyImmediatePush(other);
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        // Apply force when player or enemy is pushing door
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            ApplyImmediatePush(other);
        }
    }
    
    private void ApplyImmediatePush(Collider2D other)
    {
        hasBeenPushed = true;
        
        // Get direction vector from door to pusher
        Vector2 toPusher = (other.transform.position - transform.position).normalized;
        
        // Calculate dot product to determine which side the pusher is on
        float dotProduct = Vector2.Dot(transform.right, toPusher);
        
        // Force direction depends on which side of door
        float pushDirection = dotProduct > 0 ? 1f : -1f;
        
        // Apply a very strong push for immediate visibility
        currentAngularVelocity = pushDirection * pushForce / doorMass;
        
        // Debug
        Debug.Log("STRONG PUSH: " + pushDirection + " * " + pushForce + " = " + currentAngularVelocity);
        
        // Apply immediate rotation for testing
        currentRelativeAngle += pushDirection * 5f; // Move 5 degrees immediately
        transform.rotation = initialRotation * Quaternion.Euler(0, 0, currentRelativeAngle);
    }
}