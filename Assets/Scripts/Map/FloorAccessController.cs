using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FloorAccessController : MonoBehaviour
{
    public FloorManager currentFloor;
    public FloorManager destinationFloor;
    public Transform destinationPoint;
    
    public bool singleUseOnly = true;
    public bool isExitPoint = false;
    public bool restrictUpwardOnly = true;
    
    public GameObject unlockedIndicator;
    public float indicatorBobSpeed = 2f;
    public float indicatorBobAmount = 0.5f;
    
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.5f;
    
    private bool isUnlocked = false;
    private bool hasBeenUsed = false;
    private Vector3 indicatorStartPosition;
    private bool isTransitioning = false;
    
    public static bool isLevelComplete = false;
    
    void Start()
    {
        if (currentFloor == null)
        {
            FloorManager[] floorManagers = FindObjectsByType<FloorManager>(FindObjectsSortMode.None);
            
            foreach (FloorManager floor in floorManagers)
            {
                Bounds floorArea = new Bounds(floor.transform.position, new Vector3(floor.floorBounds.x, floor.floorBounds.y, 10f));
                
                // // if this stairway is within the bounds of this floor, set it as the current floor
                // if (floorArea.Contains(transform.position))
                // {
                //     currentFloor = floor;
                    
                //     if (!floor.stairwaysOnFloor.Contains(this))
                //     {
                //         floor.stairwaysOnFloor.Add(this);
                //     }
                    
                //     break;
                // }
            }
        }
        
        if (currentFloor == null)
        {
            Debug.LogError("FloorAccessController: no FloorManager found");
        }
        
        isUnlocked = false;
        hasBeenUsed = false;
        
        if (destinationFloor == null && destinationPoint != null)
        {
            Debug.LogWarning("no destination floor assigned");
        }
        
        if (unlockedIndicator == null)
        {
            unlockedIndicator = transform.Find("UnlockedIndicator")?.gameObject;
        }
        
        if (unlockedIndicator != null)
        {
            indicatorStartPosition = unlockedIndicator.transform.localPosition;
            unlockedIndicator.SetActive(false); // Start disabled
        }
        
        UpdateVisuals();
    }
    
    void Update()
    {
        // only check enemy status if access point is to a higher floor
        if (!isUnlocked && !hasBeenUsed && currentFloor != null)
        {
            bool isMovingUp = (destinationFloor != null && destinationFloor.floorIndex > currentFloor.floorIndex);
            
            if (!restrictUpwardOnly || isMovingUp)
            {
                CheckIfAllEnemiesDeadOnCurrentFloor();
            }
            else
            {
                if (isLevelComplete)
                {
                    isUnlocked = true;
                    UpdateVisuals();
                }
            }
        }
        
        // animate indicator
        if (unlockedIndicator != null && unlockedIndicator.activeSelf)
        {
            float newY = indicatorStartPosition.y + Mathf.Sin(Time.time * indicatorBobSpeed) * indicatorBobAmount;
            unlockedIndicator.transform.localPosition = new Vector3(
                indicatorStartPosition.x,
                newY,
                indicatorStartPosition.z
            ); //i dont know what im looking at anymore
        }
    }
    
    private void CheckIfAllEnemiesDeadOnCurrentFloor()
    {
        if (currentFloor.AreAllEnemiesDead())
        {
            isUnlocked = true;
            UpdateVisuals();
            Debug.Log("unlocked");
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
        if (isTransitioning)
            return;
            
        if (other.CompareTag("Player"))
        {
            bool isGoingUp = false;
            if (currentFloor != null && destinationFloor != null)
            {
                isGoingUp = destinationFloor.floorIndex > currentFloor.floorIndex;
            }
            
            bool isGoingDown = false;
            if (currentFloor != null && destinationFloor != null)
            {
                isGoingDown = destinationFloor.floorIndex < currentFloor.floorIndex;
            }
            
            if (isExitPoint && isLevelComplete)
            {
                UseAccessPoint(other);
                return;
            }
            
            if (isUnlocked)
            {
                if (isGoingUp)
                {
                    if (currentFloor.AreAllEnemiesDead())
                    {
                        UseAccessPoint(other);
                    }
                }
                else if (isGoingDown)
                {
                    if (isLevelComplete)
                    {
                        UseAccessPoint(other);
                    }
                }
                else
                {
                    UseAccessPoint(other);
                }
            }
        }
    }
    //handle teleport
    private void UseAccessPoint(Collider2D other)
    {
        if (destinationPoint == null) return;    
        StartCoroutine(FadeAndTeleport(other));
    }
    
    // helper function to ensure fadeCanvasGroup is set after restart
    private void EnsureFadeCanvasGroup()
    {
        if (fadeCanvasGroup == null)
        {
            GameObject fadeObj = GameObject.FindWithTag("FadeCanvas");
            if (fadeObj != null)
            {
                fadeCanvasGroup = fadeObj.GetComponent<CanvasGroup>();
            }
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
            
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = FindAnyObjectByType<CanvasGroup>();
            }
            
            // found
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                Debug.LogWarning("could not find CanvasGroup");
            }
        }
    }
    
    private IEnumerator FadeAndTeleport(Collider2D other)
    {
        isTransitioning = true;
        EnsureFadeCanvasGroup();
        
        if (fadeCanvasGroup != null)
        {
            // fade to black
            float startTime = Time.time;
            float endTime = startTime + fadeDuration;
            
            while (Time.time < endTime)
            {
                float elapsed = Time.time - startTime;
                float normalizedTime = elapsed / fadeDuration;
                fadeCanvasGroup.alpha = normalizedTime;
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
        }
        
        Camera mainCamera = Camera.main;

        other.transform.position = destinationPoint.position; //teleport
        
        if (mainCamera != null) //teleport camera
        {
            Vector3 newCameraPosition = new Vector3(
                destinationPoint.position.x,
                destinationPoint.position.y,
                -1
            );
            mainCamera.transform.position = newCameraPosition;
        }
        
        if (singleUseOnly)
        {
            hasBeenUsed = true;
            isUnlocked = false;
        }
        else
        {
            isUnlocked = false;
        }
        
        UpdateVisuals();
        
        yield return new WaitForSeconds(0.1f);
        EnsureFadeCanvasGroup();
        
        if (fadeCanvasGroup != null)
        {
            // fade back
            float startTime = Time.time;
            float endTime = startTime + fadeDuration;
            
            while (Time.time < endTime)
            {
                float elapsed = Time.time - startTime;
                float normalizedTime = elapsed / fadeDuration;
                fadeCanvasGroup.alpha = 1f - normalizedTime;
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        isTransitioning = false;
    }
}