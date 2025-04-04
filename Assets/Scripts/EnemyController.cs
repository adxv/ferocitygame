using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Enemy : MonoBehaviour
{
    private Transform player;
    public GameObject bulletPrefab;
    public float shootInterval = 1f;
    public float bulletSpeed = 30f;
    public float bulletOffset = 0.5f;
    public float forgetTime = 3f;
    public LayerMask wallLayer;
    public float shotDelayMin = 0.19f;
    public float shotDelayMax = 0.23f;
    private float nextShootTime;
    private bool hasSpottedPlayer;
    private float lastSpottedTime;
    public AudioSource shootSound;

    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float randomModeInterval = 2.5f;
    public float rotationSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 patrolDirection;
    private float enemyRadius = 0.25f;
    private float randomModeTimer;
    private bool isMovingInRandomMode;
    public float safetyDistance = 0.5f;
    private Quaternion targetRotation;
    private float waitTimer;
    private bool isWaiting;

    private enum State { Patrol, Pursue, Random, Dead } // Added Dead state
    private State currentState = State.Patrol;

    // Death Sprite System
    public Sprite deathSprite; // Assign in Inspector
    public bool isDead = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Enemy could not find Player with tag 'Player'!");
        }

        nextShootTime = Time.time + shootInterval;
        hasSpottedPlayer = false;
        lastSpottedTime = -forgetTime;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Change to Dynamic for nudge if needed
        patrolDirection = transform.up;
        randomModeTimer = randomModeInterval;
        targetRotation = transform.rotation;
        waitTimer = 0f;
        isWaiting = false;

        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        if (player == null || isDead) return;

        PlayerController playerController = player.GetComponent<PlayerController>();
        bool playerIsDead = playerController != null && playerController.IsDead();

        if (!playerIsDead && hasSpottedPlayer)
        {
            if (!CanSeePlayer())
            {
                if (Time.time >= lastSpottedTime + forgetTime)
                {
                    currentState = State.Random;
                    if (currentState != State.Random)
                    {
                        float randomAngle = GetClearRandomAngle();
                        targetRotation = Quaternion.Euler(0, 0, randomAngle);
                        patrolDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                    }
                }
            }
            else
            {
                lastSpottedTime = Time.time;
                currentState = State.Pursue;
            }

            if (Time.time >= nextShootTime && CanSeePlayer())
            {
                Shoot();
                nextShootTime = Time.time + shootInterval + Random.Range(shotDelayMin, shotDelayMax);
            }
        }
        else
        {
            hasSpottedPlayer = false;
            if (currentState != State.Random)
            {
                currentState = State.Random;
                float randomAngle = GetClearRandomAngle();
                targetRotation = Quaternion.Euler(0, 0, randomAngle);
                patrolDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
            }
        }

        if (currentState == State.Random && isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
            }
        }

        if (currentState == State.Patrol || currentState == State.Random)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (player == null || isDead) return;

        if (currentState == State.Pursue)
        {
            Vector2 desiredDirection = (player.position - transform.position).normalized;
            Vector2 repulsion = CalculateRepulsion();
            Vector2 finalDirection = (desiredDirection + repulsion).normalized;
            Vector2 desiredPosition = (Vector2)transform.position + finalDirection * chaseSpeed * Time.fixedDeltaTime;
            if (!WouldCollide(desiredPosition))
            {
                rb.MovePosition(desiredPosition);
            }
            float angle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            patrolSpeed = chaseSpeed;
        }
        else if (currentState == State.Patrol)
        {
            Vector2 repulsion = CalculateRepulsion();
            Vector2 finalDirection = (patrolDirection + repulsion).normalized;
            Vector2 desiredPosition = (Vector2)transform.position + finalDirection * patrolSpeed * Time.fixedDeltaTime;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, patrolDirection, safetyDistance + enemyRadius + enemyRadius/2, wallLayer);
            if (hit.collider != null)
            {
                Vector2 newDirection = Quaternion.Euler(0, 0, -90) * patrolDirection;
                targetRotation = Quaternion.LookRotation(Vector3.forward, newDirection);
                patrolDirection = newDirection;
                desiredPosition = (Vector2)transform.position + patrolDirection * patrolSpeed * Time.fixedDeltaTime;
                if (!WouldCollide(desiredPosition))
                {
                    rb.MovePosition(desiredPosition);
                }
            }
            else if (!WouldCollide(desiredPosition))
            {
                rb.MovePosition(desiredPosition);
            }
        }
        else if (currentState == State.Random)
        {
            if (!isWaiting)
            {
                Vector2 repulsion = CalculateRepulsion();
                Vector2 finalDirection = (patrolDirection + repulsion).normalized;
                Vector2 desiredPosition = (Vector2)transform.position + finalDirection * patrolSpeed * Time.fixedDeltaTime;

                RaycastHit2D hit = Physics2D.Raycast(transform.position, patrolDirection, safetyDistance + enemyRadius, wallLayer);
                if (hit.collider != null)
                {
                    float randomAngle = GetClearRandomAngle();
                    targetRotation = Quaternion.Euler(0, 0, randomAngle);
                    patrolDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                    waitTimer = Random.Range(0f, 2f);
                    isWaiting = true;
                }
                else if (!WouldCollide(desiredPosition))
                {
                    rb.MovePosition(desiredPosition);
                }
            }
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("bulletPrefab not assigned in Enemy!");
            return;
        }

        Vector3 spawnPosition = transform.position + (transform.up * bulletOffset);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, transform.rotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = transform.up * bulletSpeed;
        }
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetShooter(gameObject);
        }
        shootSound.pitch = Random.Range(0.8f, 1.1f);
        shootSound.PlayOneShot(shootSound.clip);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && CanSeePlayer())
        {
            PlayerController playerController = collision.GetComponent<PlayerController>();
            if (playerController != null && !playerController.IsDead())
            {
                if (!hasSpottedPlayer)
                {
                    hasSpottedPlayer = true;
                    lastSpottedTime = Time.time;
                    nextShootTime = Time.time + Random.Range(shotDelayMin, shotDelayMax);
                }
                else if (Time.time - lastSpottedTime > Time.deltaTime)
                {
                    nextShootTime = Time.time + Random.Range(shotDelayMin, shotDelayMax);
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !CanSeePlayer())
        {
            if (Time.time >= lastSpottedTime + forgetTime)
            {
                currentState = State.Random;
                float randomAngle = GetClearRandomAngle();
                targetRotation = Quaternion.Euler(0, 0, randomAngle);
                patrolDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
            }
        }
    }

    bool CanSeePlayer()
    {
        if (player == null || isDead) return false;
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsDead()) return false;
        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, wallLayer);
        return hit.collider == null;
    }

    bool WouldCollide(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, enemyRadius, wallLayer) != null;
    }

    private Vector2 CalculateRepulsion()
    {
        Vector2 repulsion = Vector2.zero;
        int rayCount = 8;
        float angleStep = 360f / rayCount;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, safetyDistance, wallLayer);
            if (hit.collider != null)
            {
                float distance = hit.distance;
                if (distance < safetyDistance)
                {
                    Vector2 repulsionDirection = (Vector2)transform.position - hit.point;
                    float repulsionStrength = (safetyDistance - distance) / safetyDistance;
                    repulsion += repulsionDirection.normalized * repulsionStrength;
                }
            }
        }
        return repulsion;
    }

    private float GetClearRandomAngle()
    {
        int maxAttempts = 5;
        for (int i = 0; i < maxAttempts; i++)
        {
            float randomAngle = Random.Range(0f, 360f);
            Vector2 testDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, testDirection, safetyDistance * 4, wallLayer);
            if (hit.collider == null)
            {
                return randomAngle;
            }
        }
        return Random.Range(0f, 360f);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = State.Dead;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && deathSprite != null)
        {
            spriteRenderer.sprite = deathSprite;
            spriteRenderer.sortingOrder = 0;
        }
        transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);

        Vector2 nudgeDirection = (transform.position - player.position).normalized;
        if (nudgeDirection == Vector2.zero) nudgeDirection = Random.insideUnitCircle.normalized;
        rb.AddForce(nudgeDirection * 1f, ForceMode2D.Impulse);

        // Disable all colliders on this GameObject and its children
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(true); // true includes inactive colliders
        foreach (Collider2D col in allColliders)
        {
            col.enabled = false;
        }

        Invoke("StopAfterNudge", 0.1f);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RecordEnemyDefeated();
        }
    }

    void StopAfterNudge()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}