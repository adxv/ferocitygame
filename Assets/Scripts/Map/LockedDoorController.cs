using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LockedDoorController : MonoBehaviour
{
    public float maxOpenAngle = 90f;
    public float pushForce = 50f;
    public float doorMass = 1f;
    public float doorDamping = 3f;
    public float unlockForce = 15f;
    public float unlockDelay = 1f;
    
    public bool openDirection = true;
    
    public AudioSource audioSource;
    public AudioClip unlockSound;
    public AudioClip lockedSound;
    public float minTimeBetweenSounds = 1f;
    
    public FloorManager currentFloor;
    public bool autoDetectFloor = true;
    
    private float currentAngularVelocity = 0f;
    private Quaternion initialRotation;
    private float currentRelativeAngle = 0f;
    private bool isDoorUnlocked = false;
    private float lastSoundTime = -1f;
    
    private void Start()
    {
        initialRotation = transform.rotation;
        currentRelativeAngle = 0f; 
        
        audioSource = GetComponent<AudioSource>();
        
        isDoorUnlocked = false;
    }

    private void Update()
    {
        if (!isDoorUnlocked && currentFloor != null)
        {
            CheckIfAllEnemiesDeadOnCurrentFloor();
        }
        
        // apply force
        if (Mathf.Abs(currentAngularVelocity) > 0.01f)
        {
            currentRelativeAngle += currentAngularVelocity * Time.deltaTime;
            
            // max angle
            currentRelativeAngle = Mathf.Clamp(currentRelativeAngle, -maxOpenAngle, maxOpenAngle);
             
            // rotate door
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, currentRelativeAngle);
            
            // damping
            currentAngularVelocity *= (1f - Time.deltaTime * doorDamping);
        }
    }
    
    private void CheckIfAllEnemiesDeadOnCurrentFloor()
    {
        if (currentFloor.AreAllEnemiesDead())
        {
            isDoorUnlocked = true;
            
            //delay
            StartCoroutine(DelayedDoorOpen());
        }
    }
    
    private IEnumerator DelayedDoorOpen()
    {
        yield return new WaitForSeconds(unlockDelay);
        PlaySound(unlockSound);

        ApplyUnlockForce(); //unlock
    }
    
    private void ApplyUnlockForce()
    {
        float pushDirection = openDirection ? 1f : -1f;
        
        // apply force
        currentAngularVelocity = pushDirection * unlockForce / doorMass;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isDoorUnlocked)
            {
                ApplyImmediatePush(other);
            }
            else
            {
                PlaySound(lockedSound);
            }
        }
        else if (isDoorUnlocked && other.CompareTag("Enemy")) //enemies can push if the door is unlocked
        {
            ApplyImmediatePush(other);
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDoorUnlocked && (other.CompareTag("Player") || other.CompareTag("Enemy")))
        {
            ApplyImmediatePush(other);
        }
    }
    
    private void ApplyImmediatePush(Collider2D other)
    {
        if (!isDoorUnlocked) return;
        
        // get direction vector from door to pusher
        Vector2 toPusher = (other.transform.position - transform.position).normalized;
        
        // determine which side the pusher is on
        float dotProduct = Vector2.Dot(transform.right, toPusher);
        float pushDirection = dotProduct > 0 ? 1f : -1f;
        
        currentAngularVelocity = pushDirection * pushForce / doorMass;
    }
} 