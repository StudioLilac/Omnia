using Enemies.Armadillo;
using System.Collections;
using UI;
using UnityEngine;
using Utils;
using Yarn.Unity;

namespace Scenes.World5 {
    public class Surface : MonoBehaviour {
        [SerializeField] private FloatCamera floatCamera;
        [SerializeField] private FadeScreenHandler fadeScreen;

        [SerializeField] private DialogueReference dialogue;

        public void Start() {
            HUDManager.Instance.gameObject.SetActive(false);
            StartCoroutine(StartSequence());
        }

        private IEnumerator StartSequence() {
            fadeScreen.SetDarkScreen();
            StartCoroutine(fadeScreen.FadeInLightScreen(3f));

            yield return new WaitForSeconds(2f);
            StartCoroutine(PlayDialogue());
            yield return new WaitForSeconds(2f);
            floatCamera.StartFloating();
        }

        private IEnumerator PlayDialogue() {
            yield return DialogueManager.Instance.StartDialogue(dialogue);

            yield return new WaitForSeconds(3f);
            HUDManager.Instance.gameObject.SetActive(true);
            LevelManager.Instance.NextLevel();

        }

    }
}
