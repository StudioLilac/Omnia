using System.Collections.Generic;
using Players;
using Enemies;
using UnityEngine;

namespace UI {
    public class Minimap : MonoBehaviour {
        [SerializeField] private RectTransform enemyDotParent;
        [SerializeField] private RectTransform enemyDotPrefab;
        // UI pixels per world unit. Also sets the minimap camera's size so the terrain and dots share a scale
        [SerializeField] private float pixelsPerUnit = 2.5f;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float obscuredAlpha = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.15f;
        [SerializeField] private float fadeInDuration = 0.4f;

        private SpriteRenderer playerSprite;
        private Transform mapCenter;
        private readonly Dictionary<Enemy, RectTransform> dots = new();
        private readonly List<Enemy> destroyed = new();

        private void OnEnable() {
            Enemy.Spawn += OnEnemySpawn;
            Enemy.Death += OnEnemyDeath;
            LevelManager.OnLevelLoaded += OnLevelLoaded;
        }

        private void OnDisable() {
            Enemy.Spawn -= OnEnemySpawn;
            Enemy.Death -= OnEnemyDeath;
            LevelManager.OnLevelLoaded -= OnLevelLoaded;
        }

        private void OnLevelLoaded(LevelData _) => mapCenter = null;

        private void OnEnemySpawn(Enemy enemy) {
            // Destructable walls inherit from Enemy but are terrain, so they don't get a dot
            if (enemy is DestructableWall || dots.ContainsKey(enemy)) return;
            dots[enemy] = Instantiate(enemyDotPrefab, enemyDotParent);
        }

        private void OnEnemyDeath(Enemy enemy) {
            if (dots.Remove(enemy, out var dot)) Destroy(dot.gameObject);
        }

        private void LateUpdate() {
            if (!mapCenter) {
                var obj = GameObject.FindGameObjectWithTag("Player");
                if (obj) {
                    playerSprite = obj.GetComponent<Player>().sprite;
                    // The minimap camera is a child of the player prefab. Its view radius must match the mask radius
                    var minimapCamera = obj.GetComponentInChildren<Camera>();
                    minimapCamera.orthographicSize = enemyDotParent.rect.width / 2f / pixelsPerUnit;
                    // camera's transform is at the center of the player while obj.transform is at the feet
                    mapCenter = minimapCamera.transform;
                }
                else return;
            }

            foreach (var (enemy, dot) in dots) {
                if (!enemy) { destroyed.Add(enemy); continue; }
                dot.anchoredPosition = (enemy.transform.position - mapCenter.position) * pixelsPerUnit;
                dot.gameObject.SetActive(enemy is not HiddenEnemy h || !h.hidden);
            }

            // Enemies unloaded with a scene never invoke Death, so they are removed here instead.
            // This can't happen inside the loop above, since the dictionary can't change while iterating
            foreach (var enemy in destroyed) OnEnemyDeath(enemy);
            destroyed.Clear();

            UpdateFade();
        }

        private void UpdateFade() {
            var target = IsPlayerBehindMinimap() ? obscuredAlpha : 1f;
            var duration = target < canvasGroup.alpha ? fadeOutDuration : fadeInDuration;
            // Speed is scaled so a full fade between 1 and obscuredAlpha takes exactly duration seconds
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, (1f - obscuredAlpha) / duration * Time.deltaTime);
        }

        /**
         * Checks the player's sprite bounds against the minimap circle, not its square rect.
         * The HUD is a Screen Space Overlay canvas, so UI world positions are already in screen pixels
         */
        private bool IsPlayerBehindMinimap() {
            var rt = (RectTransform)transform;
            Vector2 center = rt.TransformPoint(rt.rect.center);
            var radius = rt.rect.width / 2f * rt.lossyScale.x;

            var cam = Camera.main;
            var bounds = playerSprite.bounds;
            Vector2 min = cam.WorldToScreenPoint(bounds.min);
            Vector2 max = cam.WorldToScreenPoint(bounds.max);

            var closest = new Vector2(Mathf.Clamp(center.x, min.x, max.x), Mathf.Clamp(center.y, min.y, max.y));
            return (closest - center).sqrMagnitude < radius * radius;
        }
    }
}
