using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI ammoText;
    [SerializeField] public Image weaponIconImage;
    [SerializeField] public PlayerController playerController;
    private PlayerEquipment playerEquipment;
    
    void Start()
    {
        // Find player controller if not set
        if (playerController == null)
        {
            playerController = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault();
            if (playerController == null) 
            {
                Debug.LogError("AmmoDisplay: Could not find PlayerController on Start!", this);
                // Optional: Try again later or disable?
                // enabled = false;
                // return;
            }
        }
        
        // Get player equipment component
        if (playerController != null)
        {
            playerEquipment = playerController.GetComponent<PlayerEquipment>();
            
            // Subscribe to weapon change events
            if (playerEquipment != null)
            {
                // Unsubscribe first to prevent double-subscription on scene reload if object persists
                playerEquipment.OnWeaponChanged -= OnWeaponChanged; 
                playerEquipment.OnWeaponChanged += OnWeaponChanged;
                
                 // ADDED: Call UpdateAmmoDisplay again *after* subscribing
                 // This ensures the display reflects the state *after* setup.
                 UpdateAmmoDisplay(); 
            }
            else
            {
                Debug.LogError("AmmoDisplay: PlayerController found, but missing PlayerEquipment!", this);
            }
        }
        
        // Ensure we have a text component
        if (ammoText == null)
        {
            ammoText = GetComponent<TextMeshProUGUI>();
            
            if (ammoText == null)
            {
                Debug.LogError("AmmoDisplay: No TextMeshProUGUI component found!", this);
                enabled = false;
                return;
            }
        }
        
        // Update display immediately at start
        UpdateAmmoDisplay();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from event when destroyed to prevent memory leaks
        if (playerEquipment != null)
        {
            playerEquipment.OnWeaponChanged -= OnWeaponChanged;
        }
    }
    
    // Called when the weapon changes
    private void OnWeaponChanged()
    {
        UpdateAmmoDisplay();
    }
    
    // Update the ammo display based on current weapon
    private void UpdateAmmoDisplay()
    {
        if (playerEquipment != null && playerEquipment.CurrentWeapon != null)
        {
            WeaponData weapon = playerEquipment.CurrentWeapon;
            
            // Only show ammo count for weapons that:
            // 1. Actually shoot
            // 2. Have magazines
            // 3. Are NOT melee weapons
            if (weapon.canShoot && weapon.magazineSize > 0 && !weapon.isMelee)
            {
                ammoText.text = weapon.currentAmmo.ToString();
                ammoText.gameObject.SetActive(true);
                // Update weapon icon if available
                if (weaponIconImage != null && weapon.weaponIcon != null)
                {
                    weaponIconImage.sprite = weapon.weaponIcon;
                    weaponIconImage.gameObject.SetActive(true);
                }
                else if (weaponIconImage != null) // Ensure icon is hidden if no sprite
                {
                    weaponIconImage.gameObject.SetActive(false);
                }

                Debug.Log($"AmmoDisplay: Showing ammo for {weapon.weaponName}: {weapon.currentAmmo}");
            }
            else
            {
                // Hide ammo display for fists, non-shooting weapons, or melee weapons
                ammoText.gameObject.SetActive(false);
                if (weaponIconImage != null)
                {
                    weaponIconImage.gameObject.SetActive(false);
                }

                Debug.Log($"AmmoDisplay: Hiding display for {weapon.weaponName} (canShoot={weapon.canShoot}, magazineSize={weapon.magazineSize}, isMelee={weapon.isMelee})");
            }
        }
        else
        {
            ammoText.gameObject.SetActive(false);
            Debug.Log("AmmoDisplay: No weapon equipped, hiding display");
        }
    }
    
    // ADDED: Method to reset the display (called on restart)
    public void ResetDisplay()
    {
        // Assuming fists are the default and don't show ammo or icon
        if (ammoText != null) 
        {
            ammoText.gameObject.SetActive(false);
        }
        if (weaponIconImage != null)
        {
            weaponIconImage.gameObject.SetActive(false);
            weaponIconImage.sprite = null; // Clear the sprite just in case
        }
        Debug.Log("AmmoDisplay: Resetting display");
    }

    void Update()
    {
        // Keep the update method to handle ongoing changes in ammo count
        if (playerEquipment != null && playerEquipment.CurrentWeapon != null)
        {
            WeaponData weapon = playerEquipment.CurrentWeapon;
            
            if (weapon.canShoot && weapon.magazineSize > 0 && !weapon.isMelee && ammoText.gameObject.activeSelf)
            {
                int currentAmmo = weapon.currentAmmo;

                // Just update text, don't change visibility (that's done in UpdateAmmoDisplay)
                ammoText.text = currentAmmo.ToString();
            }
        }
    }
}