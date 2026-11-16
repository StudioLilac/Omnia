using UnityEngine;

namespace Enemies.Sundew.Animation {
    // Hold the last attack frame while the Sundew has no glue.
    public class NoGlueAnimation : AnimationState {
        public NoGlueAnimation(Animator animator) : base("SundewAttack", animator) {
        }

        public override void OnEnter() {
            animator.Play("Base Layer.SundewAttack", 0, 1f);
        }
    }

    public class IdleAnimation : AnimationState {
        public IdleAnimation(Animator animator) : base("SundewIdle", animator) {
        }

        public override void OnEnter() {
            animator.Play("Base Layer.SundewIdle", 0, 0f);
        }
    }

    public class AttackAnimation : AnimationState {
        public AttackAnimation(Animator animator) : base("SundewAttack", animator) {
        }

        public override void OnEnter() {
            animator.Play("Base Layer.SundewAttack", 0, 0f);
        }
    }
}
