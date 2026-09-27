using UnityEngine;

namespace Players.Behaviour {
    public class Fall : IBehaviour {
        private readonly Player self;

        public Fall(Player self) {
            this.self = self;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (self.IsPhoon()) return;

            var x = self.HorizontalVelocityOf(self.moving.x * self.moveSpeed, Time.fixedDeltaTime * self.fallAccel);
            self.rb.linearVelocity = new Vector2(x, Mathf.Max(self.jumpSpeed * 2 * -1, self.rb.linearVelocity.y));
        }

        public void OnUpdate() {
            self.UseBehaviour(Slide.If(self) ?? Move.If(self) ?? Idle.If(self));
        }

        public static IBehaviour If(Player it) {
            return !it.grounded && it.slide.x == 0 && it.rb.linearVelocity.y <= 0 ? new Fall(it) : null;
        }
    }
}
