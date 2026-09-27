using System.Collections;
using Background;
using Initializers;
using Players;
using Players.Buff;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;


namespace Scenes {
    public class WarpedDepths : MonoBehaviour
    {
        [SerializeField] private GameObject dustParent;
        [SerializeField] private FadeScreenHandler fadeScreen;

        private Image[] dustImages;

        protected void Start() {
            fadeScreen.SetLightScreen();
            dustImages = dustParent.GetComponentsInChildren<Image>();

            foreach (var img in dustImages) {
                img.gameObject.AddComponent<FloatingImage>();
            }

            DisablePersistentSingletons.DisableHUD();
            DisablePersistentSingletons.DisableInventory();
            DisablePersistentSingletons.DisablePause();
        }

        public void Back() {
            LevelManager.Instance.PrevLevel();
        }

        public void StartWarpedDepths() {
            StartCoroutine(StartSequence());

        }

        public IEnumerator StartSequence() {
            PlayerDataManager.Instance.warpedDepthsProgress = 1;
            BuffManager.Instance.ResetFragmentPoolToOriginal();
            yield return fadeScreen.FadeInDarkScreen(3f);
            LevelManager.Instance.NextLevel();
        }

    }


}
