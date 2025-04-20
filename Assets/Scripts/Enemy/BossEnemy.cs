using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Enemy))]
public class BossEnemy : MonoBehaviour
{
    public float dashSpeed = 100f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 4f;
    public float dashCooldownMin = 2f;
    public float dashCooldownMax = 4f;
    public float dashTelegraphTime = 0.5f; // display before dash
    public Color dashTelegraphColor = Color.red;
    public AudioClip dashSound;
    
    // private variables
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
    
    // store reference to the weapon that hit
    private WeaponData lastHitWeapon;

    private void Awake()
    {
        baseEnemy = GetComponent<Enemy>(); // get enemy component
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        enemyEquipment = GetComponent<EnemyEquipment>();

        // set original color for after dash
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        currentCooldown = Random.Range(dashCooldownMin, dashCooldownMax); // randomize cooldown
        lastDashTime = -currentCooldown; // immediate first dash after cooldown
    }

    private void Update()
    {
        if (baseEnemy.isDead || player == null)
            return;
        
        PlayerController playerController = player.GetComponent<PlayerController>(); // check if player is alive
        if (playerController != null && playerController.IsDead())
            return;

        if (Time.time >= lastDashTime + currentCooldown && !isDashing && CanSeePlayer()) // check if can dash
        {
            StartCoroutine(PerformDashAttack());
        }
    }

    // custom TakeDamage for immunity to non-melee weapons
    public void TakeDamage(float amount, WeaponData weapon = null)
    {
        if (weapon != null && !weapon.isMelee)
        {
            GameObject immuneEffect = Instantiate(Resources.Load<GameObject>("Particles/Immune"), //play immune particle
                transform.position, Quaternion.identity);
            
            return;
        }
        
        // if not immune, take damage
        baseEnemy.TakeDamage(amount);
    }

    public void HandleThrownWeapon(WeaponData thrownWeapon = null)
    {
        // for non-melee thrown weapons, play immune particle
        if (thrownWeapon != null && !thrownWeapon.isMelee)
        {
            GameObject immuneEffect = Instantiate(Resources.Load<GameObject>("Particles/Immune"),
                transform.position, Quaternion.identity);
        }
        else
        {
            // if melee, take 1 damage
            baseEnemy.TakeDamage(1);
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;
            
        // check if player in range and visible
        Vector2 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // only dash if player is in correct range
        if (distanceToPlayer < 5f || distanceToPlayer > 20f)
            return false;

        // check if there are walls between boss and player
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, 
            distanceToPlayer, baseEnemy.wallLayer);
        
        return hit.collider == null; // no walls hit
    }

    private IEnumerator PerformDashAttack()
    {
        isDashing = true;
        
        // store original rigidbody settings
        RigidbodyType2D originalBodyType = rb.bodyType;
        bool originalFreezeRotation = rb.freezeRotation;
        RigidbodyConstraints2D originalConstraints = rb.constraints;
        
        // telegraph
        if (spriteRenderer != null)
        {
            spriteRenderer.color = dashTelegraphColor;
        }
        
        // play charge sound
        if (audioSource != null && dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }
        
        // wait telegraph time
        yield return new WaitForSeconds(dashTelegraphTime);
        
        // player is still alive and visible
        if (player != null && !baseEnemy.isDead)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null && !playerController.IsDead())
            {
                Vector2 dashDirection = (player.position - transform.position).normalized;
                
                // set the sprite to face the dash direction
                transform.up = dashDirection;
                
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.freezeRotation = true;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 0f;
                
                // perform dash
                float dashStartTime = Time.time;
                Vector3 startPosition = transform.position;
                Vector3 targetPosition = transform.position + (Vector3)(dashDirection * dashSpeed * dashDuration);
                
                while (Time.time < dashStartTime + dashDuration && !baseEnemy.isDead)
                {
                    float t = (Time.time - dashStartTime) / dashDuration;
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                    yield return null;
                }
                // stop
                rb.linearVelocity = Vector2.zero;
            }
        }
        
        // reset
        rb.bodyType = originalBodyType;
        rb.freezeRotation = originalFreezeRotation;
        rb.constraints = originalConstraints;
        
        // reset color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        isDashing = false;
        lastDashTime = Time.time;

        // set new cooldown
        currentCooldown = Random.Range(dashCooldownMin, dashCooldownMax);
    }
    
    private void FixedUpdate()
    {
        // if dashing, nothing else can modify velocity
        if (isDashing && player != null && !baseEnemy.isDead)
        {
            Vector2 dashDirection = (player.position - transform.position).normalized;
            rb.linearVelocity = dashDirection * dashSpeed;
        }
    }
}
