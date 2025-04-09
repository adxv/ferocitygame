using UnityEngine;
using System.Collections.Generic;

public class FloorAccessController : MonoBehaviour
{
    [Header("Floor Access Settings")]
    public FloorManager currentFloor; // Reference to the current floor manager
    public FloorManager destinationFloor; // Reference to the floor this stairway leads to
    public Transform destinationPoint; // Where the player will teleport to
    
    [Header("Visual Indicators")]
    public GameObject blockedSprite; // Visual indicator when stairway is blocked
    public GameObject unlockedSprite; // Visual indicator when stairway is unlocked
        public GameObject usedSprite; // Visual indicator when stairway has been used
    
    [Header("Options")]
    [Tooltip("If true, this stairway is always unlocked regardless of enemy status")]
    public bool alwaysUnlocked = false;
    [Tooltip("If true, this stairway allows only one use after enemies are cleared")]
    public bool singleUseOnly = true;
    
    private bool isUnlocked = false;
    private bool hasBeenUsed = false;
    
    void Start()
    {
        // Find the current floor manager if not set
        if (currentFloor == null)
        {
            // Find all floor managers
            FloorManager[] floorManagers = FindObjectsOfType<FloorManager>();
            
            foreach (FloorManager floor in floorManagers)
            {
                Bounds floorArea = new Bounds(floor.transform.position, new Vector3(floor.floorBounds.x, floor.floorBounds.y, 10f));
                
                // If this stairway is within the bounds of this floor, set it as the current floor
                if (floorArea.Contains(transform.position))
                {
                    currentFloor = floor;
                    
                    // Add this stairway to the floor's list of stairways
                    if (!floor.stairwaysOnFloor.Contains(this))
                    {
                        floor.stairwaysOnFloor.Add(this);
                    }
                    
                    break;
                }
            }
        }
        
        if (currentFloor == null)
        {
            Debug.LogError("FloorAccessController: Could not find a FloorManager for this access point!");
        }
        else
        {
            Debug.Log($"Stair access at {transform.position} initialized on {currentFloor.floorName}");
        }
        
        // Set initial unlock state
        if (alwaysUnlocked)
        {
            isUnlocked = true;
            hasBeenUsed = false;
        }
        else
        {
            // Start locked, need to clear enemies
            isUnlocked = false;
            hasBeenUsed = false;
        }
        
        UpdateVisuals();
        
        // Validate destination
        if (destinationFloor == null && destinationPoint != null)
        {
            Debug.LogWarning("FloorAccessController: Destination point set but no destination floor assigned!");
        }
    }
    
    void Update()
    {
        // Always check if all enemies on current floor are dead, if not always unlocked and not used
        if (!isUnlocked && !hasBeenUsed && currentFloor != null && !alwaysUnlocked)
        {
            CheckIfAllEnemiesDeadOnCurrentFloor();
        }
    }
    
    private void CheckIfAllEnemiesDeadOnCurrentFloor()
    {
        if (currentFloor.AreAllEnemiesDead())
        {
            isUnlocked = true;
            UpdateVisuals();
            Debug.Log($"Stair access unlocked on {currentFloor.floorName}!");
        }
    }
    
    private void UpdateVisuals()
    {
        if (hasBeenUsed && singleUseOnly)
        {
            // If stairway has been used and is single-use only, show used state
            if (blockedSprite != null) blockedSprite.SetActive(false);
            if (unlockedSprite != null) unlockedSprite.SetActive(false);
            if (usedSprite != null) usedSprite.SetActive(true);
        }
        else
        {
            // Normal state (locked or unlocked)
            if (blockedSprite != null) blockedSprite.SetActive(!isUnlocked);
            if (unlockedSprite != null) unlockedSprite.SetActive(isUnlocked);
            if (usedSprite != null) usedSprite.SetActive(false);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to player
        if (other.CompareTag("Player"))
        {
            // Check if stairway is unlocked and has not been used (if single-use only)
            if (isUnlocked && destinationPoint != null && !(hasBeenUsed && singleUseOnly))
            {
                // Teleport player to destination
                other.transform.position = destinationPoint.position;
                
                if (singleUseOnly)
                {
                    // Mark as used - will be permanently locked
                    hasBeenUsed = true;
                    isUnlocked = false;
                    Debug.Log($"Stair access at {currentFloor.floorName} has been used and is now permanently locked!");
                }
                else if (!alwaysUnlocked)
                {
                    // Just lock again until enemies are cleared
                    isUnlocked = false;
                }
                
                UpdateVisuals();
            }
            else if (hasBeenUsed && singleUseOnly)
            {
                Debug.Log($"This access point has already been used and cannot be used again.");
            }
            else if (!isUnlocked)
            {
                Debug.Log($"Stair access is locked! Clear all enemies on {currentFloor.floorName} first.");
            }
        }
    }
}