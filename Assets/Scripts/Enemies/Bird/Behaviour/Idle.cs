using UnityEngine;
using Utils;

namespace Enemies.Bird.Behaviour {
    public class Idle : IBehaviour {
        private float t;
        private readonly Bird self;

        public Idle(Bird self) {
            this.self = self;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
            self.rb.linearVelocity = MathUtils.Lerpish(self.rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * self.airAcceleration);

            if (t != 0) return;
            t = 0.1f;
            if (self.IsTargetDetected()) self.UseBehaviour(new Alert(self));
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }
    }
}
