using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class LaserFocus : Fragment {
        [SerializeField] private float angleReduction;
        public override void ApplyBuff() {
            base.ApplyBuff();
            Shotgun shotgun = player.weapons.OfType<Shotgun>().FirstOrDefault();

            if (shotgun != null) {
                shotgun.blastAngle -= angleReduction;
            }
        }

        public override void RevokeBuff() {
            Shotgun shotgun = player.weapons.OfType<Shotgun>().FirstOrDefault();

            if (shotgun != null) {
                shotgun.blastAngle += angleReduction;
            }
        }
    }
}
