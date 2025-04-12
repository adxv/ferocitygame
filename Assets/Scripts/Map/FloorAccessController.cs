using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FloorAccessController : MonoBehaviour
{
    [Header("Floor Access Settings")]
    public FloorManager currentFloor; // Reference to the current floor manager
    public FloorManager destinationFloor; // Reference to the floor this stairway leads to
    public Transform destinationPoint; // Where the player will teleport to
    
    [Header("Options")]
    [Tooltip("If true, this stairway allows only one use after enemies are cleared")]
    public bool singleUseOnly = true;
    
    [Header("Exit Point")]
    [Tooltip("If true, this stairway is the exit point after level completion")]
    public bool isExitPoint = false;
    
    [Header("Direction Settings")]
    [Tooltip("When true, only applies enemy-clearing requirement for upward movement")]
    public bool restrictUpwardOnly = true;
    
    [Header("Visual")]
    [Tooltip("Child object that indicates unlocked state with animation")]
    public GameObject unlockedIndicator;
    [Tooltip("Speed of the indicator animation")]
    public float indicatorBobSpeed = 2f;
    [Tooltip("Maximum movement distance (total range will be 2x this value)")]
    public float indicatorBobAmount = 0.5f;
    
    [Header("Screen Transition")]
    [Tooltip("Reference to the Canvas/Image used for screen fading")]
    public CanvasGroup fadeCanvasGroup;
    [Tooltip("Duration of the fade transition in seconds")]
    public float fadeDuration = 0.5f;
    
    private bool isUnlocked = false;
    private bool hasBeenUsed = false;
    private Vector3 indicatorStartPosition;
    private bool isTransitioning = false;
    
    // Track level completion without levelManager reference
    [HideInInspector]
    public static bool isLevelComplete = false;
    
    void Start()
    {
        // Find the current floor manager if not set
        if (currentFloor == null)
        {
            // Find all floor managers
            FloorManager[] floorManagers = FindObjectsByType<FloorManager>(FindObjectsSortMode.None);
            
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
        isUnlocked = false;
        hasBeenUsed = false;
        
        // Validate destination
        if (destinationFloor == null && destinationPoint != null)
        {
            Debug.LogWarning("FloorAccessController: Destination point set but no destination floor assigned!");
        }
        
        // Set up the UnlockedIndicator
        if (unlockedIndicator == null)
        {
            // Try to find it as a child
            unlockedIndicator = transform.Find("UnlockedIndicator")?.gameObject;
        }
        
        if (unlockedIndicator != null)
        {
            indicatorStartPosition = unlockedIndicator.transform.localPosition;
            unlockedIndicator.SetActive(false); // Start disabled
        }
        
        // Try to find the fade canvas if not assigned
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GameObject.FindObjectOfType<Canvas>()?.GetComponentInChildren<CanvasGroup>();
            
            // Initialize fade canvas if found
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
        }
        
        UpdateVisuals();
    }
    
    void Update()
    {
        // Only check enemy status if access point is to a higher floor
        if (!isUnlocked && !hasBeenUsed && currentFloor != null)
        {
            bool isMovingUp = (destinationFloor != null && destinationFloor.floorIndex > currentFloor.floorIndex);
            
            // If we're not restricting upward only OR we are moving up
            if (!restrictUpwardOnly || isMovingUp)
            {
                CheckIfAllEnemiesDeadOnCurrentFloor();
            }
            else
            {
                // Don't auto-unlock downward paths - player can only go down when level is complete
                if (isLevelComplete)
                {
                    isUnlocked = true;
                    UpdateVisuals();
                }
            }
        }
        
        // Animate the indicator if it's active
        if (unlockedIndicator != null && unlockedIndicator.activeSelf)
        {
            float newY = indicatorStartPosition.y + Mathf.Sin(Time.time * indicatorBobSpeed) * indicatorBobAmount;
            unlockedIndicator.transform.localPosition = new Vector3(
                indicatorStartPosition.x,
                newY,
                indicatorStartPosition.z
            );
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
        if (unlockedIndicator != null)
        {
            if (isUnlocked && !hasBeenUsed)
            {
                unlockedIndicator.SetActive(true);
            }
            else
            {
                unlockedIndicator.SetActive(false);
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Prevent multiple triggers during transition
        if (isTransitioning)
            return;
            
        // Only react to player
        if (other.CompareTag("Player"))
        {
            // Determine floor direction
            bool isGoingUp = false;
            if (currentFloor != null && destinationFloor != null)
            {
                isGoingUp = destinationFloor.floorIndex > currentFloor.floorIndex;
            }
            
            // Determine if going down
            bool isGoingDown = false;
            if (currentFloor != null && destinationFloor != null)
            {
                isGoingDown = destinationFloor.floorIndex < currentFloor.floorIndex;
            }
            
            // Special case: this is the exit point and level is complete
            if (isExitPoint && isLevelComplete)
            {
                // Allow player to exit regardless of direction
                UseAccessPoint(other);
                return;
            }
            
            // Normal case: Check if stairs are unlocked
            if (isUnlocked)
            {
                // Check direction rule for going up
                if (isGoingUp)
                {
                    // Going up is only allowed if all enemies on current floor are dead
                    if (currentFloor.AreAllEnemiesDead())
                    {
                        UseAccessPoint(other);
                    }
                    else
                    {
                        Debug.Log("Cannot go up until all enemies on this floor are defeated!");
                    }
                }
                // Check direction rule for going down
                else if (isGoingDown)
                {
                    // Going down is only allowed if level is complete
                    if (isLevelComplete)
                    {
                        UseAccessPoint(other);
                    }
                    else
                    {
                        Debug.Log("Cannot go down until the level is completed!");
                    }
                }
                else
                {
                    // Horizontal movement is always allowed if unlocked
                    UseAccessPoint(other);
                }
            }
            else if (hasBeenUsed && singleUseOnly)
            {
                Debug.Log("This access point has already been used and cannot be used again.");
            }
            else
            {
                Debug.Log($"Stair access is locked! Clear all enemies on {currentFloor.floorName} first.");
            }
        }
    }
    
    // Helper function to handle teleportation
    private void UseAccessPoint(Collider2D other)
    {
        if (destinationPoint == null) return;
        
        // Start the transition coroutine
        StartCoroutine(TransitionToDestination(other));
    }
    
    // Helper function to ensure we always have a reference to the fade canvas
    private void EnsureFadeCanvasGroup()
    {
        if (fadeCanvasGroup == null)
        {
            // First try to find by tag if it exists
            GameObject fadeObj = GameObject.FindWithTag("FadeCanvas");
            if (fadeObj != null)
            {
                fadeCanvasGroup = fadeObj.GetComponent<CanvasGroup>();
            }
            
            // If still null, try to find any canvas with a CanvasGroup
            if (fadeCanvasGroup == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    CanvasGroup cg = canvas.GetComponentInChildren<CanvasGroup>();
                    if (cg != null)
                    {
                        fadeCanvasGroup = cg;
                        break;
                    }
                }
            }
            
            // If still null, try to find any CanvasGroup in the scene
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = FindAnyObjectByType<CanvasGroup>();
            }
            
            // Initialize if found
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                Debug.LogWarning("Could not find any CanvasGroup for screen fading in the scene.");
            }
        }
    }
    
    private IEnumerator TransitionToDestination(Collider2D other)
    {
        // Prevent multiple triggers during transition
        isTransitioning = true;
        
        // Ensure we have a reference to the fade canvas
        EnsureFadeCanvasGroup();
        
        // Only attempt fade if we have a canvas group
        if (fadeCanvasGroup != null)
        {
            // Fade to black
            float startTime = Time.time;
            float endTime = startTime + fadeDuration;
            
            while (Time.time < endTime)
            {
                float elapsed = Time.time - startTime;
                float normalizedTime = elapsed / fadeDuration;
                fadeCanvasGroup.alpha = normalizedTime;
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 1f; // Ensure we're fully black
            fadeCanvasGroup.blocksRaycasts = true;
        }
        
        // Store original z-position of camera before teleport
        Camera mainCamera = Camera.main;

        // Teleport the player when screen is black
        other.transform.position = destinationPoint.position;
        
        // Also move the main camera to follow the player
        if (mainCamera != null)
        {
            Vector3 newCameraPosition = new Vector3(
                destinationPoint.position.x,
                destinationPoint.position.y,
                -1 // Keep the original z-position
            );
            mainCamera.transform.position = newCameraPosition;
        }
        
        // Update stairway state
        if (singleUseOnly)
        {
            // Mark as used - will be permanently locked
            hasBeenUsed = true;
            isUnlocked = false;
            Debug.Log($"Stair access at {currentFloor.floorName} has been used and is now permanently locked!");
        }
        else
        {
            // Just lock again until enemies are cleared
            isUnlocked = false;
        }
        
        UpdateVisuals();
        
        // Give a small pause at full black
        yield return new WaitForSeconds(0.1f);
        
        // Make sure the fadeCanvasGroup is still valid
        EnsureFadeCanvasGroup();
        
        // Only attempt fade if we have a canvas group
        if (fadeCanvasGroup != null)
        {
            // Fade back in
            float startTime = Time.time;
            float endTime = startTime + fadeDuration;
            
            while (Time.time < endTime)
            {
                float elapsed = Time.time - startTime;
                float normalizedTime = elapsed / fadeDuration;
                fadeCanvasGroup.alpha = 1f - normalizedTime;
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 0f; // Ensure we're fully transparent
            fadeCanvasGroup.blocksRaycasts = false;
        }
        
        // End of transition
        isTransitioning = false;
    }
}