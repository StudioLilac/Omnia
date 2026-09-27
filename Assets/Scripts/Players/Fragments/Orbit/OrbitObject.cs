using Enemies;
using UnityEngine;

namespace Players.Fragments {
    public class OrbitObject : MonoBehaviour {
        [SerializeField] private GameObject orbitalPrefab;
        [SerializeField] public int orbitalCount;
        [SerializeField] public float orbitRadius;
        [SerializeField] public float orbitSpeed;
        [SerializeField] private float damage;

        private GameObject[] orbitals;

        private void Start() {
            GenerateOrbitals();
        }

        private void Update() {
            transform.Rotate(Vector3.forward, orbitSpeed * Time.deltaTime);
        }

        private void GenerateOrbitals() {
            orbitals = new GameObject[orbitalCount];

            for (int i = 0; i < orbitalCount; i++) {
                float angle = (360f / orbitalCount) * i;
                Vector3 positionOffset = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius,
                    0f
                );

                GameObject orbital = Instantiate(orbitalPrefab, transform.position + positionOffset, Quaternion.identity, transform);
                orbitals[i] = orbital;
                Orbital o = orbital.GetComponent<Orbital>();
                if (o != null) {
                    o.damage = damage;
                }
            }
        }
    }
}
