using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerEquipment), typeof(Collider2D))] // Add Collider2D requirement
public class PlayerController : MonoBehaviour
{
    // Player Attributes
    public float moveSpeed;
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private PlayerEquipment playerEquipment; // Reference to the equipment manager
    public int health = 1;
    private bool isDead = false;
    public Sprite deathSprite;

    // Shooting State
    private float lastFireTime;
    private bool shouldShoot;       //workaround for for consistent firerate with InputSystem
    private float lastWeaponPickupTime = 0f; // Keep for potential pickup cooldown
    private float weaponActionCooldown = 0.5f; // Keep for potential pickup cooldown

    // Camera
    public Camera mainCamera;
    public float cameraSmoothSpeed = 0.05f;
    private float shakeTimeRemaining;
    private float shakeMagnitude; // Renamed for clarity
    public float normalOffsetFactor = 0.2f;
    public float shiftOffsetFactor = 0.6f;
    public float normalMaxOffset = 2f;
    public float shiftMaxOffset = 4f;
    private bool isLooking;

    // Timer
    private TimerController timerController;
    private bool hasMovedOnce = false;

    // Audio Source (Consider moving this to a separate Audio Manager or PlayerAudio script later)
    public AudioSource playerAudioSource; // A single AudioSource on the player

    // Pickup State
    private WeaponPickup nearbyWeaponPickup; // Tracks the pickup item the player is near
    private WeaponData fistWeaponData; // Keep this to store the reference locally

    // Weapon Handling
    [Tooltip("The force applied when throwing a weapon.")]
    public float weaponThrowForce = 130f; // <-- ADD THIS PUBLIC VARIABLE

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerEquipment = GetComponent<PlayerEquipment>();
        // Get fist data via the public getter
        fistWeaponData = playerEquipment.FistWeaponData; 
        if (fistWeaponData == null)
        {
             // This error should ideally be caught by PlayerEquipment's Awake now
             Debug.LogError("PlayerEquipment did not provide FistWeaponData!", this);
        }

        // Get the shared AudioSource
        if (playerAudioSource == null) 
        {
            playerAudioSource = GetComponent<AudioSource>();
            if(playerAudioSource == null)
            {
                 Debug.LogWarning("PlayerController needs an AudioSource component assigned or attached.", this);
            }
        }

        if (mainCamera == null) mainCamera = Camera.main;
        
        // Initialize lastFireTime based on the starting weapon's fire rate (likely fists)
        if (playerEquipment.CurrentWeapon != null)
        {
             lastFireTime = -playerEquipment.CurrentWeapon.fireRate;
        }
        else
        {
            lastFireTime = -1f; // Default if no weapon somehow
        }

