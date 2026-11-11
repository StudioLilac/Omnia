using UnityEngine;

namespace Players.Behaviour {
    public class Roll : IBehaviour {
        private readonly Player self;
        private readonly bool isAirRoll;
        private readonly bool isDirectionRight;
        private float t;
        private Vector2 originalColliderSize;
        private Vector2 originalColliderOffset;

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
            originalColliderOffset = self.hitbox.offset;
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
            if (t > 0 && !IsBlockedAtRollHeight()) return;
            if (!HasStandingClearance()) return;

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
            if (it.grounded && !it.canRoll) return null;
            return new Roll(it, it.grounded == false, rollRight);
        }

        private bool IsBlockedAtRollHeight() {
            var bounds = self.hitbox.bounds;
            var direction = isDirectionRight ? 1f : -1f;
            var probeCenter = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.03125f), bounds.center.y);
            var probeSize = new Vector2(0.0625f, bounds.size.y * 0.9f);
            return Physics2D.OverlapBox(probeCenter, probeSize, 0f, self.ground);
        }

        private bool HasStandingClearance() {
            var bounds = self.hitbox.bounds;
            var lower = bounds.max.y + 0.01f;
            var upper = bounds.min.y + originalColliderSize.y * Mathf.Abs(self.transform.lossyScale.y) - 0.01f;
            var probeCenter = new Vector2(bounds.center.x, (lower + upper) / 2f);
            var probeSize = new Vector2(bounds.size.x * 0.95f, upper - lower);
            return !Physics2D.OverlapBox(probeCenter, probeSize, 0f, self.GroundedMask);
        }

        private void ShrinkCollider() {
            self.hitbox.size = new Vector2(self.hitbox.size.x, self.hitbox.size.y / 2);
            self.hitbox.offset = new Vector2(self.hitbox.offset.x, self.hitbox.offset.y - self.hitbox.size.y / 2);
        }

        private void ResetCollider() {
            self.hitbox.size = originalColliderSize;
            self.hitbox.offset = originalColliderOffset;
        }
    }
}
