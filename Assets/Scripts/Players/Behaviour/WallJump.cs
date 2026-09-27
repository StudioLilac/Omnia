using UnityEngine;

namespace Players.Behaviour {
    public class WallJump : IBehaviour {
        private readonly Player self;
        private float t;

        public WallJump(Player self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.jumpLockoutTime;

            self.jump = false;
            self.UseExternalVelocity(new Vector2(self.slide.x * self.moveSpeed * -1, self.jumpSpeed), self.jumpLockoutTime);
        }

        public void OnExit() {
        }

        public void OnTick() {
            var x = self.HorizontalVelocityOf(self.moving.x * self.moveSpeed, Time.fixedDeltaTime * self.fallAccel);
            self.rb.linearVelocity = new Vector2(x, self.rb.linearVelocity.y);
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
            if (t != 0) return;

            self.UseBehaviour(Slide.If(self) ?? Fall.If(self) ?? Move.If(self) ?? Idle.If(self));
        }

        public static IBehaviour If(Player it) {
            return !it.grounded && it.slide.x != 0 && it.jump ? new WallJump(it) : null;
        }
    }
}
