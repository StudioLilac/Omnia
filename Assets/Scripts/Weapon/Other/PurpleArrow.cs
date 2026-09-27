using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemies;
using Players;

public class PurpleArrow : MonoBehaviour {
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float initialSpeed = 8f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float deceleration = 8f;
    [SerializeField] private float acceleration = 25f;

    [SerializeField] private float angleThreshold = 5f; // degrees within target direction to start accelerating
    [SerializeField] private float lifetime = 3f;

    private float currentSpeed;
    private bool isAccelerating = false;

    private Vector3 targetPosition;
    private Vector2 currentDirection;
    private Rigidbody2D rb;
    private float damage;

    public static bool gainCritOnHit;

    [SerializeField] private ParticleSystem trailParticles;
    [SerializeField] private GameObject impactParticlesPrefab;


    public void Initialize(Vector3 targetPos, float damage) {
        targetPosition = targetPos;
        this.damage = damage;
        rb = GetComponent<Rigidbody2D>();

        // Start in completely random direction (360°)
        float randomAngle = Random.Range(0f, 360f);
        currentDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad)).normalized;

        currentSpeed = initialSpeed;
        rb.velocity = currentDirection * currentSpeed;
        StartCoroutine(SelfDestruct());
    }

    private void FixedUpdate() {
        Vector2 toTarget = ((Vector2)targetPosition - rb.position).normalized;
        float angle = Vector2.SignedAngle(currentDirection, toTarget);

        // Smooth rotation toward target
        float rotateAmount = Mathf.Clamp(angle, -turnSpeed * Time.fixedDeltaTime, turnSpeed * Time.fixedDeltaTime);
        currentDirection = Quaternion.Euler(0, 0, rotateAmount) * currentDirection;
        currentDirection.Normalize();

        // Phase switch: accelerate once we're nearly aligned
        if (!isAccelerating && Mathf.Abs(angle) < angleThreshold) {
            isAccelerating = true;
        }

        if (isAccelerating) {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
        } else {
            currentSpeed = Mathf.MoveTowards(currentSpeed, minSpeed, deceleration * Time.fixedDeltaTime);
        }

        rb.velocity = currentDirection * currentSpeed;

        float visualAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        rb.rotation = visualAngle;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null) {
            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
            enemy.Hurt(damage * player.damageMultiplier * Mathf.Clamp(player.critChance, 0.2f, 1) * player.critMultiplier, crit: true);

            if (impactParticlesPrefab != null) {
                GameObject burst = Instantiate(impactParticlesPrefab, transform.position, Quaternion.identity);
                Destroy(burst, 1.5f);
            }

            if (gainCritOnHit) {
                StartCoroutine(GainCriticalChance(player));
            }

            DetachAndCleanupParticles();
            Destroy(gameObject);
        }
    }

    private IEnumerator GainCriticalChance(Player player) {
        player.critChance += 0.1f;
        yield return new WaitForSeconds(4f);
        player.critChance -= 0.1f;
    }


    private IEnumerator SelfDestruct() {
        yield return new WaitForSeconds(lifetime);
        DetachAndCleanupParticles();
        Destroy(gameObject);
    }

    private void DetachAndCleanupParticles() {
        if (trailParticles != null) {
            trailParticles.transform.parent = null;
            trailParticles.Stop();

            float duration = trailParticles.main.duration + trailParticles.main.startLifetime.constantMax;
            Destroy(trailParticles.gameObject, duration);
        }
    }
}
