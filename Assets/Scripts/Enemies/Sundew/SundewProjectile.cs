using System;
using Omnia.Rendering;
using Omnia.Utils;
using Players;
using UnityEngine;
using Utils;

namespace Enemies.Sundew {
    public class SundewProjectile : MonoBehaviour {
        private SpriteOutlineManager outlineManager;
        private Collider2D hitbox;
        private bool skyShot;
        private bool falling;
        private Vector2 groundCheckStart;
        private int groundMask;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal LayerMask mask;
        [SerializeField] internal float time;
        [SerializeField] internal float terminalVelocity = -10f;

        public Action<Player, SundewProjectile> NotifyOnHit;

        public void Start() {
            outlineManager = this.Inject<SpriteOutlineManager>();
            outlineManager.PushColor(this);
            if (!skyShot) Destroy(gameObject, time);
        }

        public void OnDestroy() {
            if (outlineManager != null) outlineManager.OnEntityDestroyed(this);
        }

        public void ConfigureSkyShot() {
            skyShot = true;
            hitbox = GetComponent<Collider2D>();
            hitbox.enabled = false;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void BeginCurve() {
            rb.simulated = false;
        }

        public void FollowCurve(Vector2 origin, Vector2 control1, Vector2 control2, Vector2 end, float t, float duration) {
            var a = Vector2.Lerp(origin, control1, t);
            var b = Vector2.Lerp(control1, control2, t);
            var c = Vector2.Lerp(control2, end, t);
            var d = Vector2.Lerp(a, b, t);
            var e = Vector2.Lerp(b, c, t);
            var position = Vector2.Lerp(d, e, t);
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            rb.position = position;
            rb.linearVelocity = 3f * (e - d) / duration;
        }

        public void BeginFall(Vector2 direction, float speed, Vector2 groundCheckStart, int groundMask) {
            this.groundCheckStart = groundCheckStart;
            this.groundMask = groundMask;
            rb.simulated = true;
            rb.linearVelocity = direction * speed;
            hitbox.enabled = true;
            falling = true;
            Destroy(gameObject, time);
        }

        public void Update() {
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg);
        }

        public void FixedUpdate() {
            if (!skyShot) {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(terminalVelocity, rb.linearVelocity.y));
                return;
            }
            if (!falling || rb.position.y > groundCheckStart.y) return;
            if (Physics2D.Raycast(groundCheckStart, rb.linearVelocity.normalized,
                    Vector2.Distance(groundCheckStart, rb.position) + hitbox.bounds.extents.y, groundMask)) Destroy(gameObject);
        }

        public void OnTriggerEnter2D(Collider2D other) {
            if (!CollisionUtils.IsLayerInMask(other.gameObject.layer, mask)) return;
            if (skyShot && !other.TryGetComponent<Player>(out _)) return;
            Destroy(gameObject);

            if (other.TryGetComponent<Player>(out var player)) NotifyOnHit?.Invoke(player, this);
        }
    }
}
