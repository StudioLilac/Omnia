using Enemies.Common;
using FX;
using Players;
using Players.Buff;
using Omnia.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

#nullable enable
namespace Initializers {
    public interface IScenePayload { };

    public class SceneInitializer : PersistentSingleton<SceneInitializer> {
        [SerializeField] internal GameObject transition = null!;
        [SerializeField] internal SceneInitializerPrefabs prefabs = null!;
        private IScenePayload? scenePayload;
        internal Dictionary<Type, MonoBehaviour> CachedManagers = new();

        /**
         * Load the scene with a transition effect to hide any stutters.
         */
        public static void LoadScene(LevelData scene, bool shouldFade = true, IScenePayload? payload = null) {
            Instance.scenePayload = payload;
            if (shouldFade) {
                Instance.StartCoroutine(Instance.DoLoadSceneWithTransition(scene));
            } else {
                SceneManager.LoadScene(scene.SceneName);
            }
        }

        public static void Restart() {
            LoadScene(LevelData.FromScene(SceneManager.GetActiveScene()));
        }

        public void OnEnable() {
            SceneManager.sceneLoaded += OnSceneChange;
        }

        public void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneChange;
        }

        private void OnSceneChange(Scene scene, LoadSceneMode mode) {
            InitializeManagersForScene(LevelData.FromScene(scene));
        }

        private void InitializeManagersForScene(LevelData sceneData) {
            CachedManagers.Clear();
            var requiredManagers = sceneData.Init(prefabs);
            var managersParent = new GameObject("[SceneInitialized]");

            foreach (var managerPrefab in requiredManagers) {
                var existing = FindAnyObjectByType(managerPrefab.GetType()) as MonoBehaviour;
                if (existing != null) {
                    if (existing is not IPersistentSingleton)
                        Debug.LogWarning($"{managerPrefab.GetType()} already exists!");
                    Register(existing);
                    continue;
                }

                Register(InstantiatePrefab(managerPrefab, managersParent));
            }
        }

        private void Register(MonoBehaviour monoBehaviour) {
            CachedManagers.Add(monoBehaviour.GetType(), monoBehaviour);
        }

        private T InstantiatePrefab<T>(T prefab, GameObject managersParent) where T : MonoBehaviour {
            var newManagerInstance = Instantiate(prefab, managersParent.transform);
            newManagerInstance.name = prefab.name;
            return newManagerInstance;
        }

        private IEnumerator DoLoadSceneWithTransition(LevelData scene) {
            var it = Instantiate(transition).GetComponent<SceneTransition>();
            DontDestroyOnLoad(it.gameObject);

            yield return it.DoTransitionToBlack();
            SceneManager.LoadScene(scene.SceneName);
            yield return it.DoTransitionToScene();

            Destroy(it.gameObject);
        }
    }

#nullable disable

    [System.Serializable]
    public class SceneInitializerPrefabs {
        public DialogueManager dialogueManager;
        public AudioManager audioManager;
        public InventoryManager inventoryManager;
        public PauseMenu pauseMenu;
        public LevelManager levelManager;
        public ScreenShakeManager screenShakeManager;
        public HighlightManager highlightManager;
        public HUDManager HUDManager;
        public BuffManager buffManager;
        public Pathfinder pathfinder;
        public PlayerDataManager playerDataManager;
        public PlayerHealthVignettePPFX playerHealthFx;
        public TimeManager timeManager;
        public SpriteOutlineManager spriteOutlineManager;

        public List<MonoBehaviour> All => new() {
            dialogueManager,
            audioManager,
            inventoryManager,
            pauseMenu,
            levelManager,
            screenShakeManager,
            highlightManager,
            HUDManager,
            buffManager,
            pathfinder,
            playerDataManager,
            playerHealthFx,
            timeManager,
            spriteOutlineManager,
        };

        public List<MonoBehaviour> AllExcept(params MonoBehaviour[] exclude) {
            return All.Except(exclude).ToList();
        }
    }
}

#nullable enable

// Returns a singleton instance of type T found in the class above or else throws an exception.
// Careful with calling this on Awake, as some managers may not be initialized yet.
public static class SceneInitializerExtensions {
    public static T Inject<T>(this MonoBehaviour m) where T : MonoBehaviour {
        return m.InjectOptional<T>() ?? throw new KeyNotFoundException($"Manager of type {typeof(T)} not found. Make sure it is initialized in the scene or check initialization order if you called this on Awake.");
    }

    public static T? InjectOptional<T>(this MonoBehaviour m) where T : MonoBehaviour {
        return Initializers.SceneInitializer.Instance.CachedManagers.GetValueOrDefault(typeof(T)) as T;
    }
}
