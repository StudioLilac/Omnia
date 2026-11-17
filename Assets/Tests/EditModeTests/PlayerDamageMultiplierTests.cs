using NUnit.Framework;
using Players;
using UnityEngine;

public class PlayerDamageMultiplierTests {
    private GameObject playerObject;
    private Player player;

    [SetUp]
    public void SetUp() {
        playerObject = new GameObject("TestPlayer");
        player = playerObject.AddComponent<Player>();
        player.ClearAllBuffs();
    }

    [TearDown]
    public void TearDown() {
        if (playerObject != null) {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void DamageMultiplier_StartsAtBaseValue() {
        Assert.AreEqual(1f, player.damageMultiplier);
    }

    [Test]
    public void DamageMultiplier_SumsAdditiveBuffs_ThenMultiplies() {
        // (1 + 0.5 + 0.25) * 2 * 1.5 = 5.25
        player.AddAdditiveBuff(0.5f);
        player.AddAdditiveBuff(0.25f);
        player.AddMultiplicativeBuff(2f);
        player.AddMultiplicativeBuff(1.5f);

        Assert.AreEqual(5.25f, player.damageMultiplier, 0.0001f);
    }

    [Test]
    public void DamageMultiplier_RemovingBuff_RestoresPreviousValue() {
        player.AddAdditiveBuff(0.5f);
        player.AddMultiplicativeBuff(2f);
        Assert.AreEqual(3f, player.damageMultiplier, 0.0001f); // (1 + 0.5) * 2

        player.RemoveAdditiveBuff(0.5f);
        Assert.AreEqual(2f, player.damageMultiplier, 0.0001f); // 1 * 2

        player.RemoveMultiplicativeBuff(2f);
        Assert.AreEqual(1f, player.damageMultiplier, 0.0001f);
    }

    [Test]
    public void ClearAllBuffs_ResetsMultiplierToBase() {
        player.AddAdditiveBuff(1f);
        player.AddMultiplicativeBuff(3f);
        player.ClearAllBuffs();

        Assert.AreEqual(1f, player.damageMultiplier);
    }
}
