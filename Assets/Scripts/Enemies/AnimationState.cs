using UnityEngine;

namespace Enemies {
    public abstract class AnimationState : IState {
        private readonly string name;
        protected readonly Animator animator;

        protected AnimationState(string name, Animator animator) {
            this.name = name;
            this.animator = animator;
        }

        public virtual void OnEnter() {
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
