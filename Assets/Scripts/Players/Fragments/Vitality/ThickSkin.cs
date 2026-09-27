using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class ThickSkin : Fragment {
        [SerializeField] private int damageReduction;

        private float ReduceDamage(float incomingDamage) {
            float outDamage = incomingDamage * damageReduction;
            return outDamage;
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
