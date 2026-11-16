using System.Collections;
using UnityEngine;

namespace UI {
    public class SceneTransition : MonoBehaviour {
        private static readonly int ToBlack = Animator.StringToHash("TransitionToBlack");
        private static readonly int ToScene = Animator.StringToHash("TransitionToScene");

        [SerializeField] internal Animator animator;
        [SerializeField] internal float time = 1.0f;

        public void Awake() {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = animator.GetCurrentAnimatorStateInfo(0).length / time;
        }

        public IEnumerator DoTransitionToBlack() {
            animator.Play(ToBlack);
            yield return new WaitForSecondsRealtime(time);
        }

        public IEnumerator DoTransitionToScene() {
            animator.Play(ToScene);
            yield return new WaitForSecondsRealtime(time);
        }
    }
}
