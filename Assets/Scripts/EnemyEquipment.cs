using UnityEngine;
using System;

public class EnemyEquipment : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("SpriteRenderer component on the enemy to update.")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [Tooltip("WeaponData representing the default weapon.")]
    [SerializeField] private WeaponData defaultWeaponData; 

    // Public event that fires when the weapon changes
    public event Action OnWeaponChanged;

    public WeaponData CurrentWeapon { get; private set; }
    public WeaponData DefaultWeaponData => defaultWeaponData;

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
        
        if (defaultWeaponData == null)
        {
            Debug.LogWarning("Default Weapon Data not assigned in EnemyEquipment. Enemy will be unarmed.", this);
        }
        else
        {
            // Equip default weapon initially
            EquipWeapon(defaultWeaponData);
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        // Optional: Check if already equipped to prevent unnecessary updates
        if (newWeapon == CurrentWeapon) return; 

        // Create a new instance of the weapon data to prevent sharing between enemies
        WeaponData weaponInstance = Instantiate(newWeapon);
        
        // Initialize ammo to full for this instance
        weaponInstance.currentAmmo = weaponInstance.magazineSize;
        
        CurrentWeapon = weaponInstance;

        // Update Enemy Sprite
        if (CurrentWeapon != null && CurrentWeapon.playerSprite != null)
        {
            enemySpriteRenderer.sprite = CurrentWeapon.playerSprite;
        }
        else if (defaultWeaponData != null && defaultWeaponData.playerSprite != null) 
        {
            // Fallback if something is wrong
            CurrentWeapon = Instantiate(defaultWeaponData);
            CurrentWeapon.currentAmmo = CurrentWeapon.magazineSize;
            enemySpriteRenderer.sprite = defaultWeaponData.playerSprite; 
            Debug.LogWarning($"Equipped weapon or its sprite was null. Defaulting to {defaultWeaponData.weaponName}.", this);
        }

        // Notify listeners that the weapon has changed
        OnWeaponChanged?.Invoke();
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