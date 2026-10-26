using UnityEngine;

namespace Enemies.Common.Behaviour {
    public class Stunned : IBehaviour {
        private readonly Enemy self;
        private float t;

        public Stunned(Enemy self, float durationS) {
            this.self = self;
            t = durationS;
        }

        public void Refresh(float durationS) {
            t = Mathf.Max(t, durationS);
        }

        public void OnEnter() {
            self.OnStunned();
            if (self.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
        }

        public void OnExit() { }

        public void OnTick() { 
            //Do nothing
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
            if (t == 0) self.UseBehaviour(self.PostStunBehaviour);
        }
    }
}
