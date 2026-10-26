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

    public class StunnedAnimation : IState {
        private readonly Animator animator;
        private readonly int idleState;
        private readonly float idleStunPoint;
        private float speedBeforeStun = 1f;
        private GameObject stunEffect;
        private bool active;

        public StunnedAnimation(Enemy enemy, Animator animator, float idleStunPoint) {
            this.animator = animator;
            this.idleStunPoint = idleStunPoint;
            idleState = Animator.StringToHash($"Base Layer.{enemy.GetType().Name}Idle");
        }

        public void OnEnter() {
            if (!animator || !animator.runtimeAnimatorController || !animator.HasState(0, idleState)) return;

            active = true;
            speedBeforeStun = animator.speed;
            animator.Play(idleState, 0, idleStunPoint);
            animator.Update(0f);
            animator.speed = 0f;

            stunEffect = animator.transform.Find("StunEffect")?.gameObject;
            if (stunEffect) stunEffect.SetActive(true);
        }

        public void Update() {
        }

        public void FixedUpdate() {
        }

        public void OnExit() {
            if (!active) return;

            active = false;
            animator.speed = speedBeforeStun;
            if (stunEffect) stunEffect.SetActive(false);
            stunEffect = null;
        }
    }
}
