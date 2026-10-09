using System;
using Unity.VisualScripting;
using UnityEngine;
using Enemies;
using Omnia.Utils;
using Players;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/*
    The projectile for HarpoonGun
    This class should only be interacted upon by its prefabs and HarpoonGun
*/
public class HarpoonSpear : MonoBehaviour {
    public LayerMask hittableLayerMask;
    public LayerMask playerLayer;
    public LayerMask groundLayer;
    public LayerMask semisolidLayer;

    public Collider2D Collider2D;
    public Rigidbody2D Rigidbody2D;

    private bool dropped;
    private bool collectable;
    private IEnumerator cooldown;
    private HarpoonGun gun;
    private Player player;
    private bool playerAbsorb;
    private IEnumerator absorbCooldown;

    // Tracking enemy
    public List<Enemy> TaggedEnemies { get; private set; }
    public Transform PullTo { get; private set; }

    public bool IsCollectable => collectable;
    public bool IsLanded => dropped;

    public static bool CanPullToGround;

    public static event Action<Transform> OnHitEnemy;

    public void Awake() {
        dropped = false;
        TaggedEnemies = new List<Enemy>();
        PullTo = null;
        playerAbsorb = false;

        player = GameObject.Find("Player")?.GetComponent<Player>();
        if (player == null) {
            Debug.LogWarning("Player not found or Player script is not attached to the GameObject.");
        }
    }

    public void OnEnable() {
        Enemy.Death += HandleEnemyDeath;
    }

    public void OnDisable() {
        Enemy.Death -= HandleEnemyDeath;
        absorbCooldown = null;
        cooldown = null;
    }

    // Fires the spear in the rotation of the gun with its velocity
    public void Fire(HarpoonGun gun) {
        AudioManager.Instance.PlaySFX(AudioTracks.HarpoonLaunch);
        this.gun = gun;
        TaggedEnemies.Clear();

        gameObject.SetActive(true);

        // Temporary fire from center of player until idea consolidated
        transform.position = gun.transform.position;
        transform.rotation = gun.transform.rotation;

        Rigidbody2D.linearVelocity = gun.transform.right * gun.harpoonVelocity;
    }

    public void PullEnemy() {
        if (TaggedEnemies.Count == 0) {
            return;
        }

        Vector2 difference = (player.Center - transform.position).normalized;
        foreach (var enemy in TaggedEnemies) {
            enemy.GetComponent<Rigidbody2D>().AddForce(difference * gun.pullPower);
            DoDamage(enemy);
        }
        AudioManager.Instance.PlaySFX(AudioTracks.HarpoonRetract);
    }

    public void ReturnToPlayer() {
        collectable = true;
        playerAbsorb = true;
    }

