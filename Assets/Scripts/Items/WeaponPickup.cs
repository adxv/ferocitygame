using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    public WeaponData weaponData; // Assign the specific WeaponData asset in the Inspector

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Optional: Set the pickup's sprite based on the WeaponData's player sprite
        // (You might want a different sprite for the pickup item itself)
        if (weaponData != null && weaponData.playerSprite != null)
        {
             // spriteRenderer.sprite = weaponData.playerSprite; // Uncomment if pickup uses same sprite
             gameObject.name = weaponData.weaponName + " Pickup"; // Set GameObject name for clarity
        }
        else
        {
            Debug.LogWarning($"WeaponData or its playerSprite is not assigned for {gameObject.name}", this);
        }
    }

    // Optional: You could add rotation or bobbing effects here in Update()
}
