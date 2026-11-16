namespace Enemies.Sundew.Behaviour {
    public class Attack : IBehaviour {
        private readonly Sundew self;

        internal bool Spinning { get; private set; }
        internal bool Fired { get; private set; }

        public Attack(Sundew self) {
            this.self = self;
        }

        public void OnEnter() {
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
            var state = self.animator.GetCurrentAnimatorStateInfo(0);
            if (Spinning) {
                if (!state.IsName("Base Layer.SundewAttack") || state.normalizedTime < 1f) return;
                Fired = true;
                self.FireProjectiles();
                return;
            }
            if (!state.IsName("Base Layer.SundewIdle") || state.normalizedTime < 1f) return;
            Spinning = true;
        }
    }
}
