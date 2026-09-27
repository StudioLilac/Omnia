using UnityEngine;

namespace Enemies.Spawnball.Behaviour {
    public class Activate : IBehaviour {
        private float t;
        private readonly Spawnball self;

        public Activate(Spawnball self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.spawnDelay;
        }

        public void OnExit() {
        }

        public void OnTick() {
            self.rb.linearVelocity = Vector2.zero;
            if (t == 0) self.OnSpawnEnemy();
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }
    }
}
