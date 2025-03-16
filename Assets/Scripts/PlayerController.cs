using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    //player attributes
    public float moveSpeed = 10.0f;
    private Vector2 movementInput;
    private Rigidbody2D rb;

    //shooting
    public GameObject bulletPrefab;
    public float bulletOffset = 0;
    public float bulletOffsetSide = 0.45f;
    public float fireRate = 3f;
    private float nextFire;

    //gun handling
    private bool weaponEquipped = false;
    private GameObject nearbyGun;
    private float lastWeaponPickupTime = 0;
    private float weaponActionCooldown = 0.5f;

    //different weapon prefabs

    //pistol offsets: 0.65, side: 0.36
    public GameObject pistolPrefab;
    public GameObject shotgunPrefab;
    public GameObject riflePrefab;
    public GameObject smgPrefab;
    public GameObject knifePrefab;

    //different equipped weapons
    public GameObject pistolSprite;
    public GameObject shotgunSprite;
    public GameObject rifleSprite;
    public GameObject smgSprite;
    public GameObject knifeSprite;

    //camera
    public Camera mainCamera;
    public float cameraSmoothSpeed = 0.125f;
    // public float cameraShakeTime = 0.1f; //todo
    // public float cameraShakeIntensity = 0.1f;

    //audio
    public AudioSource shootSound; //todo

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pistolSprite.SetActive(false);
        if(mainCamera == null) { mainCamera = Camera.main; }
    }
    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        Vector3 direction = (mousePos - transform.position).normalized;
        transform.up = direction;
    } 
    void FixedUpdate()
    {
        //camera follow
        Vector3 targetCameraPos = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCameraPos, cameraSmoothSpeed);
        rb.linearVelocity = movementInput * moveSpeed;
    }

    //input handling
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }
    public void OnShoot(InputAction.CallbackContext context) {
        if(weaponEquipped && context.performed && Time.time > nextFire) {
            Shoot();
            nextFire = Time.time + fireRate;
        }
    }
    /*
    public void OnDrop(InputAction.CallbackContext context)
    {
        if (weaponEquipped && context.performed)
        {
            DropWeapon();
        }
    }
    public void OnEquip(InputAction.CallbackContext context)
    {
        if (!weaponEquipped && context.performed && nearbyGun != null)
        {
            EquipWeapon();
            Destroy(nearbyGun);
            nearbyGun = null;
        }
    }*/
public void OnWeaponInteract(InputAction.CallbackContext context)
{
    if (context.performed && Time.time >= lastWeaponPickupTime + weaponActionCooldown)
    {
        if (!weaponEquipped && nearbyGun != null)
        {
            EquipWeapon();
            GameObject gunToDestroy = nearbyGun;
            nearbyGun = null;
            Destroy(gunToDestroy);
            // Debug.Log("Destroyed: " + gunToDestroy.name);
            lastWeaponPickupTime = Time.time;
        }
        else if (weaponEquipped)
        {
            DropWeapon();
            lastWeaponPickupTime = Time.time;
        }
    }
}

    //actions
    void Shoot()
    {
        Vector3 spawnPosition = transform.position + (transform.up * bulletOffset) + (transform.right * bulletOffsetSide);
        Instantiate(bulletPrefab, spawnPosition, transform.rotation);
        Rigidbody2D bulletRb = bulletPrefab.GetComponent<Rigidbody2D>();
        bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        shootSound.Play(); //todo
    }


    //weapon handling
    public void EquipWeapon() {
        pistolSprite.SetActive(true);
        weaponEquipped = true;
    }
    public void DropWeapon() {
        pistolSprite.SetActive(false);
        weaponEquipped = false;
        GameObject droppedWeapon = Instantiate(pistolPrefab, transform.position, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
        Rigidbody2D weaponrb = droppedWeapon.GetComponent<Rigidbody2D>();
        weaponrb.linearVelocity = transform.up * 130f; //nudge
        weaponrb.angularVelocity = Random.Range(500f, 900f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Weapon") && !weaponEquipped)
        {
            nearbyGun = collision.gameObject.transform.root.gameObject; //get the root object
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Weapon") && collision.gameObject.transform.root.gameObject == nearbyGun)
        {
            nearbyGun = null;
        }
    }
}