using UnityEngine;
using System.Collections;

public class WeaponPickupTrigger : MonoBehaviour
{
    private float throwTime = 0f;
    private bool isThrown = false;
    private const float weaponKnockbackWindow = 0.2f; // 300 milliseconds to knock out weapons

    void OnEnable()
    {
        // Check if this is a newly thrown weapon
        WeaponPickup parentPickup = transform.parent.GetComponent<WeaponPickup>();
        if (parentPickup != null)
        {
            // Mark weapon as thrown and record the time
            isThrown = true;
            throwTime = Time.time;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if this is a thrown weapon still within the knockback window
        if (isThrown && Time.time - throwTime <= weaponKnockbackWindow)
        {
            // Check if we collided with an enemy
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Enemy enemy = collision.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    // Get the thrown weapon data to check if it's melee
                    WeaponPickup parentPickup = transform.parent.GetComponent<WeaponPickup>();
                    if (parentPickup != null && parentPickup.weaponData != null)
                    {
                        // Check if the thrown weapon is a melee weapon
                        if (parentPickup.weaponData.isMelee)
                        {
                            // Kill the enemy directly instead of disarming
                            enemy.TakeDamage(1000); // Use high damage to ensure death
                            return; // Skip the rest of the method
                        }
                    }
                    
                    // For non-melee weapons, continue with the original disarming logic
                    EnemyEquipment enemyEquipment = enemy.GetComponent<EnemyEquipment>();
                    if (enemyEquipment != null && 
                        enemyEquipment.CurrentWeapon != null && 
                        enemyEquipment.CurrentWeapon != enemyEquipment.FistWeaponData &&
                        enemyEquipment.CurrentWeapon.pickupPrefab != null)
                    {
                        // Get the weapon data before disarming
                        WeaponData enemyWeapon = enemyEquipment.CurrentWeapon;
                        
                        // Instantiate the weapon pickup
                        GameObject droppedWeapon = Instantiate(
                            enemyWeapon.pickupPrefab, 
                            enemy.transform.position, 
                            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                        );
                        
                        // Apply the same forces as when enemy dies
                        Rigidbody2D weaponRb = droppedWeapon.GetComponent<Rigidbody2D>();
                        if (weaponRb != null)
                        {
                            float randomAngle = Random.Range(0f, 360f);
                            Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                            float forceMagnitude = Random.Range(30.0f, 60.0f);
                            weaponRb.AddForce(randomDirection * forceMagnitude, ForceMode2D.Impulse);
                            weaponRb.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);
                        }
                        
                        // Switch the enemy to fists
                        enemyEquipment.EquipWeapon(enemyEquipment.FistWeaponData);
                    }
                }
            }
        }
    }
}