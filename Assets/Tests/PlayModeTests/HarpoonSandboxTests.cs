using System.Collections;
using Enemies.Dummy;
using NUnit.Framework;
using Players;
using UnityEngine;
using UnityEngine.TestTools;

public class HarpoonSandboxTests {
    private const string DummyPrefabPath = PlayModeAssetPaths.DummyPrefab;

    private PlayModeSandboxUtils utils;
    private Player player;
    private HarpoonGun harpoon;
    private Dummy dummy;

    [UnitySetUp]
    public IEnumerator SetUp() {
        LogAssert.ignoreFailingMessages = true;

        BuildSandbox();

        // let Player/HarpoonGun/Dummy Start run and Jamie settle on ground
        yield return new WaitForSeconds(0.5f);
        yield return PlayModeHelpers.WaitUntil(
            () => player != null && player.grounded,
            5f,
            "Player never became grounded");
    }

    [UnityTearDown]
    public IEnumerator TearDown() {
        DestroySandbox();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Jamie_Moves_FiresHarpoon_PullsToSpear_AndYanksEnemy() {
        Assert.IsNotNull(harpoon, "Player.weapons[0] should be HarpoonGun");

        // move right
        var startX = player.transform.position.x;
        player.facing = Vector2.right;
        player.moving = Vector2.right;
        yield return PlayModeHelpers.WaitUntil(
            () => player.transform.position.x > startX + 0.25f,
            5f,
            "Player did not move right");
        player.moving = Vector2.zero;

        // fire into the wall and wait for the spear to land
        player.facing = Vector2.right;
        player.fire = true;
        yield return PlayModeHelpers.WaitUntil(
            () => harpoon.CurrentAmmo == 0,
            3f,
            "Harpoon never consumed ammo on fire");

        var spear = GetSpear();
        Assert.IsNotNull(spear, "Harpoon spear was not created");
        Assert.IsTrue(spear.gameObject.activeSelf, "Spear should be active after firing");

        yield return PlayModeHelpers.WaitUntil(
            () => spear.IsLanded,
            5f,
            "Spear never landed on ground/wall");

        // empty-ammo fire pulls Jamie to the spear
        var beforePullX = player.transform.position.x;
        var spearX = spear.transform.position.x;
        player.fire = true;
        yield return PlayModeHelpers.WaitUntil(
            () => player.debugBehaviour == "Pull",
            3f,
            "Player never entered Pull behaviour");

        yield return PlayModeHelpers.WaitUntil(
            () => Mathf.Abs(player.transform.position.x - spearX) < Mathf.Abs(beforePullX - spearX) - 0.5f
                  || Vector2.Distance(player.transform.position, spear.transform.position) < 1.5f,
            5f,
            "Player did not move toward the landed spear");

        // wait until the spear is collected so we can fire again
        yield return PlayModeHelpers.WaitUntil(
            () => harpoon.CurrentAmmo > 0 && !spear.gameObject.activeSelf,
            8f,
            "Spear was never collected after pull");

        // place dummy to the left (away from the wall) and fire so the spear tags it mid-flight
        dummy.transform.position = new Vector3(player.transform.position.x - 3f, player.Center.y, 0f);
        var dummyStart = dummy.transform.position;

        player.facing = Vector2.left;
        player.fire = true;
        yield return PlayModeHelpers.WaitUntil(
            () => spear.gameObject.activeSelf && spear.TaggedEnemies.Count > 0,
            5f,
            "Spear never tagged the Dummy");

        // IntroSkill yanks tagged enemies toward Jamie
        harpoon.IntroSkill();
        yield return PlayModeHelpers.WaitUntil(
            () => Vector2.Distance(dummy.transform.position, player.Center)
                  < Vector2.Distance(dummyStart, player.Center) - 0.25f
                  || (dummy.GetComponent<Rigidbody2D>() != null
                      && Vector2.Dot(dummy.GetComponent<Rigidbody2D>().linearVelocity,
                          ((Vector2)player.Center - (Vector2)dummy.transform.position).normalized) > 0.5f),
            5f,
            "Dummy was not yanked toward the player after IntroSkill");
    }

    private void BuildSandbox() {
        HarpoonSpear.CanPullToGround = false;

        utils = new PlayModeSandboxUtils();
        utils.EnsureManagers();
        utils.CreateBox("Ground", "Ground", new Vector3(0f, -1f, 0f), new Vector2(40f, 2f));
        // Vertical ground surface so a rightward harpoon lands without relying on gravity arc.
        utils.CreateBox("HarpoonWall", "Ground", new Vector3(10f, 2f, 0f), new Vector2(1f, 8f));
        // not testing semi solid collisions
        player = utils.SpawnPlayer(new Vector3(0f, 1f, 0f), disableLayerCollision: true);
        harpoon = player.weapons[0] as HarpoonGun;
        dummy = SpawnDummy(new Vector3(6f, 1f, 0f));
    }

    private void DestroySandbox() {
        // spear is spawned by HarpoonGun.Start outside our tracked list; kill it first
        // so Update can't call SpearCollected on a destroyed gun
        foreach (var spear in Object.FindObjectsByType<HarpoonSpear>(FindObjectsInactive.Include)) {
            if (spear != null) Object.Destroy(spear.gameObject);
        }

        utils?.Destroy();
        utils = null;

        player = null;
        harpoon = null;
        dummy = null;
        HarpoonSpear.CanPullToGround = false;
    }

    private HarpoonSpear GetSpear() => harpoon.GetHarpoonSpearForTesting();

    private Dummy SpawnDummy(Vector3 position) {
        var prefab = PlayModeSandboxUtils.LoadPrefab(DummyPrefabPath);
        var go = Object.Instantiate(prefab, position, Quaternion.identity);
        go.name = "Dummy";
        utils.Track(go);

        var spawnedDummy = go.GetComponent<Dummy>();
        if (go.GetComponent<Rigidbody2D>() == null) {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        return spawnedDummy;
    }
}
