using UnityEngine;

namespace Enemies.Armadillo.Behaviour {
    public class Uncurl : IBehaviour {
        private readonly Armadillo self;
        private float t;

        public Uncurl(Armadillo self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.uncurlTime;
        }

        public void OnExit() {
        }

        public void OnTick() {
            self.rb.linearVelocity = new Vector2(0, self.rb.linearVelocity.y);

            if (t != 0) return;
            self.UseBehaviour(new Move(self));
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }
    }
}
