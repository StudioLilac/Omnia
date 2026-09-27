using Enemies.BirdSpawner.Animation;
using Enemies.BirdSpawner.Behaviour;
using Omnia.State;
using UnityEngine;
using Utils;
using StateMachine = Omnia.State.StateMachine;

namespace Enemies.BirdSpawner {
    public class BirdSpawner : Enemy {
        [SerializeField] internal float cooldown;
        [SerializeField] internal int spawns;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal LayerMask bg;
        [SerializeField] internal GameObject spawnable;

        public void Awake() {
            UseBehaviour(new Idle(this));
        }

        public override void Hurt(float damage, bool stagger = true, bool crit = false) {
            if (currentHealth == 0) return;
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maximumHealth);

            /* Become inactive on "death" without being destroyed. */
            if (currentHealth == 0) UseBehaviour(new Dead(this));
        }

        public void SetLayer(LayerMask it) {
            gameObject.layer = MathUtils.LayerIndexOf(it);
        }

        public void SpawnNewBird() {
            var b = Instantiate(spawnable, transform.position, Quaternion.identity).GetComponent<Bird.Bird>();
            b.NotifyOnDestroy = _ => spawns++;
            spawns--;
            b.rb.linearVelocity = Random.insideUnitSphere * b.speed;
        }

        protected override void UseAnimation(StateMachine stateMachine) {
        }
    }
}
