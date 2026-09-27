using System.Collections.Generic;
using System.Linq;
using Enemies;
using UnityEngine;

namespace Players.Fragments {
    public class Shatterburst : Fragment {
        [SerializeField] private GameObject explosion;
        [SerializeField] private float damage;
        [SerializeField] private float explosionRadius;
        [SerializeField] private float knockbackForce;
        public override void ApplyBuff() {
            base.ApplyBuff();
            HarpoonSpear.OnHitEnemy += Explode;
        }

        public override void RevokeBuff() {
            HarpoonSpear.OnHitEnemy -= Explode;
        }

        private void Explode(Transform t) {
            Instantiate(explosion, t.position, Quaternion.identity);
            AudioManager.Instance.PlaySFX(AudioTracks.FlyBoom);

            Collider2D[] hits = Physics2D.OverlapCircleAll(player.Center, explosionRadius);

            foreach (var hit in hits) {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null) {
                    enemy.Hurt(damage * player.damageMultiplier);

                    Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
                    if (enemyRb != null) {
                        Vector2 knockbackDirection = (enemy.transform.position - player.Center).normalized;
                        enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }

        }
    }
}
