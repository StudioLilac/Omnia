using UnityEngine;

namespace Enemies.Sundew.Animation {
    public class IdleAnimation : AnimationState {
        public IdleAnimation(Animator animator) : base("SundewIdle", animator) {
        }
    }

    public class AttackAnimation : AnimationState {
        public AttackAnimation(Animator animator) : base("SundewAttack", animator) {
        }
    }

    public class StunnedAnimation : AnimationState {
        public StunnedAnimation(Animator animator) : base("SundewStunned", animator) {
        }
    }
}
