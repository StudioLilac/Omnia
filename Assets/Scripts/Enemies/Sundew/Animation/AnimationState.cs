using UnityEngine;

namespace Enemies.Sundew.Animation {
    public class IdleAnimation : AnimationState {
        public IdleAnimation(Animator animator) : base("SundewIdle", animator) {
        }

        public override void OnEnter() {
            animator.Play("SundewIdle", 0, 0f);
        }
    }

    public class AttackAnimation : AnimationState {
        public AttackAnimation(Animator animator) : base("SundewAttack", animator) {
        }

        public override void OnEnter() {
            animator.Play("SundewAttack", 0, 0f);
        }
    }
}
