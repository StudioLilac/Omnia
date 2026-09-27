using System.Collections.Generic;
using UnityEngine;

namespace Players.Fragments {
    public class QuickThinking : Fragment {
        [SerializeField] private float cdr;
        public override void ApplyBuff() {
            base.ApplyBuff();
            player.skillCooldown -= cdr;
        }

        public override void RevokeBuff() {
            player.skillCooldown += cdr;
        }
    }
}
