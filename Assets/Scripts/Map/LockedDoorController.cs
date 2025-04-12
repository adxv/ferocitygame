using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LockedDoorController : MonoBehaviour
{
    [Header("Door Physics")]
    public float maxOpenAngle = 90f;     // Maximum door opening angle
    public float pushForce = 50f;        // Force to open door when unlocked
    public float doorMass = 1f;          // Door mass
    public float doorDamping = 3f;       // How quickly the door slows down
    public float unlockForce = 15f;      // Initial force to apply when door unlocks
    public float unlockDelay = 1f;       // Delay in seconds before door opens after unlocking
    
    [Header("Auto-Open Direction")]
    [Tooltip("When checked, door will open in positive direction. When unchecked, door will open in negative direction.")]
    public bool openDirection = true;  // Controls which way the door swings when auto-opening
    
    [Header("Audio")]
    public AudioSource audioSource;      // Audio source component
    public AudioClip unlockSound;        // Sound played when the door unlocks
    public AudioClip lockedSound;        // Sound played when player tries to open a locked door
    public float minTimeBetweenSounds = 1f; // Minimum time between playing sounds
    
    [Header("Floor Settings")]
    public FloorManager currentFloor;    // Reference to the floor this door is on
    public bool autoDetectFloor = true;  // Automatically detect which floor the door is on
    
    private float currentAngularVelocity = 0f;
    private Quaternion initialRotation;
    private float currentRelativeAngle = 0f;
    private bool isDoorUnlocked = false;
    private float lastSoundTime = -1f;   // Last time a sound was played
    
    private void Start()
    {
        initialRotation = transform.rotation;
        currentRelativeAngle = 0f; 
        
        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;  // 3D sound
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 15f;
            }
        }
        
        // Auto-detect floor if enabled
        if (autoDetectFloor && currentFloor == null)
        {
            DetectCurrentFloor();
        }
        
        // Door starts locked
        isDoorUnlocked = false;
        
        if (currentFloor == null)
        {
            Debug.LogError("LockedDoorController: No floor assigned! Door won't function correctly.");
        }
    }

    private void DetectCurrentFloor()
    {
        // Find all floor managers
        FloorManager[] floorManagers = FindObjectsByType<FloorManager>(FindObjectsSortMode.None);
        
        foreach (FloorManager floor in floorManagers)
        {
            Bounds floorArea = new Bounds(floor.transform.position, new Vector3(floor.floorBounds.x, floor.floorBounds.y, 10f));
            
            // If this door is within the bounds of this floor, set it as the current floor
            if (floorArea.Contains(transform.position))
            {
                currentFloor = floor;
                break;
            }
        }
    }

    private void Update()
    {
        // Check if all enemies are dead on the current floor
        if (!isDoorUnlocked && currentFloor != null)
        {
            CheckIfAllEnemiesDeadOnCurrentFloor();
        }
        
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
        }
    }
    
    private void CheckIfAllEnemiesDeadOnCurrentFloor()
    {
        if (currentFloor.AreAllEnemiesDead())
        {
            isDoorUnlocked = true;
            
            // Start delayed auto-open coroutine
            StartCoroutine(DelayedDoorOpen());
        }
    }
    
    private IEnumerator DelayedDoorOpen()
    {
        // Wait for specified delay
        yield return new WaitForSeconds(unlockDelay);
        
         // Play unlock sound
        PlaySound(unlockSound);

        // Apply initial force to crack the door open after delay
        ApplyUnlockForce();
    }
    
    private void ApplyUnlockForce()
    {
        // Use direction from the inspector setting instead of random
        float pushDirection = openDirection ? 1f : -1f;
        
        // Apply the unlock force
        currentAngularVelocity = pushDirection * unlockForce / doorMass;
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null && Time.time > lastSoundTime + minTimeBetweenSounds)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);  // Slight pitch variation
            audioSource.PlayOneShot(clip);
            lastSoundTime = Time.time;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isDoorUnlocked)
            {
                // Door is unlocked, apply push
                ApplyImmediatePush(other);
            }
            else
            {
                // Door is locked, play locked sound
                PlaySound(lockedSound);
            }
        }
        else if (isDoorUnlocked && other.CompareTag("Enemy"))
        {
            // Allow enemies to push unlocked doors
            ApplyImmediatePush(other);
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        // Apply force when player or enemy is pushing door, only if door is unlocked
        if (isDoorUnlocked && (other.CompareTag("Player") || other.CompareTag("Enemy")))
        {
            ApplyImmediatePush(other);
        }
    }
    
    private void ApplyImmediatePush(Collider2D other)
    {
        if (!isDoorUnlocked) return;
        
        // Get direction vector from door to pusher
        Vector2 toPusher = (other.transform.position - transform.position).normalized;
        
        // Calculate dot product to determine which side the pusher is on
        float dotProduct = Vector2.Dot(transform.right, toPusher);
        
        // Force direction depends on which side of door
        float pushDirection = dotProduct > 0 ? 1f : -1f;
        
        // Apply a push for immediate visibility
        currentAngularVelocity = pushDirection * pushForce / doorMass;
    }
} 