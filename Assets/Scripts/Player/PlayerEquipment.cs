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

        // Update Player Sprite using the new central method
        UpdateSpriteToCurrentWeapon();

        // Notify listeners that the weapon has changed
        OnWeaponChanged?.Invoke();
    }

    // New method to directly set the sprite (used for attack animations)
    public void SetSprite(Sprite newSprite)
    {
        if (playerSpriteRenderer != null && newSprite != null)
        {
            playerSpriteRenderer.sprite = newSprite;
        }
        else if (playerSpriteRenderer == null)
        {
             Debug.LogError("Player Sprite Renderer is null in PlayerEquipment!", this);
        }
        // Don't log error if newSprite is null, might be intentional
    }

    // New method to update the sprite based on the *currently equipped* weapon
    public void UpdateSpriteToCurrentWeapon()
    {
        if (playerSpriteRenderer == null) 
        {
            Debug.LogError("Player Sprite Renderer is null in PlayerEquipment! Cannot update sprite.", this);
            return;
        }

        if (CurrentWeapon != null && CurrentWeapon.playerSprite != null)
        {
            playerSpriteRenderer.sprite = CurrentWeapon.playerSprite;
        }
        else if (fistWeaponData != null && fistWeaponData.playerSprite != null) // Ensure fallback exists
        {
            // Fallback if CurrentWeapon is null or its sprite is null
            if(CurrentWeapon == null) EquipWeapon(fistWeaponData); // Re-equip fists if current weapon became null
            playerSpriteRenderer.sprite = fistWeaponData.playerSprite;
            Debug.LogWarning("Current weapon or its sprite was null. Defaulting sprite to fists.", this);
        }
        else
        {
             Debug.LogError("Cannot update sprite: Both current weapon/sprite and fist data/sprite are invalid!", this);
        }
    }

    // REMOVED SwitchToNext/PreviousWeapon
    // REMOVED AddWeapon / RemoveWeapon
}
