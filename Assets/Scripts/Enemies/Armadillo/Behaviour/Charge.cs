using UnityEngine;

namespace Enemies.Armadillo.Behaviour {
    public class Charge : IBehaviour {
        private readonly Armadillo self;
        private float t;

        public Charge(Armadillo self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.chargeTime;
            var vel = self.dustParticles.velocityOverLifetime;
            vel.x = -self.facing.x * Mathf.Abs(vel.x.constant);

            Vector3 dustPos = self.dustParticles.transform.localPosition;
            dustPos.x = -self.facing.x * Mathf.Abs(dustPos.x);
            self.dustParticles.transform.localPosition = dustPos;

            self.dustParticles.Play();
        }

        public void OnExit() {
        }

        public void OnTick() {
            self.rb.linearVelocity = new Vector2(0, self.rb.linearVelocity.y);
            if (t != 0) return;
            self.UseBehaviour(new Roll(self));
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }
    }
}
