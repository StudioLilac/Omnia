#nullable enable

using System;
using UnityEngine;

namespace StudioLilac.StatusEffects {
    public interface IStackable {
        int StackCount { get; }
    }

    public interface IExpirable {
        bool IsExpired { get; }
    }

    public interface IStatusEffect : IStackable, IExpirable {
        string Name { get; }
        Sprite Icon { get; }

        void OnAttack(Damage damage, Action<Damage> next);
    }
}
