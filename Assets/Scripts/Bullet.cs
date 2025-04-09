using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    public float speed = 30f;
    public float lifeDuration = 2f;
    private Rigidbody2D rb;
    private GameObject shooter; // track who fired the bullet
    private Vector2 travelDirection; // Track the bullet's travel direction
    
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Debug.Log("Bullet hit " + collision.gameObject.name + " fired by " + (shooter != null ? shooter.name : "Unknown"));
        if (collision.gameObject == shooter) return; // Don't collide with the entity that shot it

        // Check if the shooter is the player before recording a hit
        bool isPlayerShooter = shooter != null && shooter.CompareTag("Player");
        bool isEnemyShooter = shooter != null && shooter.CompareTag("Enemy");

        switch (collision.gameObject.tag)
        {
            case "Enemy":
                // Prevent enemies from killing each other
                if (isEnemyShooter)
                {
                    // If an enemy shot this bullet and hit another enemy, just destroy the bullet without damage
                    Destroy(gameObject);
                    return;
                }
                
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead && !isOutOfRange)
                {
                    // Record hit only if player shot a LIVE enemy and bullet is in range
                    if (isPlayerShooter && ScoreManager.Instance != null)
                    {
                        // For shotgun pellets, only record one hit per shotgun blast
                        if (!isShotgunPellet || !hasRecordedHit)
                        {
                            ScoreManager.Instance.RecordHit();
                            // Notify that this pellet hit an enemy
                            OnEnemyHit?.Invoke();
                        }
                    }
                    enemy.Die(travelDirection); // Pass bullet direction to Die method
                }
                // Regardless of hit, destroy the bullet
                Destroy(gameObject);
                break;
            case "Player":
                // Apply damage/effect to the player only if bullet is in range
                if (!isOutOfRange)
                {
                    PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                    if (player != null) { player.TakeDamage(1, travelDirection); } // Pass bullet direction
                }
                Destroy(gameObject); // Destroy bullet after hitting player
                break;
            case "Environment":
                 // Optionally record a miss if the player shot it (handled by accuracy calculation)
                 Destroy(gameObject); // Destroy bullet after hitting environment
                break;
            default:
                 // Destroy bullet if it hits anything else unaccounted for
                Destroy(gameObject);
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
