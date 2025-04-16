using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "New Weapon";
    public Sprite playerSprite; // Sprite to show on the player when equipped (default state)
    public Sprite weaponIcon; // Sprite for UI display
    public GameObject pickupPrefab; // Prefab representing this weapon when dropped/on the ground
    public bool canShoot = true; // Set to false for fists if they use a different input/logic later
    public bool isMelee = false; // IMPORTANT: Set this to true for fists
    public bool isSilent = false; // Whether this weapon makes noise when fired

    [Header("Shooting (Guns)")]
    public GameObject projectilePrefab; // Optional: if the weapon shoots projectiles
    public float fireRate = 1f;         // Shots per second OR Attacks per second for melee
    public bool isFullAuto = false;     // Applies to melee rapid attacks too?
    public float damage = 10f;
    public float bulletOffset = 1.0f;   // Forward offset for bullet spawn
    public float bulletOffsetSide = 0f; // Sideways offset for bullet spawn
    public float range = 50f;           // Range for projectiles OR attack reach for melee
    public float bulletSpeed = 200f;    // Speed of bullets fired by this weapon
    [Tooltip("The higher the value, the more inaccurate shots will be")]
    public float spread = 0f;           // General inaccuracy in degrees (0 = perfectly accurate)

    [Header("Ammunition")]
    public int magazineSize = 10;       // Irrelevant for fists? Set to 1 or 0?
    [System.NonSerialized]
    public int currentAmmo;             // Current ammunition (runtime only, not serialized)

    [Header("Shotgun Properties")]
    public int pelletCount = 1;         // Number of pellets to fire (1 = normal gun, >1 = shotgun)
    public float spreadAngle = 0f;      // Angle of spread in degrees (0 = no spread)

    [Header("Effects")]
    public AudioClip shootSound;        // Sound for shooting a gun
    public AudioClip emptyClickSound;   // Sound when trying to shoot an empty gun
    public float shootShakeDuration = 0.05f;
    public float shootShakeMagnitude = 0.05f;
    public GameObject muzzleFlashPrefab; // Particle effect prefab for muzzle flash
    public float muzzleFlashDuration = 0.05f; // How long the muzzle flash should last

    [Header("Melee Specifics")]
    public AudioClip hitSound;          // Sound when melee attack hits
    public AudioClip missSound;         // Sound when melee attack misses (swing/whoosh)
    public Sprite attackSprite;        // Sprite to show during the melee attack animation
    public Sprite attackSprite2;       // Alternative sprite to show during melee attack
    public bool useAlternatingSprites = false; // Whether to randomly alternate between attack sprites
    public float attackDuration = 0.2f; // Duration of the attack animation/sprite change
    // Add other weapon-specific properties as needed:
    // public float reloadTime;

    // Called when ScriptableObject instance is loaded
    private void OnEnable()
    {
        // Initialize current ammo to full magazine when created
        // For melee weapons, this might always be considered full or irrelevant
        currentAmmo = magazineSize;
    }

    // Returns true if weapon has ammo and can shoot (or attack for melee)
    public bool HasAmmo()
    {
        // Melee weapons effectively always have "ammo"
        if (isMelee) return true;
        return currentAmmo > 0;
    }

    // Decreases ammo count, returns true if successful
    public bool UseAmmo()
    {
        // Melee weapons don't consume ammo
        if (isMelee) return true;

        if (currentAmmo <= 0) return false;

        currentAmmo--;
        return true;
    }
}
