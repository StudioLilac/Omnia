using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Yarn.Unity;

// Shared play mode helpers. Dialogue skipping always goes through StopActiveDialogue.
public static class PlayModeHelpers {
    public static bool IsInDialogue =>
        DialogueManager.Instance != null && DialogueManager.Instance.inDialogue;

    // Stops the current Yarn dialogue, then waits until DialogueManager clears.
    public static IEnumerator StopActiveDialogue(float clearTimeoutSeconds = 5f) {
        if (!IsInDialogue) {
            yield break;
        }

        var runner = UnityEngine.Object.FindAnyObjectByType<DialogueRunner>();
        Assert.IsNotNull(runner, "DialogueRunner missing while dialogue is active");
        runner.Stop();

        yield return WaitUntil(
            () => !IsInDialogue,
            clearTimeoutSeconds,
            "Dialogue did not end after DialogueRunner.Stop()");
    }

    // Waits for a dialogue to start, then stops it. No-ops if none starts in time.
    public static IEnumerator SkipNextDialogue(
        float waitForStartSeconds = 3f,
        float clearTimeoutSeconds = 5f) {
        float remaining = waitForStartSeconds;
        while (remaining > 0f && !IsInDialogue) {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        yield return StopActiveDialogue(clearTimeoutSeconds);
    }

    // Keeps stopping dialogue until the condition is met (handles gaps like walks/waits).
    public static IEnumerator SkipDialoguesUntil(
        Func<bool> done,
        float timeoutSeconds = 30f,
        float clearTimeoutSeconds = 5f,
        string timeoutMessage = "Condition was not met before timeout while skipping dialogues") {
        float remaining = timeoutSeconds;
        while (!done() && remaining > 0f) {
            if (IsInDialogue) {
                yield return StopActiveDialogue(clearTimeoutSeconds);
            }

            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.IsTrue(done(), timeoutMessage);
    }

    public static IEnumerator WaitUntil(
        Func<bool> predicate,
        float timeoutSeconds,
        string timeoutMessage) {
        float remaining = timeoutSeconds;
        while (!predicate()) {
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f) {
                Assert.Fail(timeoutMessage);
            }

            yield return null;
        }
    }
}
