using System.Collections.Generic;
using System.Linq;
using Players;
using UnityEngine;

namespace Utils {
    public sealed class TimeManager : MonoBehaviour {
        private const float DefaultTimeScale = 1f;

        private sealed class TimeScale {
            public float Scale;
            public float Duration;
            public float Elapsed;
            public AnimationCurve Ease;

            public float CurrentScale => Ease == null
                ? Scale
                : Mathf.Lerp(Scale, DefaultTimeScale, Mathf.Clamp01(Ease.Evaluate(Elapsed / Duration)));
        }

        private readonly List<TimeScale> scales = new();
        private readonly RefBool frozen = new();

        public bool IsFrozen => frozen;

        public void Freeze(object source) {
            if (source == null) return;
            frozen.Set(source);
            Player.ControlsLocked.Set(source);
        }

        public void Unfreeze(object source) {
            if (source == null) return;
            frozen.Unset(source);
            Player.ControlsLocked.Unset(source);
        }

        public void Push(float scale, float duration, AnimationCurve ease) {
            scales.Add(new TimeScale {
                Scale = scale,
                Duration = duration,
                Ease = ease,
            });
        }

        private void Update() {
            if (frozen) {
                Time.timeScale = 0f;
                return;
            }

            scales.ForEach(scale => scale.Elapsed += Time.unscaledDeltaTime);
            scales.RemoveAll(scale => scale.Elapsed >= scale.Duration);

            Time.timeScale = scales.Select(scale => scale.CurrentScale).DefaultIfEmpty(DefaultTimeScale).Min();
        }
    }
}
