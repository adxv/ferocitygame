using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundDetectionField : MonoBehaviour
{
    private CircleCollider2D soundCollider;
    private PlayerEquipment playerEquipment;
    private bool weaponFired = false;
    
    public LayerMask wallLayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        soundCollider = GetComponent<CircleCollider2D>();
        playerEquipment = GetComponentInParent<PlayerEquipment>();
        
        if (soundCollider == null)
        {
            Debug.LogError("SoundDetectionField requires a CircleCollider2D component");
        }
        
        if (playerEquipment == null)
        {
            Debug.LogError("SoundDetectionField requires a parent with PlayerEquipment component");
        }

        if (wallLayer == 0)
        {
             Debug.LogWarning("SoundDetectionField: Wall Layer is not assigned in the Inspector. Sound might travel through walls.", this);
        }
    }

    public void WeaponFired(bool isSilent)
    {
        if (!isSilent)
        {
            // Set flag that a noisy weapon was fired
            weaponFired = true;
            
            // Reset the flag after a short time
            StartCoroutine(ResetWeaponFiredFlag());
        }
    }
    
    private IEnumerator ResetWeaponFiredFlag()
    {
        yield return new WaitForSeconds(0.5f);
        weaponFired = false;
    }
    
    // This is called when another collider enters this trigger
    void OnTriggerStay2D(Collider2D other)
    {
        // Check if a weapon was fired and it wasn't silent
        if (!weaponFired) return;
        
        // Check if we hit an enemy sound detector
        IncomingSoundDetector soundDetector = other.GetComponent<IncomingSoundDetector>();
        if (soundDetector != null)
        {
            Vector2 startPoint = transform.position;
            Vector2 endPoint = soundDetector.transform.position;

            RaycastHit2D hit = Physics2D.Linecast(startPoint, endPoint, wallLayer);

            if (hit.collider == null)
            {
                soundDetector.DetectSound();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
