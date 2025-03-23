using UnityEngine;

public class Enemy : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float shootInterval = 1f;
    public float bulletSpeed = 30f;
    public float bulletOffset = 0.5f;

    private Transform player;
    private float nextShootTime;
    private bool hasSpottedPlayer;

    //audio
    public AudioSource shootSound;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); // find player
        if (playerObj != null)
        {
            player = playerObj.transform; // set player transform
        }
        else
        {
            Debug.LogError("Enemy could not find Player with tag 'Player'!"); // log error if no player
        }
        nextShootTime = Time.time + shootInterval; // set initial shoot time
        hasSpottedPlayer = false; // start without spotting player
    }

    void Update()
    {
        if (player == null) return; // skip if no player

        if (hasSpottedPlayer) // only act if player is spotted
        {
            Vector2 direction = (player.position - transform.position).normalized; // direction to player
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // calculate rotation angle
            transform.rotation = Quaternion.Euler(0, 0, angle); // rotate to face player

            if (Time.time >= nextShootTime) // check if time to shoot
            {
                Shoot(); // fire bullet
                nextShootTime = Time.time + shootInterval; // update next shoot time
            }
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null) // check if bullet prefab is assigned
        {
            Debug.LogError("bulletPrefab not assigned in Enemy!"); // log error if missing
            return; // exit if no prefab
        }

        Vector3 spawnPosition = transform.position + (transform.up * bulletOffset); // calculate bullet spawn pos
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, transform.rotation); // spawn bullet
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>(); // get bullet rigidbody
        if (bulletRb != null) // check if bullet has rigidbody
        {
            bulletRb.linearVelocity = transform.up * bulletSpeed; // set bullet velocity
        }
        shootSound.PlayOneShot(shootSound.clip); // play shoot sound
    }

    void OnTriggerEnter2D(Collider2D collision) // detect entering fov
    {
        if (collision.CompareTag("Player")) // check if player enters
        {
            hasSpottedPlayer = true; // mark player as spotted
        }
    }

    void OnTriggerExit2D(Collider2D collision) // detect exiting fov
    {
        if (collision.CompareTag("Player")) // check if player leaves
        {
            hasSpottedPlayer = false; // unmark player as spotted
        }
    }
}