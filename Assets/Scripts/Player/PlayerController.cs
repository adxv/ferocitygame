using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using System.Collections; // Added for Coroutines
using UnityEngine.UI;
using System;
using System.Collections.Generic;

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
    private Coroutine attackAnimationCoroutine; // To manage the attack sprite change

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
    private UIManager uiManager; // ADDED: Reference to the UIManager

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

        // Initialize fists if no weapon is explicitly equipped
        if (playerEquipment.CurrentWeapon == null)
        {
            playerEquipment.EquipWeapon(fistWeaponData);
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
        
        // Initialize lastFireTime based on the starting weapon's fire rate
        if (playerEquipment.CurrentWeapon != null)
        {
             lastFireTime = -playerEquipment.CurrentWeapon.fireRate; // Initialize negative for immediate first action
        }
        else
        {
            lastFireTime = -1f; // Default if no weapon somehow (shouldn't happen now)
        }

        timerController = FindFirstObjectByType<TimerController>();
        uiManager = UIManager.Instance; // ADDED: Get UIManager instance
        if(uiManager == null) // ADDED: Null check for UIManager
        {
             Debug.LogWarning("PlayerController could not find the UIManager instance.", this);
        }
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
            
            // Only update rotation if the game is not paused
            if (Time.timeScale != 0f)
            {
                transform.up = direction;
            }

            // Check if current weapon can act (shoot or melee) and if fire rate allows
            if (playerEquipment.CurrentWeapon != null &&
                Time.time >= lastFireTime + (1f / playerEquipment.CurrentWeapon.fireRate))
            {
                if (shouldShoot) // Only proceed if input is active
                {
                    if (playerEquipment.CurrentWeapon.isMelee)
                    {
                        MeleeAttack();
                        lastFireTime = Time.time;
                        // Reset flag for semi-auto melee (single click = single punch)
                        if (!playerEquipment.CurrentWeapon.isFullAuto)
                        {
                            shouldShoot = false;
                        }
                    }
                    else if (playerEquipment.CurrentWeapon.canShoot)
                    {
                        if (playerEquipment.CurrentWeapon.HasAmmo())
                        {
                            Shoot();
                            lastFireTime = Time.time;
                            // Reset flag for semi-auto guns
                            if (!playerEquipment.CurrentWeapon.isFullAuto)
                            {
                                shouldShoot = false;
                            }
                        }
                        else
                        {
                            // Play empty click sound if gun is out of ammo
                            PlayEmptyClickSound();
                            shouldShoot = false; // Reset shooting flag to prevent continuous clicks
                        }
                    }
                    else
                    {
                        // If weapon cannot shoot (and isn't melee), just clear the flag
                        shouldShoot = false;
                    }
                }
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
        else // ADDED: Ensure dead player doesn't move
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
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
        if (isDead || playerEquipment.CurrentWeapon == null)
        {
             shouldShoot = false; // Ensure cannot act if dead or unarmed
             return;
        }

        WeaponData currentWep = playerEquipment.CurrentWeapon;

        // Handle both melee and ranged weapons based on input context
        if (currentWep.isFullAuto)
        {
            // For full-auto, set flag based on button held state
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
            // For semi-auto, only act on the initial press (performed)
            if (context.performed)
            {
                shouldShoot = true; // Will be reset after one action in Update()
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
                else
                {
                    // If we were holding fists and are picking up a weapon,
                    // make sure the sprite is correct (in case attack animation was interrupted)
                    if(attackAnimationCoroutine != null) StopCoroutine(attackAnimationCoroutine);
                    playerEquipment.UpdateSpriteToCurrentWeapon(); // Ensure correct sprite
                }

                // Equip the new weapon and set its ammo
                playerEquipment.EquipWeapon(weaponToPickup);
                
                // Set the weapon's current ammo from the pickup
                if (currentAmmo >= 0) // -1 indicates it wasn't explicitly set
                {
                    weaponToPickup.currentAmmo = currentAmmo;
                }
                
                UpdateLastFireTime(); // Update fire time for the new weapon

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
            // --- Drop Weapon (when not near a pickup) ---
            DropWeapon(true); // Apply cooldown when dropping manually
            // Equip fists after dropping
            playerEquipment.EquipWeapon(fistWeaponData);
            UpdateLastFireTime();
            lastWeaponPickupTime = Time.time; // Apply cooldown after dropping
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
        
        // Spawn muzzle flash if available
        if (currentWep.muzzleFlashPrefab != null)
        {
            // Instantiate muzzle flash at the same position as the bullet spawn
            GameObject muzzleFlash = Instantiate(currentWep.muzzleFlashPrefab, spawnPosition, transform.rotation);
            
            // Set the muzzle flash to automatically destroy after duration
            Destroy(muzzleFlash, currentWep.muzzleFlashDuration);
            
            // Parent muzzle flash to player if needed (uncomment if you want the flash to move with player)
            // muzzleFlash.transform.parent = transform;
        }
        
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

        // Notify PlayerEquipment that a weapon was fired (for sound detection)
        playerEquipment.Shoot();

        // Play sound from WeaponData using the player's AudioSource
        if (playerAudioSource != null && currentWep.shootSound != null)
        {
            playerAudioSource.pitch = Random.Range(1.1f, 1.3f); // Keep random pitch
            playerAudioSource.PlayOneShot(currentWep.shootSound);
        }

        // Shake camera using WeaponData values
        ShakeCamera(currentWep.shootShakeDuration, currentWep.shootShakeMagnitude);

        // Make sure to stop any attack animation if shooting a gun
        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            playerEquipment.UpdateSpriteToCurrentWeapon(); // Reset sprite immediately
            attackAnimationCoroutine = null;
        }
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

    public void Die(Vector2 bulletDirection = default)
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        movementInput = Vector2.zero;
        shouldShoot = false; // Stop shooting/attacking

        // ADDED: Reset the ammo display but keep it enabled
        AmmoDisplay ammoDisplay = FindObjectOfType<AmmoDisplay>();
        if (ammoDisplay != null)
        {
            ammoDisplay.ResetDisplay();
        }

        // ADDED: Stop timer and show Game Over UI
        if (timerController != null) 
        {
            timerController.StopTimer();
        }
        if (uiManager != null)
        {
             uiManager.ShowGameOver();
        }

        // Stop attack animation if player dies mid-punch
        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }

        // Set scale via transform, not Rigidbody2D
        transform.localScale = new Vector3(3.4f, 3.4f, 3.4f); 
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        GetComponent<Collider2D>().enabled = false; // Disable collider on death
          
        // Nudge the player back
        if (bulletDirection != default)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // Keep dynamic for nudge force
            rb.AddForce(-bulletDirection * 50f, ForceMode2D.Impulse);
            Invoke(nameof(StopAfterNudge), 0.2f);
        }
        else // If no nudge, make static immediately
        {
            rb.bodyType = RigidbodyType2D.Static; 
        }
    }

    void StopAfterNudge()
    {
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f; 
        rb.bodyType = RigidbodyType2D.Static; // Set to Static after nudge
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
                }
                else
                {
                    // Compare distances and pick the closer one
                    float currentDistance = Vector2.Distance(transform.position, nearbyWeaponPickup.transform.position);
                    float newDistance = Vector2.Distance(transform.position, pickup.transform.position);
                    
                    if (newDistance < currentDistance)
                    {
                        nearbyWeaponPickup = pickup;
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
                // Optional: Remove visual feedback
            }
        }
    }

    void DropWeapon(bool applyCooldown)
    {
        WeaponData currentWep = playerEquipment.CurrentWeapon;
        if (currentWep != null && currentWep != fistWeaponData && currentWep.pickupPrefab != null)
        {
            // Stop any attack animation before dropping
            if (attackAnimationCoroutine != null)
            {
                StopCoroutine(attackAnimationCoroutine);
                attackAnimationCoroutine = null;
                // No need to reset sprite here, EquipWeapon(fistWeaponData) will handle it
            }

            // Remember current ammo count
            int currentAmmo = currentWep.currentAmmo;
            
            // Instantiate the pickup prefab slightly in front of the player
            Vector3 dropPosition = transform.position + transform.up * 0.5f; // Adjust offset as needed
            GameObject droppedItem = Instantiate(currentWep.pickupPrefab, dropPosition, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            
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

            // Equip fists
            playerEquipment.EquipWeapon(fistWeaponData);
            UpdateLastFireTime(); // Update fire time for fists

            if (applyCooldown) 
            {
                 lastWeaponPickupTime = Time.time; // Apply cooldown only if this was the primary action
            }
        }
        else if (currentWep != fistWeaponData) // Log error if trying to drop non-fist without prefab
        {
             Debug.LogWarning($"Cannot drop {currentWep?.weaponName ?? "current weapon"}: Missing pickupPrefab in its WeaponData.", this);
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
            
            // Removed Debug.Log about empty weapon
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

    // --- MELEE ATTACK LOGIC --- (New Method)
    void MeleeAttack()
    {
        WeaponData meleeWeapon = playerEquipment.CurrentWeapon;
        if (!meleeWeapon.isMelee) return; // Safety check

        // 1. Trigger Animation/Sprite Change
        if (attackAnimationCoroutine != null) StopCoroutine(attackAnimationCoroutine);
        attackAnimationCoroutine = StartCoroutine(AttackAnimation(meleeWeapon));

        // 2. Perform Hit Detection (e.g., OverlapCircle)
        Vector2 attackOrigin = (Vector2)transform.position + (Vector2)transform.up * meleeWeapon.bulletOffset; // Use bulletOffset for attack origin
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, meleeWeapon.range, LayerMask.GetMask("Enemy")); // Use range for attack radius, check only Enemy layer

        bool didHit = false;
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && !enemy.isDead) // Make sure we hit an enemy script and it's not already dead
            {
                // Calculate direction for potential knockback/effects
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;

                // Apply damage to the enemy
                enemy.TakeDamage(meleeWeapon.damage); // Use weapon's damage value
                didHit = true;
                // Potentially add knockback here
            }
        }

        // 3. Play Sound
        if (didHit)
        {
            if (playerAudioSource != null && meleeWeapon.hitSound != null)
            {
                playerAudioSource.pitch = Random.Range(1f, 1.3f); // Keep random pitch
                playerAudioSource.PlayOneShot(meleeWeapon.hitSound);
            }
        }
        else
        {
            if (playerAudioSource != null && meleeWeapon.missSound != null)
            {
                playerAudioSource.pitch = Random.Range(0.8f, 1.1f); // Keep random pitch
                playerAudioSource.PlayOneShot(meleeWeapon.missSound);
            }
        }

        // 4. Camera Shake (Optional)
        ShakeCamera(meleeWeapon.shootShakeDuration, meleeWeapon.shootShakeMagnitude);
    }

    IEnumerator AttackAnimation(WeaponData weapon)
    {
        if (weapon.attackSprite != null)
        {
            // Choose which sprite to use
            Sprite spriteToUse = weapon.attackSprite;
            
            // If alternating sprites is enabled and second sprite exists, randomly choose
            if (weapon.useAlternatingSprites && weapon.attackSprite2 != null)
            {
                spriteToUse = (Random.value < 0.5f) ? weapon.attackSprite : weapon.attackSprite2;
            }
            
            playerEquipment.SetSprite(spriteToUse);
            yield return new WaitForSeconds(weapon.attackDuration);
            // Ensure we revert to the correct sprite *for the currently equipped weapon*
            // This handles cases where the weapon might change during the animation
            playerEquipment.UpdateSpriteToCurrentWeapon();
        }
        attackAnimationCoroutine = null; // Clear the coroutine reference
    }
}