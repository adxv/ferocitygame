using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float maxOpenAngle = 90f;
    public float pushForce = 50f;
    public float doorMass = 1f;
    public float doorDamping = 3f;       // how quickly the door slows down
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip doorOpenSound;
    public float minTimeBetweenSounds = 0.1f;
    public float closedAngleThreshold = 1.0f; // angle threshold to consider the door closed

    public Quaternion initialRotation;
    public float currentAngularVelocity = 0f;
    public float currentRelativeAngle = 0f;
    private float lastSoundTime = -1f;    // when we last played a sound
    private bool wasConsideredClosed = true; // track if the door was closed last frame
    
    // reference to child object
    private GameObject doorTriggerZone;
    
    private void Start()
    {
        initialRotation = transform.rotation;
        
        currentRelativeAngle = 0f; 
        wasConsideredClosed = Mathf.Abs(currentRelativeAngle) < closedAngleThreshold;
        
        audioSource = GetComponent<AudioSource>();
        
        for (int i = 0; i < transform.childCount; i++)
        {
            // find trigger zone
            if (transform.GetChild(i).name == "DoorTriggerZone")
            {
                doorTriggerZone = transform.GetChild(i).gameObject;
                TriggerHandler triggerHandler = doorTriggerZone.AddComponent<TriggerHandler>();
                triggerHandler.Initialize(this);
                break;
            }
        }
        
        if (doorTriggerZone == null)
        {
            Debug.LogWarning("DoorTriggerZone child not found");
        }
    }

    private void Update()
    {
        
        // check if the door is currently closed
        bool isCurrentlyClosed = Mathf.Abs(currentRelativeAngle) < closedAngleThreshold;
        
        // check if closed state changed since last frame
        if (isCurrentlyClosed != wasConsideredClosed)
        {
            // sound logic
            if (doorOpenSound != null && audioSource != null && Time.time > lastSoundTime + minTimeBetweenSounds)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(doorOpenSound);
                lastSoundTime = Time.time;
            }
        }
        
        wasConsideredClosed = isCurrentlyClosed;

        // apply physics only if the door is moving
        if (Mathf.Abs(currentAngularVelocity) > 0.01f)
        {
            // update current angle based on velocity
            currentRelativeAngle += currentAngularVelocity * Time.deltaTime;
            currentRelativeAngle = Mathf.Clamp(currentRelativeAngle, -maxOpenAngle, maxOpenAngle); //limit angle to max open angle
            
            // rotation
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, currentRelativeAngle);
            // damping
            currentAngularVelocity *= (1f - Time.deltaTime * doorDamping);
        }
    }

    // when event is triggered
    public void HandleTriggerEvent(Collider2D other)
    {
        // only apply force if its the player or the enemy pushing the door
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            ApplyImmediatePush(other);
        }
    }
    
    private void ApplyImmediatePush(Collider2D other)
    {
        // get direction vector from door to pusher
        Vector2 toPusher = (other.transform.position - transform.position).normalized;
        
        // calculate dot product to determine which side the pusher is on
        float dotProduct = Vector2.Dot(transform.right, toPusher);
        
        // force direction depends on which side of door
        float pushDirection = dotProduct > 0 ? 1f : -1f;
        
        currentAngularVelocity = pushDirection * pushForce / doorMass;
    }
}

public class TriggerHandler : MonoBehaviour
{
    private DoorController doorController;
    
    public void Initialize(DoorController controller)
    {
        doorController = controller;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (doorController != null)
        {
            doorController.HandleTriggerEvent(other);
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (doorController != null)
        {
            doorController.HandleTriggerEvent(other);
        }
    }
}