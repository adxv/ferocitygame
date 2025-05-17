using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // Player Attributes
    public float moveSpeed;
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private PlayerEquipment playerEquipment;
    public int health = 1;
    private bool isDead = false;
    public Sprite deathSprite;

    // Shooting State
    private float lastFireTime;
    private bool shouldShoot;       //workaround for for consistent firerate with InputSystem
    private float lastWeaponPickupTime = 0f;
    private float weaponActionCooldown = 0.5f;
    private Coroutine attackAnimationCoroutine;

    // Camera
    public Camera mainCamera;
    public float cameraSmoothSpeed = 0.05f;
    private float shakeTimeRemaining;
    private float shakeMagnitude;
    public float normalOffsetFactor = 0.2f;
    public float shiftOffsetFactor = 0.6f;
    public float normalMaxOffset = 2f;
    public float shiftMaxOffset = 4f;
    private bool isLooking;

    // Timer
    private TimerController timerController;
    private bool hasMovedOnce = false;
    private UIManager uiManager; 

    // Audio Source
    public AudioSource playerAudioSource;

    // Pickup State
    private WeaponPickup nearbyWeaponPickup; //tracks the closest pickup
    private WeaponData fistWeaponData;

    // Weapon Handling
    public float weaponThrowForce = 130f;

    // Walk anim
    [SerializeField] private Animator legsAnimator;
    [SerializeField] private Transform legsTransform;
    private static readonly int IsMoving = Animator.StringToHash("isMoving");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerEquipment = GetComponent<PlayerEquipment>();
        fistWeaponData = playerEquipment.FistWeaponData;
        if (fistWeaponData == null)
        {
             Debug.LogError("FistWeaponData missing");
        }

        if (playerEquipment.CurrentWeapon == null) //equip fists if no weapon is equipped
        {
            playerEquipment.EquipWeapon(fistWeaponData);
        }

        if (playerAudioSource == null) 
        {
            playerAudioSource = GetComponent<AudioSource>();
            if(playerAudioSource == null)
            {
                Debug.LogWarning("no audiosource on player");
            }
        }

        if (mainCamera == null) mainCamera = Camera.main;
        
        if (playerEquipment.CurrentWeapon != null)
        {
             lastFireTime = -playerEquipment.CurrentWeapon.fireRate; //immediately allow firing
        }
        else
        {
            lastFireTime = -1f; // no weapon
        }

        timerController = FindFirstObjectByType<TimerController>();
        uiManager = UIManager.Instance; 
        if(uiManager == null)
        {
             Debug.LogWarning("could not find the UIManager");
        }
        
        ApplyPowerUps();
    }

    public static void ResetStaticVariables()
    {
        FloorAccessController.isLevelComplete = false;
    }

    void Update()
    {
        if (!isDead)
        {
            UpdateClosestWeaponPickup();
            
            // rotation based on mouse pos
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0f;
            Vector3 direction = (mousePos - transform.position).normalized;
            
            if (Time.timeScale != 0f)
            {
                transform.up = direction;
            } //dont rotate if paused

            // check if current weapon can shoot and is ready to shoot
            if (playerEquipment.CurrentWeapon != null &&
                Time.time >= lastFireTime + (1f / playerEquipment.CurrentWeapon.fireRate))
            {
                if (shouldShoot)
                {
                    if (playerEquipment.CurrentWeapon.isMelee)
                    {
                        MeleeAttack();
                        lastFireTime = Time.time;
                        // melee semi auto
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
                            // semi auto non melee
                            if (!playerEquipment.CurrentWeapon.isFullAuto)
                            {
                                shouldShoot = false;
                            }
                        }
                        else
                        {
                            //out of ammo
                            PlayEmptyClickSound();
                            shouldShoot = false; // Reset shooting flag to prevent continuous clicks
                        }
                    }
                    else
                    {
                        //cant shoot and not melee
                        shouldShoot = false;
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        //camera update
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
            shakeOffset = Random.insideUnitSphere * shakeMagnitude; //shake
            shakeOffset.z = 0f;
            shakeTimeRemaining -= Time.fixedDeltaTime;
        }
        mainCamera.transform.position = smoothedPos + shakeOffset;
        
        if (!isDead)
        {
            rb.linearVelocity = movementInput * moveSpeed;
            
        if (legsAnimator != null)
        {
            bool isMoving = movementInput.sqrMagnitude > 0.01f;
            legsAnimator.SetBool(IsMoving, isMoving);
            
            // Rotate legs to face movement direction (only if moving)
            if (isMoving && legsTransform != null)
            {
                // Calculate legs rotation based on movement input
                float angle = Mathf.Atan2(movementInput.y, movementInput.x) * Mathf.Rad2Deg;
                // Subtract 90 degrees to align with Unity's rotation system (up is 0 degrees)
                angle -= 90f;
                
                // Apply rotation to legs only
                legsTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        }
        else 
        {
            //dead, stop movement and rotation
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Make sure legs stop animating when dead
            if (legsAnimator != null)
            {
                legsAnimator.SetBool(IsMoving, false);
            }
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
             shouldShoot = false; // disable shooting if dead or no weapon
             return;
        }

        WeaponData currentWep = playerEquipment.CurrentWeapon;

        if (currentWep.isFullAuto)
        {
            // full auto
            if (context.performed || context.started) // button held
            {
                shouldShoot = true;
            }
            else if (context.canceled) // released
            {
                shouldShoot = false;
            }
        }
        else
        {
            // semi auto
            if (context.performed) // button pressed
            {
                shouldShoot = true; //resets after shooting
            }
            else if (context.canceled)
            {
                 shouldShoot = false;
            }
        }
    }

    public void OnWeaponInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || isDead || Time.time < lastWeaponPickupTime + weaponActionCooldown) return;

        if (nearbyWeaponPickup != null)
        {
            // pick up weapon
            if (nearbyWeaponPickup.weaponData != null)
            {
                // store data and ammo count
                WeaponData weaponToPickup = nearbyWeaponPickup.weaponData;
                int currentAmmo = nearbyWeaponPickup.GetCurrentAmmo();
                
                // if currently holding a weapon, drop it first
                if (playerEquipment.CurrentWeapon != fistWeaponData)
                {
                     DropWeapon(false);
                }
                else
                {
                    if(attackAnimationCoroutine != null) StopCoroutine(attackAnimationCoroutine); // stop attack animation
                    playerEquipment.UpdateSpriteToCurrentWeapon();
                }

                playerEquipment.EquipWeapon(weaponToPickup);
                
                if (currentAmmo >= 0)
                {
                    weaponToPickup.currentAmmo = currentAmmo;
                }
                
                ProcessWeaponPickup(weaponToPickup);
                
                UpdateLastFireTime();

                Destroy(nearbyWeaponPickup.gameObject);
                nearbyWeaponPickup = null;
                lastWeaponPickupTime = Time.time;
            }
            else
            {
                Debug.LogWarning("WeaponData was null");
            }
        }
        else if (playerEquipment.CurrentWeapon != fistWeaponData)
        {
            DropWeapon(true); // true, apply cooldown
            playerEquipment.EquipWeapon(fistWeaponData);
            UpdateLastFireTime();
            lastWeaponPickupTime = Time.time;
        }
        //else: do nothing
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (isDead) return; // do nothing if dead

        if (context.started || context.performed)
        {
            // pressed or held
            isLooking = true;
        }
        else if (context.canceled)
        {
            // released
            isLooking = false;
        }
    }

    // Actions
    void Shoot()
    {
        WeaponData currentWep = playerEquipment.CurrentWeapon;
        if (currentWep == null || currentWep.projectilePrefab == null || !currentWep.canShoot || !currentWep.HasAmmo())
        {
             Debug.LogWarning("missing current weapon data or out of ammo.");
             return;
        }

        if (!currentWep.UseAmmo())
        {
            //no ammo
            PlayEmptyClickSound();
            return;
        }
        //shots fired++
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RecordShotFired();
        }

        // bullet and muzzle flash spawn position
        Vector3 spawnPosition = transform.position + (transform.up * currentWep.bulletOffset) + (transform.right * currentWep.bulletOffsetSide);
        
        // spawn muzzle flash
        if (currentWep.muzzleFlashPrefab != null)
        {
            GameObject muzzleFlash = Instantiate(currentWep.muzzleFlashPrefab, spawnPosition, transform.rotation);
            //destroy after duration
            Destroy(muzzleFlash, currentWep.muzzleFlashDuration);
        }
        
        float spreadActual = currentWep.spread;
        try {
            spreadActual = currentWep.spread * PowerUpManager.AccuracyMultiplier;
        } catch (System.Exception) {
            Debug.Log("default accuracy");
        }
        
        int pelletCount = Mathf.Max(1, currentWep.pelletCount);
        
        // create a shared hit state reference for shotgun pellets
        bool[] sharedHitState = new bool[1] { false };
        
        // generate a unique ID for this shotgun blast to ensure pellets share state
        string shotgunBlastID = System.Guid.NewGuid().ToString();
        
        for (int i = 0; i < pelletCount; i++)
        {
            float angle = 0;
            
            //weapon spread
            if (spreadActual > 0)
            {
                angle += Random.Range(-spreadActual, spreadActual);
            }
            
            if (currentWep.spreadAngle > 0 && pelletCount > 1)
            {
                // distribute pellets evenly across the spread angle
                float angleStep = currentWep.spreadAngle / (pelletCount - 1);
                angle += -currentWep.spreadAngle / 2 + angleStep * i;
            }
            
            // create the bullet with rotation adjusted for spread
            Quaternion pelletRotation = transform.rotation * Quaternion.Euler(0, 0, angle);
            GameObject bulletGO = Instantiate(currentWep.projectilePrefab, spawnPosition, pelletRotation);
            
            Bullet bulletScript = bulletGO.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetShooter(gameObject);
                
                // pass weapon data to bullet
                bulletScript.SetBulletParameters(currentWep.bulletSpeed, currentWep.range);
                bulletScript.SetDamage(currentWep.damage);
                bulletScript.SetWeaponData(currentWep);
                
                // Set shotgun flag to track only one hit per shot
                bulletScript.isShotgunPellet = pelletCount > 1;
                if (pelletCount > 1)
                {
                    // For shotgun pellets, use the same ID to track them as a group
                    bulletScript.shotgunBlastID = shotgunBlastID;
                }
            }
            
            Rigidbody2D bulletRb = bulletGO.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = bulletGO.transform.up * currentWep.bulletSpeed; // send the bullet
            }
        }

        playerEquipment.Shoot(); //for sound detection

        if (playerAudioSource != null && currentWep.shootSound != null)
        {
            playerAudioSource.pitch = Random.Range(1.1f, 1.3f);
            playerAudioSource.PlayOneShot(currentWep.shootSound);
        }

        ShakeCamera(currentWep.shootShakeDuration, currentWep.shootShakeMagnitude);

        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            playerEquipment.UpdateSpriteToCurrentWeapon();
            attackAnimationCoroutine = null;
        }
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        shakeMagnitude = magnitude;
    }

    public void TakeDamage(int damage, Vector2 bulletDirection = default)
    {
        if (!isDead)
        {
            health -= damage;
            
            
            if (bulletDirection != default)
            {
                GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"), 
                    transform.position, Quaternion.LookRotation(Vector3.forward, bulletDirection)); //blood particle
            }
            else
            {
                GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"), // no direction set
                    transform.position, Quaternion.identity);
            }
            
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
        shouldShoot = false;


        AmmoDisplay ammoDisplay = FindFirstObjectByType<AmmoDisplay>();
        if (ammoDisplay != null)
        {
            ammoDisplay.ResetDisplay();
        }

        if (timerController != null) 
        {
            timerController.StopTimer();
        }
        if (uiManager != null)
        {
             uiManager.ShowGameOver();
        }

        // stop attack animation if player dies
        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }
        transform.localScale = new Vector3(3.4f, 3.4f, 3.4f); 
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        GetComponent<Collider2D>().enabled = false; // disable collider on death
          
        // nudge backwards
        if (bulletDirection != default)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.AddForce(-bulletDirection * 50f, ForceMode2D.Impulse);
            Invoke(nameof(StopAfterNudge), 0.2f); //stop after 0.2
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Static; 
        }
    }

    void StopAfterNudge()
    {
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f; 
        rb.bodyType = RigidbodyType2D.Static;
    }
    public bool IsDead()
    {
        return isDead;
    }

    //pickup collision handling
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDead && collision.gameObject.layer == LayerMask.NameToLayer("Pickup"))
        {
            // get the WeaponPickup component from the PARENT of the trigger object
            WeaponPickup pickup = collision.transform.parent.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                // if there's no current nearby weapon, set this one
                if (nearbyWeaponPickup == null)
                {
                    nearbyWeaponPickup = pickup;
                }
                else
                {
                    // compare distances and pick the closer one
                    float currentDistance = Vector2.Distance(transform.position, nearbyWeaponPickup.transform.position);
                    float newDistance = Vector2.Distance(transform.position, pickup.transform.position);
                    
                    if (newDistance < currentDistance)
                    {
                        nearbyWeaponPickup = pickup;
                    }
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
         if (!isDead && collision.gameObject.layer == LayerMask.NameToLayer("Pickup"))
        {
            if (nearbyWeaponPickup != null && collision.transform.parent == nearbyWeaponPickup.transform)
            {
                nearbyWeaponPickup = null;
            }
        }
    }

    void DropWeapon(bool applyCooldown)
    {
        WeaponData currentWep = playerEquipment.CurrentWeapon;
        if (currentWep != null && currentWep != fistWeaponData && currentWep.pickupPrefab != null)
        {
            // stop attack animation
            if (attackAnimationCoroutine != null)
            {
                StopCoroutine(attackAnimationCoroutine);
                attackAnimationCoroutine = null;
            }

            int currentAmmo = currentWep.currentAmmo;
            
            // spawn pickup prefab
            Vector3 dropPosition = transform.position + transform.up * 0.5f;
            GameObject droppedItem = Instantiate(currentWep.pickupPrefab, dropPosition, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            
            // set the dropped weapon's ammo count
            WeaponPickup pickup = droppedItem.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                pickup.SetCurrentAmmo(currentAmmo);
            }
            
            // throw physics
            Rigidbody2D itemRb = droppedItem.GetComponent<Rigidbody2D>();
            if (itemRb != null)
            {
                itemRb.linearVelocity = transform.up * weaponThrowForce;
                itemRb.angularVelocity = Random.Range(300f, 600f); // random rotation
            }

            // equip fists
            playerEquipment.EquipWeapon(fistWeaponData);
            UpdateLastFireTime();

            if (applyCooldown) 
            {
                 lastWeaponPickupTime = Time.time;
            }
        }
        else if (currentWep != fistWeaponData)
        {
             Debug.LogWarning("cannot drop: missing pickupPrefab");
        }
        // else: do nothing
    }

    void UpdateLastFireTime()
    {
        // reset lastFireTime based on weapon's fire rate
        if (playerEquipment.CurrentWeapon != null && playerEquipment.CurrentWeapon.fireRate > 0)
        {
            // set last fire time far enough in the past to allow immediate firing
            lastFireTime = Time.time - (1f / playerEquipment.CurrentWeapon.fireRate) - 0.01f; 
        }
        else
        {
            lastFireTime = Time.time; //invalid fire rate
        }
    }

    void PlayEmptyClickSound()
    {
        if (playerAudioSource != null)
        {
            WeaponData currentWep = playerEquipment.CurrentWeapon;
            if (currentWep != null && currentWep.emptyClickSound != null)
            {
                playerAudioSource.pitch = 1.0f;
                playerAudioSource.PlayOneShot(currentWep.emptyClickSound);
            }
        }
    }

    void UpdateClosestWeaponPickup()
    {
        // get all nearby weapon pickups
        Collider2D[] pickupColliders = Physics2D.OverlapCircleAll(transform.position, 2.0f, LayerMask.GetMask("Pickup"));
        
        // no pickups nearby
        if (pickupColliders.Length == 0)
        {
            nearbyWeaponPickup = null;
            return;
        }
        
        WeaponPickup closestPickup = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in pickupColliders)
        {
            WeaponPickup pickup = col.transform.parent.GetComponent<WeaponPickup>(); // get the parent pickup with the WeaponPickup component
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
        nearbyWeaponPickup = closestPickup;
    }

    void MeleeAttack()
    {
        WeaponData meleeWeapon = playerEquipment.CurrentWeapon;
        if (!meleeWeapon.isMelee) return;

        // trigger animation
        if (attackAnimationCoroutine != null) StopCoroutine(attackAnimationCoroutine);
        attackAnimationCoroutine = StartCoroutine(AttackAnimation(meleeWeapon));

        // hit detection
        Vector2 attackOrigin = (Vector2)transform.position + (Vector2)transform.up;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, meleeWeapon.range, LayerMask.GetMask("Enemy")); // range = attack radius, check only Enemy layer

        bool didHit = false;
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && !enemy.isDead)
            {
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;

                // blood particle
                GameObject bloodEffect = Instantiate(Resources.Load<GameObject>("Particles/Blood"),
                    enemy.transform.position, Quaternion.LookRotation(Vector3.forward, directionToEnemy));

                enemy.TakeDamage(meleeWeapon.damage);
                didHit = true;
            }
        }
        // play hitsound
        if (didHit)
        {
            if (playerAudioSource != null && meleeWeapon.hitSound != null)
            {
                playerAudioSource.pitch = Random.Range(1f, 1.3f);
                playerAudioSource.PlayOneShot(meleeWeapon.hitSound);
            }
        }
        //play miss sound
        else
        {
            if (playerAudioSource != null && meleeWeapon.missSound != null)
            {
                playerAudioSource.pitch = Random.Range(0.8f, 1.1f);
                playerAudioSource.PlayOneShot(meleeWeapon.missSound);
            }
        }
        ShakeCamera(meleeWeapon.shootShakeDuration, meleeWeapon.shootShakeMagnitude);
    }

    IEnumerator AttackAnimation(WeaponData weapon)
    {
        if (weapon.attackSprite != null)
        {
            Sprite spriteToUse = weapon.attackSprite;
            
            if (weapon.useAlternatingSprites && weapon.attackSprite2 != null)
            {
                spriteToUse = (Random.value < 0.5f) ? weapon.attackSprite : weapon.attackSprite2;
            }
            
            playerEquipment.SetSprite(spriteToUse);
            yield return new WaitForSeconds(weapon.attackDuration);
            playerEquipment.UpdateSpriteToCurrentWeapon();
        }
        attackAnimationCoroutine = null;
    }

    private void ApplyPowerUps()
    {
        // 2 lives
        if (PowerUpManager.HasDoubleHealth)
        {
            health = 2;
        }
        
        // speed boost
        moveSpeed *= PowerUpManager.MovementSpeedMultiplier;
        
        // double ammoo
        if (PowerUpManager.HasDoubleAmmo && playerEquipment.CurrentWeapon != null)
        {
            ProcessWeaponPickup(playerEquipment.CurrentWeapon);
        }
    }
    
    //apply double ammo
    private void ProcessWeaponPickup(WeaponData weaponData)
    {
        if (PowerUpManager.HasDoubleAmmo && !weaponData.isMelee && weaponData.magazineSize > 0)
        {
            weaponData.currentAmmo = weaponData.magazineSize * 2;
        }
    }
}