using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class ScopedIn : Fragment {
        [SerializeField] private float angleReduction;
        public override void ApplyBuff() {
            base.ApplyBuff();
            Flamethrower flamethrower = player.weapons.OfType<Flamethrower>().FirstOrDefault();

            if (flamethrower != null) {
                flamethrower.blastAngle -= angleReduction;
            }
        }

        public override void RevokeBuff() {
            Flamethrower flamethrower = player.weapons.OfType<Flamethrower>().FirstOrDefault();

            if (flamethrower != null) {
                flamethrower.blastAngle += angleReduction;
            }
        }
    }
}
