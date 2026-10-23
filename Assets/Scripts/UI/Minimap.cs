 using System.Collections.Generic;
using Players;
using Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI {
    public class Minimap : MonoBehaviour {
        [SerializeField] private RectTransform enemyDotParent;
        [SerializeField] private RectTransform enemyDotPrefab;
        // UI pixels per world unit. Also sets the minimap camera's size so the terrain and dots share a scale
        [SerializeField] private float pixelsPerUnit = 2.5f;
        private float minDotSize = 5f;
        private float maxDotSize = 20f;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private InputActionReference toggleAction;
        [SerializeField] private float obscuredAlpha = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.15f;
        [SerializeField] private float fadeInDuration = 0.4f;

        private SpriteRenderer playerSprite;
        private Camera minimapCamera;
        private bool shown = false;
        private readonly Dictionary<Enemy, (RectTransform dot, Collider2D body)> dots = new();
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

        private void OnLevelLoaded(LevelData _) => minimapCamera = null;

        private void Update() {
            if (toggleAction != null && toggleAction.action.WasPressedThisFrame()) SetShown(!shown);
        }

        private void OnEnemySpawn(Enemy enemy) {
            // Destructable walls inherit from Enemy but are terrain, so they don't get a dot
            if (enemy is DestructableWall || dots.ContainsKey(enemy)) return;
            RectTransform dot = Instantiate(enemyDotPrefab, enemyDotParent);
            Collider2D body = enemy.GetComponent<Collider2D>();
            if (body) {
                Vector3 size = body.bounds.size;
                dot.sizeDelta = Vector2.one * Mathf.Clamp(Mathf.Max(size.x, size.y) * pixelsPerUnit, minDotSize, maxDotSize);
            }
            dots[enemy] = (dot, body);
        }

        private void OnEnemyDeath(Enemy enemy) {
            if (dots.Remove(enemy, out (RectTransform dot, Collider2D body) entry)) Destroy(entry.dot.gameObject);
        }

        private void LateUpdate() {
            if (!minimapCamera) {
                GameObject obj = GameObject.FindGameObjectWithTag("Player");
                if (obj) {
                    playerSprite = obj.GetComponent<Player>().sprite;
                    // The minimap camera is a child of the player prefab. Its view radius must match the mask radius
                    minimapCamera = obj.GetComponentInChildren<Camera>();
                    minimapCamera.orthographicSize = enemyDotParent.rect.width / 2f / pixelsPerUnit;
                    minimapCamera.enabled = shown;
                }
                else return;
            }

            if (!shown) return;

            foreach (var (enemy, (dot, body)) in dots) {
                if (!enemy) { destroyed.Add(enemy); continue; }
                // Not every enemy has a collider
                Vector3 position = body ? body.bounds.center : enemy.transform.position;
                dot.anchoredPosition = (position - minimapCamera.transform.position) * pixelsPerUnit;
                dot.gameObject.SetActive(enemy is not HiddenEnemy h || !h.hidden);
            }

            // Enemies unloaded with a scene never invoke Death, so they are removed here instead.
            // This can't happen inside the loop above, since the dictionary can't change while iterating
            foreach (Enemy enemy in destroyed) OnEnemyDeath(enemy);
            destroyed.Clear();

            UpdateFade();
        }

        private void SetShown(bool value) {
            shown = value;
            canvasGroup.alpha = value ? 1f : 0f;
            if (minimapCamera) minimapCamera.enabled = value;
        }

        private void UpdateFade() {
            float target = IsPlayerBehindMinimap() ? obscuredAlpha : 1f;
            float duration = target < canvasGroup.alpha ? fadeOutDuration : fadeInDuration;
            // Speed is scaled so a full fade between 1 and obscuredAlpha takes exactly duration seconds
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, (1f - obscuredAlpha) / duration * Time.deltaTime);
        }

        /**
         * Checks the player's sprite bounds against the minimap circle, not its square rect.
         * The HUD is a Screen Space Overlay canvas, so UI world positions are already in screen pixels
         */
        private bool IsPlayerBehindMinimap() {
            RectTransform rt = (RectTransform)transform;
            Vector2 center = rt.TransformPoint(rt.rect.center);
            float radius = rt.rect.width / 2f * rt.lossyScale.x;

            Camera cam = Camera.main;
            Bounds bounds = playerSprite.bounds;
            Vector2 min = cam.WorldToScreenPoint(bounds.min);
            Vector2 max = cam.WorldToScreenPoint(bounds.max);

            Vector2 closest = new Vector2(Mathf.Clamp(center.x, min.x, max.x), Mathf.Clamp(center.y, min.y, max.y));
            return (closest - center).sqrMagnitude < radius * radius;
        }
    }
}
