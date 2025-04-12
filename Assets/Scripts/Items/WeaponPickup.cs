using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WeaponPickup : MonoBehaviour
{
    public WeaponData weaponData; // Assign the specific WeaponData asset in the Inspector
    
    // Variable to store current ammo state
    private int currentAmmo = -1;
    private SpriteRenderer spriteRenderer;
    
    // Reference to the TriggerZone's component
    private WeaponPickupTrigger triggerComponent;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Find the TriggerZone child and its component
        Transform triggerZone = transform.Find("TriggerZone");
        if (triggerZone != null)
        {
            triggerComponent = triggerZone.GetComponent<WeaponPickupTrigger>();
            if (triggerComponent == null)
            {
                // Add the component if it doesn't exist
                triggerComponent = triggerZone.gameObject.AddComponent<WeaponPickupTrigger>();
            }
        }
        
        // Optional: Set the pickup's sprite based on the WeaponData's player sprite
        // (You might want a different sprite for the pickup item itself)
        if (weaponData != null && weaponData.playerSprite != null)
        {
             // spriteRenderer.sprite = weaponData.playerSprite; // Uncomment if pickup uses same sprite
             gameObject.name = weaponData.weaponName + " Pickup"; // Set GameObject name for clarity
             
             // Initialize ammo to full if it hasn't been explicitly set
             if (currentAmmo < 0)
             {
                 currentAmmo = weaponData.magazineSize;
             }
        }
        else
        {
            Debug.LogWarning($"WeaponData or its playerSprite is not assigned for {gameObject.name}", this);
        }
    }
    
    // Get the current ammo count for this pickup
    public int GetCurrentAmmo()
    {
        // Return the stored ammo state or default to weapon data's magazine size if not set
        return currentAmmo >= 0 ? currentAmmo : (weaponData != null ? weaponData.magazineSize : 0);
    }
    
    // Set the current ammo count for this pickup
    public void SetCurrentAmmo(int ammo)
    {
        currentAmmo = ammo;
    }

    // Optional: You could add rotation or bobbing effects here in Update()
}
