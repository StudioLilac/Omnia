using UnityEngine;

namespace Enemies.Armadillo.Behaviour {
    public class Move : IBehaviour {
        private readonly Armadillo self;

        public Move(Armadillo self) {
            this.self = self;
        }

        public void OnEnter() {
            self.FaceAggrodPlayer();
        }

        public void OnExit() {
        }

        public void OnTick() {
            self.rb.linearVelocity = new Vector2(self.facing.x * self.moveSpeed, self.rb.linearVelocity.y);

            if (self.IsTargetDetected()) self.UseBehaviour(new Alert(self));
            else if (self.IsReversing()) self.UseBehaviour(new Idle(self));
        }

        public void OnUpdate() {
        }
    }
}
