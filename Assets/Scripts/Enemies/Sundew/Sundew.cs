using Enemies.Sundew.Behaviour;
using Enemies.Sundew.Animation;
using Omnia.State;
using Players;
using UnityEngine;

namespace Enemies.Sundew {
    public class Sundew : Enemy {
        [SerializeField] internal float windup;
        [SerializeField] internal float reload;
        [SerializeField] internal float spread;
        [SerializeField] internal float lag;
        [SerializeField] internal int count;
        [SerializeField] internal float projectileAngle;
        [SerializeField] internal float projectileSpeed;
        [SerializeField] internal float randomAttackDelayOffset;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal LayerMask ground;
        [SerializeField] internal LayerMask player;
        [SerializeField] internal GameObject projectile;

        [SerializeField] internal GameObject deathExplosion;

        public void Awake() {
            UseBehaviour(new Idle(this));
        }

        public override void Die() {
            base.Die();
            Instantiate(deathExplosion, rb.worldCenterOfMass, Quaternion.identity);
        }

        public void FireProjectiles() {
            var step = count == 1 ? 0 : projectileAngle / (count - 1);
            var initial = projectileAngle / 2;
            for (var a = initial * -1; a <= initial; a += step) FireProjectile(Quaternion.Euler(0, 0, a) * Vector2.up);
            AudioManager.Instance.PlaySFX(AudioTracks.PlantShoot);
        }

        private void FireProjectile(Vector2 direction) {
            var p = Instantiate(projectile, rb.worldCenterOfMass, Quaternion.identity).GetComponent<SundewProjectile>();
            p.NotifyOnHit = Attack;
            p.rb.linearVelocity = projectileSpeed * Vector2.Lerp(transform.rotation * direction.normalized, Random.insideUnitSphere, spread);
        }

        private void Attack(Player it, SundewProjectile by) {
            it.Hurt(attack, knockbackForce * new Vector2(Mathf.Sign(by.rb.linearVelocity.x) * Mathf.Cos(knockbackAngle * Mathf.Deg2Rad), Mathf.Sin(knockbackAngle * Mathf.Deg2Rad)), 1);
        }

        protected override void UseAnimation(StateMachine stateMachine) {
            var idleAnim = new IdleAnimation(animator);
            stateMachine.AddAnyTransition(idleAnim, new FuncPredicate(() => behaviour is Idle));
            stateMachine.AddAnyTransition(new AttackAnimation(animator), new FuncPredicate(() => behaviour is Attack));

            stateMachine.SetState(idleAnim);
            animationStateMachine = stateMachine;
        }
    }
}
