using UnityEngine;

namespace Players.Behaviour {
    public class Pull : IBehaviour {
        private readonly Player self;
        private float t;
        private Vector2 direction;

        private readonly Transform target;

        public Pull(Player self, Transform target) {
            this.self = self;
            this.target = target;
        }

        /* TODO: This will need another pass once player input is figured out! */
        public void OnEnter() {
            t = Vector2.Distance(self.transform.position, target.position) / self.pullSpeed;
            direction = target.position - self.transform.position;
        }

        public void OnExit() {
            self.rb.linearVelocity = new Vector2(self.rb.linearVelocity.x, Mathf.Min(self.jumpSpeed, self.rb.linearVelocity.y));
        }

        public void OnTick() {
            self.rb.linearVelocity = direction.normalized * self.pullSpeed;
        }

        public void OnUpdate() {
            t = Mathf.Max(0, t - Time.deltaTime);
            if (t != 0) return;

            self.UseBehaviour(new Fall(self));
        }
    }
}
