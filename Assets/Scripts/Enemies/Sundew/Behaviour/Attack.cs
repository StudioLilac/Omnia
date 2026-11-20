namespace Enemies.Sundew.Behaviour {
    public class Attack : IBehaviour {
        private readonly Sundew self;
        private bool fired;

        public Attack(Sundew self) {
            this.self = self;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (fired && !self.VolleyActive) self.UseBehaviour(new Idle(self));
        }

        public void OnUpdate() {
            if (fired) return;
            var state = self.animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Base Layer.SundewAttack") || state.normalizedTime < 1f) return;
            fired = true;
            self.FireProjectiles();
        }
    }
}