    public void Update() {
        // Rotate based on velocity
        if (!dropped && Rigidbody2D.linearVelocity != Vector2.zero) {
            float angle = Mathf.Atan2(Rigidbody2D.linearVelocity.y, Rigidbody2D.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }

        if (playerAbsorb) {
            transform.position = Vector2
                .MoveTowards(transform.position, player.Center, gun.spearReturnSpeed * Time.deltaTime);

            // Rotate over Z axis to face away from Player
            Vector3 difference = transform.position - player.Center;
            float rotationZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0.0f, 0.0f, rotationZ);

            if (collectable && Vector3.Distance(transform.position, player.Center) <= 0.1f) {
                HandlePlayerCollision();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.GetComponent<Player>() != null && collectable) {
            HandlePlayerCollision();
            return;
        }

        if (other.GetComponent<Enemy>() != null && !dropped) {
            AudioManager.Instance.PlaySFX(AudioTracks.HarpoonHit);
            HandleEnemyCollision(other.GetComponent<Enemy>());
            return;
        }

        if (Collider2D.IsTouchingLayers(semisolidLayer) && !dropped) {
            AudioManager.Instance.PlaySFX(AudioTracks.HarpoonHit);
            HandleSemisolidCollision(other.gameObject);
            return;
        }

        if (Collider2D.IsTouchingLayers(groundLayer) && !dropped) {
            AudioManager.Instance.PlaySFX(AudioTracks.HarpoonHit);
            HandleGroundCollision(other.gameObject);
            return;
        }
    }

    private void OnTriggerStay2D(Collider2D other) {
        // Prevents edge cases of the player already being in the harpoon as it is set to collectable
        // Having both OnTriggerEnter2D and OnTriggerStay2D should be okay as we check the `collectable`
        // condition, which is set to false if HandlePlayerCollision() was already called
        if (Collider2D.IsTouchingLayers(playerLayer) && collectable) {
            HandlePlayerCollision();
        }
    }

    private void HandlePlayerCollision() {
        KillSkeweredEnemies();
        Unfreeze();
        playerAbsorb = false;
        collectable = false;
        PullTo = null;
        TaggedEnemies.Clear();

        // Tell gun to mark this spear as available
        gun.SpearCollected(this);
    }

    private void HandleEnemyCollision(Enemy enemy) {
        StartCooldown();
        StartHarpoonTimer();

        TaggedEnemies.Add(enemy);

        if (enemy.WeightType == EnemyWeightType.Light) {
            SkewerLightEnemy(enemy);
            return;
        }

        DoDamage(enemy);
        OnHitEnemy?.Invoke(transform);
    }

    private void DoDamage(Enemy enemy) {
        bool isCrit = Random.Range(0f, 1f) < player.critChance;

        float damageAmount = gun.damage * player.damageMultiplier * (isCrit ? player.critMultiplier : 1);
        enemy.Hurt(damageAmount, crit: isCrit);

        player?.OnHit(damageAmount, enemy);
    }

    private void HandleSemisolidCollision(GameObject semi) {
        Freeze();
        AttachToRigidBody(semi.GetComponent<Rigidbody2D>());
        KillSkeweredEnemies();
        PullTo = gameObject.transform;
        StartCooldown();
        StartHarpoonTimer();
    }

    private void HandleGroundCollision(GameObject ground) {
        Freeze();
        AttachToRigidBody(ground.GetComponent<Rigidbody2D>());
        KillSkeweredEnemies();
        if (CanPullToGround) {
            PullTo = gameObject.transform;
        }
        StartCooldown();
        StartHarpoonTimer();
    }

    private void SkewerLightEnemy(Enemy enemy) {
        enemy.transform.SetParent(transform, true);
        enemy.enabled = false;

        Rigidbody2D enemyRigidbody = enemy.GetComponent<Rigidbody2D>();
        if (enemyRigidbody != null) {
            enemyRigidbody.linearVelocity = Vector2.zero;
            enemyRigidbody.angularVelocity = 0;
            enemyRigidbody.simulated = false;
        }

        foreach (UnityEngine.Collider2D collider in enemy.GetComponentsInChildren<UnityEngine.Collider2D>()) {
            collider.enabled = false;
        }
    }

    private void KillSkeweredEnemies() {
        List<Enemy> skeweredEnemies = TaggedEnemies.FindAll(enemy => enemy != null && enemy.WeightType == EnemyWeightType.Light);

        foreach (Enemy enemy in skeweredEnemies) {
            TaggedEnemies.Remove(enemy);
            enemy.transform.SetParent(null, true);
            enemy.Die();
        }
    }

    // To make the spear move, the hit object should have a rigidbody
    private void AttachToRigidBody(Rigidbody2D rb) {
        if (!rb) return;
        // HingeJoints are created during runtime as it can't be disabled
        HingeJoint2D hj = this.AddComponent<HingeJoint2D>();
        hj.connectedBody = rb;
    }

    private void HandleEnemyDeath(Enemy enemy) {
        TaggedEnemies.Remove(enemy);
    }

    // Unfreezes the spear
    private void Unfreeze() {
        if (cooldown != null) {
            StopCoroutine(cooldown);
        }


        Rigidbody2D.gravityScale = 1 * gun.harpoonSpearGravityScale;
        Rigidbody2D.freezeRotation = false;
        dropped = false;

        Destroy(GetComponent<HingeJoint2D>());
    }

    // Freezes the spear and marks as collectable
    private void Freeze() {
        if (cooldown != null) {
            StopCoroutine(cooldown);
        }

        Rigidbody2D.gravityScale = 0;
        Rigidbody2D.linearVelocity = Vector2.zero;
        Rigidbody2D.freezeRotation = true;
        dropped = true;
    }

    // Begin pickup cooldown at the first spear contact, after that free to collect anytime.
    // Enemy hits no longer latch the spear, so TaggedEnemies must not block pickup.
    private void StartCooldown() {
        if (collectable) return;

        if (cooldown != null) {
            StopCoroutine(cooldown);
        }

        cooldown = DropCooldown();
        if (!gameObject.activeInHierarchy) {
            Debug.LogWarning("HarpoonSpear is inactive! StartCooldown Coroutine will not start.");
            return;
        }
        StartCoroutine(cooldown);
    }

    private IEnumerator DropCooldown() {
        yield return new WaitForSeconds(gun.harpoonSpearPickupCooldown);
        collectable = true;
    }

    private void StartHarpoonTimer() {
        if (playerAbsorb) return;

        if (absorbCooldown != null) {
            StopCoroutine(absorbCooldown);
        }

        absorbCooldown = DropHarpoonTimer();
        if (!gameObject.activeInHierarchy) {
            Debug.LogWarning("HarpoonSpear is inactive! StartHarpoonTimer Coroutine will not start.");
            return;
        }
        StartCoroutine(absorbCooldown);
    }

    private IEnumerator DropHarpoonTimer() {
        yield return new WaitForSeconds(gun.harpoonTimer);
        ReturnToPlayer();
    }
}
