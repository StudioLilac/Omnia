using System.Collections.Generic;
using UnityEngine;

namespace Players.Fragments {
    public class AbsoluteDefence : Fragment {
        [SerializeField] private int increase;
        [SerializeField] private int radiusDecrease;

		private OrbitObject orbitObject;
        public override void ApplyBuff() {
            base.ApplyBuff();
            orbitObject = player.GetComponentInChildren<OrbitObject>();
            if (orbitObject) {
                orbitObject.orbitalCount += increase;
                orbitObject.orbitRadius -= radiusDecrease;
            }
        }

        public override void RevokeBuff() {
            orbitObject = player.GetComponentInChildren<OrbitObject>();
            if (orbitObject) {
                orbitObject.orbitalCount -= increase;
                orbitObject.orbitRadius += radiusDecrease;
            }
        }
    }
}
