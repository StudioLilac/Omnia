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
        [SerializeField] internal float attackTimingMultiplier;
        [SerializeField] internal float lag;
        [SerializeField] internal float projectileSpeed;
        [SerializeField] private float projectileVelocityMultiplier;
        [SerializeField] internal float randomAttackDelayOffset;
        [SerializeField] private float warningDuration;
        [SerializeField] private float fallSpeed;

        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal LayerMask ground;
        [SerializeField] internal GameObject projectile;

        [SerializeField] internal GameObject deathExplosion;

        private const int VolleySize = 5;
        private const float LaunchSpacing = 0.25f;
        private readonly List<SundewProjectile> activeProjectiles = new();
        private GameObject activeTelegraph;
        private bool volleyActive;
        private static Material telegraphMaterial;

        private struct ShotPath {
            public Vector2 FallStart;
            public Vector2 Direction;
            public Vector2 GroundCheckStart;
        }

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
            StartCoroutine(FireVolley());
        }

        private IEnumerator FireVolley() {
            volleyActive = true;
            activeProjectiles.RemoveAll(shot => shot == null);
            var shots = new (SundewProjectile projectile, float launchOffset)[VolleySize];
            var camera = Camera.main;
            var firstLaunchTime = Time.time;
            AudioManager.Instance.PlaySFX(AudioTracks.PlantShoot);

            for (var i = 0; i < VolleySize; i++) {
                if (i > 0) yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
                var launchOffset = Time.time - firstLaunchTime;
                var launchPosition = rb.worldCenterOfMass + new Vector2((i - 2) * LaunchSpacing, 0f);
                var shot = Instantiate(projectile, launchPosition, Quaternion.identity).GetComponent<SundewProjectile>();
                shot.NotifyOnHit = Attack;
                shot.rb.linearVelocity = Vector2.up * projectileSpeed * projectileVelocityMultiplier;
                shots[i] = (shot, launchOffset);
                activeProjectiles.Add(shot);
            }
            while (camera.WorldToViewportPoint(shots[VolleySize - 1].projectile.transform.position).y <= 1.1f) yield return null;
            foreach (var (shot, _) in shots) shot.BeginCurve();

            // Aim once all five shots have left the view.
            var target = FindAnyObjectByType<Player>();
            var aimPoint = (Vector2)target.hitbox.bounds.center;
            var aimRadius = target.hitbox.bounds.size.x;
            var paths = new ShotPath[VolleySize];
            var lines = new LineRenderer[VolleySize];
            var cameraTop = camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, 1f)).y;
            activeTelegraph = new GameObject("Sundew telegraphs");
            for (var i = 0; i < VolleySize; i++) {
                paths[i] = AimShot(aimPoint, aimRadius, cameraTop);
                lines[i] = CreateTelegraph(paths[i]);
            }

            Coroutine lastCurve = null;
            for (var i = 0; i < VolleySize; i++)
                lastCurve = StartCoroutine(CurveAndFall(shots[i].projectile, paths[i], warningDuration + shots[i].launchOffset));
            for (var t = 0f; t < warningDuration; t += Time.deltaTime) {
                var alpha = 0.8f * Mathf.Clamp01(Mathf.Min(t, warningDuration - t) / Mathf.Min(0.2f, warningDuration * 0.5f));
                foreach (var line in lines) line.startColor = line.endColor = new Color(1f, 0.1f, 0.1f, alpha);
                yield return null;
            }
            foreach (var line in lines) line.startColor = line.endColor = Color.clear;
            yield return lastCurve;
            volleyActive = false;
            Destroy(activeTelegraph);
        }

        private static ShotPath AimShot(Vector2 aimPoint, float radius, float cameraTop) {
            var targetPoint = aimPoint + Random.insideUnitCircle * radius;
            // Begin the fall above both the screen and the target.
            var startY = Mathf.Max(cameraTop + 6f, targetPoint.y + 7.5f) + Random.Range(0f, 2f);
            var fallStart = new Vector2(targetPoint.x + (startY - targetPoint.y) * Mathf.Tan(Random.Range(-15f, 15f) * Mathf.Deg2Rad), startY);
            var direction = (targetPoint - fallStart).normalized;
            // Check for ground only after the shot reaches the player's height.
            var groundCheckStart = fallStart + direction * ((aimPoint.y - fallStart.y) / direction.y);
            return new ShotPath { FallStart = fallStart, Direction = direction, GroundCheckStart = groundCheckStart };
        }

        private IEnumerator CurveAndFall(SundewProjectile shot, ShotPath path, float duration) {
            var origin = shot.rb.position;
            var control1 = origin + Vector2.up * (projectileSpeed * projectileVelocityMultiplier * duration / 3f);
            var control2 = path.FallStart - path.Direction * (fallSpeed * projectileVelocityMultiplier * duration / 3f);
            for (var t = 0f; t < duration; t += Time.deltaTime) {
                shot.FollowCurve(origin, control1, control2, path.FallStart, t / duration, duration);
                yield return null;
            }
            shot.FollowCurve(origin, control1, control2, path.FallStart, 1f, duration);
            shot.BeginFall(path.Direction, fallSpeed * projectileVelocityMultiplier, path.GroundCheckStart, ground);
        }

        private LineRenderer CreateTelegraph(ShotPath path) {
            var hit = Physics2D.Raycast(path.GroundCheckStart, path.Direction, Mathf.Infinity, ground);
            var start = path.FallStart - path.Direction * 50f;
            var end = hit ? hit.point : path.GroundCheckStart + path.Direction * 100f;
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
            var noGlueAnim = new NoGlueAnimation(animator);
            var idleAnim = new IdleAnimation(animator);
            stateMachine.AddAnyTransition(noGlueAnim, new FuncPredicate(() => behaviour is not Attack { Fired: false }));
            stateMachine.AddAnyTransition(idleAnim, new FuncPredicate(() => behaviour is Attack { Spinning: false, Fired: false }));
            stateMachine.AddAnyTransition(new AttackAnimation(animator), new FuncPredicate(() => behaviour is Attack { Spinning: true, Fired: false }));

            stateMachine.SetState(noGlueAnim);
            animationStateMachine = stateMachine;
        }
    }
}
