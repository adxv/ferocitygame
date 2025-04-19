using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    public float speed = 30f;
    public float lifeDuration = 2f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bulletCollider;
    private GameObject shooter; // track who fired the bullet
    private Vector2 travelDirection; // Track the bullet's travel direction
    private bool hasHitSomething = false; // Track if bullet has already hit something
    
    // Damage property
    private float damage = 1f; // Default damage value
    
    // Weapon reference
    private WeaponData weaponData; // Reference to the weapon that fired this bullet
    
    // Range properties
    private float maxRange = 50f; // Maximum effective range
    private bool isOutOfRange = false; // Flag to track if bullet has exceeded its max range
    private Vector3 startPosition; // Starting position to track distance traveled
    
    // Shotgun pellet properties
    [HideInInspector] public bool isShotgunPellet = false;
    [HideInInspector] public bool hasRecordedHit = false;
    
    // Tracer effect properties
    [Header("Tracer Effect")]
    public bool useTracerEffect = true;
    public float tracerWidth = 0.1f;
    public float tracerFadeTime = 0.5f;
    public Color tracerColor = Color.yellow;
    [Range(0f, 0.5f)]
    public float colorVariation = 0.1f; // How much the color can vary
    
    private LineRenderer lineRenderer;
    private float tracerTimer;
    
    // Event that fires when this bullet hits an enemy
    public event Action OnEnemyHit;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletCollider = GetComponent<Collider2D>();
        
        rb.linearVelocity = transform.up * speed;
        travelDirection = transform.up; // Store the travel direction
        Destroy(gameObject, lifeDuration);
        
        // Store start position for range calculation
        startPosition = transform.position;
        
        // Set up tracer effect
        if (useTracerEffect)
        {
            // Add LineRenderer if it doesn't exist
            if (!TryGetComponent(out lineRenderer))
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            
            // Randomize the color slightly
            Color randomizedColor = RandomizeColor(tracerColor, colorVariation);
            
            // Configure LineRenderer
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = tracerWidth;
            lineRenderer.endWidth = tracerWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = randomizedColor;
            lineRenderer.endColor = randomizedColor;
            
            // Set start position for line renderer
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, startPosition);
            
            tracerTimer = tracerFadeTime;
        }
    }

    // Set the bullet's speed and range from weapon data
    public void SetBulletParameters(float speed, float range)
    {
        this.speed = speed;
        this.maxRange = range;
        
        // Update velocity with new speed
        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }
    }
    
    // Set the bullet's damage from the weapon data
    public void SetDamage(float damage)
    {
        this.damage = damage;
    }

    // Randomize color with small variations
    private Color RandomizeColor(Color baseColor, float variation)
    {
        // Add random variation to each RGB component
        float r = Mathf.Clamp01(baseColor.r + UnityEngine.Random.Range(-variation, variation));
        float g = Mathf.Clamp01(baseColor.g + UnityEngine.Random.Range(-variation, variation));
        float b = Mathf.Clamp01(baseColor.b + UnityEngine.Random.Range(-variation, variation));
        
        // Keep the same alpha
        return new Color(r, g, b, baseColor.a);
    }

    // Public method to set the shooter when spawned
    public void SetShooter(GameObject shooterObj)
    {
        shooter = shooterObj;
    }
    
    // Public method to get the bullet's travel direction
    public Vector2 GetTravelDirection()
    {
        return travelDirection;
    }

    // Disable bullet visuals and physics after hit
    private void DisableBullet()
    {
        if (hasHitSomething) return; // Already hit something
        
        hasHitSomething = true;
        
        // Disable the sprite renderer
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Disable the collider
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }
        
        // Stop moving
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Keep the LineRenderer active for the tracer effect
    }

    // Method to set the weapon that fired this bullet
    public void SetWeaponData(WeaponData weapon)
    {
        weaponData = weapon;
    }

    // Modified OnCollisionEnter2D to pass weapon data
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Don't process collision if already hit something
        if (hasHitSomething) return;
        
        // Don't collide with the entity that shot it
        if (collision.gameObject == shooter) return;

        // Check if the shooter is the player before recording a hit
        bool isPlayerShooter = shooter != null && shooter.CompareTag("Player");
        bool isEnemyShooter = shooter != null && shooter.CompareTag("Enemy");

        switch (collision.gameObject.tag)
        {
            case "Enemy":
                // Prevent enemies from killing each other
                if (isEnemyShooter)
                {
                    // If an enemy shot this bullet and hit another enemy, just disable the bullet without damage
                    DisableBullet();
                    return;
                }
                
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead && !isOutOfRange)
                {
                    // Check for BossEnemy component first
                    BossEnemy bossEnemy = enemy.GetComponent<BossEnemy>();
                    if (bossEnemy != null)
                    {
                        // Use the specialized TakeDamage method for boss that accepts weapon data
                        bossEnemy.TakeDamage(damage, weaponData);
                        
                        // Don't add blood effect here as the boss implementation will handle it
                        // based on whether it's immune or not
                    }
                    else
                    {
                        // Regular enemy - show blood and apply damage normally
                        // Instantiate blood particle effect at collision point with proper rotation
                        GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"),
                            collision.contacts[0].point, Quaternion.LookRotation(Vector3.forward, travelDirection));
                        
                        // Apply damage to the regular enemy
                        enemy.TakeDamage(damage);
                    }
                    
                    // Record hit only if player shot a LIVE enemy and bullet is in range
                    if (isPlayerShooter && ScoreManager.Instance != null)
                    {
                        // For shotgun pellets, only record one hit per shotgun blast
                        if (!isShotgunPellet || !hasRecordedHit)
                        {
                            ScoreManager.Instance.RecordHit();
                            // Notify that this pellet hit an enemy
                            OnEnemyHit?.Invoke();
                            
                            // Mark that a hit has been recorded for this shotgun blast
                            hasRecordedHit = true;
                        }
                    }
                    
                    // If enemy dies from this damage, pass the bullet direction so they die in the right direction
                    if (enemy.isDead)
                    {
                        enemy.Die(travelDirection);
                    }
                }
                // Disable the bullet after hitting an enemy
                DisableBullet();
                break;
            case "Player":
                // Apply damage/effect to the player only if bullet is in range
                if (!isOutOfRange)
                {
                    // Instantiate blood particle effect at collision point with proper rotation
                    GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"),
                        collision.contacts[0].point, Quaternion.LookRotation(Vector3.forward, travelDirection));
                        
                    PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                    if (player != null) { player.TakeDamage(1, travelDirection); } // Pass bullet direction
                }
                // Disable the bullet after hitting the player
                DisableBullet();
                break;
            case "Environment":
                // Optionally record a miss if the player shot it (handled by accuracy calculation)
                // Disable the bullet after hitting the environment
                DisableBullet();
                break;
            default:
                // Disable the bullet if it hits anything else unaccounted for
                DisableBullet();
                break;
        }
    }
    
    void Update()
    {
        // Check if bullet has exceeded its range
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        isOutOfRange = distanceTraveled > maxRange;
        
        // Update tracer effect
        if (useTracerEffect && lineRenderer != null)
        {
            // Update the end position of the line to follow the bullet
            lineRenderer.SetPosition(1, transform.position);
            
            // Fade out the tracer
            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0)
            {
                // Gradually fade out the color
                Color currentColor = lineRenderer.startColor;
                float alpha = currentColor.a - (Time.deltaTime / tracerFadeTime);
                alpha = Mathf.Clamp01(alpha);
                
                Color fadeColor = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
                lineRenderer.startColor = fadeColor;
                lineRenderer.endColor = fadeColor;
            }
        }
    }
}
