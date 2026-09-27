using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enemies.Common;
using UnityEngine;
using Utils;

namespace Enemies.Spawnball.Behaviour {
    public class Move : IBehaviour {
        private List<Vector3> path;
        private Coroutine co;
        private readonly Spawnball self;

        public Move(Spawnball self) {
            this.self = self;
        }

        public void OnEnter() {
            path = CalculatePath();
            co = self.StartCoroutine(DoRecalculatePathToTarget());
        }

        public void OnExit() {
            self.StopCoroutine(co);
        }

        public void OnTick() {
            if (IsNear(self.target.position, self.activationRange)) self.UseBehaviour(new Activate(self));
            else {
                Vector2 next = path.LastOrDefault(it => IsNear(it, self.smoothPath));
                if (next == default) return;
                var direction = next - self.rb.worldCenterOfMass;
                self.rb.linearVelocity = MathUtils.Lerpish(self.rb.linearVelocity, direction.normalized * self.speed, Time.fixedDeltaTime * self.airAcceleration);
            }
        }

        public void OnUpdate() {
        }

        private IEnumerator DoRecalculatePathToTarget() {
            for (; self; path = CalculatePath()) {
                yield return new WaitForSeconds(0.150f);
            }
        }

        private bool IsNear(Vector2 it, float threshold) => Vector2.Distance(self.rb.worldCenterOfMass, it) < threshold;

        private List<Vector3> CalculatePath() => Pathfinder.FindPath(self.rb.worldCenterOfMass, self.target.position);
    }
}
