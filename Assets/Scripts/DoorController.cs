using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float maxOpenAngle = 90f;     // Maximum door opening angle
    public float pushForce = 50f;        // Dramatically increased force
    public float doorMass = 1f;          // Even lighter door
    public float doorDamping = 3f;       // How quickly the door slows down
    
    [Header("Audio")]
    public AudioSource audioSource;       // Reference to the AudioSource component
    public AudioClip doorOpenSound;      // Sound to play when door is pushed
    public float minTimeBetweenSounds = 0.1f;  // Minimum time between playing sounds
    public float closedAngleThreshold = 1.0f; // Angle threshold (degrees) to consider the door closed

    private float currentAngularVelocity = 0f;
    private Quaternion initialRotation;
    private float currentRelativeAngle = 0f;
    private float lastSoundTime = -1f;    // Track when we last played a sound
    private bool wasConsideredClosed = true; // Track if the door was closed last frame
    
    // For debugging
    private bool hasBeenPushed = false;
    
    private void Start()
    {
        initialRotation = transform.rotation;
        // Initialize based on starting angle
        currentRelativeAngle = 0f; 
        wasConsideredClosed = Mathf.Abs(currentRelativeAngle) < closedAngleThreshold;
        Debug.Log("Door initialized at " + transform.position + " with initial rotation " + initialRotation.eulerAngles);
        
        // Get or add AudioSource component
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;  // Make the sound fully 3D
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 15f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
    }

    private void Update()
    {
        // --- Sound Logic --- 
        // Check if the door is currently considered closed based on its angle
        bool isCurrentlyClosed = Mathf.Abs(currentRelativeAngle) < closedAngleThreshold;
        
        // Check if the closed state changed since last frame
        if (isCurrentlyClosed != wasConsideredClosed)
        {
             // Check if enough time has passed since the last sound
            if (doorOpenSound != null && audioSource != null && Time.time > lastSoundTime + minTimeBetweenSounds)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);  // Slight pitch variation
                audioSource.PlayOneShot(doorOpenSound);
                lastSoundTime = Time.time;
            }
        }
        
        // Update the state for the next frame, regardless of movement
        wasConsideredClosed = isCurrentlyClosed;
        // --- End Sound Logic ---

        // Apply physics (damping, angle change, rotation) only if the door is moving
        if (Mathf.Abs(currentAngularVelocity) > 0.01f)
        {
            // Update current angle based on velocity
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
        // No 'else' block needed here anymore, as wasConsideredClosed is updated above
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for player or enemy and apply initial push
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            // Sound logic moved to Update
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
        // Commenting out the direct rotation adjustment as it might interfere with the physics-based rotation in Update
        // currentRelativeAngle += pushDirection * 5f; 
        // transform.rotation = initialRotation * Quaternion.Euler(0, 0, currentRelativeAngle);
    }
}