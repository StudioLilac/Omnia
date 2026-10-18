using System.Collections;
using NPC.Dinky;
using Players;
using Players.Mixin;
using UI;
using UnityEngine;
using Utils;

namespace Scenes.World5 {
    public class Lab_Epilogue : MonoBehaviour, IInteractable {
        [SerializeField] private Dinky dinky;
        [SerializeField] private SpriteRenderer dinkySpriteRenderer;
        [SerializeField] private GameObject boss;
        [SerializeField] private Animator bossAnimator;
        [SerializeField] private FadeScreenHandler fadeScreen;
        [SerializeField] private Interactable interactable;

        [SerializeField] Yarn.Unity.DialogueReference beginDialogue;
        private bool interacted = false; 
        public void Interact() {
            if (interacted) return;
            interacted = true;
            interactable.SetEnable(false);
            StartCoroutine(BeginSequence());
        }

        public void Start() {
            fadeScreen.SetLightScreen();
        }

        private IEnumerator BeginSequence() {
            yield return new WaitForSeconds(1f);

            bossAnimator.SetTrigger("Open");

            yield return new WaitUntil(() => bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("Opened"));

            Rigidbody2D rb = dinky.GetComponent<Rigidbody2D>();
            if (rb == null) {
                rb = dinky.gameObject.AddComponent<Rigidbody2D>();
            }

            rb.gravityScale = 1f;

            yield return StartCoroutine(FadeHelpers.FadeSpriteRendererColor(dinkySpriteRenderer, dinkySpriteRenderer.color, new Color(1,1,1), 1f));

            yield return DialogueManager.Instance.StartDialogue(beginDialogue);

            yield return StartCoroutine(fadeScreen.FadeInDarkScreen(3f));

            LevelManager.Instance.NextLevel();
        }


    }

}
