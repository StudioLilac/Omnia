using System.Collections.Generic;
using UnityEngine;

namespace Players.Fragments {
    public class Springloaded : Fragment {
        [SerializeField] private float jumpBoost;
        public override void ApplyBuff() {
            base.ApplyBuff();
            player.jumpSpeed += jumpBoost;
        }

        public override void RevokeBuff() {
            player.jumpSpeed -= jumpBoost;
        }
    }
}
