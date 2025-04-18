using UnityEngine;
using System.Collections;

public class EntranceDoubleDoorController : MonoBehaviour
{
    [Header("Door References")]
    public GameObject leftDoor;
    public GameObject rightDoor;
    
    [Header("Door Settings")]
    public float closingDelay = 0.5f;
    public float openingDelay = 0.5f;
    [Tooltip("Base force for closing doors - use small values (0.01-0.5)")]
    public float closingForce = 0.05f;
    [Tooltip("Base force for opening doors - use small values (0.01-0.5)")]
    public float openingForce = 0.05f;
    [Range(0.01f, 1.0f)]
    [Tooltip("Fine-tune how sensitive doors are to force values")]
    public float forceSensitivity = 0.1f;
    public bool openDirectionLeft = false;  // Direction the left door should open when unlocked
    public bool openDirectionRight = true;  // Direction the right door should open when unlocked
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip closingSound;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    public float minTimeBetweenSounds = 1f;
    
    [Header("Floor Settings")]
    public FloorManager currentFloor;
    public bool autoDetectFloor = true;
    
    // Private door controllers
    private DoorController leftDoorController;
    private DoorController rightDoorController;
    
    // Colliders
    private Collider2D leftDoorCollider;
    private Collider2D rightDoorCollider;
    
    // Soft lock prevention
    private GameObject softLockPreventor;
    private bool playerInPreventionZone = false;
    private Coroutine closingCoroutine = null;
    
    // State tracking
    private bool isLocked = false;
    private bool playerHasPassed = false;
    private float lastSoundTime = -1f;
    
    private void Start()
    {
        // Setup the audio source if needed
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
        
        if (currentFloor == null)
        {
            Debug.LogWarning("EntranceDoubleDoorController: No floor assigned! Level completion detection won't work correctly.");
        }
        
        // Get door controllers and colliders
        if (leftDoor != null)
        {
            leftDoorController = leftDoor.GetComponent<DoorController>();
            leftDoorCollider = leftDoor.GetComponent<Collider2D>();
            
            if (leftDoorController == null)
            {
                Debug.LogError("Left door reference is missing DoorController component!");
            }
            
            if (leftDoorCollider == null)
            {
                Debug.LogError("Left door reference is missing Collider2D component!");
            }
        }
        else
        {
            Debug.LogError("Left door reference is missing!");
        }
        
        if (rightDoor != null)
        {
            rightDoorController = rightDoor.GetComponent<DoorController>();
            rightDoorCollider = rightDoor.GetComponent<Collider2D>();
            
            if (rightDoorController == null)
            {
                Debug.LogError("Right door reference is missing DoorController component!");
            }
            
            if (rightDoorCollider == null)
            {
                Debug.LogError("Right door reference is missing Collider2D component!");
            }
        }
        else
        {
            Debug.LogError("Right door reference is missing!");
        }
        
        // Find the soft lock prevention zone
        FindSoftLockPreventor();
    }
    
    private void FindSoftLockPreventor()
    {
        // Find child with the EntranceSoftLockPreventor layer
        foreach (Transform child in transform)
        {
            if (child.gameObject.layer == LayerMask.NameToLayer("EntranceSoftLockPreventor"))
            {
                softLockPreventor = child.gameObject;
                
                // Add the prevention zone script
                SoftLockPreventorTrigger preventorScript = softLockPreventor.GetComponent<SoftLockPreventorTrigger>();
                if (preventorScript == null)
                {
                    preventorScript = softLockPreventor.AddComponent<SoftLockPreventorTrigger>();
                    preventorScript.Initialize(this);
                }
                
                Debug.Log("Found EntranceSoftLockPreventor zone: " + softLockPreventor.name);
                break;
            }
        }
        
        if (softLockPreventor == null)
        {
            Debug.LogWarning("No child with 'EntranceSoftLockPreventor' layer found. Door will close after delay without checking player position.");
        }
    }

    private void DetectCurrentFloor()
    {
        // Find all floor managers
        FloorManager[] floorManagers = FindObjectsByType<FloorManager>(FindObjectsSortMode.None);
        
        foreach (FloorManager floor in floorManagers)
        {
            Bounds floorArea = new Bounds(floor.transform.position, new Vector3(floor.floorBounds.x, floor.floorBounds.y, 10f));
            
            // If this entrance is within the bounds of this floor, set it as the current floor
            if (floorArea.Contains(transform.position))
            {
                currentFloor = floor;
                break;
            }
        }
    }
    
    private void Update()
    {
        // If the doors are locked, check if all enemies are dead in the entire level
        if (isLocked)
        {
            CheckForLevelCompletion();
        }
    }
    
    private void CheckForLevelCompletion()
    {
        if (AreAllEnemiesDeadInLevel())
        {
            // Unlock the doors and open them
            isLocked = false;
            
            // Start delayed auto-open coroutine
            StartCoroutine(DelayedDoorsOpen());
            
            // Update door collider settings
            UpdateDoorColliders(false);
            
            Debug.Log("All enemies in the level defeated! Entrance unlocking.");
        }
    }
    
