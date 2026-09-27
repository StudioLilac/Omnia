using UnityEngine;

namespace Players.Fragments {
    public class LightningStrikes : Fragment {
        [SerializeField] private float damageMultiplierBuff;

        private Rigidbody2D rb;

        private float currentBuffValue;

        private void Update() {
            if (rb == null || player == null) return;

            float speed = rb.velocity.magnitude;
            float newBuffValue = Mathf.Floor(speed / 5f) * damageMultiplierBuff;


            player.RemoveAdditiveBuff(currentBuffValue);
            player.AddAdditiveBuff(newBuffValue);
            currentBuffValue = newBuffValue;
        }


        public override void ApplyBuff() {
            base.ApplyBuff();
            rb = player.GetComponent<Rigidbody2D>();
        }

        public override void RevokeBuff() {
            player.RemoveAdditiveBuff(currentBuffValue);
            currentBuffValue = 0f;
        }
    }
}
