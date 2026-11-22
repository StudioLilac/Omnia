#nullable enable

using StudioLilac.StatusEffects;

namespace StatusEffects {
    public class StrengthStatusEffect : BaseStatusEffect {
        public StrengthStatusEffect(StatusEffectConfig config, int stacks) : base(config) {
            StackCount = stacks;
        }

        public override Damage WrapAttack(Damage damage, DamageNext next) {
            damage.Amount *= 1 + 0.1f * StackCount;
            return next(damage);
        }

        public override int StackCount { get; }
    }
}