    private bool AreAllEnemiesDeadInLevel()
    {
        // Find all floor managers
        FloorManager[] floorManagers = FindObjectsByType<FloorManager>(FindObjectsSortMode.None);
        
        // If there are no floor managers, return false
        if (floorManagers.Length == 0)
        {
            Debug.LogWarning("No FloorManagers found in scene. Door will remain locked.");
            return false;
        }
        
        // Check every floor
        foreach (FloorManager floor in floorManagers)
        {
            // If any floor has living enemies, return false
            if (!floor.AreAllEnemiesDead())
            {
                return false;
            }
        }
        
        // All floors have all enemies dead
        return true;
    }
    
    private IEnumerator DelayedDoorsOpen()
    {
        // Wait for specified delay
        yield return new WaitForSeconds(openingDelay);
        
        // Play unlock sound
        PlaySound(unlockSound);
        
        // Apply force to open doors
        OpenDoors();
    }
    
    private void OpenDoors()
    {
        // Use direction from the inspector setting
        float leftPushDirection = openDirectionLeft ? 1f : -1f;
        float rightPushDirection = openDirectionRight ? 1f : -1f;
        
        // Apply forces directly using our helper methods
        if (leftDoorController != null)
        {
            ApplyForceToDoor(leftDoor, leftPushDirection);
        }
        
        if (rightDoorController != null)
        {
            ApplyForceToDoor(rightDoor, rightPushDirection);
        }
    }
    
    private void CloseDoors()
    {
        // Apply smooth closing force to both doors
        if (leftDoorController != null)
        {
            ApplySmoothClosingForce(leftDoor);
        }
        
        if (rightDoorController != null)
        {
            ApplySmoothClosingForce(rightDoor);
        }
    }
    
    private void ApplySmoothClosingForce(GameObject door)
    {
        DoorController controller = door.GetComponent<DoorController>();
        if (controller != null)
        {
            // Access the private fields via reflection
            System.Reflection.FieldInfo currentRelativeAngleField = typeof(DoorController).GetField("currentRelativeAngle", 
                                                                       System.Reflection.BindingFlags.NonPublic | 
                                                                       System.Reflection.BindingFlags.Instance);
            
            System.Reflection.FieldInfo currentAngularVelocityField = typeof(DoorController).GetField("currentAngularVelocity", 
                                                                         System.Reflection.BindingFlags.NonPublic | 
                                                                         System.Reflection.BindingFlags.Instance);
            
            if (currentRelativeAngleField != null && currentAngularVelocityField != null)
            {
                // Get the current door angle
                float currentAngle = (float)currentRelativeAngleField.GetValue(controller);
                
                // Determine the closing direction - we want to go back to 0
                // If angle is positive, we need negative force and vice versa
                float closingDirection = currentAngle > 0 ? -1f : 1f;
                
                // If the door is already near-closed (within 1 degree), just reset it to avoid oscillation
                if (Mathf.Abs(currentAngle) < 1.0f)
                {
                    // Reset the angle and velocity
                    currentRelativeAngleField.SetValue(controller, 0f);
                    currentAngularVelocityField.SetValue(controller, 0f);
                    
                    // Get initial rotation and set it directly
                    System.Reflection.FieldInfo initialRotationField = typeof(DoorController).GetField("initialRotation", 
                                                                          System.Reflection.BindingFlags.NonPublic | 
                                                                          System.Reflection.BindingFlags.Instance);
                    if (initialRotationField != null)
                    {
                        Quaternion initialRotation = (Quaternion)initialRotationField.GetValue(controller);
                        door.transform.rotation = initialRotation;
                    }
                }
                else
                {
                    // Apply a closing force based on how far the door is open
                    // The further open, the stronger the force, but with much more sensitivity
                    // to small force values
                    float angleRatio = Mathf.Abs(currentAngle) / 90f;
                    float forceMagnitude = closingForce * angleRatio * forceSensitivity;
                    
                    // Ensure minimum force
                    forceMagnitude = Mathf.Max(forceMagnitude, closingForce * 0.1f * forceSensitivity);
                    
                    // Get doorMass via reflection
                    System.Reflection.FieldInfo doorMassField = typeof(DoorController).GetField("doorMass", 
                                                                    System.Reflection.BindingFlags.Public | 
                                                                    System.Reflection.BindingFlags.Instance);
                    
                    float doorMass = 1.0f;
                    if (doorMassField != null)
                    {
                        doorMass = (float)doorMassField.GetValue(controller);
                    }
                    
                    // Apply the angular velocity for smooth closing
                    float angularVelocity = closingDirection * forceMagnitude / doorMass;
                    currentAngularVelocityField.SetValue(controller, angularVelocity);
                }
            }
        }
    }
    
