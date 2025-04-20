using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyEquipment))]
public class Enemy : MonoBehaviour
{
    public EnemyData enemyData;
    
    private Transform player;
    public LayerMask wallLayer;
    public float minReactionTime = 0.19f;
    public float maxReactionTime = 0.23f;
    private float nextShootTime;
    private bool hasSpottedPlayer;
    private bool hasReacted = false;
    private float lastSpottedTime;
    private float lastAttackTime;
    private Coroutine attackAnimationCoroutine; // manage the attack sprite change
    private bool playerInFOV = false; // track if player is within FOV
    

    private EnemyEquipment enemyEquipment;
    private WeaponData fistWeaponData;
    private byte emptyClickSoundCount = 0;
    public float randomModeInterval = 2.5f;
    public float turnPauseDuration = 0.7f; // duration to pause after turning while patrolling
    public float minTurnPauseDuration = 0.5f; // minimum pause time after turning
    public float maxTurnPauseDuration = 1.5f; // maximum
    private Rigidbody2D rb;
    private Vector2 patrolDirection;
    private float enemyRadius = 0.25f;
    private float randomModeTimer;
    private bool isMovingInRandomMode;
    public float safetyDistance = 0.5f;
    private Quaternion targetRotation;
    private float waitTimer;
    private bool isWaiting;
    private bool isPausedAfterTurn = false; // if paused after turning
    private float turnPauseTimer = 0f;
    private bool turnRight = true;

    // a* 
    private List<Vector2> path = new List<Vector2>();
    private int currentPathIndex;
    private float pathUpdateTime = 0.5f; // recalculate path every 0.5 seconds
    private float lastPathUpdateTime;
    private float nodeSize = 0.5f; // node size
    private float pathNodeReachedDistance = 0.1f;
    
    // stuck detection variables
    private Vector2 lastPosition;
    private float stuckCheckTime = 0.5f;
    private float lastStuckCheckTime;
    private float stuckThreshold = 0.05f; // distance threshold for stuck detection
    private int consecutiveStuckFrames = 0;
    private int stuckFrameThreshold = 3; // consecutive stuck checks before taking action

    private enum State { Patrol, Pursue, Random, Dead }


    private State currentState;
    

    // from EnemyData
    private float currentHealth;

    public bool isDead = false;

    void Start()
    {
        if (enemyData == null)
        {
            Debug.LogError("EnemyData not assigned", this);
            enabled = false;
            return;
        }
        currentState = Random.value < 0.2f ? State.Random : State.Patrol; // 20% chance for Random, else Patrol
        currentHealth = enemyData.health;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Enemy could not find Player");
        }

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        
        patrolDirection = transform.up;
        randomModeTimer = randomModeInterval;
        targetRotation = transform.rotation;
        waitTimer = 0f;
        isWaiting = false;
        
        // get EnemyEquipment
        enemyEquipment = GetComponent<EnemyEquipment>();
        if (enemyEquipment == null)
        {
            Debug.LogError("EnemyEquipment component not found", this);
            enabled = false; //no equipment
            return;
        }
        
        // get fist data
        fistWeaponData = enemyEquipment.FistWeaponData; 
        if(fistWeaponData == null || !fistWeaponData.isMelee)
        {
            Debug.LogWarning("no FistWeaponData assigned", this);
        }

        if (enemyEquipment.CurrentWeapon != null)
        {
            float initialDelay = 1f / enemyEquipment.CurrentWeapon.fireRate;
            nextShootTime = Time.time + initialDelay + Random.Range(minReactionTime, maxReactionTime); // randomize delay
            lastAttackTime = -initialDelay; // for melee
        }

