using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

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
    public float turnPauseDuration = 0.7f; // Duration to pause after turning in patrol mode
    private Rigidbody2D rb;
    private Vector2 patrolDirection;
    private float enemyRadius = 0.25f;
    private float randomModeTimer;
    private bool isMovingInRandomMode;
    public float safetyDistance = 0.5f;
    private Quaternion targetRotation;
    private float waitTimer;
    private bool isWaiting;
    private bool isPausedAfterTurn = false; // Track if paused after turning in patrol mode
    private float turnPauseTimer = 0f; // Timer for pause after turning

    // A* Pathfinding variables
    private List<Vector2> path = new List<Vector2>();
    private int currentPathIndex;
    private float pathUpdateTime = 0.5f; // How often to recalculate path
    private float lastPathUpdateTime;
    private float nodeSize = 0.5f; // Size of A* grid nodes
    private float pathNodeReachedDistance = 0.1f;
    
    // Stuck detection variables
    private Vector2 lastPosition;
    private float stuckCheckTime = 0.5f;
    private float lastStuckCheckTime;
    private float stuckThreshold = 0.05f; // Distance threshold to consider "stuck"
    private int consecutiveStuckFrames = 0;
    private int stuckFrameThreshold = 3; // How many consecutive stuck checks before taking action

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
        
        // Initialize for A* pathfinding
        lastPathUpdateTime = -pathUpdateTime; // Force immediate path update when first chasing
        lastStuckCheckTime = Time.time;
        lastPosition = transform.position;
    }

    void Update()
    {
        if (player == null || isDead) return;

        PlayerController playerController = player.GetComponent<PlayerController>();
        bool playerIsDead = playerController != null && playerController.IsDead();

        // Stuck detection for chase mode
        if (currentState == State.Pursue && Time.time >= lastStuckCheckTime + stuckCheckTime)
        {
            float distanceMoved = Vector2.Distance(lastPosition, transform.position);
            if (distanceMoved < stuckThreshold && path.Count > 0)
            {
                consecutiveStuckFrames++;
                
                // If stuck for several consecutive checks, take corrective action
                if (consecutiveStuckFrames >= stuckFrameThreshold)
                {
                    // Force path recalculation with increased node size temporarily
                    float oldNodeSize = nodeSize;
                    nodeSize *= 1.5f; // Temporarily increase node size
                    CalculatePath();
                    nodeSize = oldNodeSize; // Restore original node size
                    
                    // As a fallback, if still stuck, try a random direction
                    if (consecutiveStuckFrames >= stuckFrameThreshold * 2)
                    {
                        // Clear path and generate a random direction to move
                        path.Clear();
                        float randomAngle = GetClearRandomAngle();
                        Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                        Vector2 randomTarget = (Vector2)transform.position + randomDirection * 3f;
                        path.Add(randomTarget);
                        currentPathIndex = 0;
                        consecutiveStuckFrames = 0; // Reset stuck counter after taking drastic action
                    }
                }
            }
            else
            {
                // Not stuck, reset counter
                consecutiveStuckFrames = 0;
            }
            
            lastPosition = transform.position;
            lastStuckCheckTime = Time.time;
        }

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
            // The enemy hasn't spotted the player yet
            hasSpottedPlayer = false;
            
            // Only change to Random mode if the enemy has ever spotted the player before
            // This keeps new enemies in their initial Patrol state
            if (currentState != State.Patrol && currentState != State.Random)
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

        // Apply smooth rotation for Patrol and Random states
        if (currentState == State.Patrol || currentState == State.Random)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Update path to player at regular intervals when in pursue mode
        if (currentState == State.Pursue && Time.time >= lastPathUpdateTime + pathUpdateTime)
        {
            CalculatePath();
            lastPathUpdateTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        if (player == null || isDead) return;

        if (currentState == State.Pursue)
        {
            // A* pathfinding movement
            Vector2 moveDirection = Vector2.zero;
            
            // If we have path nodes to follow
            if (path.Count > 0 && currentPathIndex < path.Count)
            {
                // Get next path point
                Vector2 targetPosition = path[currentPathIndex];
                
                // Calculate distance and direction to the next node
                float distanceToNode = Vector2.Distance(transform.position, targetPosition);
                Vector2 directionToNode = (targetPosition - (Vector2)transform.position).normalized;
                
                // Enhanced wall avoidance with dynamic repulsion strength
                Vector2 repulsion = CalculateRepulsion() * 0.8f; // Increased from 0.5f for better wall avoidance
                moveDirection = (directionToNode + repulsion).normalized;
                
                // Check if we reached the current path node
                if (distanceToNode <= pathNodeReachedDistance)
                {
                    currentPathIndex++;
                }
                
                // Add additional checks to skip unreachable nodes
                if (currentPathIndex < path.Count && !CanSeePoint(transform.position, path[currentPathIndex]) && path.Count > currentPathIndex + 1)
                {
                    // If we can see a future node directly, skip to it
                    for (int i = currentPathIndex + 1; i < path.Count; i++)
                    {
                        if (CanSeePoint(transform.position, path[i]))
                        {
                            currentPathIndex = i;
                            break;
                        }
                    }
                }
                
                // Slow down when approaching turns for more natural movement
                float speedMultiplier = 1.0f;
                if (currentPathIndex < path.Count - 1)
                {
                    Vector2 currentDirection = directionToNode;
                    Vector2 nextDirection = Vector2.zero;
                    
                    if (currentPathIndex + 1 < path.Count)
                    {
                        nextDirection = (path[currentPathIndex + 1] - path[currentPathIndex]).normalized;
                        float dot = Vector2.Dot(currentDirection, nextDirection);
                        
                        // Slow down more for sharper turns
                        if (dot < 0.7f) 
                        {
                            speedMultiplier = 0.6f + (dot * 0.4f);
                        }
                    }
                }
                
                // Calculate the desired position with speed modifier
                Vector2 desiredPosition = (Vector2)transform.position + moveDirection * chaseSpeed * speedMultiplier * Time.fixedDeltaTime;
                
                // Move if not colliding with a wall
                if (!WouldCollide(desiredPosition))
                {
                    rb.MovePosition(desiredPosition);
                }
                else
                {
                    // Try to find an alternative direction if stuck
                    for (int i = 15; i <= 75; i += 15)
                    {
                        // Try deflecting both left and right
                        Vector2 deflectedDirection = Quaternion.Euler(0, 0, i) * moveDirection;
                        Vector2 deflectedPosition = (Vector2)transform.position + deflectedDirection * chaseSpeed * 0.5f * Time.fixedDeltaTime;
                        
                        if (!WouldCollide(deflectedPosition))
                        {
                            rb.MovePosition(deflectedPosition);
                            break;
                        }
                        
                        deflectedDirection = Quaternion.Euler(0, 0, -i) * moveDirection;
                        deflectedPosition = (Vector2)transform.position + deflectedDirection * chaseSpeed * 0.5f * Time.fixedDeltaTime;
                        
                        if (!WouldCollide(deflectedPosition))
                        {
                            rb.MovePosition(deflectedPosition);
                            break;
                        }
                    }
                }
                
                // Smooth rotation towards movement direction
                if (moveDirection != Vector2.zero)
                {
                    float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
                    Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
                }
            }
            else
            {
                // Direct approach if no path or reached end of path
                Vector2 directDirection = (player.position - transform.position).normalized;
                Vector2 repulsion = CalculateRepulsion();
                Vector2 finalDirection = (directDirection + repulsion).normalized;
                Vector2 desiredPosition = (Vector2)transform.position + finalDirection * chaseSpeed * Time.fixedDeltaTime;
                
                if (!WouldCollide(desiredPosition))
                {
                    rb.MovePosition(desiredPosition);
                }
                
                // Smooth rotation
                float angle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg - 90f;
                Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else if (currentState == State.Patrol)
        {
            // Handle pause timer after turning
            if (isPausedAfterTurn)
            {
                turnPauseTimer -= Time.fixedDeltaTime;
                if (turnPauseTimer <= 0)
                {
                    isPausedAfterTurn = false;
                }
                return; // Skip movement while paused
            }
            
            // Updated patrol logic - move forward until hitting a wall, then turn RIGHT 90 degrees
            Vector2 repulsion = CalculateRepulsion();
            Vector2 finalDirection = (patrolDirection + repulsion * 0.3f).normalized; // Reduced repulsion influence
            Vector2 desiredPosition = (Vector2)transform.position + finalDirection * patrolSpeed * Time.fixedDeltaTime;

            // Check if there's a wall directly ahead
            RaycastHit2D hit = Physics2D.Raycast(transform.position, patrolDirection, safetyDistance + enemyRadius, wallLayer);
            if (hit.collider != null)
            {
                // Turn 90 degrees to the RIGHT (clockwise)
                Vector2 newDirection = Quaternion.Euler(0, 0, -90) * patrolDirection;
                targetRotation = Quaternion.LookRotation(Vector3.forward, newDirection);
                patrolDirection = newDirection;
                
                // Start pause after turning
                isPausedAfterTurn = true;
                turnPauseTimer = turnPauseDuration;
            }
            else if (!WouldCollide(desiredPosition))
            {
                // Continue moving forward
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
    
    // A* Pathfinding implementation
    private void CalculatePath()
    {
        if (player == null) return;
        
        path.Clear();
        currentPathIndex = 0;
        
        // Start and goal positions
        Vector2 startPos = transform.position;
        Vector2 goalPos = player.position;
        
        // If we can see the player directly, just set a direct path
        if (CanSeePlayer())
        {
            path.Add(goalPos);
            return;
        }
        
        // Define our grid bounds around the start and goal
        float gridRadius = Vector2.Distance(startPos, goalPos) * 1.5f; // Buffer around direct path
        gridRadius = Mathf.Max(gridRadius, 10f); // Minimum search radius
        
        // Create nodes grid (simplified implementation)
        Dictionary<Vector2Int, PathNode> nodes = new Dictionary<Vector2Int, PathNode>();
        
        // Convert world positions to grid coordinates
        Vector2Int startNode = WorldToGrid(startPos);
        Vector2Int goalNode = WorldToGrid(goalPos);
        
        // Open and closed sets for A*
        List<Vector2Int> openSet = new List<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        
        // Initialize start node
        nodes[startNode] = new PathNode { gCost = 0, hCost = HeuristicDistance(startNode, goalNode), parent = null };
        openSet.Add(startNode);
        
        // Main A* loop
        int iterations = 0;
        int maxIterations = 500; // Prevent infinite loops
        
        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            // Find node with lowest fCost
            Vector2Int current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                PathNode currentNode = nodes[current];
                PathNode nextNode = nodes[openSet[i]];
                
                if (nextNode.fCost < currentNode.fCost || 
                    (nextNode.fCost == currentNode.fCost && nextNode.hCost < currentNode.hCost))
                {
                    current = openSet[i];
                }
            }
            
            // Goal check
            if (current == goalNode)
            {
                // Reconstruct path
                ReconstructPath(nodes, current);
                return;
            }
            
            // Move current from open to closed
            openSet.Remove(current);
            closedSet.Add(current);
            
            // Check neighbors - include diagonals for smoother paths
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // Skip center 
                    if (x == 0 && y == 0) continue;
                    
                    // Make diagonal movement more expensive
                    bool isDiagonal = x != 0 && y != 0;
                    
                    Vector2Int neighbor = new Vector2Int(current.x + x, current.y + y);
                    
                    // Skip if in closed set
                    if (closedSet.Contains(neighbor)) continue;
                    
                    // Check if walkable with improved collision detection
                    Vector2 neighborWorldPos = GridToWorld(neighbor);
                    if (IsPositionBlocked(neighborWorldPos, isDiagonal))
                    {
                        closedSet.Add(neighbor); // Mark as not walkable
                        continue;
                    }
                    
                    // Calculate costs - diagonal costs more
                    int moveCost = isDiagonal ? 14 : 10; // 1.4 cost for diagonal vs 1.0 for straight
                    
                    // If the neighbor is not in our dict, add it
                    if (!nodes.ContainsKey(neighbor))
                    {
                        nodes[neighbor] = new PathNode 
                        { 
                            gCost = int.MaxValue, 
                            hCost = HeuristicDistance(neighbor, goalNode),
                            parent = null
                        };
                    }
                    
                    PathNode currentNode = nodes[current];
                    PathNode neighborNode = nodes[neighbor];
                    
                    int tentativeGCost = currentNode.gCost + moveCost;
                    
                    if (tentativeGCost < neighborNode.gCost)
                    {
                        // This path is better
                        neighborNode.parent = current;
                        neighborNode.gCost = tentativeGCost;
                        
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
        }
        
        // If we got here, no path was found
        // Set a direct path to the goal as fallback
        path.Add(goalPos);
    }
    
    private void ReconstructPath(Dictionary<Vector2Int, PathNode> nodes, Vector2Int current)
    {
        // Create path from goal to start
        List<Vector2> reversePath = new List<Vector2>();
        
        // Add goal position
        reversePath.Add(player.position);
        
        // Trace back through parents
        while (nodes.ContainsKey(current) && nodes[current].parent.HasValue)
        {
            // Add midpoint position to smooth path
            Vector2 worldPos = GridToWorld(current);
            reversePath.Add(worldPos);
            
            // Move to parent
            current = nodes[current].parent.Value;
        }
        
        // Reverse to get path from start to goal
        reversePath.Reverse();
        
        // Simplify path by checking line of sight between points
        path = SimplifyPath(reversePath);
    }
    
    private List<Vector2> SimplifyPath(List<Vector2> inputPath)
    {
        List<Vector2> simplifiedPath = new List<Vector2>();
        
        if (inputPath.Count <= 2)
        {
            return new List<Vector2>(inputPath);
        }
        
        simplifiedPath.Add(inputPath[0]);
        
        // Check line of sight between points and skip those with clear path
        for (int i = 1; i < inputPath.Count - 1; i++)
        {
            Vector2 current = inputPath[i];
            Vector2 lastAdded = simplifiedPath[simplifiedPath.Count - 1];
            Vector2 next = inputPath[i+1];
            
            Vector2 dirToCurrent = (current - lastAdded).normalized;
            Vector2 dirToNext = (next - lastAdded).normalized;
            
            // If direction change is significant or we can't see through, add point
            float dot = Vector2.Dot(dirToCurrent, dirToNext);
            if (dot < 0.95f || !CanSeePoint(lastAdded, next))
            {
                simplifiedPath.Add(current);
            }
        }
        
        // Always add the last point (goal)
        simplifiedPath.Add(inputPath[inputPath.Count - 1]);
        
        return simplifiedPath;
    }
    
    private bool CanSeePoint(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        float distance = Vector2.Distance(from, to);
        
        RaycastHit2D hit = Physics2D.Raycast(from, direction, distance, wallLayer);
        return hit.collider == null;
    }
    
    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / nodeSize),
            Mathf.RoundToInt(worldPos.y / nodeSize)
        );
    }
    
    private Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(
            gridPos.x * nodeSize,
            gridPos.y * nodeSize
        );
    }
    
    private int HeuristicDistance(Vector2Int a, Vector2Int b)
    {
        // Manhattan distance
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return 10 * (dx + dy);
    }
    
    // PathNode class for A* algorithm
    private class PathNode
    {
        public int gCost; // Cost from start to this node
        public int hCost; // Heuristic estimated cost to goal
        public int fCost => gCost + hCost; // Total cost
        public Vector2Int? parent; // Parent node for path reconstruction
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
                    currentState = State.Pursue;
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
            if (hasSpottedPlayer && Time.time >= lastSpottedTime + forgetTime)
            {
                currentState = State.Random;
                float randomAngle = GetClearRandomAngle();
                targetRotation = Quaternion.Euler(0, 0, randomAngle);
                patrolDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
            }
        }
    }

    // Handle direct collisions with the player
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return; // Dead enemies can't kill the player
        
        if (collision.gameObject.CompareTag("Player"))
        {
            // Get the player controller
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null && !playerController.IsDead())
            {
                // Kill the player immediately
                playerController.TakeDamage(1);
                
                // Play the enemy's shoot sound
                if (shootSound != null)
                {
                    shootSound.pitch = Random.Range(0.8f, 1.1f);
                    shootSound.PlayOneShot(shootSound.clip);
                }
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
        rb.AddForce(nudgeDirection * 10f, ForceMode2D.Impulse);

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

    // Improved position blocking check
    private bool IsPositionBlocked(Vector2 position, bool isDiagonal)
    {
        // Basic circle overlap check
        bool isBlocked = Physics2D.OverlapCircle(position, nodeSize * 0.4f, wallLayer);
        
        // For diagonal movement, check additional points to prevent corner cutting
        if (!isBlocked && isDiagonal)
        {
            // Get the 4 grid points around this position
            Vector2Int gridPos = WorldToGrid(position);
            Vector2 worldPos = GridToWorld(gridPos);
            
            // Check the orthogonal neighbors too (to avoid cutting corners)
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(gridPos.x - 1, gridPos.y),
                new Vector2Int(gridPos.x + 1, gridPos.y),
                new Vector2Int(gridPos.x, gridPos.y - 1),
                new Vector2Int(gridPos.x, gridPos.y + 1)
            };
            
            // If two adjacent nodes are blocked, diagonal movement is not allowed
            foreach (Vector2Int neighbor in neighbors)
            {
                if (Physics2D.OverlapCircle(GridToWorld(neighbor), nodeSize * 0.4f, wallLayer))
                {
                    isBlocked = true;
                    break;
                }
            }
        }
        
        return isBlocked;
    }
}