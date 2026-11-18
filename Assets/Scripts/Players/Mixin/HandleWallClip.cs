using UnityEngine;

namespace Players.Mixin {
    public class HandleWallClip : MonoBehaviour {
        [SerializeField] internal BoxCollider2D check;
        [SerializeField] internal Player player;
        private LayerMask ground;

        void Start() {
            ground = player.ground;
            // Ensure the collider is set as a trigger if using triggers
            check.isTrigger = true;
        }

        // Called once when another collider enters the trigger zone
        void OnTriggerEnter2D(Collider2D other) {
            if (player.hitbox.bounds.max.y < check.bounds.min.y) return;
            if (IsGroundLayer(other.gameObject.layer)) {
                Debug.Log("Player entered encased state.");
                player.Die();
            }
        }

        bool IsGroundLayer(int layer) {
            // Check if the object's layer matches the ground layer mask
            return (ground.value & (1 << layer)) != 0;
        }
    }
}
