using UnityEngine;

namespace Enemies.Sundew.Behaviour {
    public class Attack : IBehaviour {
        private const float PostSpinDelay = 0.5f;
        private readonly Sundew self;
        private float t;
        private float fireTime;
        private bool spinning;
        private bool spinComplete;
        private bool fired;

        internal bool Spinning => spinning;
        internal bool Fired => fired;

        public Attack(Sundew self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.windup * self.attackTimingMultiplier;
            fireTime = 0f;
            spinning = false;
            spinComplete = false;
            fired = false;
            self.lag = 0;
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (!fired || self.VolleyActive) return;
            self.UseBehaviour(new Idle(self));
        }

        public void OnUpdate() {
            if (fired) return;
            if (spinning) {
                if (!spinComplete) {
                    var state = self.animator.GetCurrentAnimatorStateInfo(0);
                    if (!state.IsName("SundewAttack") || state.normalizedTime < 1f) return;
                    spinComplete = true;
                    fireTime = Time.time + PostSpinDelay;
                }
                if (Time.time < fireTime) return;
                fired = true;
                self.FireProjectiles();
                return;
            }
            t = Mathf.Max(0, t - Time.deltaTime);
            if (t > 0) return;
            spinning = true;
        }
    }
}
