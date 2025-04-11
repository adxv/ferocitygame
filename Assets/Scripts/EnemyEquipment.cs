using UnityEngine;
using System;

public class EnemyEquipment : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("SpriteRenderer component on the enemy to update.")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [Tooltip("WeaponData representing the default weapon (can be fists).")]
    [SerializeField] private WeaponData defaultWeaponData;
    [Tooltip("WeaponData representing the enemy's fists (assign if different from default or if default can change).")]
    [SerializeField] private WeaponData fistWeaponData; // Assign enemy fist WeaponData here

    // Public event that fires when the weapon changes
    public event Action OnWeaponChanged;

    public WeaponData CurrentWeapon { get; private set; }
    public WeaponData DefaultWeaponData => defaultWeaponData;
    public WeaponData FistWeaponData => fistWeaponData; // Public getter for enemy fist data

    void Awake()
    {
        if (enemySpriteRenderer == null)
        {
            enemySpriteRenderer = GetComponent<SpriteRenderer>();
            if (enemySpriteRenderer == null)
            {
                Debug.LogError("Enemy Sprite Renderer not found on EnemyEquipment!", this);
                enabled = false;
                return;
            }
        }
        
        if (fistWeaponData == null)
        {
            Debug.LogWarning("Enemy Fist Weapon Data not assigned in EnemyEquipment! Melee attacks might not work correctly.", this);
            // Optionally, try to use defaultWeaponData if it's melee?
            if(defaultWeaponData != null && defaultWeaponData.isMelee) fistWeaponData = defaultWeaponData;
        }

        if (defaultWeaponData == null)
        {
            Debug.LogWarning("Default Weapon Data not assigned in EnemyEquipment. Equipping fists if available.", this);
            if (fistWeaponData != null)
            {
                 EquipWeapon(fistWeaponData); // Equip fists if no default weapon assigned
            }
            else
            {
                Debug.LogError("Neither Default nor Fist Weapon Data assigned! Enemy cannot attack.", this);
                enabled = false; // Disable if no weapons are possible
            }
        }
        else
        {
            // Equip default weapon initially
            EquipWeapon(defaultWeaponData);
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) 
        {
             Debug.LogError("Attempted to equip a null weapon.", this);
             return;
        }
        // Optional: Check if already equipped to prevent unnecessary updates
        if (newWeapon == CurrentWeapon) return;

        // Create a new instance of the weapon data to prevent sharing state
        WeaponData weaponInstance = Instantiate(newWeapon);

        // Initialize ammo to full for this instance (important even for melee for consistency)
        weaponInstance.currentAmmo = weaponInstance.magazineSize;

        CurrentWeapon = weaponInstance;

        // Update Enemy Sprite using the new central method
        UpdateSpriteToCurrentWeapon();

        // Notify listeners that the weapon has changed
        OnWeaponChanged?.Invoke();
    }

    // New method to directly set the sprite (used for attack animations)
    public void SetSprite(Sprite newSprite)
    {
        if (enemySpriteRenderer != null && newSprite != null)
        {
            enemySpriteRenderer.sprite = newSprite;
        }
        else if (enemySpriteRenderer == null)
        {
            Debug.LogError("Enemy Sprite Renderer is null in EnemyEquipment!", this);
        }
    }

    // New method to update the sprite based on the *currently equipped* weapon
    public void UpdateSpriteToCurrentWeapon()
    {
        if (enemySpriteRenderer == null) 
        {
            Debug.LogError("Enemy Sprite Renderer is null in EnemyEquipment! Cannot update sprite.", this);
            return;
        }

        Sprite spriteToSet = null;
        if (CurrentWeapon != null && CurrentWeapon.playerSprite != null) // Use playerSprite for consistency
        {
            spriteToSet = CurrentWeapon.playerSprite;
        }
        else if (defaultWeaponData != null && defaultWeaponData.playerSprite != null)
        {
            // Fallback to default weapon sprite if current is invalid
            spriteToSet = defaultWeaponData.playerSprite;
            Debug.LogWarning("Current weapon or its sprite was null. Defaulting sprite to default weapon.", this);
            // If current weapon became null, re-equip default
            if(CurrentWeapon == null) EquipWeapon(defaultWeaponData);
        }
        else if (fistWeaponData != null && fistWeaponData.playerSprite != null)
        {
            // Further fallback to fist sprite if default is also invalid
            spriteToSet = fistWeaponData.playerSprite;
            Debug.LogWarning("Current and default weapon/sprites were null. Defaulting sprite to fists.", this);
            // If current weapon became null, re-equip fists
             if(CurrentWeapon == null) EquipWeapon(fistWeaponData);
        }

        if (spriteToSet != null)
        {
            enemySpriteRenderer.sprite = spriteToSet;
        }
        else
        {
            Debug.LogError("Cannot update sprite: Current, Default, and Fist weapon data/sprites are all invalid!", this);
        }
    }
    
    // Alternative method that allows setting weapon data from a pickup
    public void EquipWeaponFromPickup(WeaponPickup pickup)
    {
        if (pickup != null && pickup.weaponData != null)
        {
            EquipWeapon(pickup.weaponData);
        }
    }
} 