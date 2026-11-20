namespace Enemies.Sundew.Behaviour {
    public class Idle : IBehaviour {
        private readonly Sundew self;

        public Idle(Sundew self) {
            this.self = self;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
        }

        public void OnUpdate() {
            if (self.VolleyActive) return;
            var state = self.animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Base Layer.SundewIdle") || state.normalizedTime < 1f) return;
            self.UseBehaviour(new Attack(self));
        }
    }
}
