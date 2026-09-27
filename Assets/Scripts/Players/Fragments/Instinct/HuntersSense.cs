using UnityEngine;

namespace Players.Fragments {
    public class HuntersSense : Fragment {
        [SerializeField] private float critDamageBoost;
        public override void ApplyBuff() {
            base.ApplyBuff();
            player.critMultiplier += critDamageBoost;
        }

        public override void RevokeBuff() {
            player.critMultiplier -= critDamageBoost;
        }
    }
}
