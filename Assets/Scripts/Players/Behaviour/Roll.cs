using UnityEngine;

namespace Players.Behaviour {
    public class Roll : IBehaviour {
        private readonly Player self;
        private float t;
        private float direction;
        private Vector2 originalColliderSize;

        public Roll(Player self) {
            this.self = self;
        }

        public void OnEnter() {
            t = self.rollDuration;
            self.roll = false;
            self.invulnerable = true;

            originalColliderSize = self.hitbox.size;
            ShrinkCollider();

            // If player is moving above threshold, roll in that direction, else roll in facing direction
            direction = Mathf.Sign(Mathf.Abs(self.rb.linearVelocity.x) > self.rollThreshold ? self.rb.linearVelocity.x : self.moving.x != 0 ? self.moving.x : self.facing.x);

            self.OnRoll();
        }

        public void OnExit() {
            self.invulnerable = false;
            ResetCollider();
        }

        public void OnTick() {
            self.rb.linearVelocity = new Vector2(self.rollSpeed * direction, self.rb.linearVelocity.y);
        }

        public void OnUpdate() {
            t -= Time.deltaTime;
            // If needed, flip direction if stuck in tunnel and deadend

            // If roll into wall, pass and enter new state
            if (self.checks[0].IsTouchingLayers(self.ground) || self.checks[2].IsTouchingLayers(self.ground)) {
            }
            // else check if roll is active or stuck in small space to continue
            else if (t > 0 || self.checks[3].IsTouchingLayers(self.GroundedMask)) return;

            self.UseBehaviour(Slide.If(self) ?? Fall.If(self) ?? Jump.If(self) ?? Move.If(self) ?? Idle.If(self));
        }

        public static IBehaviour If(Player it) {
            return it.roll && it.canRoll && it.grounded && it.checks[1].IsTouchingLayers(it.GroundedMask) ? new Roll(it) : null;
        }

        // Probably better to have a separate dedicated collider for rolling state
        private void ShrinkCollider() {
            self.hitbox.size = new Vector2(self.hitbox.size.x, self.hitbox.size.y / 2);
            self.hitbox.offset = new Vector2(self.hitbox.offset.x, self.hitbox.offset.y - self.hitbox.size.y / 2);
        }

        private void ResetCollider() {
            self.hitbox.size = originalColliderSize;
            self.hitbox.offset = new Vector2(self.hitbox.offset.x, self.hitbox.size.y / 2);
        }
    }
}
