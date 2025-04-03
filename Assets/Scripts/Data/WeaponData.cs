using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "New Weapon";
    public Sprite playerSprite; // Sprite to show on the player when equipped
    public GameObject pickupPrefab; // Prefab representing this weapon when dropped/on the ground
    public bool canShoot = true; // Add this to easily identify non-shooting weapons like fists

    [Header("Shooting")]
    public GameObject projectilePrefab; // Optional: if the weapon shoots projectiles
    public float fireRate = 1f;         // Shots per second
    public float damage = 10f;
    public float bulletOffset = 1.0f;   // Forward offset for bullet spawn
    public float bulletOffsetSide = 0f; // Sideways offset for bullet spawn

    [Header("Effects")]
    public AudioClip shootSound;
    public float shootShakeDuration = 0.05f;
    public float shootShakeMagnitude = 0.05f;

    // Add other weapon-specific properties as needed:
    // public int maxAmmo;
    // public float reloadTime;
    // public GameObject muzzleFlash;
    // etc.
}
