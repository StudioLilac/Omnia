using Players;
using UnityEngine;

namespace Enemies.Armadillo.Behaviour {
    public class Roll : IBehaviour {
        private readonly Armadillo self;

        public Roll(Armadillo self) {
            this.self = self;
        }

        public void OnEnter() {
        }

        public void OnExit() {
        }

        public void OnTick() {
            self.rb.linearVelocity = new Vector2(self.facing.x * self.rollSpeed, self.rb.linearVelocity.y);

            if (IsHitTarget(out var target)) {
                self.UseBehaviour(new Recoil(self));
                self.Attack(target);
            } else if (IsHitWall()) {
                self.UseBehaviour(new Recoil(self));
            }
        }

        public void OnUpdate() {
        }

        private bool IsHitTarget(out Player player) {
            var hit = Physics2D.OverlapCircle(self.rb.worldCenterOfMass, self.attackRadius, self.player);
            player = null;
            return hit && hit.TryGetComponent(out player);
        }

        private bool IsHitWall() => self.checks[self.facing.x > 0 ? 0 : 3].IsTouchingLayers(self.ground);
    }
}
