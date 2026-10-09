using Players;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class HarpoonGun : WeaponClass
{

    [FormerlySerializedAs("harpoons")]
    [Header("HarpoonGun Stats")]
    [SerializeField] public float harpoonVelocity;
    [SerializeField] public float harpoonSpearGravityScale;
    [SerializeField] public float harpoonSpearPickupCooldown; // seconds
    [SerializeField] public float collectionRadius;
    [SerializeField] public float harpoonTimer; // seconds
    [SerializeField] public float spearReturnSpeed;
    [SerializeField] public float pullPower;

    [Header("HarpoonGun References")]
    public GameObject harpoonSpearPrefab;

    private HarpoonSpear harpoonSpear;

    [SerializeField] internal GameObject muzzleFlash;
    [SerializeField] internal GameObject barrelPosition;

    override public void Start()
    {
        harpoonSpear = Instantiate(harpoonSpearPrefab, transform.position, transform.rotation)
            .GetComponent<HarpoonSpear>();
        harpoonSpear.gameObject.SetActive(false);
        base.Start();
    }

    protected override void HandleAttack()
    {
        if (CurrentAmmo <= 0) {
            PullHarpoon();
            return;
        }
        harpoonSpear.Fire(this);
        CurrentAmmo--;
        Instantiate(muzzleFlash, barrelPosition.transform.position, transform.rotation);
    }

    public void PullHarpoon()
    {
        if (!harpoonSpear.gameObject.activeSelf) return;
        Transform target = harpoonSpear.PullTo ??
            (harpoonSpear.IsLanded ? harpoonSpear.transform : null);

        if (target == null) return;
        playerComponent.UsePull(target);

        AudioManager.Instance.PlaySFX(AudioTracks.HarpoonRetract);
    }

    public override bool UseSkill()
    {
        //TODO
        return false;
    }

    public override void IntroSkill()
    {
        // Pull all enemies
        if (harpoonSpear.gameObject.activeSelf) {
            harpoonSpear.ReturnToPlayer();
            harpoonSpear.PullEnemy();
        }
    }

    void Update()
    {
        HandleWeaponRotation();

        if (harpoonSpear.gameObject.activeSelf && harpoonSpear.IsCollectable &&
                Vector2.Distance(playerComponent.Center, harpoonSpear.transform.position) <= collectionRadius)
        {
            harpoonSpear.ReturnToPlayer();
        }
    }

    public void SpearCollected(HarpoonSpear spear) {
        if (spear != harpoonSpear) return;

        harpoonSpear.gameObject.SetActive(false);
        CurrentAmmo++;
    }

    private void HandleWeaponRotation() {
        Vector2 facing = playerComponent.facing;

        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        SpriteRenderer[] children = GetComponentsInChildren<SpriteRenderer>();
        bool shouldFlip = angle > 90 || angle < -90;
        foreach (SpriteRenderer sr in children)
        {
            sr.flipY = shouldFlip;
        }
    }
}
