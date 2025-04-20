using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundDetectionField : MonoBehaviour
{
    private CircleCollider2D soundCollider;
    private PlayerEquipment playerEquipment;
    private bool weaponFired = false;
    
    public LayerMask wallLayer;

    private float autoWeaponNotificationDelay = 1.0f; //min time between notif
    private Dictionary<IncomingSoundDetector, float> enemyNotificationTimes = new Dictionary<IncomingSoundDetector, float>();

    void Start()
    {
        soundCollider = GetComponent<CircleCollider2D>();
        playerEquipment = GetComponentInParent<PlayerEquipment>();        
    }

    public void WeaponFired(bool isSilent)
    {
        if (!isSilent)
        {
            weaponFired = true;
            StartCoroutine(ResetWeaponFiredFlag());
        }
    }
    
    private IEnumerator ResetWeaponFiredFlag()
    {
        yield return new WaitForSeconds(0.5f); //
        weaponFired = false;
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (!weaponFired) return;
        
        // check if hit an enemy sound detector
        IncomingSoundDetector soundDetector = other.GetComponent<IncomingSoundDetector>();
        if (soundDetector != null)
        {
            if (enemyNotificationTimes.TryGetValue(soundDetector, out float lastTime)) //returns false if not found
            {
                if (Time.time - lastTime < autoWeaponNotificationDelay)
                {
                    return;
                }
            }
            else
            {
                // if the enemy was notified recently, don't notify again
                if (Time.time - lastTime < autoWeaponNotificationDelay)
                {
                    return;
                }
            }
            
            Vector2 startPoint = transform.position;
            Vector2 endPoint = soundDetector.transform.position;

            RaycastHit2D hit = Physics2D.Linecast(startPoint, endPoint, wallLayer);

            if (hit.collider == null)
            {
                soundDetector.DetectSound();
                enemyNotificationTimes[soundDetector] = Time.time;
            }
        }
    }

    void Update()
    {
        //clean old notifications
        List<IncomingSoundDetector> keysToRemove = new List<IncomingSoundDetector>();
        
        foreach (var entry in enemyNotificationTimes)
        {
            if (Time.time - entry.Value > autoWeaponNotificationDelay * 2)
            {
                keysToRemove.Add(entry.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            enemyNotificationTimes.Remove(key);
        }
    }
}
