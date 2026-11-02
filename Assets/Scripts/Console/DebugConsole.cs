using IngameDebugConsole;
using Initializers;
using UnityEngine;
using Utils;

// Adapts the IngameDebugConsole prefab with our SceneInitializer + PersistentSingleton logic
public class DebugConsole : MonoBehaviour, IPersistentSingleton {
    private void Awake() {
        transform.SetParent(null); // Equivalent to PersistentSingleton L30
    }

    [ConsoleMethod("reset", "Resets/reloads the current scene")]
    public static void ReloadScene() {
        SceneInitializer.Restart();
    }
}
