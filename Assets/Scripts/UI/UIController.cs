using System.Collections.Generic;
using Cinemachine;
using Enemies;
using UnityEngine;

namespace UI {
    public class UIController : MonoBehaviour {
        public static UIController Instance { get; set; }

        /**
            0: EnemyUI,
            1: DamageMarker,
         */
        private enum UiPrefabs {
            EnemyUI = 0,
            DamageMarker = 1
        }
        [SerializeField] internal GameObject[] prefabs;
        [SerializeField] internal Canvas canvas;
        private Dictionary<Enemy, EnemyUI> enemies;

        public void Awake() {
            Instance = Instance != null ? Instance : this;
            if (Instance != this) Destroy(gameObject);

            enemies = new Dictionary<Enemy, EnemyUI>();
            canvas = GetComponentInChildren<Canvas>();
            canvas.worldCamera = Camera.main;
        }

        public void OnEnable() {
            Enemy.Spawn += OnEnemySpawn;
            Enemy.Damage += OnEnemyDamage;
            Enemy.Death += OnEnemyDeath;
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdate);
        }

        public void OnDisable() {
            Enemy.Spawn -= OnEnemySpawn;
            Enemy.Damage -= OnEnemyDamage;
            Enemy.Death -= OnEnemyDeath;
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdate);
        }

        private void OnCameraUpdate(CinemachineBrain it) {
            foreach (var ui in enemies.Values) ui.OnCameraUpdate();
        }

        private void OnEnemyDeath(Enemy enemy) {
            if (enemies.Remove(enemy, out var ui)) Destroy(ui.gameObject);
        }

        private void OnEnemyDamage(Enemy enemy, float damage, bool crit) {
            var marker = Instantiate(prefabs[(int) UiPrefabs.DamageMarker]).GetComponent<DamageMarker>();
            marker.Initialize(enemy.transform.position, damage, crit);

            if (enemies.TryGetValue(enemy, out var ui)) {
                ui.ShowParent(true);
            }
        }


        private void OnEnemySpawn(Enemy enemy) {
            var ui = prefabs[(int) UiPrefabs.EnemyUI];
            var prefab = Instantiate(ui, canvas.transform).GetComponent<EnemyUI>()!.Of(enemy);
            enemies[enemy] = prefab;
        }
    }
}
