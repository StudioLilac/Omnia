using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class Anchor : Fragment {
        public override void ApplyBuff() {
            base.ApplyBuff();
            HarpoonSpear.CanPullToGround = true;
        }

        public override void RevokeBuff() {
            HarpoonSpear.CanPullToGround = false;
        }
    }
}
