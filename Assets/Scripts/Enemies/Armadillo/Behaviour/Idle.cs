using UnityEngine;

namespace Enemies.Armadillo.Behaviour {
    public class Idle : IBehaviour {
        private readonly Armadillo self;
        private float t;

        public Idle(Armadillo self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.idleDuration;
        }

        public void OnExit() {
            self.facing = new Vector2(-1 * self.facing.x, self.facing.y);
        }

        public void OnTick() {
            self.rb.linearVelocity = new Vector2(0, self.rb.linearVelocity.y);

            if (self.IsTargetDetected()) self.UseBehaviour(new Alert(self));
            else if (t != 0) return;
            self.UseBehaviour(new Move(self));
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }
    }
}
