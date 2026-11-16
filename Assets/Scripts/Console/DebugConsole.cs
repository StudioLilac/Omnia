using IngameDebugConsole;
using Initializers;
using UnityEngine;
using Utils;

// Adapts the IngameDebugConsole prefab with our SceneInitializer + PersistentSingleton logic
public class DebugConsole : MonoBehaviour, IPersistentSingleton {
    private TimeManager TimeManager => this.Inject<TimeManager>();
    private DebugLogManager debugLogManager;

    private void Awake() {
        debugLogManager = GetComponent<DebugLogManager>();
        transform.SetParent(null); // Equivalent to PersistentSingleton L30

        debugLogManager.OnLogWindowShown += () => {
            TimeManager.Freeze(this);
        };

        debugLogManager.OnLogWindowHidden += () => {
            TimeManager.Unfreeze(this);
        };
    }

    private void Start() {
        DebugLogConsole.AddCommand("reset", "Resets/reloads the scene", ReloadScene);
    }

    private void ReloadScene() {
        debugLogManager.HideLogWindow();
        SceneInitializer.Restart();
    }
}
