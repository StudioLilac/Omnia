using System;
using Enemies;
using UnityEngine;

namespace Players.Fragments {
    public class SheathObject : MonoBehaviour {
        [SerializeField] private ParticleSystem sheathEffect;
        public float damage;
        public float velocityThreshold;

        private Player player;
        private bool active;
        private bool wasActive;

        private void Start() {
            player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
            if (sheathEffect == null) {
                Debug.LogWarning("Sheath Particle System is not assigned!");
            }
        }

        private void Update() {
            bool shouldBeActive = player.rb.velocity.y < -velocityThreshold;

            if (shouldBeActive && !wasActive) {
                ActivateEffect();
            } else if (!shouldBeActive && wasActive) {
                DeactivateEffect();
            }

            wasActive = shouldBeActive;
            active = shouldBeActive;
        }

        private void ActivateEffect() {
            if (sheathEffect != null) {
                sheathEffect.Play(); // Play particle effect
            }
            player.invulnerable = true;
        }

        private void DeactivateEffect() {
            if (sheathEffect != null) {
                sheathEffect.Stop(); // Stop particle effect
            }
            player.invulnerable = false;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.Hurt(damage);
            }
        }
    }
}
