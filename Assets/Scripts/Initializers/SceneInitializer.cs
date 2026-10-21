using Enemies.Common;
using FX;
using Players;
using Players.Buff;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

#nullable enable
namespace Initializers {
    public interface IScenePayload { };

    public class SceneInitializer : PersistentSingleton<SceneInitializer> {
        [SerializeField] internal GameObject[] managers;
        [SerializeField] internal GameObject transition;
        [SerializeField] internal SceneInitializerPrefabs prefabs;
        private IScenePayload? scenePayload;

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
            var requiredManagers = sceneData.Init(prefabs);
            var managersParent = new GameObject("[SceneInitialized]");

            foreach (var managerPrefab in requiredManagers) {
                var existing = FindAnyObjectByType(managerPrefab.GetType());
                if (existing != null) {
                    if (existing is not IPersistentSingleton)
                        Debug.LogWarning($"{managerPrefab.GetType()} already exists!");
                    continue;
                }

                InstantiatePrefab(managerPrefab, managersParent);
            }
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

        public List<MonoBehaviour> Always => new List<MonoBehaviour>() {
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
        };
    }
}
