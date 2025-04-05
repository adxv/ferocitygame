using UnityEngine;
using System.Collections.Generic;
using System;
// using System.Linq; // No longer needed

public class PlayerEquipment : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("SpriteRenderer component on the player to update.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [Tooltip("WeaponData representing the 'unarmed' state.")]
    [SerializeField] private WeaponData fistWeaponData; // Assign your 'Fists' WeaponData here

    // Public event that fires when the weapon changes
    public event Action OnWeaponChanged;

    // REMOVED List<WeaponData> availableWeapons

    // private int currentWeaponIndex = -1; // REMOVED index tracking
    public WeaponData CurrentWeapon { get; private set; } // Public property to get current weapon info
    public WeaponData FistWeaponData => fistWeaponData; // Public getter for fist data

    void Awake()
    {
        if (playerSpriteRenderer == null)
        {
            Debug.LogError("Player Sprite Renderer not assigned in PlayerEquipment!", this);
            enabled = false;
            return;
        }
        if (fistWeaponData == null)
        {
            Debug.LogError("Fist Weapon Data not assigned in PlayerEquipment!", this);
            enabled = false;
            return;
        }

        // Equip fists initially
        EquipWeapon(fistWeaponData); 
    }

    // REMOVED Update method (weapon switching logic)

    // Changed to accept WeaponData directly
    public void EquipWeapon(WeaponData newWeapon)
    {
        // Optional: Check if already equipped to prevent unnecessary updates
        if (newWeapon == CurrentWeapon) return; 

        CurrentWeapon = newWeapon;

        // Update Player Sprite
        if (CurrentWeapon != null && CurrentWeapon.playerSprite != null)
        {
            playerSpriteRenderer.sprite = CurrentWeapon.playerSprite;
            Debug.Log($"Equipped: {CurrentWeapon.weaponName}");
        }
        else if (fistWeaponData != null && fistWeaponData.playerSprite != null) // Ensure fallback exists
        {
            // Fallback if something is wrong (e.g., null weapon passed, or its sprite is null)
            CurrentWeapon = fistWeaponData; // Equip fists as a safe default
            playerSpriteRenderer.sprite = fistWeaponData.playerSprite; 
            Debug.LogWarning($"Equipped weapon or its sprite was null. Defaulting to fists.", this);
        }
        else
        {
             Debug.LogError("Cannot equip weapon: Both new weapon/sprite and fist data/sprite are invalid!", this);
        }

        // Notify listeners that the weapon has changed
        OnWeaponChanged?.Invoke();
    }

    // REMOVED SwitchToNext/PreviousWeapon
    // REMOVED AddWeapon / RemoveWeapon
}
