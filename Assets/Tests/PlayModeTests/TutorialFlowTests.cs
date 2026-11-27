using System.Collections;
using Enemies.Dummy;
using NPC.Dinky;
using NUnit.Framework;
using Players;
using Players.Mixin;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Utils;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class TutorialFlowTests {
    private const string ScenePath = PlayModeAssetPaths.TutorialScene;
    private const string SceneName = "4_Tutorial";

    [UnitySetUp]
    public IEnumerator LoadTutorial() {
        // ignore FMOD/Yarn noise in batchmode so we only assert on flow
        LogAssert.ignoreFailingMessages = true;

#if UNITY_EDITOR
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            ScenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
#endif

        // give SceneInitializer / Player / Tutorial a moment to start
        yield return new WaitForSeconds(0.5f);
    }

    [UnityTest]
    public IEnumerator Tutorial_BootsAndProgressesCombatFlow_ToNextLevel() {
        var tutorial = Object.FindAnyObjectByType<Scenes.Tutorial>();
        var player = Object.FindAnyObjectByType<Player>();
        var dinky = Object.FindAnyObjectByType<Dinky>();
        var dummies = Object.FindObjectsByType<Dummy>();

        Assert.IsNotNull(tutorial, "Tutorial director missing");
        Assert.IsNotNull(player, "Jamie/Player missing");
        Assert.IsNotNull(dinky, "Dinky missing");
        Assert.AreEqual(5, dummies.Length, "Tutorial should contain 5 dummies");
        Assert.IsNotNull(DialogueManager.Instance, "DialogueManager should be initialized");
        Assert.IsNotNull(LevelManager.Instance, "LevelManager should be initialized");
        Assert.IsNotNull(Object.FindAnyObjectByType<TimeManager>(), "TimeManager missing");

        // drive dummy hits ourselves so Input System doesn't overwrite moving
        foreach (var input in Object.FindObjectsByType<UseInput>()) {
            input.enabled = false;
        }

        var dummy1 = tutorial.Dummy1ForTesting;
        var dummy2 = tutorial.Dummy2ForTesting;
        var dummy3 = tutorial.Dummy3ForTesting;
        var dummy4 = tutorial.Dummy4ForTesting;
        var dummy5 = tutorial.Dummy5ForTesting;
        var airCheck = tutorial.DummyAirCheckForTesting;

        Assert.IsNotNull(dummy1);
        Assert.IsNotNull(dummy2);
        Assert.IsNotNull(dummy3);
        Assert.IsNotNull(dummy4);
        Assert.IsNotNull(dummy5);
        Assert.IsNotNull(airCheck);

        // begin dialogue -> first dummy wave
        yield return PlayModeHelpers.SkipNextDialogue();
        yield return PlayModeHelpers.WaitUntil(
            () => dummy1.canBeHurt && dummy2.canBeHurt,
            10f,
            "First dummy wave never unlocked");

        dummy1.Hurt(999f);
        dummy2.Hurt(999f);

        // platform dialogue -> remaining dummies
        yield return PlayModeHelpers.SkipNextDialogue();
        yield return PlayModeHelpers.WaitUntil(
            () => dummy4.canBeHurt && dummy5.canBeHurt,
            10f,
            "Platform dummy wave never unlocked");

        dummy4.Hurt(999f);
        dummy5.Hurt(999f);

        yield return PlayModeHelpers.WaitUntil(
            () => dummy3.canBeHurt,
            10f,
            "Final grounded dummy never unlocked");
        dummy3.Hurt(999f);

        // pull dialogue -> air trigger (needs Player tag on Jamie)
        yield return PlayModeHelpers.SkipNextDialogue();
        yield return new WaitForSeconds(0.25f);

        player.transform.position = airCheck.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        yield return PlayModeHelpers.WaitUntil(() => {
            var rb = dummy3.GetComponent<Rigidbody2D>();
            return rb != null && rb.gravityScale > 0f;
        }, 5f, "Air-dummy fall sequence never started");

        yield return PlayModeHelpers.WaitUntil(
            () => dummy3.transform.position.y < -4.5f,
            10f,
            "Falling dummy never reached ground threshold");

        // skip scared/gone dialogue across the walk+burrow gap, then advance level
        yield return PlayModeHelpers.SkipDialoguesUntil(
            () => SceneManager.GetActiveScene().name != SceneName,
            timeoutSeconds: 30f,
            timeoutMessage: "Tutorial never advanced to the next level");

        Assert.AreEqual("5_City", SceneManager.GetActiveScene().name);
    }
}
