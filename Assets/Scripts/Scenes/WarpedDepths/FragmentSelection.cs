using System.Collections;
using System.Collections.Generic;
using Background;
using Initializers;
using Players.Buff;
using Players.Fragments;
using Scenes.Descent;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;


namespace Scenes {
    public class FragmentSelection : MonoBehaviour
    {
        [SerializeField] private GameObject dustParent;

        private Image[] dustImages;

        private void Start() {
            dustImages = dustParent.GetComponentsInChildren<Image>();

            foreach (var img in dustImages) {
                img.gameObject.AddComponent<FloatingImage>();
            }

            DisablePersistentSingletons.DisableHUD();
            DisablePersistentSingletons.DisableInventory();
            DisablePersistentSingletons.DisablePause();

            RandomFragments();
            AudioManager.Instance.PlayBGM(AudioTracks.JamiesTheme);
        }

        private void RandomFragments() {
            FragmentChoice[] choices = FindObjectsOfType<FragmentChoice>();
            List<Fragment> fragmentOptions = BuffManager.Instance.GetRandomizedFragments(choices.Length);

            int count = Mathf.Min(choices.Length, fragmentOptions.Count);

            for (int i = 0; i < count; i++) {
                choices[i].SetFragment(fragmentOptions[i]);
            }
        }

        public void StartLevel() {
            LevelManager.Instance.NextLevel();
        }

        #if UNITY_EDITOR
        private void Update() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                RandomFragments();
            }
        }

        #endif

    }


}
