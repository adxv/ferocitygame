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

        timerController = FindObjectOfType<TimerController>();
    }

    void Update()
    {
        if (!isDead)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0f;
            Vector3 direction = (mousePos - transform.position).normalized;
            transform.up = direction;

            // Check if current weapon can shoot and if fire rate allows
            if (playerEquipment.CurrentWeapon != null && playerEquipment.CurrentWeapon.canShoot && 
                shouldShoot && Time.time >= lastFireTime + (1f / playerEquipment.CurrentWeapon.fireRate)) // Use 1/fireRate for delay
            {
                Shoot();
                lastFireTime = Time.time;
                shouldShoot = false; // Reset shooting flag after a shot
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
        if (!isDead && playerEquipment.CurrentWeapon != null && playerEquipment.CurrentWeapon.canShoot)
        {
            if (context.performed) shouldShoot = true;
            // Optional: Handle held input for automatic weapons
            // else if (context.canceled) shouldShoot = false; 
        }
        // If weapon cannot shoot, ensure flag is false
        else if (context.canceled || (playerEquipment.CurrentWeapon != null && !playerEquipment.CurrentWeapon.canShoot))
        {
            shouldShoot = false;
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
                // If currently holding a weapon (not fists), drop it first
                if (playerEquipment.CurrentWeapon != fistWeaponData)
                {
                     DropWeapon(false); // Drop without applying cooldown again
                }

                // Equip the new weapon
                playerEquipment.EquipWeapon(nearbyWeaponPickup.weaponData);
                UpdateLastFireTime(); // Update fire time for the new weapon
                Debug.Log($"Picked up {nearbyWeaponPickup.weaponData.weaponName}");

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
        if (!isDead)
        {
            isLooking = context.performed;
        }
    }

    // Actions
    void Shoot()
    {
        WeaponData currentWep = playerEquipment.CurrentWeapon;
        if (currentWep == null || currentWep.projectilePrefab == null || !currentWep.canShoot)
        {
             Debug.LogWarning("Shoot called but current weapon data or prefab is missing/invalid.");
             return; // Cannot shoot if data is missing
        }

        // Use data from WeaponData
        Vector3 spawnPosition = transform.position + (transform.up * currentWep.bulletOffset) + (transform.right * currentWep.bulletOffsetSide);
        GameObject bulletGO = Instantiate(currentWep.projectilePrefab, spawnPosition, transform.rotation);
        
        Bullet bulletScript = bulletGO.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetShooter(gameObject);
            // Optionally set damage here if bullet doesn't handle it:
            // bulletScript.damage = currentWep.damage; 
        }
        
        Rigidbody2D bulletRb = bulletGO.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = transform.up * 50f; // Keep bullet speed for now, could add to WeaponData
        }

        // Play sound from WeaponData using the player's AudioSource
        if (playerAudioSource != null && currentWep.shootSound != null)
        {
            playerAudioSource.pitch = Random.Range(1.1f, 1.3f); // Keep random pitch
            playerAudioSource.PlayOneShot(currentWep.shootSound);
        }

        // Shake camera using WeaponData values
        ShakeCamera(currentWep.shootShakeDuration, currentWep.shootShakeMagnitude);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RecordShotFired();
        }
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        shakeMagnitude = magnitude; // Assign to the renamed variable
    }

    public void TakeDamage(int damage)
    {
        if (!isDead)
        {
            health -= damage;
            if (health <= 0)
            {
                Die();
            }
        }
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero; 
        transform.localScale = new Vector3(3.2f, 3.2f, 3.2f); 
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        Vector2 nudgeDirection = -transform.up; 
        rb.AddForce(nudgeDirection * 1f, ForceMode2D.Impulse); 
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
        if (!isDead && collision.CompareTag("WeaponPickup")) // Make sure your pickup prefabs have this tag
        {
            WeaponPickup pickup = collision.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                nearbyWeaponPickup = pickup;
                Debug.Log($"Near weapon pickup: {pickup.weaponData?.weaponName ?? "Unknown"}");
                // Optional: Add visual feedback (highlight, UI prompt)
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!isDead && collision.CompareTag("WeaponPickup"))
        {
            WeaponPickup pickup = collision.GetComponent<WeaponPickup>();
            // Check if the object we are exiting is the one we currently have stored
            if (pickup != null && pickup == nearbyWeaponPickup)
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
            // Instantiate the pickup prefab slightly in front of the player
            Vector3 dropPosition = transform.position + transform.up * 0.5f; // Adjust offset as needed
            GameObject droppedItem = Instantiate(weaponToDrop.pickupPrefab, dropPosition, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            
            // Apply Hotline Miami style throw physics (from user's previous code)
            Rigidbody2D itemRb = droppedItem.GetComponent<Rigidbody2D>();
            if (itemRb != null)
            {
                // Apply Hotline Miami style throw physics (from user's previous code)
                itemRb.linearVelocity = transform.up * 130f; // Strong forward nudge
                itemRb.angularVelocity = Random.Range(300f, 600f); // Rotation
            }
            else
            {
                Debug.LogWarning("Dropped weapon pickup prefab does not have a Rigidbody2D!", droppedItem);
            }

            Debug.Log($"Dropped {weaponToDrop.weaponName}");

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
}