using UnityEngine;
using System.Collections.Generic;
using System;
// using System.Linq;

public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private WeaponData fistWeaponData;

    public event Action OnWeaponChanged;

    public WeaponData CurrentWeapon { get; private set; } // get current weapon data
    public WeaponData FistWeaponData => fistWeaponData; // get fist data

    void Awake()
    {
        if (playerSpriteRenderer == null)
        {
            Debug.LogError("player sprite renderer not assigned in PlayerEquipment");
            enabled = false;
            return;
        }
        if (fistWeaponData == null)
        {
            Debug.LogError("fist not assigned in PlayerEquipment");
            enabled = false;
            return;
        }
        EquipWeapon(fistWeaponData); 
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        //check if already equipped
        if (newWeapon == CurrentWeapon) return; 

        CurrentWeapon = newWeapon;

        UpdateSpriteToCurrentWeapon();

        OnWeaponChanged?.Invoke(); //notify that weapon has changed
    }

    public void SetSprite(Sprite newSprite)
    {
        if (playerSpriteRenderer != null && newSprite != null)
        {
            playerSpriteRenderer.sprite = newSprite;
        }
        else if (playerSpriteRenderer == null)
        {
             Debug.LogError("player sprite renderer is null");
        }
    }

    public void UpdateSpriteToCurrentWeapon()
    {
        if (CurrentWeapon != null && CurrentWeapon.playerSprite != null)
        {
            playerSpriteRenderer.sprite = CurrentWeapon.playerSprite;
        }
        else if (fistWeaponData != null && fistWeaponData.playerSprite != null)
        {
            if(CurrentWeapon == null) EquipWeapon(fistWeaponData); //fallback
            
            playerSpriteRenderer.sprite = fistWeaponData.playerSprite;
            Debug.LogWarning("weapon or its sprite was null");
        }
    }

    public void Shoot()
    {
        if (CurrentWeapon != null && !CurrentWeapon.isSilent)
        {
            var soundField = GetComponentInChildren<SoundDetectionField>(); // sound detection
            if (soundField != null)
            {
                soundField.WeaponFired(CurrentWeapon.isSilent);
            }
        }
    }
}
