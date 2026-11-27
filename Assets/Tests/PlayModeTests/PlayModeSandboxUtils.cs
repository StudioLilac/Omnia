using System.Collections.Generic;
using Initializers;
using Omnia.Rendering;
using Players;
using Players.Mixin;
using UnityEngine;
using Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Shared scene-less sandbox helpers for play mode tests.
public class PlayModeSandboxUtils {
    public const string PlayerPrefabPath = PlayModeAssetPaths.PlayerPrefab;
    public const string PlayerDataManagerPrefabPath = PlayModeAssetPaths.PlayerDataManagerPrefab;
    public const string SceneInitializerPrefabPath = PlayModeAssetPaths.SceneInitializerPrefab;

    private readonly List<Object> spawned = new();
    private bool createdSceneInitializer;
    private bool createdPlayerDataManager;
    private bool createdAudioManager;

    public void EnsureManagers() {
        SceneInitializer initializer;
        if (SceneInitializer.Instance) {
            initializer = SceneInitializer.Instance;
        } else {
            var sceneInitializer = Object.Instantiate(LoadPrefab(SceneInitializerPrefabPath));
            initializer = sceneInitializer.GetComponent<SceneInitializer>();
            Track(sceneInitializer);
            createdSceneInitializer = true;
        }

        if (!initializer.CachedManagers.ContainsKey(typeof(SpriteOutlineManager))
            || initializer.CachedManagers[typeof(SpriteOutlineManager)] == null) {
            var outline = new GameObject("SpriteOutlineManager").AddComponent<SpriteOutlineManager>();
            Track(outline.gameObject);
            initializer.CachedManagers[typeof(SpriteOutlineManager)] = outline;
        }

        if (!PlayerDataManager.Instance) {
            var playerDataPrefab = LoadPrefab(PlayerDataManagerPrefabPath);
            var playerData = Object.Instantiate(playerDataPrefab);
            playerData.name = "PlayerDataManager";
            Track(playerData);
            createdPlayerDataManager = true;
        }

        if (!AudioManager.Instance) {
            var audio = new GameObject("AudioManager").AddComponent<AudioManager>();
            // avoid leaking into later tests via DontDestroyOnLoad
            audio.persistent = false;
            Track(audio.gameObject);
            createdAudioManager = true;
        }
    }

    public BoxCollider2D CreateBox(string name, string layerName, Vector3 position, Vector2 size) {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer(layerName);
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        Track(go);
        return col;
    }

    public Player SpawnPlayer(Vector3 position, bool disableLayerCollision = false) {
        var prefab = LoadPrefab(PlayerPrefabPath);
        var go = Object.Instantiate(prefab, position, Quaternion.identity);
        // HarpoonSpear finds the player by this exact name.
        go.name = "Player";
        Track(go);

        foreach (var input in go.GetComponentsInChildren<UseInput>(true)) {
            input.enabled = false;
        }

        if (disableLayerCollision) {
            // prefab leaves semisolidCollider unassigned, disable so IgnoreCollision doesn't throw
            foreach (var layerCollision in go.GetComponentsInChildren<HandleLayerCollision>(true)) {
                layerCollision.enabled = false;
            }
        }

        return go.GetComponent<Player>();
    }

    public void Track(Object obj) => spawned.Add(obj);

    public void Destroy() {
        foreach (var obj in spawned) {
            if (obj == null) continue;
            Object.Destroy(obj);
        }
        spawned.Clear();

        if (createdSceneInitializer) ClearSingleton<SceneInitializer>();
        if (createdPlayerDataManager) ClearSingleton<PlayerDataManager>();
        if (createdAudioManager) ClearSingleton<AudioManager>();

        createdSceneInitializer = false;
        createdPlayerDataManager = false;
        createdAudioManager = false;
    }

    public static GameObject LoadPrefab(string path) {
#if UNITY_EDITOR
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) {
            throw new System.InvalidOperationException($"Missing prefab at {path}");
        }
        return prefab;
#else
        throw new System.NotSupportedException("Play mode sandbox tests require the Unity Editor Play Mode runner.");
#endif
    }

    private static void ClearSingleton<T>() where T : Component {
        PersistentSingleton<T>.ResetForTesting();
    }
}
