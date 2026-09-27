using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enemies.Common;
using UnityEngine;
using Utils;

namespace Enemies.Bird.Behaviour {
    public class Attack : IBehaviour {
        private readonly Bird self;
        private List<Vector3> path;
        private float t;
        private Coroutine co;

        public Attack(Bird self) {
            this.self = self;
        }

        public void OnEnter() {
            path = CalculatePath();
            t = self.fuse;
            co = self.StartCoroutine(DoRecalculatePathToTarget());
        }

        public void OnExit() {
            self.StopCoroutine(co);
        }

        public void OnTick() {
            if (t == 0 || IsNear(self.targetInstance.rb.worldCenterOfMass)) self.OnExplode();
            else {
                Vector2 next = path.LastOrDefault(it => IsNear(it));
                if (next == default) return;
                var direction = next - self.rb.worldCenterOfMass;
                self.rb.linearVelocity = MathUtils.Lerpish(self.rb.linearVelocity, direction.normalized * self.speed, Time.fixedDeltaTime * self.airAcceleration);
            }
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
        }

        private IEnumerator DoRecalculatePathToTarget() {
            for (; self; path = CalculatePath()) {
                yield return new WaitForSeconds(0.150f);
            }
        }

        private bool IsNear(Vector2 it) => Vector2.Distance(self.rb.worldCenterOfMass, it) < self.triggerDistance;

        private List<Vector3> CalculatePath() => Pathfinder.FindPath(self.rb.worldCenterOfMass, self.targetInstance.rb.worldCenterOfMass);
    }
}
