using Enemies;
using UnityEngine;

namespace Players.Fragments {
    public class Volley : Fragment {
        [SerializeField] private GameObject purpleArrowPrefab;
        [SerializeField] private int numberOfArrows = 16;
        [SerializeField] private float damage;

        public void SpawnArrows() {
            // Find all enemies in the scene
            Enemy[] enemies = FindObjectsOfType<Enemy>();

            if (enemies.Length == 0) return;

            // Find the closest enemy
            Enemy closestEnemy = null;
            float closestDistance = Mathf.Infinity;

            foreach (Enemy enemy in enemies) {
                float distance = Vector3.Distance(player.Center, enemy.transform.position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }

            // Spawn arrows targeting the closest enemy
            if (closestEnemy != null) {
                for (int i = 0; i < numberOfArrows; i++) {
                    Vector3 spawnPos = player.Center;
                    GameObject arrow = Instantiate(purpleArrowPrefab, spawnPos, Quaternion.identity);
                    PurpleArrow arrowScript = arrow.GetComponent<PurpleArrow>();

                    arrowScript.Initialize(closestEnemy.transform.position, damage);
                }
            }
        }

        public override void ApplyBuff() {
            base.ApplyBuff();
            Player.OnSkill += SpawnArrows;
        }

        public override void RevokeBuff() {
            Player.OnSkill -= SpawnArrows;
        }

        void OnDisable() {
            Player.OnSkill -= SpawnArrows;
        }
    }
}