        turnRight = Random.value > 0.5f; // random turn direction

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        
        // initialize A* pathfinding
        lastPathUpdateTime = -pathUpdateTime;
        lastStuckCheckTime = Time.time;
        lastPosition = transform.position;
    }

    void Update()
    {
        if (player == null || isDead) return;

        PlayerController playerController = player.GetComponent<PlayerController>();
        bool playerIsDead = playerController != null && playerController.IsDead();

        // stuck detection
        if (currentState == State.Pursue && Time.time >= lastStuckCheckTime + stuckCheckTime)
        {
            float distanceMoved = Vector2.Distance(lastPosition, transform.position);
            if (distanceMoved < stuckThreshold && path.Count > 0)
            {
                consecutiveStuckFrames++;
                
                // if stuck for several consecutive checks
                if (consecutiveStuckFrames >= stuckFrameThreshold)
                {
                    // force path recalculation with increased node size temporarily
                    float oldNodeSize = nodeSize;
                    nodeSize *= 1.5f;
                    CalculatePath(); // recalculate then reset node size
                    nodeSize = oldNodeSize;
                    
                    // if still stuck, try a random direction
                    if (consecutiveStuckFrames >= stuckFrameThreshold * 2)
                    {
                        path.Clear();
                        float randomAngle = GetClearRandomAngle();
                        Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                        Vector2 randomTarget = (Vector2)transform.position + randomDirection * 3f;
                        path.Add(randomTarget);
                        currentPathIndex = 0;
                        consecutiveStuckFrames = 0;
                    }
                }
            }
            else
            {
                consecutiveStuckFrames = 0;
            }
            
            lastPosition = transform.position;
            lastStuckCheckTime = Time.time;
        }

        HandleSightAndState(); // check if player is in sight

        if (currentState == State.Pursue && !playerIsDead)
        {
            // attack Logic
            WeaponData currentWep = enemyEquipment.CurrentWeapon;
            if (currentWep != null)
            {
                bool isInMeleeRange = Vector2.Distance(transform.position, player.position) <= fistWeaponData.range; // check fist range
                bool canSee = CanSeePlayer();

                // prioritize Melee if in range
                if (fistWeaponData != null && fistWeaponData.isMelee && isInMeleeRange && Time.time >= lastAttackTime + (1f / fistWeaponData.fireRate))
                {
                    // check if reacted to player
                    if (!hasReacted && Time.time >= nextShootTime)
                    {
                        // first attack after spotting player, apply reaction time
                        MeleeAttack(fistWeaponData); // pass fist data explicitly
                        lastAttackTime = Time.time;
                        hasReacted = true;

                    }
                    else if (hasReacted)
                    {
                        MeleeAttack(fistWeaponData);
                        lastAttackTime = Time.time;
                    }
                }
                // if not melee, shoot
                else if (!currentWep.isMelee && currentWep.canShoot && canSee && Time.time >= nextShootTime)
                {
                    if (currentWep.HasAmmo())
                    {
                        Shoot();
                        nextShootTime = Time.time + (1f / currentWep.fireRate); // use fire rate
                        hasReacted = true;
                    }
                    else
                    {
                        // enemy has no ammo
                        nextShootTime = Time.time + 1f;
                        if (emptyClickSoundCount < 3) { //play click 3 times
                            PlayEmptyClickSound(); 
                            emptyClickSoundCount++;
                        }
                    }
                }
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

        // apply smooth rotation
        if (currentState == State.Patrol || currentState == State.Random)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enemyData.rotationSpeed * Time.deltaTime);
        }
        
        // update path
        if (currentState == State.Pursue && Time.time >= lastPathUpdateTime + pathUpdateTime)
        {
            CalculatePath();
            lastPathUpdateTime = Time.time;
        }
    }

    void HandleSightAndState()
    {
        if (player == null || player.GetComponent<PlayerController>() == null) return;
        bool playerIsDead = player.GetComponent<PlayerController>().IsDead();
        bool canSee = CanSeePlayer();

        if (!playerIsDead && canSee)
        {
            if (!hasSpottedPlayer)
            {
                hasSpottedPlayer = true;
                lastSpottedTime = Time.time;
                hasReacted = false; // reset reaction

                float reactionDelay = Random.Range(minReactionTime, maxReactionTime);
                nextShootTime = Time.time + reactionDelay;
                // delay melee
                if (fistWeaponData != null) {
                    lastAttackTime = Time.time - (1f / fistWeaponData.fireRate) + reactionDelay;
                }
            }
            
            if (currentState != State.Pursue)
            {
                currentState = State.Pursue;
                lastPathUpdateTime = -pathUpdateTime;
                
                // only apply reaction time if not already in reaction delay
                if(!hasReacted && Time.time >= nextShootTime && enemyEquipment.CurrentWeapon != null)
                {
                // set random reaction time delay
                nextShootTime = Time.time + Random.Range(minReactionTime, maxReactionTime);
                }
            }
        }
        else // cannot see player OR player is dead
        {
            if (hasSpottedPlayer && Time.time >= lastSpottedTime + enemyData.forgetTime)
            {
                // timer expired after spotting player
                hasSpottedPlayer = false; 
                hasReacted = false; // reset the reaction flag when forgetting player

                if (currentState == State.Pursue)
                {
                     // if player is not dead, switch to Random after forgetTime
                     if (!playerIsDead) 
                     {
                         currentState = State.Random;
                         path.Clear();
                         rb.linearVelocity = Vector2.zero;
                         // randomModeTimer = randomModeInterval; 
                         // isMovingInRandomMode = false;
                     }
                     // player is dead, switch to random
                     else 
                     {
                         currentState = State.Random; 
                         path.Clear();
                         rb.linearVelocity = Vector2.zero;
                     }
                }
            }
            else if (currentState == State.Pursue && playerIsDead) 
            {
                // player dies while being pursued before forgetTime expires
                currentState = State.Patrol; 
                path.Clear(); 
                rb.linearVelocity = Vector2.zero; 
                hasSpottedPlayer = false; 
                hasReacted = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (player == null || isDead) 
        {
            rb.linearVelocity = Vector2.zero; // stop if enemy or player is dead
            return;
        }

        PlayerController playerController = player.GetComponent<PlayerController>();
        bool playerIsDead = playerController != null && playerController.IsDead();

        // stop movement if pursuing a dead player
        if (currentState == State.Pursue && playerIsDead)
        {
            rb.linearVelocity = Vector2.zero;
            path.Clear(); // Clear path
            // update already does this
            currentState = State.Random; 
            return; 
        }

        if (currentState == State.Pursue)
        {
            // A* pathfinding movement
            Vector2 moveDirection = Vector2.zero;
            
            //  check if we have a path
            if (path.Count > 0 && currentPathIndex < path.Count)
            {
                // next node
                Vector2 targetPosition = path[currentPathIndex];
                
                // calculate distance and direction to the next node
                float distanceToNode = Vector2.Distance(transform.position, targetPosition);
                Vector2 directionToNode = (targetPosition - (Vector2)transform.position).normalized;
                
                // wall avoidance
                Vector2 repulsion = CalculateRepulsion() * 0.8f;
                moveDirection = (directionToNode + repulsion).normalized;
                
                // check if reached the current path node
                if (distanceToNode <= pathNodeReachedDistance)
                {
                    currentPathIndex++;
                }
                
                // skip unreachable nodes
                if (currentPathIndex < path.Count && !CanSeePoint(transform.position, path[currentPathIndex]) && path.Count > currentPathIndex + 1)
                {
                    // if can see a future node directly, skip to it
                    for (int i = currentPathIndex + 1; i < path.Count; i++)
                    {
                        if (CanSeePoint(transform.position, path[i]))
                        {
                            currentPathIndex = i;
                            break;
                        }
                    }
                }
                
                // slow down when approaching turns for more natural movement
                float speedMultiplier = 1.0f;
                if (currentPathIndex < path.Count - 1)
                {
                    Vector2 currentDirection = directionToNode;
                    Vector2 nextDirection = Vector2.zero;
                    
                    if (currentPathIndex + 1 < path.Count)
                    {
                        nextDirection = (path[currentPathIndex + 1] - path[currentPathIndex]).normalized;
                        float dot = Vector2.Dot(currentDirection, nextDirection);
                        
                        // slow down more for sharper turns
                        if (dot < 0.7f) 
                        {
                            speedMultiplier = 0.6f + (dot * 0.4f);
                        }
                    }
                }
                
                Vector2 desiredPosition = (Vector2)transform.position + moveDirection * enemyData.chaseSpeed * speedMultiplier * Time.fixedDeltaTime;
                
                // move if not colliding with a wall
                if (!WouldCollide(desiredPosition))
                {
                    rb.MovePosition(desiredPosition);
                }
                else
                {
                    // find an alternative direction if stuck
                    for (int i = 15; i <= 75; i += 15)
                    {
                        Vector2 deflectedDirection = Quaternion.Euler(0, 0, i) * moveDirection;
                        Vector2 deflectedPosition = (Vector2)transform.position + deflectedDirection * enemyData.chaseSpeed * 0.5f * Time.fixedDeltaTime;
                        
                        if (!WouldCollide(deflectedPosition))
                        {
                            rb.MovePosition(deflectedPosition);
                            break;
                        }
                        
                        deflectedDirection = Quaternion.Euler(0, 0, -i) * moveDirection;
                        deflectedPosition = (Vector2)transform.position + deflectedDirection * enemyData.chaseSpeed * 0.5f * Time.fixedDeltaTime;
                        
                        if (!WouldCollide(deflectedPosition))
                        {
                            rb.MovePosition(deflectedPosition);
                            break;
                        }
                    }
                }
                
                // smooth rotation
                if (moveDirection != Vector2.zero)
                {
                    float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
                    Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, enemyData.rotationSpeed * Time.fixedDeltaTime);
                }
            }
            else
            {
                // no path or reached end of path
                Vector2 directDirection = (player.position - transform.position).normalized;
                Vector2 repulsion = CalculateRepulsion();
                Vector2 finalDirection = (directDirection + repulsion).normalized;
                Vector2 desiredPosition = (Vector2)transform.position + finalDirection * enemyData.chaseSpeed * Time.fixedDeltaTime;
                
                if (!WouldCollide(desiredPosition))
                {
                    rb.MovePosition(desiredPosition);
                }
                
                // smooth rotation
                float angle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg - 90f;
                Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, enemyData.rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else if (currentState == State.Patrol)
        {
            // pause timer after turning
            if (isPausedAfterTurn)
            {
                turnPauseTimer -= Time.fixedDeltaTime;
                if (turnPauseTimer <= 0)
                {
                    isPausedAfterTurn = false;
                }
                return; // skip movement while paused
            }
            
            // move forward until hitting a wall, then turn 90 degrees
            Vector2 repulsion = CalculateRepulsion();
            Vector2 finalDirection = (patrolDirection + repulsion * 0.3f).normalized;
            Vector2 desiredPosition = (Vector2)transform.position + finalDirection * enemyData.patrolSpeed * Time.fixedDeltaTime;

            // check for wall directly ahead
            RaycastHit2D hit = Physics2D.Raycast(transform.position, patrolDirection, safetyDistance + enemyRadius, wallLayer);
            if (hit.collider != null)
            {
                // random turn direction
                turnRight = Random.value > 0.5f;
                float turnAngle = turnRight ? -90 : 90; // negative for right, positive for left turn
                Vector2 newDirection = Quaternion.Euler(0, 0, turnAngle) * patrolDirection;
                targetRotation = Quaternion.LookRotation(Vector3.forward, newDirection);
                patrolDirection = newDirection;
                
                // pause
                isPausedAfterTurn = true;
                turnPauseTimer = Random.Range(minTurnPauseDuration, maxTurnPauseDuration);
            }
            else if (!WouldCollide(desiredPosition))
            {
                rb.MovePosition(desiredPosition);
            }
        }
        else if (currentState == State.Random) // random state
        {
            if (!isWaiting)
            {
                Vector2 repulsion = CalculateRepulsion();
                Vector2 finalDirection = (patrolDirection + repulsion).normalized;
                Vector2 desiredPosition = (Vector2)transform.position + finalDirection * enemyData.patrolSpeed * Time.fixedDeltaTime;

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
        
        // ensure rotation is only on Z-axis
        if (!isDead && rb != null)
        {
            transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z);
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
    
    // A* 
    private void CalculatePath()
    {
        if (player == null) return;
        
        path.Clear();
        currentPathIndex = 0;
        
        Vector2 startPos = transform.position;
        Vector2 goalPos = player.position;
        
        // if can see the player directly, set a direct path
        if (CanSeePlayer())
        {
            path.Add(goalPos);
            return;
        }
        
        // define our grid around the start and goal
        float gridRadius = Vector2.Distance(startPos, goalPos) * 1.5f;
        gridRadius = Mathf.Max(gridRadius, 10f); // minimum search radius
        
        // create nodes grid
        Dictionary<Vector2Int, PathNode> nodes = new Dictionary<Vector2Int, PathNode>();
        
        // convert world positions to grid coordinates
        Vector2Int startNode = WorldToGrid(startPos);
        Vector2Int goalNode = WorldToGrid(goalPos);
        
        List<Vector2Int> openSet = new List<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        
        // init start node
        nodes[startNode] = new PathNode { gCost = 0, hCost = HeuristicDistance(startNode, goalNode), parent = null };
        openSet.Add(startNode);
        
        // loop
        int iterations = 0;
        int maxIterations = 500;
        
        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            // find node with lowest fCost
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
            
            // check if arrived
            if (current == goalNode)
            {
                // reconstruct path
                ReconstructPath(nodes, current);
                return;
            }
            
            openSet.Remove(current);
            closedSet.Add(current);
            
            // check neighbors
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // skip the center node
                    if (x == 0 && y == 0) continue;
                    
                    bool isDiagonal = x != 0 && y != 0;
                    
                    Vector2Int neighbor = new Vector2Int(current.x + x, current.y + y);
                    
                    // skip if in closed set
                    if (closedSet.Contains(neighbor)) continue;
                    
                    // check if walkable
                    Vector2 neighborWorldPos = GridToWorld(neighbor);
                    if (IsPositionBlocked(neighborWorldPos, isDiagonal))
                    {
                        closedSet.Add(neighbor); // mark not walkable
                        continue;
                    }
                    
                    // calculate cost
                    int moveCost = isDiagonal ? 14 : 10; // 1.4 diagonal 1 for straight
                    
                    // if neighbor is not in our dict, add
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
                        // this path is better
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
        
        // no path was found
        path.Add(goalPos);
    }
    
    private void ReconstructPath(Dictionary<Vector2Int, PathNode> nodes, Vector2Int current)
    {
        // create path from goal to start
        List<Vector2> reversePath = new List<Vector2>();
        reversePath.Add(player.position);
        
        while (nodes.ContainsKey(current) && nodes[current].parent.HasValue)
        {
            // add current node to path
            Vector2 worldPos = GridToWorld(current);
            reversePath.Add(worldPos);
            
            // move to parent node
            current = nodes[current].parent.Value;
        }
        
        // reverse path to get start to goal order
        reversePath.Reverse();
        
        // simplify by checking line of sight between points
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
        
        for (int i = 1; i < inputPath.Count - 1; i++)
        {
            Vector2 current = inputPath[i];
            Vector2 lastAdded = simplifiedPath[simplifiedPath.Count - 1];
            Vector2 next = inputPath[i+1];
            
            Vector2 dirToCurrent = (current - lastAdded).normalized;
            Vector2 dirToNext = (next - lastAdded).normalized;
            
            // if direction change is significant or we can't see through, add point
            float dot = Vector2.Dot(dirToCurrent, dirToNext);
            if (dot < 0.95f || !CanSeePoint(lastAdded, next))
            {
                simplifiedPath.Add(current);
            }
        }
        
        // add last
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
        // manhattan distance
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return 10 * (dx + dy);
    }
    
    // pathnode
    private class PathNode
    {
        public int gCost; // cost from start to this node
        public int hCost; // heuristic estimated cost to goal
        public int fCost => gCost + hCost; // total cost
        public Vector2Int? parent; // parent node for path reconstruction
    }

    void MeleeAttack(WeaponData meleeWeapon)
    {
        if (player == null || isDead || meleeWeapon == null || !meleeWeapon.isMelee) return; // safety checks

        // aim at player
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        transform.up = directionToPlayer;
        
        // ensure rotation stays on Z-axis only after aiming
        transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z);
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // trigger sprite change
        if (attackAnimationCoroutine != null) StopCoroutine(attackAnimationCoroutine);
        attackAnimationCoroutine = StartCoroutine(AttackAnimation(meleeWeapon));

        // hit detection: OverlapCircle focused on Player
        Vector2 attackOrigin = (Vector2)transform.position + (Vector2)transform.up * meleeWeapon.bulletOffset;
        Collider2D hit = Physics2D.OverlapCircle(attackOrigin, meleeWeapon.range * 0.7f, LayerMask.GetMask("Player"));

        bool didHit = false;
        if (hit != null)
        {
            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController != null && !playerController.IsDead())
            {
                // check for wall between enemy and player 
                Vector2 direction = (player.position - transform.position).normalized;
                float distance = Vector2.Distance(transform.position, player.position);
                RaycastHit2D wallHit = Physics2D.Raycast(transform.position, direction, distance, wallLayer);
                
                // only damage player if there's no wall in between
                if (wallHit.collider == null)
                {
                    // blood particle
                    GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"),
                        player.position, Quaternion.LookRotation(Vector3.forward, directionToPlayer));
                    
                    // apply damage to the player
                    //playerController.Die(directionToPlayer);
                    playerController.TakeDamage((int)meleeWeapon.damage, directionToPlayer);
                    didHit = true;
                }
            }
        }

        // play sound
        AudioSource source = GetComponent<AudioSource>();
        if (source != null)
        {
            if (didHit && meleeWeapon.hitSound != null)
            {
                source.PlayOneShot(meleeWeapon.hitSound);
            }
            else if (!didHit && meleeWeapon.missSound != null)
            {
                source.PlayOneShot(meleeWeapon.missSound);
            }
        }
        else
        {
            Debug.LogWarning("Enemy requires an AudioSource component for attack sounds.", this);
        }
    }

    IEnumerator AttackAnimation(WeaponData weapon)
    {
        if (weapon.attackSprite != null)
        {
            Sprite spriteToUse = weapon.attackSprite;
            
            // alternating sprites is on and second sprite exists, randomly choose
            if (weapon.useAlternatingSprites && weapon.attackSprite2 != null)
            {
                spriteToUse = (Random.value < 0.5f) ? weapon.attackSprite : weapon.attackSprite2;
            }
            
            enemyEquipment.SetSprite(spriteToUse);
            yield return new WaitForSeconds(weapon.attackDuration);
            enemyEquipment.UpdateSpriteToCurrentWeapon(); // revert to normal sprite
        }
        attackAnimationCoroutine = null; // clear the coroutine reference
    }

    void Shoot()
    {
        WeaponData currentWep = enemyEquipment.CurrentWeapon;
        if (player == null || currentWep == null || currentWep.isMelee || !currentWep.canShoot || isDead || currentWep.projectilePrefab == null)
        {
            return; 
        }

        // stop melee animation if switching to shooting, unused (i think)
        if (attackAnimationCoroutine != null)
        {
             StopCoroutine(attackAnimationCoroutine);
             enemyEquipment.UpdateSpriteToCurrentWeapon();
             attackAnimationCoroutine = null;
        }

        // aim at player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        transform.up = directionToPlayer;
        
        // rotation only on Z
        transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z);
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // use ammo (already happened in Update)
        if (!currentWep.UseAmmo())
        {
            if (emptyClickSoundCount < 3) {
                PlayEmptyClickSound();
                emptyClickSoundCount++;
            }
             return; 
        }

        // bullet spawn position
        Vector3 spawnPosition = transform.position + (transform.up * currentWep.bulletOffset) + (transform.right * currentWep.bulletOffsetSide);
        // spawn muzzle flash
        if (currentWep.muzzleFlashPrefab != null)
        {
            GameObject muzzleFlash = Instantiate(currentWep.muzzleFlashPrefab, spawnPosition, transform.rotation);
            Destroy(muzzleFlash, currentWep.muzzleFlashDuration); 
        }

        Quaternion bulletRotation = transform.rotation; // start with enemy's rotation

        // handle shotgun pellets / spread
        int pelletCount = Mathf.Max(1, currentWep.pelletCount);
        bool isShotgun = pelletCount > 1;

        for (int i = 0; i < pelletCount; i++)
        {
            float angleOffset = 0;
            // apply weapon spread
            if (currentWep.spread > 0)
            {
                angleOffset += Random.Range(-currentWep.spread / 2f, currentWep.spread / 2f);
            }
            // apply shotgun spread angle
            if (isShotgun && currentWep.spreadAngle > 0)
            {
                angleOffset += Random.Range(-currentWep.spreadAngle / 2f, currentWep.spreadAngle / 2f); 
                 // alternative:
                 // float angleStep = currentWep.spreadAngle / (pelletCount - 1);
                 // angleOffset += -currentWep.spreadAngle / 2 + angleStep * i;
            }

            Quaternion finalRotation = bulletRotation * Quaternion.Euler(0, 0, angleOffset);
            GameObject bulletGO = Instantiate(currentWep.projectilePrefab, spawnPosition, finalRotation);
            
            // bullet component
            Bullet bulletScript = bulletGO.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetShooter(gameObject); // identify shooter
                bulletScript.SetBulletParameters(currentWep.bulletSpeed, currentWep.range); 
                bulletScript.SetDamage(currentWep.damage);
                bulletScript.SetWeaponData(currentWep);
            }
            
            // apply velocity
            Rigidbody2D bulletRb = bulletGO.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = bulletGO.transform.up * currentWep.bulletSpeed;
            }
            else
            {
                Debug.LogWarning("bullet prefab missing a Rigidbody2D.");
            }
        }
        // play sound
        AudioSource source = GetComponent<AudioSource>();
        if(source != null && currentWep.shootSound != null)
        {
            source.pitch = Random.Range(0.9f, 1.1f); // pitch variation
            source.PlayOneShot(currentWep.shootSound);
        }
    }

    /*void OnCollisionEnter2D(Collision2D collision)
    {
        
        if (collision.gameObject.CompareTag("Player"))
        {

            return; 
        }

        if (currentState == State.Patrol && ((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
        
        }
    }*/

    bool CanSeePlayer()
    {
        if (player == null || isDead) return false;
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsDead()) return false;
        
        // check if player is in FOV collider
        if (!playerInFOV) return false;
        
        // then check for line of sight
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
        
        // wall repulsion
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
        
        // enemy repulsion
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, safetyDistance * 1.5f, LayerMask.GetMask("Enemy"));
        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            // skip self
            if (enemyCollider.gameObject == gameObject)
                continue;
                
            // calculate distance and direction
            Vector2 enemyPos = enemyCollider.transform.position;
            float distance = Vector2.Distance(transform.position, enemyPos);
            
            // stronger repulsion for enemies than for walls to prevent overlapping
            if (distance < safetyDistance * 1.5f)
            {
                Vector2 repulsionDirection = (Vector2)transform.position - enemyPos;
                float repulsionStrength = (safetyDistance * 1.5f - distance) / (safetyDistance * 1.5f);
                repulsion += repulsionDirection.normalized * repulsionStrength * 1.5f; // Stronger than wall repulsion
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

    public void Die(Vector2 bulletDirection = default)
    {
        if (isDead) return;
        isDead = true;
        currentState = State.Dead;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // record enemy defeat in ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RecordEnemyDefeated();
        }

        // stop attack animation
        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }
        
        // set Death Sprite
        if (enemyEquipment != null && enemyData.deathSprite != null)
        {
            enemyEquipment.SetSprite(enemyData.deathSprite);
        }
        else
        {
            Debug.LogWarning("cannot set death sprite");
        }

        // change the sorting layer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = "DeadEnemies";
        }
        
        // set scale 
        transform.localScale = new Vector3(3.4f, 3.4f, 3.4f);

        // weapon drop
        WeaponData deadEnemyWeapon = null;
        if (enemyEquipment != null && enemyEquipment.CurrentWeapon != null && enemyEquipment.CurrentWeapon != fistWeaponData)
        {
            deadEnemyWeapon = enemyEquipment.CurrentWeapon;
            
            if (deadEnemyWeapon.pickupPrefab != null)
            {
                GameObject weaponPickupGO = Instantiate(deadEnemyWeapon.pickupPrefab, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
                WeaponPickup pickupScript = weaponPickupGO.GetComponent<WeaponPickup>();
                
                // add force to dropped weapon
                Rigidbody2D weaponRb = weaponPickupGO.GetComponent<Rigidbody2D>();
                if (weaponRb != null)
                {
                    float randomAngle = Random.Range(0f, 360f);
                    Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                    float forceMagnitude = Random.Range(30.0f, 60.0f);
                    weaponRb.AddForce(randomDirection * forceMagnitude, ForceMode2D.Impulse);
                    weaponRb.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);
                }
            }
            else
            {
                 Debug.LogWarning("no pickupPrefab assigned");
            }
        }
        GetComponent<Collider2D>().enabled = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // apply nudge force
        if (bulletDirection != default)
        {
            rb.AddForce(-bulletDirection * 10f, ForceMode2D.Impulse);
            Invoke(nameof(StopAfterNudge), 0.2f);
        }
    }

    void StopAfterNudge()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // position blocking check
    private bool IsPositionBlocked(Vector2 position, bool isDiagonal)
    {
        bool isBlocked = Physics2D.OverlapCircle(position, nodeSize * 0.4f, wallLayer);
        
        // prevent corner cutting
        if (!isBlocked && isDiagonal)
        {
            Vector2Int gridPos = WorldToGrid(position);
            Vector2 worldPos = GridToWorld(gridPos);
            
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(gridPos.x - 1, gridPos.y),
                new Vector2Int(gridPos.x + 1, gridPos.y),
                new Vector2Int(gridPos.x, gridPos.y - 1),
                new Vector2Int(gridPos.x, gridPos.y + 1)
            };
            
            // if two adjacent nodes are blocked, diagonal movement is not allowed
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

    public void HearSound()
    {
        hasSpottedPlayer = true;
        lastSpottedTime = Time.time;
        hasReacted = false;
        
        // apply reaction time
        float reactionDelay = Random.Range(minReactionTime, maxReactionTime);
        nextShootTime = Time.time + reactionDelay;
        
        if (fistWeaponData != null) {
            lastAttackTime = Time.time - (1f / fistWeaponData.fireRate) + reactionDelay;
        }
        
        if (currentState != State.Pursue)
        {
            currentState = State.Pursue;
            lastPathUpdateTime = -pathUpdateTime; // force path recalc
        }
        else
        {
            hasReacted = false;
        }
    }
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        currentHealth -= amount;
        
        // blood particle
        GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"),
            transform.position, Quaternion.identity);
        
        // reset rotation
        transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z);
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        if (currentHealth <= 0)
        {
            // stop shooting logic
            if (attackAnimationCoroutine != null)
            {
                StopCoroutine(attackAnimationCoroutine);
                attackAnimationCoroutine = null;
            }
            Die();
        }
    }

    // used for boss invulnerability
    public void HealDamage(float amount)
    {
        if (isDead) return;
        
        // add the damage back
        currentHealth += amount;
        
        // dont exceed max health
        if (currentHealth > enemyData.health)
        {
            currentHealth = enemyData.health;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInFOV = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInFOV = false;
        }
    }
    void PlayEmptyClickSound()
    {
        WeaponData currentWep = enemyEquipment.CurrentWeapon;
        if (currentWep != null && currentWep.emptyClickSound != null)
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source != null)
            {
                source.pitch = 1.0f;
                source.PlayOneShot(currentWep.emptyClickSound);
            }
        }
    }
}