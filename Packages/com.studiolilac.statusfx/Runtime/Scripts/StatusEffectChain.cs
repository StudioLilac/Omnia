using System;
using System.Collections.Generic;
using System.Linq;

namespace StudioLilac.StatusEffects {
    public static class StatusEffectChain {
        public static void ChainAction<T1>(this IEnumerable<IStatusEffect> effects, Action<T1> root, Func<IStatusEffect, Action<T1, Action<T1>>> selector, T1 value) => effects
            .Reverse()
            .Aggregate(root, (next, effect) => v => selector(effect)(v, next))(value);
    }
}
