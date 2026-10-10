using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace UI {
    public class Minimap : MonoBehaviour {
        [SerializeField] private RectTransform enemyDotParent;
        [SerializeField] private RectTransform enemyDotPrefab;
        [SerializeField] private float pixelsPerUnit = 2.5f;

        private Transform player;
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

        private void OnLevelLoaded(LevelData _) => player = null;

        private void OnEnemySpawn(Enemy enemy) {
            if (enemy is DestructableWall || dots.ContainsKey(enemy)) return;
            dots[enemy] = Instantiate(enemyDotPrefab, enemyDotParent);
        }

        private void OnEnemyDeath(Enemy enemy) {
            if (dots.Remove(enemy, out var dot)) Destroy(dot.gameObject);
        }

        private void LateUpdate() {
            if (!player) {
                var obj = GameObject.FindGameObjectWithTag("Player");
                if (obj) {
                    player = obj.transform;
                    obj.GetComponentInChildren<Camera>().orthographicSize = enemyDotParent.rect.width / 2f / pixelsPerUnit;
                }
                else return;
            }

            foreach (var (enemy, dot) in dots) {
                if (!enemy) { destroyed.Add(enemy); continue; }
                dot.anchoredPosition = (enemy.transform.position - player.position) * pixelsPerUnit;
                dot.gameObject.SetActive(enemy is not HiddenEnemy h || !h.hidden);
            }

            foreach (var enemy in destroyed) OnEnemyDeath(enemy);
            destroyed.Clear();
        }
    }
}
