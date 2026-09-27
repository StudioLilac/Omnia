using System;
using Enemies.Dummy;
using Initializers;
using Players;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

#nullable enable
public class LevelManager : PersistentSingleton<LevelManager> {
    // If this throws a key-not-found error, that means no LevelData was found for the corresponding scene name. Will have to define one to use these level methods.
    private LevelData LevelData { get => LevelData.SceneToLevelMap[SceneManager.GetActiveScene().name]; }

    public static event Action<LevelData>? OnLevelLoaded;

    protected override void OnAwake() { }

    private void OnEnable() {
        Player.Death += HandlePlayerDeath;
    }

    private void OnDisable() {
        Player.Death -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath() {
        if (PlayerDataManager.Instance.warpedDepthsProgress < 1) {
            Restart();
        }
    }

    public void Restart() {
        SceneInitializer.LoadScene(LevelData.SceneName);
    }

    public void NextLevel() {
        SceneInitializer.LoadScene(LevelData.NextLevel.SceneName);
        OnLevelLoaded?.Invoke(LevelData.NextLevel);
    }

    public void PrevLevel() {
        SceneInitializer.LoadScene(LevelData.PrevLevel.SceneName);
        OnLevelLoaded?.Invoke(LevelData.NextLevel);
    }

    public void CustomLevel(LevelData data) {
        SceneInitializer.LoadScene(data.SceneName);
        OnLevelLoaded?.Invoke(LevelData.NextLevel);
    }
}

