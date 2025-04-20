using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "weapon";
    public Sprite playerSprite; // default, when equipped
    public Sprite weaponIcon; // unused, for ui display
    public GameObject pickupPrefab; // prefab to instantiate when the weapon is dropped
    public bool canShoot = true;
    public bool isMelee = false;
    public bool isSilent = false;

    public GameObject projectilePrefab; // bullet prefab
    public float fireRate = 1f;         // attack per second
    public bool isFullAuto = false;
    public float damage = 10f;
    public float bulletOffset = 1.0f;   // forward offset for bullet spawn
    public float bulletOffsetSide = 0f; // sideways offset
    public float range = 50f;           // bullet damage range or max melee distance
    public float bulletSpeed = 200f;
    public float spread = 0f;           // accuracy
    public int magazineSize = 10;



    [Header("shotgun only")]
    public int pelletCount = 1;         // > 1 for shotgun
    public float spreadAngle = 0f;      // spread angle

    [Header("effects")]
    public AudioClip shootSound;   
    public AudioClip emptyClickSound; // when empty
    public float shootShakeDuration = 0.05f;
    public float shootShakeMagnitude = 0.05f;
    public GameObject muzzleFlashPrefab; // particle
    public float muzzleFlashDuration = 0.05f;

    [Header("melee only")]
    public AudioClip hitSound;
    public AudioClip missSound;
    public Sprite attackSprite;
    public Sprite attackSprite2;
    public bool useAlternatingSprites = false; // alternate between attackSprite and attackSprite2
    public float attackDuration = 0.2f;
    

    [System.NonSerialized]
    public int currentAmmo;             // current ammo count, runtime only

    // called when ScriptableObject instance is created
    private void OnEnable()
    {
        currentAmmo = magazineSize;
    }

    public bool HasAmmo() // true if weapon has ammo, always true if melee
    {
        if (isMelee) return true;
        return currentAmmo > 0;
    }

    // decreases ammo count, true if successful
    public bool UseAmmo()
    {
        if (isMelee) return true;

        if (currentAmmo <= 0) return false;
        currentAmmo--;

        return true;
    }
}
