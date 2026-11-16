using System.Collections;
using System.Collections.Generic;
using Enemies.Sundew.Behaviour;
using Enemies.Sundew.Animation;
using Enemies.Common.Behaviour;
using Omnia.State;
using Players;
using UnityEngine;

namespace Enemies.Sundew {
    public class Sundew : Enemy {
        public override EnemyWeightType WeightType => EnemyWeightType.Heavy;
        internal override IBehaviour PostStunBehaviour => new Idle(this);

        [SerializeField] internal float windup;
        [SerializeField] internal float reload;
        [SerializeField] internal float attackTimingMultiplier = 3f;
        [SerializeField] internal float lag;
        [SerializeField] internal float projectileSpeed;
        [SerializeField] private float projectileVelocityMultiplier = 4.5f;
        [SerializeField] internal float randomAttackDelayOffset;
        [SerializeField] private float warningDuration = 1f;
        [SerializeField] private float fallSpeed = 12f;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal LayerMask ground;
        [SerializeField] internal LayerMask player;
        [SerializeField] internal GameObject projectile;

        [SerializeField] internal GameObject deathExplosion;

        private const int VolleySize = 5;
        private const float LaunchSpacing = 0.25f;
        private readonly List<SundewProjectile> activeProjectiles = new();
        private GameObject activeTelegraph;
        private bool volleyActive;
        private static Material telegraphMaterial;

        internal bool VolleyActive => volleyActive;

        public void Awake() {
            UseBehaviour(new Idle(this));
        }

        public override void Die() {
            base.Die();
            Instantiate(deathExplosion, rb.worldCenterOfMass, Quaternion.identity);
        }

        public override void OnDestroy() {
            foreach (var shot in activeProjectiles) {
                if (shot != null) Destroy(shot.gameObject);
            }
            if (activeTelegraph != null) Destroy(activeTelegraph);
            base.OnDestroy();
        }

        public void FireProjectiles() {
            if (!volleyActive) StartCoroutine(FireVolley());
        }

        private IEnumerator FireVolley() {
            volleyActive = true;
            activeProjectiles.RemoveAll(shot => shot == null);
            var shots = new SundewProjectile[VolleySize];
            var launchOffsets = new float[VolleySize];
            var camera = Camera.main;
            var firstLaunchTime = Time.time;
            AudioManager.Instance.PlaySFX(AudioTracks.PlantShoot);

            for (var i = 0; i < VolleySize; i++) {
                if (i > 0) yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
                launchOffsets[i] = Time.time - firstLaunchTime;
                var launchPosition = rb.worldCenterOfMass + new Vector2((i - 2) * LaunchSpacing, 0f);
                var shot = Instantiate(projectile, launchPosition, Quaternion.identity).GetComponent<SundewProjectile>();
                shot.ConfigureSkyShot();
                shot.NotifyOnHit = Attack;
                shot.rb.linearVelocity = Vector2.up * projectileSpeed * projectileVelocityMultiplier;
                shots[i] = shot;
                activeProjectiles.Add(shot);
            }
            while (shots[VolleySize - 1] != null && !AboveScreen(shots[VolleySize - 1].transform.position, camera)) yield return null;
            foreach (var shot in shots) if (shot != null) shot.BeginCurve();

            // Aim once all five shots have left the view.
            var target = FindFirstObjectByType<Player>();
            if (target == null) {
                foreach (var shot in shots) if (shot != null) Destroy(shot.gameObject);
                volleyActive = false;
                yield break;
            }

            var aimPoint = (Vector2)target.hitbox.bounds.center;
            var starts = new Vector2[VolleySize];
            var directions = new Vector2[VolleySize];
            var groundChecks = new Vector2[VolleySize];
            var lines = new LineRenderer[VolleySize];
            var groundMask = (int)shots[0].mask & ~(int)player;
            var top = camera != null ? camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, 1f)).y : aimPoint.y + 8f;
            activeTelegraph = new GameObject("Sundew telegraphs");
            for (var i = 0; i < VolleySize; i++) {
                var landing = aimPoint + Random.insideUnitCircle * target.hitbox.bounds.size.x;
                var height = Random.Range(0f, 2f);
                var startY = Mathf.Max(top + 1.5f + height, landing.y + 3f + height) + 4.5f;
                starts[i] = new Vector2(landing.x + (startY - landing.y) * Mathf.Tan(Random.Range(-15f, 15f) * Mathf.Deg2Rad), startY);
                directions[i] = (landing - starts[i]).normalized;
                groundChecks[i] = starts[i] + directions[i] * ((starts[i].y - aimPoint.y) / -directions[i].y);
                var hit = Physics2D.Raycast(groundChecks[i], directions[i], Mathf.Infinity, groundMask);
                lines[i] = CreateTelegraph(starts[i] - directions[i] * 50f, hit ? hit.point : groundChecks[i] + directions[i] * 100f);
            }

