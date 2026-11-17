using System;
using IngameDebugConsole;
using Initializers;
using UnityEngine;
using Utils;

// Adapts the IngameDebugConsole prefab with our SceneInitializer + PersistentSingleton logic
public class DebugConsole : MonoBehaviour, IPersistentSingleton {
    private DebugLogManager debugLogManager;

    public static event Action OnConsoleOpened;
    public static event Action OnConsoleClosed;

    private void Awake() {
        debugLogManager = GetComponent<DebugLogManager>();
        transform.SetParent(null); // Equivalent to PersistentSingleton L30

        debugLogManager.OnLogWindowShown += () => OnConsoleOpened?.Invoke();
        debugLogManager.OnLogWindowHidden += () => OnConsoleClosed?.Invoke();
    }

    private void Start() {
        DebugLogConsole.AddCommand("reset", "Resets/reloads the scene", ReloadScene);
    }

    private void ReloadScene() {
        debugLogManager.HideLogWindow();
        SceneInitializer.Restart();
    }
}
