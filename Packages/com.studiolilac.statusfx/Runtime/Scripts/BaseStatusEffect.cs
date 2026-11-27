#nullable enable

using System;
using UnityEngine;

namespace StudioLilac.StatusEffects {
    public class BaseStatusEffect : IStatusEffect {
        public BaseStatusEffect(StatusEffectConfig config) : this(config.Name, config.Icon) {
        }

        public BaseStatusEffect(string name, Sprite icon) {
            Name = name;
            Icon = icon;
        }

        public string Name { get; }
        public Sprite Icon { get; }

        public virtual void OnAttack(Damage damage, Action<Damage> next) {
            next(damage);
        }

        public virtual int StackCount => 0;
        public virtual bool IsExpired => false;
    }
}
