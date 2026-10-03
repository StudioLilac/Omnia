using System;
using System.Linq;
using Enemies.Bird.Animation;
using Enemies.Bird.Behaviour;
using Omnia.State;
using Players;
using UnityEngine;
using StateMachine = Omnia.State.StateMachine;

namespace Enemies.Bird {
    public class Bird : Enemy {
        [SerializeField] internal float detectionRadius;
        [SerializeField] internal float fuse;
        [SerializeField] internal float triggerDistance;
        [SerializeField] internal float explosionRadius;
        [SerializeField] internal float delay;
        [SerializeField] internal float speed;
        [SerializeField] internal float airAcceleration;
        [SerializeField] internal float screenShakeIntensity;
        [SerializeField] internal float screenShakeDuration;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal GameObject explosion;
        [SerializeField] internal LayerMask ground;
        [SerializeField] internal LayerMask player;
        [SerializeField] internal Player targetInstance;

        public Action<Bird> NotifyOnDestroy;

        public void OnDestroy() => NotifyOnDestroy?.Invoke(this);

        public void Awake() {
            targetInstance ??= FindObjectsByType<Player>().FirstOrDefault();
            UseBehaviour(new Idle(this));
        }

        public override void Update() {
            base.Update();
            sprite.flipX = rb.linearVelocity.x == 0 ? sprite.flipX : rb.linearVelocity.x > 0;
        }

        public void OnExplode() {
            Attack();
            Die();
        }

        public override void Die() {
            ScreenShakeManager.Instance.Shake(screenShakeIntensity, screenShakeDuration);

            base.Die();
            Instantiate(explosion, rb.worldCenterOfMass, Quaternion.identity);
        }

        public bool IsTargetDetected() =>
            targetInstance &&
            Sweep(rb.worldCenterOfMass, targetInstance.rb.worldCenterOfMass - rb.worldCenterOfMass, 45, detectionRadius, 5, ground | player).Any(hit => IsOnLayer(hit, player));

        private void Attack() {
            AudioManager.Instance.PlaySFX(AudioTracks.FlyBoom);
            var hit = Physics2D.OverlapCircle(rb.worldCenterOfMass, explosionRadius, player);
            if (!hit || !hit.TryGetComponent<Player>(out var it)) return;
            var direction = it.rb.worldCenterOfMass - rb.worldCenterOfMass;

            it.Hurt(attack, direction.normalized * knockbackForce, 5);
        }

        protected override void UseAnimation(StateMachine stateMachine) {
            var idleAnim = new IdleAnimation(animator);
            stateMachine.AddAnyTransition(idleAnim, new FuncPredicate(() => behaviour is Idle));
            stateMachine.AddAnyTransition(new AlertAnimation(animator), new FuncPredicate(() => behaviour is Alert));
            stateMachine.AddAnyTransition(new AttackAnimation(animator), new FuncPredicate(() => behaviour is Attack));

            stateMachine.SetState(idleAnim);
            animationStateMachine = stateMachine;
        }
    }
}
