using UnityEngine;

namespace Players.Fragments {
    public class Instinct : Fragment {
        [SerializeField] private float critChanceBoost;
        public override void ApplyBuff() {
            base.ApplyBuff();
            player.critChance += critChanceBoost;
        }

        public override void RevokeBuff() {
            player.critChance -= critChanceBoost;
        }
    }
}
