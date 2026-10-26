using UnityEngine;

namespace Enemies {
    public abstract class AnimationState : IState {
        private readonly string name;
        private readonly Animator animator;

        protected AnimationState(string name, Animator animator) {
            this.name = name;
            this.animator = animator;
        }

        public void OnEnter() {
            animator.CrossFade(name, 0.1f);
        }

        public void Update() {
        }

        public void FixedUpdate() {
        }

        public void OnExit() {
        }
    }
}
