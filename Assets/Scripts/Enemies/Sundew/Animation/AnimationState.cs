using UnityEngine;

namespace Enemies.Sundew.Animation {
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
