using System.Collections;
using Enemies;
using Omnia.Fire;
using Players;
using UnityEngine;

[RequireComponent(typeof(FlameEmitter))]
public class Flamethrower : WeaponClass {
    [Header("Flamethrower")]
    [Tooltip("Enable to restore fuel consumption. Disabled while tuning the fire.")]
    public bool useFuel;
    [SerializeField] public float reloadTime = 1.2f;
    [SerializeField, Min(0.01f)] private float fuelPerSecond = 12;
    [SerializeField, Min(0.01f)] private float refillInterval = 0.08f;
    [SerializeField] internal GameObject barrelPosition;
    [SerializeField] internal float skillForce = 10;
    [SerializeField] private float skillLockDuration = 0.25f;
    [SerializeField] private float introDelayTime = 0.25f;
    // Preserve serialized spread upgrades on existing shotgun prefabs and save content.
    [HideInInspector] public float blastAngle = 35;
    public FlameEmitter Emitter { get; private set; }
    private SpriteRenderer weaponSprite;
    private bool triggerHeld;
    private bool selected = true;
    private bool gravityLocked;
    private float skillTimer;
    private float burstTimer;
    private float fuelSpent;
    private float refillTimer;
    private float idleTime;
    private int defaultSpread;
    private readonly Collider2D[] candidates = new Collider2D[256];
    private readonly Enemy[] targets = new Enemy[256];
    private readonly float[] targetHeat = new float[256];
    private Vector3 barrelOffset;
    private readonly System.Random critRandom = new System.Random(1729);

    protected virtual void Awake() {
        weaponSprite = GetComponentInChildren<SpriteRenderer>();
        Emitter = GetComponent<FlameEmitter>();
        Emitter.nozzle = barrelPosition != null ? barrelPosition.transform : transform;
        Emitter.solidLayers = groundLayer;
        defaultSpread = Emitter.spread;
        if (barrelPosition != null) barrelOffset = barrelPosition.transform.localPosition;
        Emitter.SimulationTick += DamageTick;
    }

    public override void SetTriggerHeld(bool held) { triggerHeld = held; }
    protected override void HandleAttack() { /* Continuous input is supplied by SetTriggerHeld. */ }

    public override void SetSpriteActive(bool active) {
        selected = active;
        base.SetSpriteActive(active);
        if (!active && Emitter != null) {
            triggerHeld = false;
            burstTimer = 0;
            Emitter.IsFiring = false;
        }
    }

    protected virtual void Update() {
        if (player == null) return;
        var character = player.GetComponent<Player>();
        var body = player.GetComponent<Rigidbody2D>();
        if (gravityLocked && body != null && body.linearVelocity.y < 0) {
            gravityLocked = false;
            character.SetGravityLock(false, 3);
        }
        skillTimer = Mathf.Max(0, skillTimer - Time.deltaTime);
        if (skillTimer <= 0) {
            Vector2 facing = character.facing;
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

        }
        bool flip = transform.right.x < -0.0001f;
        if (weaponSprite != null) weaponSprite.flipY = flip;
        if (barrelPosition != null) barrelPosition.transform.localPosition = new Vector3(barrelOffset.x, flip ? -barrelOffset.y : barrelOffset.y, barrelOffset.z);
        burstTimer = Mathf.Max(0, burstTimer - Time.deltaTime);
        bool firing = selected && (burstTimer > 0 || triggerHeld && (!useFuel || CurrentAmmo > 0));
        Emitter.IsFiring = firing;
        Emitter.spread = Mathf.Max(0, Mathf.RoundToInt(defaultSpread * Mathf.Max(0, blastAngle) / 35f));
        if (!useFuel) {
            CurrentAmmo = maxAmmoCount;
            fuelSpent = refillTimer = idleTime = 0;
        } else if (firing) {
            idleTime = refillTimer = 0;
            if (triggerHeld && burstTimer <= 0) {
                fuelSpent += Time.deltaTime * fuelPerSecond;
                int consumed = Mathf.FloorToInt(fuelSpent);
                if (consumed > 0) { CurrentAmmo = Mathf.Max(0, CurrentAmmo - consumed); fuelSpent -= consumed; }
            }
        } else {
            idleTime += Time.deltaTime;
            if (idleTime >= reloadTime) {
                refillTimer += Time.deltaTime;
                while (refillTimer >= refillInterval && CurrentAmmo < maxAmmoCount) {
                    refillTimer -= refillInterval;
                    CurrentAmmo++;
                }
                if (CurrentAmmo >= maxAmmoCount) { refillTimer = 0; fuelSpent = 0; }
            }
        }
    }

    private void DamageTick(float seconds) {
        // Old fire keeps damaging in world space even after this weapon is hidden.
        if (player == null || Emitter.Renderer.VisibleCount == 0) return;
        Player character = player.GetComponent<Player>();
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(hittableLayerMask);
        int count = Physics2D.OverlapBox(Emitter.RasterCenter, Emitter.Size, 0, filter, candidates);
        int targetCount = 0;
        var renderer = Emitter.Renderer;
        for (int c = 0; c < count; c++) {
            var collider = candidates[c];
            Enemy enemy = collider.GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            float intensity = 0;
            var bounds = collider.bounds;
            for (int i = 0; i < renderer.VisibleCount; i++) {
                int pixel = renderer.VisibleIndex(i);
                float heat = renderer.PixelHeat(pixel);
                if (heat <= Emitter.damageThreshold) continue;
                float weighted = heat * (Emitter.IsCoreHeat(heat) ? Emitter.coreDamageMultiplier : Emitter.wingDamageMultiplier);
                if (weighted <= intensity) continue;
                Vector2 point = renderer.PixelWorld(pixel);
                if (bounds.Contains(new Vector3(point.x, point.y, bounds.center.z)) && collider.OverlapPoint(point)) intensity = weighted;
            }
            if (intensity <= 0) continue;
            int index = 0;
            while (index < targetCount && targets[index] != enemy) index++;
            if (index == targetCount) { targets[index] = enemy; targetHeat[index] = intensity; targetCount++; }
            else targetHeat[index] = Mathf.Max(targetHeat[index], intensity);
        }
        for (int i = 0; i < targetCount; i++) {
            Enemy enemy = targets[i];
            targets[i] = null;
            if (enemy == null) continue;
            bool crit = critRandom.NextDouble() < character.critChance;
            float amount = damage * seconds * targetHeat[i] * character.damageMultiplier;
            if (crit) amount *= character.critMultiplier;
            enemy.Hurt(amount, stagger: false, crit: crit);
            character.OnHit(amount, enemy);
        }
    }

    public override bool UseSkill() {
        transform.rotation = Quaternion.Euler(0, 0, 270);
        skillTimer = skillLockDuration;
        burstTimer = Mathf.Max(burstTimer, 0.6f);
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) {
            body.linearVelocity = new Vector2(body.linearVelocity.x, skillForce);
            gravityLocked = true;
            player.GetComponent<Player>().SetGravityLock(true, 1);
        }
        return true;
    }
    public override void IntroSkill() { StartCoroutine(IntroBurst()); }
    private IEnumerator IntroBurst() {
        CurrentAmmo = maxAmmoCount;
        yield return new WaitForSeconds(introDelayTime);
        burstTimer = 1;
        player.GetComponent<Player>().UseRecoil(10);
        if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.Shake(3);
    }
    protected virtual void OnDisable() {
        triggerHeld = false;
        burstTimer = 0;
        if (Emitter != null) Emitter.IsFiring = false;
    }
    protected virtual void OnDestroy() { if (Emitter != null) Emitter.SimulationTick -= DamageTick; }
}
