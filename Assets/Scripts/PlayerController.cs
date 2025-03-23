using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    // Player Attributes
    public float moveSpeed;
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private int health = 1;
    private bool isDead = false;

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

    // Audio
    public AudioSource shootSound;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (pistolSprite != null) pistolSprite.SetActive(false);
        if (mainCamera == null) mainCamera = Camera.main;
        if (shootSound == null) Debug.LogError("shootSound not assigned in " + gameObject.name);
        lastFireTime = -fireRate;
    }

    void Update()
    {
        if (!isDead)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0f;
            Vector3 direction = (mousePos - transform.position).normalized;
            transform.up = direction;
            if (weaponEquipped && shouldShoot && Time.time >= lastFireTime + fireRate) // checks if can shoot
            {
                Shoot();                                        // fires bullet
                lastFireTime = Time.time;                       // updates last shot time
                shouldShoot = false;                            // resets shoot state after firing
            }
        }

    }

    void FixedUpdate()
    {
        if (!isDead)
        {
            Vector3 targetCameraPos = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCameraPos, cameraSmoothSpeed);
            rb.linearVelocity = movementInput * moveSpeed;
        }
    }

    // Input Handling
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!isDead)
        {
            movementInput = context.ReadValue<Vector2>();
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

        if (shootSound != null && shootSound.clip != null)
        {
            shootSound.PlayOneShot(shootSound.clip);
        }
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
        gameObject.SetActive(false);
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
}