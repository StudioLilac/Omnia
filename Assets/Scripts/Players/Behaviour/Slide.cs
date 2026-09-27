using UnityEngine;

namespace Players.Behaviour {
    public class Slide : IBehaviour {
        private readonly Player self;

        public Slide(Player self) {
            this.self = self;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
            var x = self.HorizontalVelocityOf(self.moving.x * self.moveSpeed, Time.fixedDeltaTime * self.moveAccel);
            self.rb.linearVelocity = new Vector2(x, Mathf.Max(self.jumpSpeed / 2 * -1, self.rb.linearVelocity.y));
        }

        public void OnUpdate() {
            self.UseBehaviour(WallJump.If(self) ?? Fall.If(self) ?? Move.If(self) ?? Idle.If(self));
        }

        public static IBehaviour If(Player it) {
            return !it.grounded && it.slide.x != 0 && it.checks[it.slide.x > 0 ? 0 : 2].IsTouchingLayers(it.ground | it.destructable) ? new Slide(it) : null;
        }
    }
}
