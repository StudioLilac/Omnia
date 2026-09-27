using Enemies;
using UnityEngine;

namespace Players.Fragments {
    public class Orbital : MonoBehaviour {
        public float damage;
        private Player player;

        private void Start() {
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        }

        private void OnTriggerEnter2D(Collider2D other) {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.Hurt(damage * player.damageMultiplier);
            }
        }
    }
}
