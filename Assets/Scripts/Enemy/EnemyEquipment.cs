using UnityEngine;
using System;

public class EnemyEquipment : MonoBehaviour
{
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private WeaponData defaultWeaponData;
    [SerializeField] private WeaponData fistWeaponData;

    public event Action OnWeaponChanged; // fires when the weapon changes

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
                Debug.LogError("enemy sprite renderer not assigned");
                enabled = false;
                return;
            }
        }
        
        if (fistWeaponData == null)
        {
            Debug.LogWarning("enemy fist weapon data not assigned");
            if(defaultWeaponData != null && defaultWeaponData.isMelee) fistWeaponData = defaultWeaponData; // use default if it's melee
        }

        if (defaultWeaponData == null)
        {
            Debug.LogWarning("default weapon data not assigned");
            if (fistWeaponData != null)
            {
                 EquipWeapon(fistWeaponData); // equip fists
            }
            else
            {
                Debug.LogError("no weapon data assigned", this);
                enabled = false;
            }
        }
        else
        {
            EquipWeapon(defaultWeaponData);
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) 
        {
             Debug.LogError("attempted to equip a null weapon.", this);
             return;
        }
        if (newWeapon == CurrentWeapon) return;

        // create a new instance to prevent sharing state
        WeaponData weaponInstance = Instantiate(newWeapon);

        // init ammo
        weaponInstance.currentAmmo = weaponInstance.magazineSize;

        CurrentWeapon = weaponInstance;

        UpdateSpriteToCurrentWeapon();

        // notify: weapon has changed
        OnWeaponChanged?.Invoke();
    }

    // might be redundant or unused idk
    public void SetSprite(Sprite newSprite)
    {
        if (enemySpriteRenderer != null && newSprite != null)
        {
            enemySpriteRenderer.sprite = newSprite;
        }
        else if (enemySpriteRenderer == null)
        {
            Debug.LogError("enemy sprite renderer is null", this);
        }
    }

    public void UpdateSpriteToCurrentWeapon()
    {
        if (enemySpriteRenderer == null) 
        {
            Debug.LogError("enemy sprite renderer is null", this);
            return;
        }

        Sprite spriteToSet = null;
        if (CurrentWeapon != null && CurrentWeapon.playerSprite != null)
        {
            spriteToSet = CurrentWeapon.playerSprite;
        }
        else if (defaultWeaponData != null && defaultWeaponData.playerSprite != null)
        {
            // fallback to default if current is invalid
            spriteToSet = defaultWeaponData.playerSprite;
            Debug.LogWarning("current weapon or its sprite was null", this);
            // equip default
            if(CurrentWeapon == null) EquipWeapon(defaultWeaponData);
        }
        else if (fistWeaponData != null && fistWeaponData.playerSprite != null)
        {
            // fallback to fist sprite if default is also invalid
            spriteToSet = fistWeaponData.playerSprite;
            Debug.LogWarning("defaulting sprite to fists.", this);
            // equip fists
             if(CurrentWeapon == null) EquipWeapon(fistWeaponData);
        }

        if (spriteToSet != null)
        {
            enemySpriteRenderer.sprite = spriteToSet;
        }
        else
        {
            Debug.LogError("cannot update sprite: current, default, and fist weapon are invalid", this);
        }
    }
    
    public void EquipWeaponFromPickup(WeaponPickup pickup)
    {
        if (pickup != null && pickup.weaponData != null)
        {
            EquipWeapon(pickup.weaponData);
        }
    }
} 