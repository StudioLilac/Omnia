using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

namespace NPC.Dinky {
    public class Dinky : MonoBehaviour, IInteractable {
        [SerializeField] private Interactable interactable;
        [SerializeField] private GameObject graphics;
        [SerializeField] public Animator animator;

        [SerializeField] private DialogueReference interactDialogue;

        [SerializeField] private List<Transform> locations;

        public static readonly int AppearTrigger = Animator.StringToHash("Appear");
        public static readonly int DisappearTrigger = Animator.StringToHash("Disappear");
        public static readonly int IdleTrigger = Animator.StringToHash("Idle");
        public static readonly int WalkTrigger = Animator.StringToHash("Walk");

        public static readonly int BrownWalkTrigger = Animator.StringToHash("BrownWalk");
        public static readonly int BrownIdleTrigger = Animator.StringToHash("BrownIdle");
        public static readonly int BrownDisappearTrigger = Animator.StringToHash("BrownBurrow");

        public static event Action OnInteract;
        private Coroutine walkCoroutine;

        private bool brownMode;

        private void Awake() {
            if (!animator && graphics) {
                animator = graphics.GetComponent<Animator>();
            }
        }

        public void TurnBrown(bool brown) {
            brownMode = brown;
            animator.SetTrigger(BrownIdleTrigger);
        }

        public void Appear(Transform t) {
            if (!animator) return;

            gameObject.transform.position = t.position;
            setVisible(true);
            animator.SetTrigger(AppearTrigger);
        }

        public void Disappear() {
            if (!animator) return;

            interactable?.SetEnable(false);

            if (brownMode) {
                animator.SetTrigger(BrownDisappearTrigger);
            } else {
                animator.SetTrigger(DisappearTrigger);
            }
            StartCoroutine(WaitForAnimation());
        }

        private IEnumerator WaitForAnimation() {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length); // Wait for the animation duration
            gameObject.SetActive(false);
        }

        public void Interact() {
            StartCoroutine(StartDialogue());
        }

        private IEnumerator StartDialogue() {
            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(interactDialogue));
            OnInteract?.Invoke();
        }

        public void Walk(float distance, float speed) {
            if (walkCoroutine != null) {
                StopCoroutine(walkCoroutine);
            }

            walkCoroutine = StartCoroutine(WalkRoutine(distance, speed));
        }

        private IEnumerator WalkRoutine(float distance, float speed) {
            if (!animator) yield break;

            if (brownMode) {
                animator.SetTrigger(BrownWalkTrigger);
            } else {
                animator.SetTrigger(WalkTrigger);
            }

            float startX = transform.position.x;
            float targetX = startX + distance;
            float direction = Mathf.Sign(distance); // 1 for right, -1 for left

            // Flip Dinky to face the correct direction
            Vector3 scale = transform.localScale;
            scale.x = -direction * Mathf.Abs(scale.x);
            transform.localScale = scale;

            while (Mathf.Abs(transform.position.x - startX) < Mathf.Abs(distance)) {
                transform.position += new Vector3(direction * speed * Time.deltaTime, 0, 0);
                yield return null;
            }

            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

            if (brownMode) {
                animator.SetTrigger(BrownIdleTrigger);
            } else {
                animator.SetTrigger(IdleTrigger);
            }
        }


        private void setVisible(bool visible) {
            if (graphics) {
                Renderer renderer = graphics.GetComponentInChildren<Renderer>();
                if (renderer) renderer.enabled = visible;
            }
        }
    }
}
