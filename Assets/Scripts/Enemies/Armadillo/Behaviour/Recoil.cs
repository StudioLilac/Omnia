using UnityEngine;

namespace Enemies.Armadillo.Behaviour {
    public class Recoil : IBehaviour {
        private readonly Armadillo self;
        private float t;

        public Recoil(Armadillo self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.recoilTime;
            self.rb.linearVelocity = CalculateRecoil(self.recoilAngle * Mathf.Deg2Rad);
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (t != 0) return;
            self.UseBehaviour(new Uncurl(self));
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }

        private Vector2 CalculateRecoil(float rad) => self.recoilSpeed * new Vector2(Mathf.Cos(rad) * -1 * self.facing.x, Mathf.Sin(rad));
    }
}
