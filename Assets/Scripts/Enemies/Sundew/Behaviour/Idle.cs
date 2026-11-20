using UnityEngine;

namespace Enemies.Sundew.Behaviour {
    public class Idle : IBehaviour {
        private readonly Sundew self;
        private float t;

        public Idle(Sundew self) {
            this.self = self;
            t = self.reload;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (t != 0 || self.VolleyActive) return;
            self.UseBehaviour(new Attack(self));
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }
    }
}
