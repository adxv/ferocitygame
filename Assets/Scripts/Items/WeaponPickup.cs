using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WeaponPickup : MonoBehaviour
{
    public WeaponData weaponData;
    
    private int currentAmmo = -1;
    private SpriteRenderer spriteRenderer;
    
    // reference TriggerZone component
    private WeaponPickupTrigger triggerComponent;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        Transform triggerZone = transform.Find("TriggerZone");
        if (triggerZone != null)
        {
            triggerComponent = triggerZone.GetComponent<WeaponPickupTrigger>();
            if (triggerComponent == null)
            {
                triggerComponent = triggerZone.gameObject.AddComponent<WeaponPickupTrigger>(); //add if not found
            }
        }
        
        if (weaponData != null && weaponData.playerSprite != null)
        {
             gameObject.name = weaponData.weaponName + " Pickup"; //set GameObject name
             
             // initialize ammo to full
             if (currentAmmo < 0)
             {
                 currentAmmo = weaponData.magazineSize;
             }
        }
        else
        {
            Debug.LogWarning($"WeaponData or playerSprite is not assigned for {gameObject.name}", this);
        }
    }
    
    // get current ammo count for this pickup
    public int GetCurrentAmmo()
    {
        // return the stored ammo state or default to magazine size
        return currentAmmo >= 0 ? currentAmmo : (weaponData != null ? weaponData.magazineSize : 0);
    }
    
    // set current ammo count for this pickup
    public void SetCurrentAmmo(int ammo)
    {
        currentAmmo = ammo;
    }
}
