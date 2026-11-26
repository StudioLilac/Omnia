using System;
using Omnia.Rendering;
using Omnia.Utils;
using Players;
using UnityEngine;

namespace Enemies.Sundew {
    public class SundewProjectile : MonoBehaviour {
        private SpriteOutlineManager outlineManager;
        private Collider2D hitbox;
        private Vector2 groundCheckStart;
        private int groundMask;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal float time;

        public Action<Player, SundewProjectile> NotifyOnHit;

        public void Awake() {
            hitbox = GetComponent<Collider2D>();
        }

        public void Start() {
            outlineManager = this.Inject<SpriteOutlineManager>();
            outlineManager.PushColor(this);
        }

        public void OnDestroy() {
            if (outlineManager != null) outlineManager.OnEntityDestroyed(this);
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
            Destroy(gameObject, time);
        }

        public void Update() {
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg);
        }

        public void FixedUpdate() {
            if (!hitbox.enabled || rb.position.y > groundCheckStart.y) return;
            if (Physics2D.Raycast(groundCheckStart, rb.linearVelocity.normalized,
                    Vector2.Distance(groundCheckStart, rb.position) + hitbox.bounds.extents.y, groundMask)) Destroy(gameObject);
        }

        public void OnTriggerEnter2D(Collider2D other) {
            if (!other.TryGetComponent<Player>(out var player)) return;
            Destroy(gameObject);
            NotifyOnHit?.Invoke(player, this);
        }
    }
}