        timerController = FindFirstObjectByType<TimerController>();
    }

    void Update()
    {
        if (!isDead)
        {
            // Update which weapon pickup is closest
            UpdateClosestWeaponPickup();
            
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0f;
            Vector3 direction = (mousePos - transform.position).normalized;
            transform.up = direction;

            // Check if current weapon can shoot and if fire rate allows
            if (playerEquipment.CurrentWeapon != null && 
                playerEquipment.CurrentWeapon.canShoot && 
                playerEquipment.CurrentWeapon.HasAmmo() && 
                shouldShoot && 
                Time.time >= lastFireTime + (1f / playerEquipment.CurrentWeapon.fireRate)) // Use 1/fireRate for delay
            {
                Shoot();
                lastFireTime = Time.time;
                
                // Only reset the shooting flag for semi-auto weapons
                if (!playerEquipment.CurrentWeapon.isFullAuto)
                {
                    shouldShoot = false; 
                }
            }
            else if (playerEquipment.CurrentWeapon != null && 
                     playerEquipment.CurrentWeapon.canShoot && 
                     !playerEquipment.CurrentWeapon.HasAmmo() && 
                     shouldShoot)
            {
                // Play empty click sound if weapon is out of ammo
                PlayEmptyClickSound();
                shouldShoot = false; // Reset shooting flag to prevent continuous clicks
            }
        }
    }

    void FixedUpdate()
    {
        // Always update camera, even when dead
        Vector2 mouseScreenPos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        Vector2 offsetDirection = new Vector2(mouseScreenPos.x - 0.5f, mouseScreenPos.y - 0.5f);
        float offsetFactor = isLooking ? shiftOffsetFactor : normalOffsetFactor;
        float maxOffset = isLooking ? shiftMaxOffset : normalMaxOffset;
        float horizontalOffset = offsetDirection.x * mainCamera.orthographicSize * mainCamera.aspect * 2 * offsetFactor;
        float verticalOffset = offsetDirection.y * mainCamera.orthographicSize * 2 * offsetFactor;
        Vector3 offset = new Vector3(horizontalOffset, verticalOffset, 0);
        offset = Vector3.ClampMagnitude(offset, maxOffset);
        Vector3 targetCameraPos = transform.position + offset;
        targetCameraPos.z = -1f;
        Vector3 smoothedPos = Vector3.Lerp(mainCamera.transform.position, targetCameraPos, cameraSmoothSpeed/10f);

        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimeRemaining > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude; // Use the renamed variable
            shakeOffset.z = 0f;
            shakeTimeRemaining -= Time.fixedDeltaTime;
        }
        mainCamera.transform.position = smoothedPos + shakeOffset;
        
        if (!isDead)
        {
            rb.linearVelocity = movementInput * moveSpeed;
        }
    }

    // Input Handling
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!isDead)
        {
            movementInput = context.ReadValue<Vector2>();
            
            if (!hasMovedOnce && movementInput != Vector2.zero && timerController != null)
            {
                hasMovedOnce = true;
                timerController.StartTimer();
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.StartLevel();
                }
            }
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (isDead || playerEquipment.CurrentWeapon == null || !playerEquipment.CurrentWeapon.canShoot) 
        {
             shouldShoot = false; // Ensure cannot shoot if dead, unarmed, or weapon can't shoot
             return;
        }

        WeaponData currentWep = playerEquipment.CurrentWeapon;

        if (currentWep.isFullAuto)
        {
            // For full-auto, set shooting flag based on button held state
            if (context.performed || context.started) // Started or Performed means held
            {
                shouldShoot = true;
            }
            else if (context.canceled) // Canceled means released
            {
                shouldShoot = false;
            }
        }
        else
        {
            // For semi-auto, only shoot on the initial press (performed)
            if (context.performed)
            {
                shouldShoot = true; // Will be reset after one shot in Update()
            }
            else if (context.canceled)
            {
                 shouldShoot = false; // Ensure flag is cleared on release
            }
        }
    }

    public void OnWeaponInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || isDead || Time.time < lastWeaponPickupTime + weaponActionCooldown) return;

        if (nearbyWeaponPickup != null)
        {
            // --- Pick up Weapon --- 
            if (nearbyWeaponPickup.weaponData != null)
            {
                // Store the weapon data and its current ammo before destroying the pickup object
                WeaponData weaponToPickup = nearbyWeaponPickup.weaponData;
                int currentAmmo = nearbyWeaponPickup.GetCurrentAmmo();
                
                // If currently holding a weapon (not fists), drop it first
                if (playerEquipment.CurrentWeapon != fistWeaponData)
                {
                     DropWeapon(false); // Drop without applying cooldown again
                }

                // Equip the new weapon and set its ammo
                playerEquipment.EquipWeapon(weaponToPickup);
                
                // Set the weapon's current ammo from the pickup
                if (currentAmmo >= 0) // -1 indicates it wasn't explicitly set
                {
                    weaponToPickup.currentAmmo = currentAmmo;
                }
                
                UpdateLastFireTime(); // Update fire time for the new weapon
                Debug.Log($"Picked up {weaponToPickup.weaponName} with {weaponToPickup.currentAmmo}/{weaponToPickup.magazineSize} ammo");

                Destroy(nearbyWeaponPickup.gameObject);
                nearbyWeaponPickup = null;
                lastWeaponPickupTime = Time.time; // Apply cooldown after successful pickup
            }
            else
            {
                Debug.LogWarning("Tried to pick up weapon but WeaponData was null.", nearbyWeaponPickup.gameObject);
            }
        }
        else if (playerEquipment.CurrentWeapon != fistWeaponData)
        {   
            // --- Drop Current Weapon --- 
            DropWeapon(true); // Drop and apply cooldown
        }
        // else: Holding fists and no pickup nearby, do nothing.
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (isDead) return; // Don't process look if dead

        if (context.started || context.performed)
        {
            // Button is pressed or held
            isLooking = true;
        }
        else if (context.canceled)
        {
            // Button is released
            isLooking = false;
        }
    }

    // Actions
    void Shoot()
    {
        WeaponData currentWep = playerEquipment.CurrentWeapon;
        if (currentWep == null || currentWep.projectilePrefab == null || !currentWep.canShoot || !currentWep.HasAmmo())
        {
             Debug.LogWarning("Shoot called but current weapon data or prefab is missing/invalid or out of ammo.");
             return; // Cannot shoot if data is missing or out of ammo
        }

        // Use ammo
        if (!currentWep.UseAmmo())
        {
            // Couldn't use ammo (weapon empty)
            PlayEmptyClickSound();
            return;
        }

        // Record that a shot was fired (one shot = one trigger pull, regardless of pellet count)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RecordShotFired();
        }

        // Use data from WeaponData
        Vector3 spawnPosition = transform.position + (transform.up * currentWep.bulletOffset) + (transform.right * currentWep.bulletOffsetSide);
        
        // Handle shotgun pellets
        bool hasHitEnemy = false; // Track if any pellet hits an enemy
        
        // Number of pellets to fire (regular guns use 1)
        int pelletCount = Mathf.Max(1, currentWep.pelletCount);
        
        for (int i = 0; i < pelletCount; i++)
        {
            // Calculate spread angle for this pellet
            float angle = 0;
            
            // Apply weapon general spread (random inaccuracy)
            if (currentWep.spread > 0)
            {
                // Random deviation within the spread range
                angle += Random.Range(-currentWep.spread, currentWep.spread);
            }
            
            // Apply shotgun spread for multiple pellets
            if (currentWep.spreadAngle > 0 && pelletCount > 1)
            {
                // Distribute pellets evenly across the spread angle
                float angleStep = currentWep.spreadAngle / (pelletCount - 1);
                angle += -currentWep.spreadAngle / 2 + angleStep * i;
            }
            
            // Create the bullet with rotation adjusted for spread
            Quaternion pelletRotation = transform.rotation * Quaternion.Euler(0, 0, angle);
            GameObject bulletGO = Instantiate(currentWep.projectilePrefab, spawnPosition, pelletRotation);
            
            Bullet bulletScript = bulletGO.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                // Pass the player controller to allow tracking hits
                bulletScript.SetShooter(gameObject);
                
                // Pass weapon data parameters to the bullet
                bulletScript.SetBulletParameters(currentWep.bulletSpeed, currentWep.range);
                
                // Set shotgun flag to track only one hit per shot
                bulletScript.isShotgunPellet = pelletCount > 1;
                bulletScript.hasRecordedHit = hasHitEnemy;
                bulletScript.OnEnemyHit += () => hasHitEnemy = true;
            }
            
            Rigidbody2D bulletRb = bulletGO.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = bulletGO.transform.up * currentWep.bulletSpeed; // Use weapon's bullet speed
            }
        }

        // Play sound from WeaponData using the player's AudioSource
        if (playerAudioSource != null && currentWep.shootSound != null)
        {
            playerAudioSource.pitch = Random.Range(1.1f, 1.3f); // Keep random pitch
            playerAudioSource.PlayOneShot(currentWep.shootSound);
        }

        // Shake camera using WeaponData values
        ShakeCamera(currentWep.shootShakeDuration, currentWep.shootShakeMagnitude);
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        shakeMagnitude = magnitude; // Assign to the renamed variable
    }

    public void TakeDamage(int damage, Vector2 bulletDirection = default)
    {
        if (!isDead)
        {
            health -= damage;
            if (health <= 0)
            {
                Die(bulletDirection);
            }
        }
    }

    void Die(Vector2 bulletDirection = default)
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero; 
        transform.localScale = new Vector3(3.2f, 3.2f, 3.2f); 
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        
        // If we have a valid bullet direction, face that direction
        if (bulletDirection != default && bulletDirection != Vector2.zero)
        {
            // Invert the direction to face WHERE the bullet came FROM
            Vector2 sourceDirection = -bulletDirection;
            
            // Calculate the rotation to face the source of the bullet
            float angle = Mathf.Atan2(sourceDirection.y, sourceDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Apply force in the direction of the bullet's travel (away from source)
            rb.AddForce(bulletDirection * 1f, ForceMode2D.Impulse);
        }
        else
        {
            // Fallback to the original behavior if no bullet direction is provided
            Vector2 nudgeDirection = -transform.up;
            rb.AddForce(nudgeDirection * 1f, ForceMode2D.Impulse);
        }
        
        Invoke("StopAfterNudge", 0.1f);
        GetComponent<Collider2D>().enabled = false; 
    }

    void StopAfterNudge()
    {
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f; 
    }
    public bool IsDead()
    {
        return isDead;
    }

    // Collision Handling for Pickups
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collider belongs to the "Pickup" layer
        if (!isDead && collision.gameObject.layer == LayerMask.NameToLayer("Pickup"))
        {
            // Try to get the WeaponPickup component from the PARENT of the trigger object
            WeaponPickup pickup = collision.transform.parent.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                // If there's no current nearby weapon, set this one
                if (nearbyWeaponPickup == null)
                {
                    nearbyWeaponPickup = pickup;
                    Debug.Log($"Near weapon pickup: {pickup.weaponData?.weaponName ?? "Unknown"} (Triggered by: {collision.gameObject.name})");
                }
                else
                {
                    // Compare distances and pick the closer one
                    float currentDistance = Vector2.Distance(transform.position, nearbyWeaponPickup.transform.position);
                    float newDistance = Vector2.Distance(transform.position, pickup.transform.position);
                    
                    if (newDistance < currentDistance)
                    {
                        nearbyWeaponPickup = pickup;
                        Debug.Log($"Switched to closer weapon pickup: {pickup.weaponData?.weaponName ?? "Unknown"} (Triggered by: {collision.gameObject.name})");
                    }
                }
                // Optional: Add visual feedback
            }
            else
            {
                 Debug.LogWarning($"Object on Pickup layer entered trigger, but parent lacks WeaponPickup script! Trigger Object: {collision.gameObject.name}", collision.transform.parent.gameObject);
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // Check if the collider belongs to the "Pickup" layer
         if (!isDead && collision.gameObject.layer == LayerMask.NameToLayer("Pickup"))
        {
            // Check if the exiting trigger's parent matches the currently stored pickup
            if (nearbyWeaponPickup != null && collision.transform.parent == nearbyWeaponPickup.transform)
            {
                nearbyWeaponPickup = null;
                Debug.Log("Left weapon pickup area.");
                // Optional: Remove visual feedback
            }
        }
    }

    void DropWeapon(bool applyCooldown)
    {
        WeaponData weaponToDrop = playerEquipment.CurrentWeapon;

        // Check if it's a droppable weapon with a valid pickup prefab
        if (weaponToDrop != null && weaponToDrop != fistWeaponData && weaponToDrop.pickupPrefab != null)
        {            
            // Remember current ammo count
            int currentAmmo = weaponToDrop.currentAmmo;
            
            // Instantiate the pickup prefab slightly in front of the player
            Vector3 dropPosition = transform.position + transform.up * 0.5f; // Adjust offset as needed
            GameObject droppedItem = Instantiate(weaponToDrop.pickupPrefab, dropPosition, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            
            // Set the dropped weapon's ammo count
            WeaponPickup pickup = droppedItem.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                pickup.SetCurrentAmmo(currentAmmo);
            }
            
            // Apply Hotline Miami style throw physics using the public variable
            Rigidbody2D itemRb = droppedItem.GetComponent<Rigidbody2D>();
            if (itemRb != null)
            {
                itemRb.linearVelocity = transform.up * weaponThrowForce; // <-- USE THE VARIABLE HERE
                itemRb.angularVelocity = Random.Range(300f, 600f); // Rotation
            }
            else
            {
                Debug.LogWarning("Dropped weapon pickup prefab does not have a Rigidbody2D!", droppedItem);
            }

            Debug.Log($"Dropped {weaponToDrop.weaponName} with {currentAmmo}/{weaponToDrop.magazineSize} ammo");

            // Equip fists
            playerEquipment.EquipWeapon(fistWeaponData);
            UpdateLastFireTime(); // Update fire time for fists

            if (applyCooldown) 
            {
                 lastWeaponPickupTime = Time.time; // Apply cooldown only if this was the primary action
            }
        }
        else if (weaponToDrop != fistWeaponData) // Log error if trying to drop non-fist without prefab
        {
             Debug.LogWarning($"Cannot drop {weaponToDrop?.weaponName ?? "current weapon"}: Missing pickupPrefab in its WeaponData.", this);
        }
        // Do nothing if trying to drop fists
    }

    void UpdateLastFireTime()
    {
        // Reset lastFireTime based on the currently equipped weapon's fire rate
        if (playerEquipment.CurrentWeapon != null && playerEquipment.CurrentWeapon.fireRate > 0)
        {
            // Set last fire time far enough in the past to allow immediate firing if desired
            lastFireTime = Time.time - (1f / playerEquipment.CurrentWeapon.fireRate) - 0.01f; 
        }
        else
        {
            lastFireTime = Time.time; // Cannot fire immediately if rate is 0 or invalid
        }
    }

    // Play a sound for empty magazine
    void PlayEmptyClickSound()
    {
        if (playerAudioSource != null)
        {
            // You can add a specific empty click sound here
            playerAudioSource.pitch = 1.0f;
            // playerAudioSource.PlayOneShot(emptyClickSound);
            
            // For now, just log that the gun is empty
            Debug.Log("*Click* - Weapon is empty!");
        }
    }

    // Continuously update which weapon pickup is closest
    void UpdateClosestWeaponPickup()
    {
        // Find all nearby weapon pickups
        Collider2D[] pickupColliders = Physics2D.OverlapCircleAll(transform.position, 2.0f, LayerMask.GetMask("Pickup"));
        
        // If no pickups found, clear the reference
        if (pickupColliders.Length == 0)
        {
            nearbyWeaponPickup = null;
            return;
        }
        
        // Find the closest one
        WeaponPickup closestPickup = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in pickupColliders)
        {
            // Get the parent that has the WeaponPickup component
            WeaponPickup pickup = col.transform.parent.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                float distance = Vector2.Distance(transform.position, pickup.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPickup = pickup;
                }
            }
        }
        
        // Update the reference to the closest pickup
        nearbyWeaponPickup = closestPickup;
    }
}