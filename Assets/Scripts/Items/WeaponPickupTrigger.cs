using UnityEngine;
using System.Collections;

public class WeaponPickupTrigger : MonoBehaviour
{
    private float throwTime = 0f;
    private bool isThrown = false;
    private const float weaponKnockbackWindow = 0.2f; // 200 milliseconds to knock out weapons

    void OnEnable()
    {
        // check if this is a newly thrown weapon
        WeaponPickup parentPickup = transform.parent.GetComponent<WeaponPickup>();
        if (parentPickup != null)
        {
            // mark weapon as thrown and record the time
            isThrown = true;
            throwTime = Time.time;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // chekc if still within the knockback window
        if (isThrown && Time.time - throwTime <= weaponKnockbackWindow)
        {
            // check if collided with an enemy
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Enemy enemy = collision.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    BossEnemy bossEnemy = enemy.GetComponent<BossEnemy>(); // check if boss
                    
                    // get the thrown weapon data
                    WeaponPickup parentPickup = transform.parent.GetComponent<WeaponPickup>();
                    WeaponData thrownWeaponData = null;
                    if (parentPickup != null)
                    {
                        thrownWeaponData = parentPickup.weaponData;
                    }
                    
                    // boss enemies
                    if (bossEnemy != null)
                    {
                        bossEnemy.HandleThrownWeapon(thrownWeaponData);
                        return;
                    }
                    
                    // default enemy
                    if (thrownWeaponData != null && thrownWeaponData.isMelee)
                    {
                        enemy.TakeDamage(1);
                        return;
                    }
                    
                    // for non-melee weapons on regular enemies
                    EnemyEquipment enemyEquipment = enemy.GetComponent<EnemyEquipment>();
                    if (enemyEquipment != null && 
                        enemyEquipment.CurrentWeapon != null && 
                        enemyEquipment.CurrentWeapon != enemyEquipment.FistWeaponData &&
                        enemyEquipment.CurrentWeapon.pickupPrefab != null)
                    {
                        // get weapon data before disarming
                        WeaponData enemyWeapon = enemyEquipment.CurrentWeapon;
                        
                        // instantiate weapon pickup
                        GameObject droppedWeapon = Instantiate(
                            enemyWeapon.pickupPrefab, 
                            enemy.transform.position, 
                            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                        );
                        
                        // apply forces
                        Rigidbody2D weaponRb = droppedWeapon.GetComponent<Rigidbody2D>();
                        if (weaponRb != null)
                        {
                            float randomAngle = Random.Range(0f, 360f);
                            Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                            float forceMagnitude = Random.Range(30.0f, 60.0f);
                            weaponRb.AddForce(randomDirection * forceMagnitude, ForceMode2D.Impulse);
                            weaponRb.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);
                        }
                        
                        // switch the enemy to fists
                        enemyEquipment.EquipWeapon(enemyEquipment.FistWeaponData);
                    }
                }
            }
        }
    }
}