            for (var i = 0; i < VolleySize; i++)
                if (shots[i] != null) StartCoroutine(CurveAndFall(shots[i], starts[i], directions[i], groundChecks[i], groundMask, lines[i], warningDuration + launchOffsets[i]));
            for (var t = 0f; t < warningDuration; t += Time.deltaTime) {
                var alpha = 0.8f * Mathf.Clamp01(Mathf.Min(t, warningDuration - t) / Mathf.Min(0.2f, warningDuration * 0.5f));
                foreach (var line in lines) if (line != null) line.startColor = line.endColor = new Color(1f, 0.1f, 0.1f, alpha);
                yield return null;
            }
            foreach (var line in lines) if (line != null) line.startColor = line.endColor = Color.clear;
            while (lines[VolleySize - 1] != null) yield return null;
            volleyActive = false;
            Destroy(activeTelegraph);
            activeTelegraph = null;
        }

        private IEnumerator CurveAndFall(SundewProjectile shot, Vector2 end, Vector2 direction, Vector2 groundCheck, int groundMask, LineRenderer line, float duration) {
            duration = Mathf.Max(0.01f, duration);
            var origin = shot.rb.position;
            var control1 = origin + Vector2.up * (projectileSpeed * projectileVelocityMultiplier * duration / 3f);
            var control2 = end - direction * (fallSpeed * projectileVelocityMultiplier * duration / 3f);
            for (var t = 0f; t < duration; t += Time.deltaTime) {
                if (shot == null) { Destroy(line.gameObject); yield break; }
                shot.FollowCurve(origin, control1, control2, end, t / duration, duration);
                yield return null;
            }
            if (shot == null) { Destroy(line.gameObject); yield break; }
            shot.FollowCurve(origin, control1, control2, end, 1f, duration);
            Destroy(line.gameObject);
            shot.BeginFall(direction, fallSpeed * projectileVelocityMultiplier, groundCheck, groundMask);
        }

        private bool AboveScreen(Vector3 position, Camera camera) {
            if (camera != null) return camera.WorldToViewportPoint(position).y > 1.1f;
            return position.y > transform.position.y + 10f;
        }

        private LineRenderer CreateTelegraph(Vector2 start, Vector2 end) {
            if (telegraphMaterial == null) telegraphMaterial = new Material(Shader.Find("Sprites/Default"));
            var line = new GameObject("Sundew path").AddComponent<LineRenderer>();
            line.transform.SetParent(activeTelegraph.transform);
            line.sharedMaterial = telegraphMaterial;
            line.startColor = line.endColor = Color.clear;
            line.startWidth = line.endWidth = 0.06f;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.sortingLayerID = sprite.sortingLayerID;
            line.sortingOrder = sprite.sortingOrder + 20;
            return line;
        }

        private void Attack(Player it, SundewProjectile by) {
            it.Hurt(attack, knockbackForce * new Vector2(Mathf.Sign(by.rb.linearVelocity.x) * Mathf.Cos(knockbackAngle * Mathf.Deg2Rad), Mathf.Sin(knockbackAngle * Mathf.Deg2Rad)), 1);
        }

        protected override void UseAnimation(StateMachine stateMachine) {
            var idleAnim = new IdleAnimation(animator);
            stateMachine.AddAnyTransition(idleAnim, new FuncPredicate(() => behaviour is not Attack attack || !attack.Spinning || attack.Fired));
            stateMachine.AddAnyTransition(new AttackAnimation(animator), new FuncPredicate(() => behaviour is Attack { Spinning: true, Fired: false }));

            stateMachine.SetState(idleAnim);
            animationStateMachine = stateMachine;
        }
    }
}
