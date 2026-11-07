using UnityEngine;

namespace Players.Behaviour {
    public class Roll : IBehaviour {
        private readonly Player self;
        private readonly bool isAirRoll;
        private readonly bool isDirectionRight;
        private float t;
        private Vector2 originalColliderSize;

        public Roll(Player self, bool isAirRoll, bool isDirectionRight) {
            this.self = self;
            this.isAirRoll = isAirRoll;
            this.isDirectionRight = isDirectionRight;
        }

        public void OnEnter() {
            t = self.rollDuration;
            self.roll = false;
            self.invulnerable = true;
            originalColliderSize = self.hitbox.size;
            ShrinkCollider();
            var verticalVelocity = isAirRoll ? self.airRollBoost : self.rb.linearVelocity.y;
            self.rb.linearVelocity = new Vector2(self.rollSpeed * (isDirectionRight ? 1f : -1f), verticalVelocity);
            self.OnRoll(isAirRoll);
        }

        public void OnExit() {
            self.invulnerable = false;
            ResetCollider();
        }

        public void OnTick() {
        }

        public void OnUpdate() {
            t -= Time.deltaTime;
            // If needed, flip direction if stuck in tunnel and deadend

            // If roll into wall, pass and enter new state
            if (self.checks[isDirectionRight ? 0 : 2].IsTouchingLayers(self.ground)) {
            }
            // else check if roll is active or stuck in small space to continue
            else if (t > 0 || self.checks[3].IsTouchingLayers(self.GroundedMask)) return;

            self.UseBehaviour(Slide.If(self) ?? Fall.If(self) ?? Jump.If(self) ?? Move.If(self) ?? Idle.If(self));
        }

        public static IBehaviour If(Player it) {
            return IfRollingRight(it, it.lastDirectionIsRight);
        }

        public static IBehaviour If(Player it, bool isOnRightWall) {
            return IfRollingRight(it, !isOnRightWall);
        }

        private static IBehaviour IfRollingRight(Player it, bool rollRight) {
            if (!it.roll || it.AirRollsRemaining <= 0) return null;
            return new Roll(it, it.grounded == false, rollRight);
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
