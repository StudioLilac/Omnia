// Place in a folder named "Editor", e.g. Assets/Editor/ReplaceEventSystems.cs

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Editor {
    public class ReplaceEventSystemsWindow : EditorWindow {
        [SerializeField] private GameObject prefab;

        [MenuItem("Tools/Replace EventSystems With Prefab")]
        private static void Open() {
            GetWindow<ReplaceEventSystemsWindow>("Replace EventSystems");
        }

        private void OnGUI() {
            EditorGUILayout.HelpBox(
                "Replaces every EventSystem GameObject in every scene under Assets/ with an instance of the prefab below. " +
                "Scenes are saved automatically. Commit or back up first.",
                MessageType.Info);

            prefab = (GameObject)EditorGUILayout.ObjectField("EventSystem Prefab", prefab, typeof(GameObject), false);

            bool valid = IsValidPrefab(prefab);
            if (prefab != null && !valid) {
                EditorGUILayout.HelpBox("Pick a prefab asset that has an EventSystem component.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!valid)) {
                if (GUILayout.Button("Replace In All Scenes")) {
                    ReplaceAll(prefab);
                }
            }
        }

        private static bool IsValidPrefab(GameObject go) {
            return go != null
                   && PrefabUtility.IsPartOfPrefabAsset(go)
                   && go.GetComponentInChildren<EventSystem>(true) != null;
        }

        private static void ReplaceAll(GameObject prefab) {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            string[] paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            int replaced = 0;
            int skipped = 0;

            try {
                for (int i = 0; i < paths.Length; i++) {
                    EditorUtility.DisplayProgressBar("Replacing EventSystems", paths[i], (float)i / paths.Length);

                    Scene scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                    int count = ReplaceInScene(scene, prefab, paths[i], ref skipped);

                    if (count > 0) {
                        EditorSceneManager.SaveScene(scene);
                        replaced += count;
                    }
                }
            } finally {
                EditorUtility.ClearProgressBar();
                if (originalSetup.Length > 0) {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            Debug.Log($"Replaced {replaced} EventSystem(s) across {paths.Length} scene(s). Skipped {skipped}.");
        }

        private static int ReplaceInScene(Scene scene, GameObject prefab, string scenePath, ref int skipped) {
            var systems = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                .ToArray();

            int count = 0;

            foreach (var es in systems) {
                GameObject go = es.gameObject;

                if (IsInstanceOfPrefab(go, prefab)) continue; // already converted

                if (!CanReplace(go, out string reason)) {
                    Debug.LogWarning($"Skipped '{go.name}' in {scenePath}: {reason}", go);
                    skipped++;
                    continue;
                }

                Transform parent = go.transform.parent;
                int siblingIndex = go.transform.GetSiblingIndex();

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(parent, false);
                instance.transform.SetSiblingIndex(siblingIndex);

                Object.DestroyImmediate(go);
                count++;
            }

            return count;
        }

        private static bool IsInstanceOfPrefab(GameObject go, GameObject prefab) {
            if (!PrefabUtility.IsPartOfPrefabInstance(go)) return false;
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            return PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab;
        }

        // Only replaces "plain" EventSystem objects so nothing custom is destroyed by accident.
        private static bool CanReplace(GameObject go, out string reason) {
            if (PrefabUtility.IsPartOfPrefabInstance(go) && PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go) {
                reason = "it is nested inside another prefab instance (edit that prefab instead).";
                return false;
            }

            if (go.transform.childCount > 0) {
                reason = "it has child objects.";
                return false;
            }

            foreach (Component c in go.GetComponents<Component>()) {
                if (c == null) continue; // missing script, safe to drop
                if (c is Transform || c is EventSystem || c is BaseInputModule) continue;

                reason = $"it has an extra component ({c.GetType().Name}).";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
