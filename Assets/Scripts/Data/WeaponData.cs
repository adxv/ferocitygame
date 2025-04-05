using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "New Weapon";
    public Sprite playerSprite; // Sprite to show on the player when equipped
    public GameObject pickupPrefab; // Prefab representing this weapon when dropped/on the ground
    public bool canShoot = true; // Add this to easily identify non-shooting weapons like fists
    public bool isMelee = false; // Whether this is a melee weapon

    [Header("Shooting")]
    public GameObject projectilePrefab; // Optional: if the weapon shoots projectiles
    public float fireRate = 1f;         // Shots per second
    public bool isFullAuto = false;
    public float damage = 10f;
    public float bulletOffset = 1.0f;   // Forward offset for bullet spawn
    public float bulletOffsetSide = 0f; // Sideways offset for bullet spawn
    
    [Header("Ammunition")]
    public int magazineSize = 10;       // Maximum rounds in magazine
    [System.NonSerialized]
    public int currentAmmo;             // Current ammunition (runtime only, not serialized)
    
    [Header("Shotgun Properties")]
    public int pelletCount = 1;         // Number of pellets to fire (1 = normal gun, >1 = shotgun)
    public float spreadAngle = 0f;      // Angle of spread in degrees (0 = no spread)

    [Header("Effects")]
    public AudioClip shootSound;
    public float shootShakeDuration = 0.05f;
    public float shootShakeMagnitude = 0.05f;

    // Add other weapon-specific properties as needed:
    // public float reloadTime;
    // public GameObject muzzleFlash;
    // etc.
    
    // Called when ScriptableObject instance is loaded
    private void OnEnable()
    {
        // Initialize current ammo to full magazine when created
        // This is for newly created weapons, not picked up ones
        currentAmmo = magazineSize;
    }
    
    // Returns true if weapon has ammo and can shoot
    public bool HasAmmo()
    {
        return currentAmmo > 0;
    }
    
    // Decreases ammo count, returns true if successful
    public bool UseAmmo()
    {
        if (currentAmmo <= 0) return false;
        
        currentAmmo--;
        return true;
    }
}