    private void ApplyForceToDoor(GameObject door, float direction)
    {
        // Create a temporary trigger object to simulate a player pushing the door
        GameObject tempPusher = new GameObject("TempPusher");
        tempPusher.tag = "Player";
        
        // Position the pusher on the appropriate side of the door
        tempPusher.transform.position = door.transform.position + (door.transform.right * direction * 2f);
        
        // Add a collider component
        BoxCollider2D collider = tempPusher.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        
        // Get the door controller
        DoorController controller = door.GetComponent<DoorController>();
        if (controller != null)
        {
            // Before we let the door handle the collision, we'll reduce the effective opening force
            // by modifying the pushForce property temporarily
            System.Reflection.FieldInfo pushForceField = typeof(DoorController).GetField("pushForce", 
                                                             System.Reflection.BindingFlags.Public | 
                                                             System.Reflection.BindingFlags.Instance);
            if (pushForceField != null)
            {
                // Store original push force
                float originalPushForce = (float)pushForceField.GetValue(controller);
                
                // Apply a much smaller push force for controlled opening
                float adjustedPushForce = openingForce * forceSensitivity;
                pushForceField.SetValue(controller, adjustedPushForce);
                
                // Let the door handle the collision with our fake pusher
                controller.HandleTriggerEvent(collider);
                
                // Restore original push force value
                pushForceField.SetValue(controller, originalPushForce);
            }
            else
            {
                // Fallback in case reflection fails
                controller.HandleTriggerEvent(collider);
            }
        }
        
        // Destroy the temporary object
        Destroy(tempPusher, 0.1f);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to the player
        if (other.CompareTag("Player") && !playerHasPassed && !isLocked)
        {
            playerHasPassed = true;
            
            // Only start the closing sequence if there's no prevention zone
            // or if the player is not in the prevention zone
            if (softLockPreventor == null || !playerInPreventionZone)
            {
                // Start the closing sequence with delay
                closingCoroutine = StartCoroutine(DelayedDoorsClose());
            }
            else
            {
                Debug.Log("Player has entered main trigger but is still in prevention zone. Waiting for exit.");
            }
        }
        else if (other.CompareTag("Player") && isLocked)
        {
            // Play locked sound if player tries to open locked doors
            PlaySound(lockedSound);
        }
    }
    
    // Called from the SoftLockPreventorTrigger
    public void PlayerEnteredPreventionZone()
    {
        playerInPreventionZone = true;
        
        // If we're already trying to close the doors, cancel it
        if (closingCoroutine != null)
        {
            StopCoroutine(closingCoroutine);
            closingCoroutine = null;
            Debug.Log("Door closing canceled because player entered prevention zone.");
        }
    }
    
    // Called from the SoftLockPreventorTrigger
    public void PlayerExitedPreventionZone()
    {
        playerInPreventionZone = false;
        
        // Only start closing sequence if the player has already triggered the main zone
        if (playerHasPassed && !isLocked && gameObject.activeInHierarchy)
        {
            // Start the closing sequence with delay
            closingCoroutine = StartCoroutine(DelayedDoorsClose());
            Debug.Log("Player exited prevention zone. Starting door closing sequence.");
        }
    }
    
    private IEnumerator DelayedDoorsClose()
    {
        // Wait for specified delay
        yield return new WaitForSeconds(closingDelay);
        
        // Check again in case player entered prevention zone during delay
        if (playerInPreventionZone)
        {
            Debug.Log("Player entered prevention zone during closing delay. Aborting door close.");
            closingCoroutine = null;
            yield break;
        }
        
        // Play closing sound
        PlaySound(closingSound);
        
        // Close and lock the doors
        CloseDoors();
        isLocked = true;
        
        // Update door collider settings
        UpdateDoorColliders(true);
        
        closingCoroutine = null;
        Debug.Log("Entrance doors locked! Clear the level to unlock them.");
    }
    
    private void UpdateDoorColliders(bool lockDoors)
    {
        if (lockDoors)
        {
            // When locking doors, ensure door colliders are non-trigger
            if (leftDoorCollider != null)
            {
                leftDoorCollider.isTrigger = false;
            }
            
            if (rightDoorCollider != null)
            {
                rightDoorCollider.isTrigger = false;
            }
        }
        else
        {
            // When unlocking doors, set colliders as needed
            if (leftDoorCollider != null)
            {
                // We need to check if the colliders should be triggers based on original settings
                // For now, we'll set them to triggers to allow pushing
                leftDoorCollider.isTrigger = true;
            }
            
            if (rightDoorCollider != null)
            {
                rightDoorCollider.isTrigger = true;
            }
        }
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
    
    // Public method to check if doors are locked
    public bool IsLocked()
    {
        return isLocked;
    }
}

// Helper class to handle trigger events in the prevention zone
public class SoftLockPreventorTrigger : MonoBehaviour
{
    private EntranceDoubleDoorController doorController;
    
    public void Initialize(EntranceDoubleDoorController controller)
    {
        doorController = controller;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (doorController != null && doorController.gameObject.activeInHierarchy && other.CompareTag("Player"))
        {
            doorController.PlayerEnteredPreventionZone();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (doorController != null && doorController.gameObject.activeInHierarchy && other.CompareTag("Player"))
        {
            doorController.PlayerExitedPreventionZone();
        }
    }
}
