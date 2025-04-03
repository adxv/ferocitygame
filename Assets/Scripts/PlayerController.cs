using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    // Player Attributes
    public float moveSpeed;
    private Vector2 movementInput;
    private Rigidbody2D rb;
    public int health = 1; //temporarily public for testing
    private bool isDead = false;
    public Sprite deathSprite;

    // Shooting
    public GameObject bulletPrefab;
    public float bulletOffset;
    public float bulletOffsetSide;
    public float fireRate;
    private float lastFireTime;
    private bool shouldShoot;       //workaround for for consistent firerate with InputSystem


    // Gun Handling
    private bool weaponEquipped = false;
    private GameObject nearbyGun;
    private float lastWeaponPickupTime = 0f;
    private float weaponActionCooldown = 0.5f;

    // Weapon Prefabs
    public GameObject pistolPrefab;
    public GameObject shotgunPrefab;
    public GameObject riflePrefab;
    public GameObject smgPrefab;
    public GameObject knifePrefab;

    // Equipped Weapon Sprites
    public GameObject pistolSprite;
    public GameObject shotgunSprite;
    public GameObject rifleSprite;
    public GameObject smgSprite;
    public GameObject knifeSprite;

    // Camera
    public Camera mainCamera;
    public float cameraSmoothSpeed = 0.05f;
    public float shootShakeDuration = 0.05f;
    public float shootShakemagnitude = 0.05f;
    private float shakeTimeRemaining;
    public float normalOffsetFactor = 0.2f;  // Normal offset towards cursor
    public float shiftOffsetFactor = 0.6f;   // Larger offset when Shift is held
    public float normalMaxOffset = 2f;       // Max offset in normal mode
    public float shiftMaxOffset = 4f;        // Max offset when Shift is held
    private bool isLooking; // New flag for Shift state

    // Timer
    private TimerController timerController;
    private bool hasMovedOnce = false;

    // Audio
    public AudioSource shootSound;

void Start()
{
    rb = GetComponent<Rigidbody2D>();
    if (pistolSprite != null) pistolSprite.SetActive(false);
    if (mainCamera == null) mainCamera = Camera.main;
    if (shootSound == null) Debug.LogError("shootSound not assigned in " + gameObject.name);
    lastFireTime = -fireRate;
    
    // Find the TimerManager
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
            if (weaponEquipped && shouldShoot && Time.time >= lastFireTime + fireRate)
            {
                Shoot();
                lastFireTime = Time.time;
                shouldShoot = false;
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
            shakeOffset = Random.insideUnitSphere * shootShakemagnitude;
            shakeOffset.z = 0f;
            shakeTimeRemaining -= Time.fixedDeltaTime;
        }
        mainCamera.transform.position = smoothedPos + shakeOffset;

        // Only update movement if not dead
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
        
        // Check if this is the first movement
        if (!hasMovedOnce && movementInput != Vector2.zero && timerController != null)
        {
            hasMovedOnce = true;
            timerController.StartTimer();
        }
    }
}

    public void OnShoot(InputAction.CallbackContext context)
    {
        //set shooting state if the shoot action happened, referenced in Update()
        if (!isDead && weaponEquipped)                          // checks if alive and armed
        {
            if (context.performed) shouldShoot = true;          // sets shoot state on click only
        }
    }

    public void OnWeaponInteract(InputAction.CallbackContext context)
    {
        if (context.performed && Time.time >= lastWeaponPickupTime + weaponActionCooldown && !isDead)
        {
            if (!weaponEquipped && nearbyGun != null)
            {
                EquipWeapon();
                GameObject gunToDestroy = nearbyGun;
                nearbyGun = null;
                Destroy(gunToDestroy);
                lastWeaponPickupTime = Time.time;
            }
            else if (weaponEquipped)
            {
                DropWeapon();
                lastWeaponPickupTime = Time.time;
            }
        }
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
        Vector3 spawnPosition = transform.position + (transform.up * bulletOffset) + (transform.right * bulletOffsetSide);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, transform.rotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = transform.up * 50f;
        }
        shootSound.pitch = UnityEngine.Random.Range(1.1f, 1.3f);
        shootSound.PlayOneShot(shootSound.clip);
        ShakeCamera(shootShakeDuration, shootShakemagnitude);
    }
    public void ShakeCamera(float duration, float magnitude) // shakes camera with given duration and magnitude
    {
        shakeTimeRemaining = duration; // sets shake duration
        shakeMagnitude = magnitude; // sets shake strength
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
        rb.linearVelocity = Vector2.zero; // Stop current movement
        transform.localScale = new Vector3(3.2f, 3.2f, 3.2f); // Scale down for death effect
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        Vector2 nudgeDirection = -transform.up; // Nudge back in opposite direction
        rb.AddForce(nudgeDirection * 1f, ForceMode2D.Impulse); // Apply nudge
        // Wait briefly then stop completely
        Invoke("StopAfterNudge", 0.1f); // Delay to allow nudge to take effect
        GetComponent<Collider2D>().enabled = false; // Prevent further collisions
    }

    void StopAfterNudge()
    {
        rb.linearVelocity = Vector2.zero; // Reset velocity after nudge
        rb.angularVelocity = 0f; // Stop any rotation
    }
    public bool IsDead() //getter
    {
        return isDead;
    }

    void EquipWeapon()
    {
        weaponEquipped = true;
        if (pistolSprite != null) pistolSprite.SetActive(true);
    }

    void DropWeapon()
    {
        weaponEquipped = false;
        if (pistolSprite != null) pistolSprite.SetActive(false);

        GameObject droppedWeapon = Instantiate(pistolPrefab, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
        Rigidbody2D weaponRb = droppedWeapon.GetComponent<Rigidbody2D>();
        if (weaponRb != null)
        {
            weaponRb.linearVelocity = transform.up * 130f;
            weaponRb.angularVelocity = Random.Range(500f, 900f);
        }
    }

    // Collision Handling
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDead && collision.CompareTag("Weapon") && !weaponEquipped)
        {
            nearbyGun = collision.gameObject.transform.root.gameObject;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!isDead && collision.CompareTag("Weapon") && collision.gameObject.transform.root.gameObject == nearbyGun)
        {
            nearbyGun = null;
        }
    }
    private float shakeMagnitude;
}