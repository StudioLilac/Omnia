#nullable enable

using UnityEngine;

namespace StudioLilac.StatusEffects {
    public delegate void DamageNext(Damage damage);

    public class Damage {
        public float Amount;
        public bool IsCrit;
        public float Lockout;
        public bool IsStagger = true;
        public Vector2 Velocity;
    }
}
