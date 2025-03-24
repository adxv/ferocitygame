using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float shootInterval = 1f;
    public float bulletSpeed = 30f;
    public float bulletOffset = 0.5f;
    public float forgetTime = 3f;
    public LayerMask wallLayer;
    //public float firstShotDelay = 0.5f; replaced with random delay
    public float shotDelayMin = 0.19f; // min delay before shooting (190ms)
    public float shotDelayMax = 0.23f; // max delay before shooting (230ms)

    private Transform player;
    private float nextShootTime;
    private bool hasSpottedPlayer;
    private Quaternion originalRotation;
    private float lastSpottedTime;


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
        originalRotation = transform.rotation;
        lastSpottedTime = -forgetTime;
    }

    void Update()
    {
        if (player == null) return; // skip if no player

        if (hasSpottedPlayer) // only act if player is spotted
        {
            Vector2 direction = (player.position - transform.position).normalized; // direction to player
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // calculate rotation angle
            transform.rotation = Quaternion.Euler(0, 0, angle); // rotate to face player

            if (!CanSeePlayer()) // check if player is behind wall
            {
                if (Time.time >= lastSpottedTime + forgetTime) // check if forget time elapsed
                {
                    hasSpottedPlayer = false; // forget player
                    transform.rotation = originalRotation; // revert to original rotation
                }
            }
            else
            {
                lastSpottedTime = Time.time; // update last spotted time
            }

            if (Time.time >= nextShootTime && CanSeePlayer()) // check if time to shoot
            {
                Shoot(); // fire bullet
                nextShootTime = Time.time + shootInterval + UnityEngine.Random.Range(shotDelayMin, shotDelayMax); // set next shoot time
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
        shootSound.pitch = UnityEngine.Random.Range(0.8f, 1.1f); // randomize pitch
        shootSound.PlayOneShot(shootSound.clip); // play shoot sound

    }

    void OnTriggerStay2D(Collider2D collision) // check player in fov continuously
    {
        if (collision.CompareTag("Player") && CanSeePlayer()) // check if player is in fov and visible
        {
            if (!hasSpottedPlayer) // check if newly spotted
            {
                hasSpottedPlayer = true; // mark player as spotted
                lastSpottedTime = Time.time; // record spotting time
                nextShootTime = Time.time + Random.Range(shotDelayMin, shotDelayMax); // set initial random delay
            }
            else if (Time.time - lastSpottedTime > Time.deltaTime) // check if player was hidden recently
            {
                nextShootTime = Time.time + Random.Range(shotDelayMin, shotDelayMax); // reset timer when re-spotted
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision) // detect exiting fov
    {
        if (collision.CompareTag("Player")) // check if player leaves
        {
            if (!CanSeePlayer()) // check if player is invisible
            {
                if (Time.time >= lastSpottedTime + forgetTime) // check if forget time elapsed
                {
                    hasSpottedPlayer = false; // forget player
                    transform.rotation = originalRotation; // revert to original rotation
                }
            }
        }
    }

    bool CanSeePlayer() // check line of sight to player
    {
        if (player == null) return false; // return false if no player
        Vector2 direction = (player.position - transform.position).normalized; // direction to player
        float distance = Vector2.Distance(transform.position, player.position); // distance to player
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, wallLayer); // raycast to player
        if (hit.collider != null) // check if ray hits something
        {
            return false; // wall blocks sight
        }
        return true; // clear line of sight
    }
}
