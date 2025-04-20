using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    public float speed = 30f;
    public float lifeDuration = 2f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bulletCollider;
    private GameObject shooter;
    private Vector2 travelDirection;
    private bool hasHitSomething = false;
    
    private float damage = 1f;
    
    private WeaponData weaponData; // reference to the weapon that fired this bullet
    
    private float maxRange = 50f;
    private bool isOutOfRange = false;
    private Vector3 startPosition; // to track distance traveled
    
    
    public bool isShotgunPellet = false;
    public bool hasRecordedHit = false;
    
    // tracer
    public bool useTracerEffect = true;
    public float tracerWidth = 0.1f;
    public float tracerFadeTime = 0.5f;
    public Color tracerColor = Color.yellow;
    [Range(0f, 0.5f)]
    public float colorVariation = 0.1f;
    
    private LineRenderer lineRenderer;
    private float tracerTimer;
    
    // event that fires when this bullet hits an enemy
    public event Action OnEnemyHit;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletCollider = GetComponent<Collider2D>();
        
        rb.linearVelocity = transform.up * speed;
        travelDirection = transform.up; // store travel direction
        Destroy(gameObject, lifeDuration);
        
        // store start position for range calculation
        startPosition = transform.position;
        
        if (useTracerEffect)
        {
            if (!TryGetComponent(out lineRenderer))
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>(); //add linerenderer, idk doesnt work without this even though it's added in the inspector
            }
            
            Color randomizedColor = RandomizeColor(tracerColor, colorVariation);
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = tracerWidth;
            lineRenderer.endWidth = tracerWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = randomizedColor;
            lineRenderer.endColor = randomizedColor;
            
            // set pos
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, startPosition);
            
            tracerTimer = tracerFadeTime;
        }
    }

    // set params from weapon data
    public void SetBulletParameters(float speed, float range)
    {
        this.speed = speed;
        this.maxRange = range;
        
        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }
    }
    public void SetDamage(float damage)
    {
        this.damage = damage;
    }

    private Color RandomizeColor(Color baseColor, float variation)
    {
        float r = Mathf.Clamp01(baseColor.r + UnityEngine.Random.Range(-variation, variation));
        float g = Mathf.Clamp01(baseColor.g + UnityEngine.Random.Range(-variation, variation));
        float b = Mathf.Clamp01(baseColor.b + UnityEngine.Random.Range(-variation, variation));
        
        return new Color(r, g, b, baseColor.a);
    }

    // set shooter
    public void SetShooter(GameObject shooterObj)
    {
        shooter = shooterObj;
    }
    
    // get travel direction
    public Vector2 GetTravelDirection()
    {
        return travelDirection;
    }

    // disable after hit
    private void DisableBullet()
    {
        if (hasHitSomething) return;
        
        hasHitSomething = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    // set weapon that fired this bullet
    public void SetWeaponData(WeaponData weapon)
    {
        weaponData = weapon;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHitSomething) return;
        
        // dont collide with shooter
        if (collision.gameObject == shooter) return;

        
        bool isPlayerShooter = shooter != null && shooter.CompareTag("Player");
        bool isEnemyShooter = shooter != null && shooter.CompareTag("Enemy");

        switch (collision.gameObject.tag)
        {
            case "Enemy":
                // prevent enemies from killing each other
                if (isEnemyShooter)
                {
                    DisableBullet();
                    return;
                }
                
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead && !isOutOfRange)
                {
                    BossEnemy bossEnemy = enemy.GetComponent<BossEnemy>(); // check if boss enemy
                    if (bossEnemy != null)
                    {
                        bossEnemy.TakeDamage(damage, weaponData);
                    }
                    else
                    {
                        GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"),
                            collision.contacts[0].point, Quaternion.LookRotation(Vector3.forward, travelDirection));
                        enemy.TakeDamage(damage);
                    }
                    
                    if (isPlayerShooter && ScoreManager.Instance != null)
                    {
                        // shotgun pellets, only record one hit per shotgun blast
                        if (!isShotgunPellet || !hasRecordedHit)
                        {
                            ScoreManager.Instance.RecordHit();
                            OnEnemyHit?.Invoke();
                            hasRecordedHit = true;
                        }
                    }
                    
                    // pass the bullet direction so they die in the right direction
                    if (enemy.isDead)
                    {
                        enemy.Die(travelDirection);
                    }
                }
                DisableBullet();
                break;
            case "Player":
                // apply damage/effect to the player only if bullet is in range
                if (!isOutOfRange)
                {
                    GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"),
                        collision.contacts[0].point, Quaternion.LookRotation(Vector3.forward, travelDirection));
                        
                    PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                    if (player != null) { player.TakeDamage(1, travelDirection); } // pass bullet direction
                }
                DisableBullet();
                break;
            case "Environment":
                DisableBullet();
                break;
            default:
                DisableBullet();
                break;
        }
    }
    
    void Update()
    {
        // if bullet has exceeded its range
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        isOutOfRange = distanceTraveled > maxRange;
        
        // update tracer
        if (useTracerEffect && lineRenderer != null)
        {
            lineRenderer.SetPosition(1, transform.position);
            
            // fade out the tracer
            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0)
            {
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
