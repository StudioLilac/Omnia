#nullable enable

using System;
using StudioLilac.StatusEffects;
using UnityEngine;

namespace StatusEffects {
    public class WeaknessStatusEffect : BaseStatusEffect {
        private readonly float expiredAt;

        public WeaknessStatusEffect(StatusEffectConfig config, int stacks, float duration) : base(config) {
            StackCount = stacks;
            expiredAt = Time.time + duration;
        }

        public override void OnAttack(Damage damage, Action<Damage> next) {
            damage.Amount *= Mathf.Max(0, 1 - 0.1f * StackCount);
            next(damage);
        }

        public override int StackCount { get; }
        public override bool IsExpired => Time.time > expiredAt;
    }
}
