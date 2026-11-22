#nullable enable

using StudioLilac.StatusEffects;
using UnityEngine;

namespace StatusEffects {
    public class WeaknessStatusEffect : BaseStatusEffect {
        public WeaknessStatusEffect(StatusEffectConfig config, int stacks) : base(config) {
            StackCount = stacks;
        }

        public override Damage WrapAttack(Damage damage, DamageNext next) {
            damage.Amount *= Mathf.Max(0, 1 - 0.1f * StackCount);
            return next(damage);
        }

        public override int StackCount { get; }
    }
}
