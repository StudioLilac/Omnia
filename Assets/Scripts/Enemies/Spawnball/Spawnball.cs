using System;
using System.Linq;
using Enemies.Spawnball.Animation;
using Enemies.Spawnball.Behaviour;
using Omnia.State;
using Players;
using UnityEngine;
using StateMachine = Omnia.State.StateMachine;

namespace Enemies.Spawnball {
    public class Spawnball : Enemy {
        [SerializeField] internal float activationRange;
        [SerializeField] internal float startDelay;
        [SerializeField] internal float spawnDelay;
        [SerializeField] internal float speed;
        [SerializeField] internal float smoothPath;
        [SerializeField] internal float airAcceleration;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Rigidbody2D rb;

        [SerializeField] internal Transform target;
        [SerializeField] internal Enemy spawn;
        [SerializeField] internal GameObject explosion;

        public Action<Spawnball> NotifyOnDestroy;
        public Action<Enemy> NotifyOnSpawn;

        public void OnDestroy() => NotifyOnDestroy?.Invoke(this);

        public void Awake() {
            UseBehaviour(new Idle(this));
        }

        public override void Update() {
            base.Update();
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg);
        }

        public void OnSpawnEnemy() {
            Enemy enemy = Instantiate(spawn, target.position, target.rotation);
            NotifyOnSpawn?.Invoke(enemy);
            Die();
            Instantiate(explosion, rb.worldCenterOfMass, Quaternion.identity);
        }

        protected override void UseAnimation(StateMachine stateMachine) {
            var idleAnim = new IdleAnimation(animator);
            stateMachine.AddAnyTransition(idleAnim, new FuncPredicate(() => behaviour is Idle or Move));
            stateMachine.AddAnyTransition(new ActivateAnimation(animator), new FuncPredicate(() => behaviour is Activate));

            stateMachine.SetState(idleAnim);
            animationStateMachine = stateMachine;
        }
    }
}
