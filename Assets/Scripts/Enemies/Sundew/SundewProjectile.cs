using System;
using Omnia.Utils;
using Players;
using UnityEngine;

namespace Enemies.Sundew {
    public class SundewProjectile : MonoBehaviour {
        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal LayerMask mask;
        [SerializeField] internal float time;
        [SerializeField] internal float terminalVelocity = -10f;

        public Action<Player, SundewProjectile> NotifyOnHit;

        public void Start() => Destroy(gameObject, time);

        public void Update() {
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg);
        }

        public void FixedUpdate() {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(terminalVelocity, rb.linearVelocity.y));
        }

        public void OnTriggerEnter2D(Collider2D other) {
            if (!CollisionUtils.IsLayerInMask(other.gameObject.layer, mask)) return;
            Destroy(gameObject);

            if (other.TryGetComponent<Player>(out var player)) NotifyOnHit?.Invoke(player, this);
        }
    }
}
