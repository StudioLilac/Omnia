using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Players.Fragments {
    public class ShortFuse : Fragment {
        [SerializeField] internal GameObject explosion;
        [SerializeField] internal float explosionRadius;
        [SerializeField] internal float damage;
        [SerializeField] internal float knockbackForce;
        private float Explode(float incomingDamage) {
            Instantiate(explosion, player.Center, Quaternion.identity);
            AudioManager.Instance.PlaySFX(AudioTracks.FlyBoom);
            Collider2D[] hits = Physics2D.OverlapCircleAll(player.Center, explosionRadius);

            foreach (var hit in hits) {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null) {
                    enemy.Hurt(damage);

                    Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
                    if (enemyRb != null) {
                        Vector2 knockbackDirection = (enemy.transform.position - player.Center).normalized;
                        enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }

            return incomingDamage;
        }

        public override void ApplyBuff() {
            base.ApplyBuff();
            OnDamageTaken.AddLast(Explode);
        }

        public override void RevokeBuff() {
            OnDamageTaken.Remove(Explode);
        }
    }
}
