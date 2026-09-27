using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class VitalOvergrowth : Fragment {
        [SerializeField] private int healthBoost;

        public override void ApplyBuff() {
            base.ApplyBuff();
            player.maximumHealth += healthBoost;
            player.CurrentHealth += healthBoost;
            player.HealthBoosted = true;
        }

        public override void RevokeBuff() {
            player.maximumHealth -= healthBoost;
            player.CurrentHealth = Mathf.Max(1, player.CurrentHealth - healthBoost);
            player.HealthBoosted = false;
        }
    }
}
