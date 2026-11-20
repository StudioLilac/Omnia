using UnityEngine;

namespace Enemies.Sundew.Behaviour {
    public class Attack : IBehaviour {
        private readonly Sundew self;
        private float t;
        private bool fired;

        public Attack(Sundew self) {
            this.self = self;
            t = self.windup;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (!fired || self.VolleyActive) return;
            self.UseBehaviour(new Idle(self));
        }

        public void OnUpdate() {
            if (fired) return;
            t = Mathf.Max(0, t - Time.deltaTime);
            if (t != 0) return;
            fired = true;
            self.FireProjectiles();
        }
    }
}
