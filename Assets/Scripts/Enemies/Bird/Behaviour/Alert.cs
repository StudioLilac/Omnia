using UnityEngine;
using Utils;

namespace Enemies.Bird.Behaviour {
    public class Alert : IBehaviour {
        private float t;
        private readonly Bird self;

        public Alert(Bird self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.delay;
            self.rb.linearVelocity = Vector2.zero;
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (t != 0) return;
            self.UseBehaviour(new Attack(self));
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }
    }
}
