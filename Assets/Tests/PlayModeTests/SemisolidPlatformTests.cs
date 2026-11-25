using System.Collections;
using NUnit.Framework;
using Players;
using Players.Mixin;
using UnityEngine;
using UnityEngine.TestTools;

public class SemisolidPlatformTests {
    private const float PlatformBottomY = 1.5f;
    private const float PlatformTopY = 1.75f;

    private PlayModeSandboxUtils utils;
    private Player player;
    private Collider2D platformCollider;

    [UnitySetUp]
    public IEnumerator SetUp() {
        LogAssert.ignoreFailingMessages = true;

        BuildSandbox();

        // let Player Start run and Jamie settle on ground under the platform
        yield return new WaitForSeconds(0.5f);
        yield return PlayModeHelpers.WaitUntil(
            () => player != null && player.grounded,
            5f,
            "Player never became grounded");
    }

    [UnityTearDown]
    public IEnumerator TearDown() {
        utils?.Destroy();
        utils = null;
        player = null;
        platformCollider = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Jamie_JumpsUpThrough_StandsOn_AndDropsThroughSemisolid() {
        // jump up through the one-way platform
        player.held = true;
        player.jump = true;
        yield return PlayModeHelpers.WaitUntil(
            () => player.transform.position.y > PlatformTopY,
            5f,
            "Player never passed up through the semisolid platform");
        player.held = false;

        // land and stand on the platform
        yield return PlayModeHelpers.WaitUntil(
            () => player.grounded && player.transform.position.y > PlatformBottomY,
            5f,
            "Player never landed on the semisolid platform");

        yield return new WaitForSeconds(0.5f);
        Assert.GreaterOrEqual(
            player.transform.position.y,
            1.7f,
            "Player fell through the semisolid platform while standing on it");
        Assert.IsTrue(player.grounded, "Player should remain grounded on the semisolid platform");

        // drop through with down input
        player.moving = Vector2.down;
        yield return PlayModeHelpers.WaitUntil(
            () => player.transform.position.y < PlatformBottomY,
            5f,
            "Player never dropped through the semisolid platform");
        player.moving = Vector2.zero;

        // re-enable runs once not grounded after fall; check before landing so the wait isn't stuck
        yield return PlayModeHelpers.WaitUntil(
            () => !Physics2D.GetIgnoreCollision(player.hitbox, platformCollider),
            5f,
            "Semisolid collision was never re-enabled after dropping through");

        yield return PlayModeHelpers.WaitUntil(
            () => player.grounded && player.transform.position.y < 0.5f,
            5f,
            "Player never landed on the ground after dropping through");
    }

    private void BuildSandbox() {
        utils = new PlayModeSandboxUtils();
        utils.EnsureManagers();
        utils.CreateBox("Ground", "Ground", new Vector3(0f, -1f, 0f), new Vector2(40f, 2f));
        platformCollider = CreateSemisolidPlatform();
        player = utils.SpawnPlayer(new Vector3(0f, 0f, 0f), disableLayerCollision: false);
        AssignSemisolidCollider(player, platformCollider);
    }

    private BoxCollider2D CreateSemisolidPlatform() {
        var go = new GameObject("SemisolidPlatform");
        go.layer = LayerMask.NameToLayer("Semisolid");
        // centre at 1.625 with height 0.25 -> bottom 1.5, top 1.75
        go.transform.position = new Vector3(0f, 1.625f, 0f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(6f, 0.25f);
        col.usedByEffector = true;

        var effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 90f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        utils.Track(go);
        return col;
    }

    private static void AssignSemisolidCollider(Player target, Collider2D platform) {
        var layerCollision = target.GetComponentInChildren<HandleLayerCollision>(true);
        Assert.IsNotNull(layerCollision, "HandleLayerCollision missing on Player");
        layerCollision.SetSemisolidColliderForTesting(platform);
    }
}
