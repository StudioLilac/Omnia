using UnityEngine;

namespace Players.Behaviour {
    public class Jump : IBehaviour {
        private readonly Player self;
        private float t;

        public Jump(Player self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.jumpLockoutTime;

            self.jump = false;
            self.UseExternalVelocity(new Vector2(self.rb.linearVelocity.x, self.jumpSpeed), 0);
        }

        public void OnExit() {
        }

        public void OnTick() {
            if (self.IsPhoon()) return;

            var x = self.HorizontalVelocityOf(self.moving.x * self.moveSpeed, Time.fixedDeltaTime * self.fallAccel);
            self.rb.linearVelocity = new Vector2(x, self.rb.linearVelocity.y);
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
            if (t != 0) return;

            self.UseBehaviour(Slide.If(self) ?? Fall.If(self) ?? Move.If(self) ?? Idle.If(self));
        }

        public static IBehaviour If(Player it) {
            //Bugfix: When player stands still and uses shotgun skill, they should enter this state via the lockGravity flag instead of being stuck in Idle and not allowed to move.
            return it.lockGravity || it.grounded && it.jump ? new Jump(it) : null;
        }
    }
}
