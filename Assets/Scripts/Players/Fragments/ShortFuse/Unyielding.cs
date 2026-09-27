using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class Unyielding : Fragment {
        private float ReduceDamage(float incomingDamage) {
            if (player.CurrentHealth > 1) {
                float damageToReduce = Mathf.Max(0f, player.CurrentHealth - 1);
                return Mathf.Min(incomingDamage, damageToReduce);
            } else {
                return incomingDamage;
            }
        }

        public override void ApplyBuff() {
            base.ApplyBuff();
            OnDamageTaken.AddLast(ReduceDamage);
        }

        public override void RevokeBuff() {
            OnDamageTaken.Remove(ReduceDamage);
        }
    }
}
