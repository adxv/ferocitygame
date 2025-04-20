using UnityEngine;
using System.Collections;

public class EntranceDoubleDoorController : MonoBehaviour
{
    public GameObject leftDoor;
    public GameObject rightDoor;
    
    public float closingDelay = 0.5f;
    public float openingDelay = 0.5f;
    public float closingForce = 0.05f;
    public float openingForce = 0.05f;
    public float forceSensitivity = 0.1f;
    public bool openDirectionLeft = false;  // direction to open
    public bool openDirectionRight = true;
    
    public AudioSource audioSource;
    public AudioClip closingSound;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    public float minTimeBetweenSounds = 1f;
    
    public FloorManager currentFloor;
    
    private DoorController leftDoorController;
    private DoorController rightDoorController;
    
    private Collider2D leftDoorCollider;
    private Collider2D rightDoorCollider;
    
    // softlock prevention
    private GameObject softLockPreventor;
    private bool playerInPreventionZone = false;
    private Coroutine closingCoroutine = null;
    
    private bool isLocked = false;
    private bool playerHasPassed = false;
    private float lastSoundTime = -1f;
    
    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        if (currentFloor == null)
        {
            Debug.LogWarning("EntranceDoubleDoorController: no floor assigned");
        }
        
        // get door controllers and colliders
        if (leftDoor != null)
        {
            leftDoorController = leftDoor.GetComponent<DoorController>();
            leftDoorCollider = leftDoor.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogError("left door reference is missing");
        }
        
        if (rightDoor != null)
        {
            rightDoorController = rightDoor.GetComponent<DoorController>();
            rightDoorCollider = rightDoor.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogError("right door reference is missing");
        }
        // find soft lock preventor
        foreach (Transform child in transform)
        {
            if (child.gameObject.layer == LayerMask.NameToLayer("EntranceSoftLockPreventor"))
            {
                softLockPreventor = child.gameObject;
                SoftLockPreventorTrigger preventorScript = softLockPreventor.GetComponent<SoftLockPreventorTrigger>();
                if (preventorScript == null)
                {
                    preventorScript = softLockPreventor.AddComponent<SoftLockPreventorTrigger>();
                    preventorScript.Initialize(this);
                }
                break;
            }
        }
    }
    
    private void Update()
    {
        // if the doors are locked, check if all enemies are dead in the entire level
        // should have done this with a setter in floormanager
        if (isLocked)
        {
            CheckForLevelCompletion();
        }
    }
    
    private void CheckForLevelCompletion()
    {
        if (AreAllEnemiesDeadInLevel())
        {
            isLocked = false;
            
            StartCoroutine(DelayedDoorsOpen());
            
            UpdateDoorColliders(false);
            
            Debug.Log("unlocking main door");
        }
    }
    
    private bool AreAllEnemiesDeadInLevel() //tehres a better way to do this
    {
        // find all floor managers
        FloorManager[] floorManagers = FindObjectsByType<FloorManager>(FindObjectsSortMode.None);
        
        if (floorManagers.Length == 0)
        {
            Debug.LogWarning("no FloorManagers.");
            return false;
        }
        
        // check every floor
        foreach (FloorManager floor in floorManagers)
        {
            // if any floor has living enemies, return false
            if (!floor.AreAllEnemiesDead())
            {
                return false;
            }
        }
        
        //all dead:
        return true;
    }
    
    private IEnumerator DelayedDoorsOpen()
    {
        // wait
        yield return new WaitForSeconds(openingDelay);
        
        PlaySound(unlockSound);
        
        if (leftDoor != null)
        {
            ApplyForceToDoor(leftDoor, openDirectionLeft ? 1f : -1f);
        }
        
        if (rightDoor != null)
        {
            ApplyForceToDoor(rightDoor, openDirectionRight ? 1f : -1f);
        }
    }
    
        
    
    private void CloseDoors()
    {
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
            float currentAngle = controller.currentRelativeAngle;

            // determine closing direction
            // if angle is positive, we need negative force and vice versa
            float closingDirection = currentAngle > 0 ? -1f : 1f;

            // if the door is already near-closed, reset
            if (Mathf.Abs(currentAngle) < 1.0f)
            {
                controller.currentRelativeAngle = 0f;
                controller.currentAngularVelocity = 0f;

                door.transform.rotation = controller.initialRotation;
            }
            else
            {
                // apply a force proportional to the angle
                float angleRatio = Mathf.Abs(currentAngle) / 90f;
                float forceMagnitude = closingForce * angleRatio * forceSensitivity;

                forceMagnitude = Mathf.Max(forceMagnitude, closingForce * 0.1f * forceSensitivity);

                float angularVelocity = closingDirection * forceMagnitude / controller.doorMass;
                controller.currentAngularVelocity = angularVelocity;
            }
        }
    }
    
    private void ApplyForceToDoor(GameObject door, float direction)
    {
        // temporary pusher object
        GameObject tempPusher = new GameObject("TempPusher");
        tempPusher.tag = "Player";
        
        tempPusher.transform.position = door.transform.position + (door.transform.right * direction * 2f);
        
        BoxCollider2D collider = tempPusher.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        DoorController controller = door.GetComponent<DoorController>();
        if (controller != null)
        {
            float originalPushForce = controller.pushForce;
            
            float adjustedPushForce = openingForce * forceSensitivity;
            controller.pushForce = adjustedPushForce;
            
            controller.HandleTriggerEvent(collider);
            
            controller.pushForce = originalPushForce;
        }
        Destroy(tempPusher, 0.1f);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !playerHasPassed && !isLocked)
        {
            playerHasPassed = true;
            
            // only close if the player is not in the prevention zone
            if (softLockPreventor == null || !playerInPreventionZone)
            {
                closingCoroutine = StartCoroutine(DelayedDoorsClose()); //delay
            }
        }
        else if (other.CompareTag("Player") && isLocked) //does not work, fix
        {
            PlaySound(lockedSound);
        }
    }
    
    public void PlayerEnteredPreventionZone()
    {
        playerInPreventionZone = true;
        
        if (closingCoroutine != null)
        {
            StopCoroutine(closingCoroutine);
            closingCoroutine = null;
            Debug.Log("door closing canceled");
        }
    }
    
    public void PlayerExitedPreventionZone()
    {
        playerInPreventionZone = false;
        
        if (playerHasPassed && !isLocked && gameObject.activeInHierarchy)
        {
            closingCoroutine = StartCoroutine(DelayedDoorsClose());
            Debug.Log("closing");
        }
    }
    
    private IEnumerator DelayedDoorsClose()
    {
        yield return new WaitForSeconds(closingDelay);
        
        // check again in case player entered prevention zone during delay
        if (playerInPreventionZone)
        {
            closingCoroutine = null;
            yield break;
        }
        
        PlaySound(closingSound);
        
        CloseDoors();
        isLocked = true;
        
        UpdateDoorColliders(true);
        
        closingCoroutine = null;
        Debug.Log("main door closed");
    }
    
    private void UpdateDoorColliders(bool lockDoors)
    {
        // toggles collider to non trigger to block access
        if (lockDoors)
        {
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
            if (leftDoorCollider != null)
            {
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
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
            lastSoundTime = Time.time;
        }
    }
    
    // getter for isLocked
    public bool IsLocked()
    {
        return isLocked;
    }
}

// helper class
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
