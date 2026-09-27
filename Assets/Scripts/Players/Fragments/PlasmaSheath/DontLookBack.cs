using System.Collections.Generic;
using UnityEngine;

namespace Players.Fragments {
    public class DontLookBack : Fragment {
        public override void ApplyBuff() {
            base.ApplyBuff();
            Player.knockbackImmune = true;
        }

        public override void RevokeBuff() {
            Player.knockbackImmune = false;
        }
    }
}
