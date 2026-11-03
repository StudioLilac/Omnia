using System;
using System.Reflection;
using Omnia.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Run with: unity command run_script --file Tests/SpriteOutlineManagerVerification.cs --entry SpriteOutlineManagerVerification.Run
public static class SpriteOutlineManagerVerification {
    public static string Run() {
        var scene = EditorSceneManager.NewPreviewScene();
        SpriteOutlineManager manager = null;
        int checks = 0;
        void Check(bool condition, string message) {
            checks++;
            if (!condition) throw new Exception(message);
        }
        GameObject Make(string name, Transform parent = null) {
            var go = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(parent, false);
            return go;
        }
        void Lifecycle(string name, params object[] args) => typeof(SpriteOutlineManager)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, args);
        void ColorIs(SpriteRenderer renderer, Color color, string message) {
            var outliner = renderer.GetComponent<PixelOutliner>();
            Check(outliner != null && outliner.enabled && outliner.OutlineColor == color, message);
        }
        void Notify(Transform root) => new SpriteOutlineHierarchyChanged(root).Invoke();
        try {
            manager = Make("Manager").AddComponent<SpriteOutlineManager>();
            Lifecycle("OnEnable");
            Check(typeof(SpriteOutlineManager).GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance) == null, "Manager still has a per-frame Update");

            var parent = Make("Parent").AddComponent<SpriteRenderer>();
            var child = Make("Nested entity", parent.transform).AddComponent<SpriteRenderer>();
            var leaf = Make("Inactive leaf", child.transform).AddComponent<SpriteRenderer>();
            leaf.gameObject.SetActive(false);
            manager.PushColor(parent);
            ColorIs(parent, Color.black, "Registration must immediately apply the default outline");
            ColorIs(leaf, Color.black, "Inactive descendants must be discovered during registration");
            manager.PushColor(parent, Color.red);
            ColorIs(child, Color.red, "A parent's pushed color must propagate immediately");
            manager.PushColor(child, Color.blue);
            ColorIs(leaf, Color.blue, "Nearest registered color must win");
            manager.PushColor(parent, Color.green);
            ColorIs(parent, Color.green, "Top of the parent's stack must win");
            ColorIs(leaf, Color.blue, "Ancestor changes must preserve a child's override");
            manager.PushColor(child, Color.yellow);
            manager.PopColor(child);
            ColorIs(leaf, Color.blue, "Pop must restore the previous color immediately");
            manager.PopColor(child);
            ColorIs(leaf, Color.green, "Empty child stack must inherit the nearest ancestor color");
            manager.PopColor(parent);
            ColorIs(leaf, Color.red, "Ancestor pop must refresh nested subscribers");
            manager.PopColor(parent);
            manager.PopColor(parent);
            ColorIs(parent, Color.black, "Popping an empty stack must retain the default outline");

            var added = Make("Late nested visual", leaf.transform).AddComponent<SpriteRenderer>();
            Notify(added.transform);
            ColorIs(added, Color.black, "Hierarchy events must discover new inactive nested sprites");
            manager.PushColor(parent, Color.red);
            ColorIs(added, Color.red, "Later colors must reach event-discovered sprites");
            Notify(added.transform);
            Check(added.GetComponents<PixelOutliner>().Length == 1, "Repeated events duplicated an outliner");

            var other = Make("Other entity").AddComponent<SpriteRenderer>();
            manager.PushColor(other, Color.magenta);
            child.transform.SetParent(other.transform, false);
            Notify(child.transform);
            ColorIs(child, Color.magenta, "Reparented subscribers must inherit their new ancestor color");
            ColorIs(added, Color.magenta, "Reparenting must refresh the entire moved subtree");
            manager.PushColor(parent, Color.cyan);
            ColorIs(child, Color.magenta, "The former ancestor must no longer change moved sprites");

            var detached = Make("Unregistered child", other.transform).AddComponent<SpriteRenderer>();
            Notify(detached.transform);
            ColorIs(detached, Color.magenta, "New child was not attached");
            detached.transform.SetParent(null, false);
            Notify(detached.transform);
            Check(!detached.GetComponent<PixelOutliner>().enabled, "A detached unregistered subtree must lose its managed outline");
            child.transform.SetParent(null, false);
            Notify(child.transform);
            ColorIs(child, Color.black, "A detached subscriber must retain its default outline");
            manager.PushColor(child, Color.blue);
            ColorIs(added, Color.blue, "Detached registered descendants must still receive color changes");

            var independent = Make("Standalone").AddComponent<SpriteRenderer>();
            PixelOutliner.Attach(independent, color: Color.yellow);
            Notify(independent.transform);
            ColorIs(independent, Color.yellow, "Unrelated hierarchy events must preserve standalone outliners");

            child.transform.SetParent(other.transform, false);
            Notify(child.transform);
            Lifecycle("OnEntityDestroyed", other);
            Check(!other.GetComponent<PixelOutliner>().enabled, "Unregistering the last owner must disable its outline");
            ColorIs(child, Color.blue, "Unregistering an ancestor must retain independently registered descendants");
            manager.PushColor(other, Color.magenta);
            Lifecycle("OnEntityDestroyed", child);
            ColorIs(child, Color.magenta, "Unregistering a child must fall back to the registered ancestor");

            // Disabled managers must not leave detached visuals enabled or miss new children on re-enable.
            manager.enabled = false;
            Lifecycle("OnDisable");
            Check(!child.GetComponent<PixelOutliner>().enabled, "Disabling the manager must hide managed outlines");
            child.transform.SetParent(null, false);
            Notify(child.transform);
            var whileDisabled = Make("Added while disabled", parent.transform).AddComponent<SpriteRenderer>();
            Notify(whileDisabled.transform);
            Check(whileDisabled.GetComponent<PixelOutliner>() == null, "Disabled manager still handled bus events");
            manager.enabled = true;
            Lifecycle("OnEnable");
            ColorIs(whileDisabled, Color.cyan, "Re-enabling must discover sprites added while disabled");
            Check(!child.GetComponent<PixelOutliner>().enabled, "A subtree detached while disabled was re-enabled");

            Object.DestroyImmediate(whileDisabled.gameObject);
            manager.PopColor(parent);
            ColorIs(parent, Color.red, "Destroyed children must not break later color changes");
            Lifecycle("OnDestroy");
            Check(!parent.GetComponent<PixelOutliner>().enabled, "Destroying the manager must release its outlines");
            var afterDestroy = Make("After manager cleanup", parent.transform).AddComponent<SpriteRenderer>();
            Notify(afterDestroy.transform);
            Check(afterDestroy.GetComponent<PixelOutliner>() == null, "Manager leaked its EventBus subscription");
            return $"PASS: {checks} checks covering immediate colors, nested ownership, inactive/new sprites, reparenting, removal and event subscription cleanup; no manager Update method.";
        } finally {
            if (manager != null) Lifecycle("OnDestroy");
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
