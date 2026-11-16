using System.Linq;
using UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Players.Buff {
    public class GearBuff : Buff {
        [SerializeField] private float angleReduction;

        public override void ApplyBuff() {
            Flamethrower flamethrower = player.weapons.OfType<Flamethrower>().FirstOrDefault();

            if (flamethrower) {
                flamethrower.blastAngle -= angleReduction;
            }
        }

        public override void RevokeBuff() {
            Flamethrower flamethrower = player.weapons.OfType<Flamethrower>().FirstOrDefault();

            if (flamethrower) {
                flamethrower.blastAngle += angleReduction;
            }
        }
    }
}
