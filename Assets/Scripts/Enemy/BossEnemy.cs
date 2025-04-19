using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Enemy))]
public class BossEnemy : MonoBehaviour
{
    [Header("Dash Attack Settings")]
    public float dashSpeed = 100f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 4f;
    public float dashCooldownMin = 2f;
    public float dashCooldownMax = 4f;
    public float dashTelegraphTime = 0.5f;
    public Color dashTelegraphColor = Color.red;
    public AudioClip dashSound;
    
    // Private variables
    private Enemy baseEnemy;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Transform player;
    private bool isDashing = false;
    private float lastDashTime;
    private float currentCooldown;
    private AudioSource audioSource;
    private EnemyEquipment enemyEquipment;
    
    // Store reference to the weapon that hit the boss
    private WeaponData lastHitWeapon;

    private void Awake()
    {
        baseEnemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        enemyEquipment = GetComponent<EnemyEquipment>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Initialize last dash time and cooldown
        currentCooldown = Random.Range(dashCooldownMin, dashCooldownMax);
        lastDashTime = -currentCooldown; // Allow immediate first dash after cooldown
    }

    private void Update()
    {
        if (baseEnemy.isDead || player == null)
            return;
        
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsDead())
            return;

        // Check if dash is off cooldown
        if (Time.time >= lastDashTime + currentCooldown && !isDashing && CanSeePlayer())
        {
            StartCoroutine(PerformDashAttack());
        }
    }

    // Custom TakeDamage method to handle immunity to non-melee weapons
    public void TakeDamage(float amount, WeaponData weapon = null)
    {
        // If we're hit by a weapon and it's not melee, we're immune
        if (weapon != null && !weapon.isMelee)
        {
            // Play immune effect instead of taking damage
            GameObject immuneEffect = Instantiate(Resources.Load<GameObject>("Particles/Immune"),
                transform.position, Quaternion.identity);
                
            // Optional: play an immune sound if you have one
            if (audioSource != null)
            {
                // audioSource.PlayOneShot(immuneSound); // Uncomment if you have an immune sound
            }
            
            return; // Don't apply damage
        }
        
        // If we reach here, it's either a melee weapon or no weapon info was provided
        // In both cases, pass to the base enemy's TakeDamage method
        baseEnemy.TakeDamage(amount);
    }

    // Handle thrown weapons (for cases where the WeaponPickupTrigger doesn't pass weapon info)
    public void HandleThrownWeapon(WeaponData thrownWeapon = null)
    {
        // If a thrown weapon hits the boss, we can handle it here
        // For non-melee thrown weapons, show immune effect
        if (thrownWeapon != null && !thrownWeapon.isMelee)
        {
            // Play immune effect
            GameObject immuneEffect = Instantiate(Resources.Load<GameObject>("Particles/Immune"),
                transform.position, Quaternion.identity);
                
            // Optional: play an immune sound if you have one
            if (audioSource != null)
            {
                // audioSource.PlayOneShot(immuneSound); // Uncomment if you have an immune sound
            }
        }
        else
        {
            // For melee thrown weapons or unspecified weapons, take 1 damage
            baseEnemy.TakeDamage(1);
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;
            
        // Check if player is in range and visible
        Vector2 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // Only dash if player is within a reasonable range (5-20 units)
        if (distanceToPlayer < 5f || distanceToPlayer > 20f)
            return false;
            
        // Cast a ray to check if there are walls between boss and player
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, 
            distanceToPlayer, baseEnemy.wallLayer);
            
        // Can see player if no walls in the way
        return hit.collider == null;
    }

    private IEnumerator PerformDashAttack()
    {
        isDashing = true;
        
        // Store original rigidbody settings
        RigidbodyType2D originalBodyType = rb.bodyType;
        bool originalFreezeRotation = rb.freezeRotation;
        RigidbodyConstraints2D originalConstraints = rb.constraints;
        
        // Telegraph the dash
        if (spriteRenderer != null)
        {
            spriteRenderer.color = dashTelegraphColor;
        }
        
        // Play charge sound
        if (audioSource != null && dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }
        
        // Wait telegraph time
        yield return new WaitForSeconds(dashTelegraphTime);
        
        // Make sure player is still alive and visible
        if (player != null && !baseEnemy.isDead)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null && !playerController.IsDead())
            {
                // Calculate dash direction
                Vector2 dashDirection = (player.position - transform.position).normalized;
                
                // Set the sprite to face the dash direction
                transform.up = dashDirection;
                
                // Make kinematic and freeze rotation to ensure consistent dash
                rb.bodyType = RigidbodyType2D.Dynamic; // Changed to Dynamic for better physics
                rb.freezeRotation = true;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 0f; // Ensure gravity doesn't affect the dash
                
                // Perform the dash - directly move the transform for more reliability
                float dashStartTime = Time.time;
                Vector3 startPosition = transform.position;
                Vector3 targetPosition = transform.position + (Vector3)(dashDirection * dashSpeed * dashDuration);
                
                while (Time.time < dashStartTime + dashDuration && !baseEnemy.isDead)
                {
                    float t = (Time.time - dashStartTime) / dashDuration;
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                    yield return null;
                }
                
                // Stop movement
                rb.linearVelocity = Vector2.zero;
            }
        }
        
        // Reset rigidbody to original settings
        rb.bodyType = originalBodyType;
        rb.freezeRotation = originalFreezeRotation;
        rb.constraints = originalConstraints;
        
        // Reset color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        isDashing = false;
        lastDashTime = Time.time;
        // Set a new random cooldown for the next dash
        currentCooldown = Random.Range(dashCooldownMin, dashCooldownMax);
    }
    
    // Add this to prevent any other scripts from changing velocity during dash
    private void FixedUpdate()
    {
        // If currently dashing, ensure nothing else is modifying our velocity
        if (isDashing && player != null && !baseEnemy.isDead)
        {
            Vector2 dashDirection = (player.position - transform.position).normalized;
            rb.linearVelocity = dashDirection * dashSpeed;
        }
    }
}
