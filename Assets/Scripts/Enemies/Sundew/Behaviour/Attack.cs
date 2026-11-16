using UnityEngine;

namespace Enemies.Sundew.Behaviour {
    public class Attack : IBehaviour {
        private const float PostSpinDelay = 0.5f;
        private readonly Sundew self;
        private float t;
        private float fireTime;
        private bool spinComplete;

        internal bool Spinning { get; private set; }
        internal bool Fired { get; private set; }

        public Attack(Sundew self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.windup * self.attackTimingMultiplier;
            self.lag = 0;
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (!Fired || self.VolleyActive) return;
            self.UseBehaviour(new Idle(self));
        }

        public void OnUpdate() {
            if (Fired) return;
            if (Spinning) {
                if (!spinComplete) {
                    var state = self.animator.GetCurrentAnimatorStateInfo(0);
                    if (state.normalizedTime < 1f) return;
                    spinComplete = true;
                    fireTime = Time.time + PostSpinDelay;
                }
                if (Time.time < fireTime) return;
                Fired = true;
                self.FireProjectiles();
                return;
            }
            t -= Time.deltaTime;
            if (t > 0) return;
            Spinning = true;
        }
    }
}
