using UnityEngine;
using System.Collections;

namespace Players.Mixin {
    public class HandleLayerCollision : MonoBehaviour {
        [SerializeField] private Player self;
        [SerializeField] private Collider2D semisolidCollider;

        private bool fell = true;

        void FixedUpdate() {
            if (self.moving.y < 0) {
                Physics2D.IgnoreCollision(self.hitbox, semisolidCollider, true);
            } else if (self.moving.y >= 0 && fell) {
                StartCoroutine(ReenableCollisionAfterFall());
            }
        }

        private IEnumerator ReenableCollisionAfterFall() {
            fell = false;

            while (self.grounded) {
                yield return null;
            }

            Physics2D.IgnoreCollision(self.hitbox, semisolidCollider, false);
            fell = true;
        }

        // For testing only. Game code must not call this.
        internal void SetSemisolidColliderForTesting(Collider2D collider) => semisolidCollider = collider;
    }
}
