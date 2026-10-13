using System.Collections.Generic;
using System.Linq;
using Enemies.Armadillo.Animation;
using Enemies.Armadillo.Behaviour;
using Enemies.Common.Behaviour;
using Omnia.State;
using Players;
using UnityEngine;

namespace Enemies.Armadillo {
    public class Armadillo : ShieldedEnemy {
        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal LayerMask ground;
        [SerializeField] internal LayerMask player;
        [SerializeField] internal Collider2D[] checks;
        [SerializeField] internal Vector2 facing = Vector2.right;

        [SerializeField] internal float attackRadius = 0.5f;
        [SerializeField] internal float moveSpeed = 1.0f;
        [SerializeField] internal float rollSpeed = 4.0f;
        [SerializeField] internal float idleDuration = 0.5f;
        [SerializeField] internal float alertTime = 1.0f;
        [SerializeField] internal float chargeTime = 1.0f;
        [SerializeField] internal float recoilTime = 1.0f;
        [SerializeField] internal float uncurlTime = 1.0f;
        [SerializeField] internal float detectionRange = 5.0f;
        [SerializeField] internal float recoilAngle = 45;
        [SerializeField] internal float recoilSpeed = 5.0f;

        [SerializeField] internal GameObject deathExplosion;
        [SerializeField] internal ParticleSystem dustParticles;

#nullable enable
        internal Player? aggrodPlayer;
        public void Awake() {
            UseBehaviour(new Move(this));
        }

        public override void Update() {
            base.Update();
            sprite.flipX = facing.x == 0 ? sprite.flipX : facing.x > 0;
        }

        public override void Die() {
            base.Die();
            Instantiate(deathExplosion, rb.worldCenterOfMass, Quaternion.identity);
        }

        public void Attack(Player it) {
            it.Hurt(attack, knockbackForce * new Vector2(facing.x * Mathf.Cos(knockbackAngle * Mathf.Deg2Rad), Mathf.Sin(knockbackAngle * Mathf.Deg2Rad)), 1);
            AudioManager.Instance.PlaySFX(AudioTracks.ArmadilloAttack);
        }

        public bool IsReversing() {
            var c = checks.Select(it => it.IsTouchingLayers(ground)).ToArray();
            var l = c[0] || !c[1];
            var r = c[3] || !c[2];
            return (facing.x > 0 && l && !r) || (facing.x < 0 && !l && r);
        }
        public void FaceAggrodPlayer() {
            if (aggrodPlayer != null) {
                facing = new Vector2(
                    (aggrodPlayer.transform.position - transform.position).x >= 0 ? 1 : -1
                    , 0
                    );
            }
        }

        public bool IsTargetDetected() {
            List<RaycastHit2D> sweptCast = new (Sweep(rb.worldCenterOfMass, facing, 45, detectionRange, 5, player));
            RaycastHit2D sweepPlayer = sweptCast.Find(hit => IsOnLayer(hit, player));

            if (sweepPlayer) {
                var playerComponent = sweepPlayer.collider.GetComponent<Player>();
                if (playerComponent != null) {
                    aggrodPlayer = playerComponent;
                    return true;
                }
            }

            return false;
        }

        protected override void UseAnimation(StateMachine stateMachine) {
            var idleAnim = new IdleAnimation(animator);
            stateMachine.AddAnyTransition(idleAnim, new FuncPredicate(() => behaviour is Idle));
            stateMachine.AddAnyTransition(new MoveAnimation(animator), new FuncPredicate(() => behaviour is Move));
            stateMachine.AddAnyTransition(new AlertAnimation(animator), new FuncPredicate(() => behaviour is Alert));
            stateMachine.AddAnyTransition(new RollAnimation(animator), new FuncPredicate(() => behaviour is Charge or Roll));
            stateMachine.AddAnyTransition(new RecoilAnimation(animator), new FuncPredicate(() => behaviour is Recoil));
            stateMachine.AddAnyTransition(new UncurlAnimation(animator), new FuncPredicate(() => behaviour is Uncurl));

            stateMachine.SetState(idleAnim);
            animationStateMachine = stateMachine;
        }
    }
}
