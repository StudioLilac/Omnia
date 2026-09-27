using UnityEngine;

namespace Players.Fragments {
    public class PerfectAccuracy : Fragment {
        public override void ApplyBuff() {
            base.ApplyBuff();
            PurpleArrow.gainCritOnHit = true;
        }

        public override void RevokeBuff() {
            PurpleArrow.gainCritOnHit = false;
        }
    }
}
