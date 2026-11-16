using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class PocketShells : Fragment {
        [SerializeField] private int extraAmmo;
        public override void ApplyBuff() {
            base.ApplyBuff();
            Flamethrower flamethrower = player.weapons.OfType<Flamethrower>().FirstOrDefault();

            if (flamethrower != null) {
                flamethrower.maxAmmoCount += extraAmmo;
            }
        }

        public override void RevokeBuff() {
            Flamethrower flamethrower = player.weapons.OfType<Flamethrower>().FirstOrDefault();

            if (flamethrower != null) {
                flamethrower.maxAmmoCount -= extraAmmo;
            }
        }
    }
}
