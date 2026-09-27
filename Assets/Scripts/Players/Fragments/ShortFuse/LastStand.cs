using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Players.Fragments {
    public class LastStand : Fragment {
        [SerializeField] public float damageMultiplier;

        private bool buffApplied = false;

        public override void ApplyBuff() {
            base.ApplyBuff();
            Player.OnHealthChanged += HandlePlayerHealthChanged;

            HandlePlayerHealthChanged(player.CurrentHealth);
        }

        public override void RevokeBuff() {
            Player.OnHealthChanged -= HandlePlayerHealthChanged;

            if (buffApplied) {
                player.RemoveMultiplicativeBuff(damageMultiplier);
                buffApplied = false;
            }
        }

        private void HandlePlayerHealthChanged(int hp) {
            if (hp <= 1 && !buffApplied) {
                player.AddMultiplicativeBuff(damageMultiplier);
                buffApplied = true;
            } else if (hp > 1 && buffApplied) {
                player.RemoveMultiplicativeBuff(damageMultiplier);
                buffApplied = false;
            }
        }
    }
}
