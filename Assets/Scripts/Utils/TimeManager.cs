using System.Collections.Generic;
using System.Text;
using Utils;
using UnityEngine;

public sealed class TimeManager : PersistentSingleton<TimeManager> {
    private sealed class TimeScale {
        public float Scale;
        public float Duration;
        public float Elapsed;
        public AnimationCurve Ease;
    }

    private const float defaultTimeScale = 1f;

    private readonly List<TimeScale> scales = new();
    private readonly HashSet<object> freezeSources = new();

    private bool IsFrozen => freezeSources.Count > 0;

    public void Freeze(object source) {
        if (source == null) return;
        freezeSources.Add(source);
    }

    public void Unfreeze(object source) {
        if (source == null) return;
        freezeSources.Remove(source);
    }

    public void Push(float scale, float duration, AnimationCurve ease) {
        scales.Add(new TimeScale {
            Scale = scale,
            Duration = duration,
            Ease = ease,
        });
    }

    private void Update() {
        if (IsFrozen) {
            Time.timeScale = 0f;
            return;
        }
        scales.ForEach(scale => scale.Elapsed += Time.unscaledDeltaTime);
        int expiredCount = scales.RemoveAll(scale => scale.Elapsed >= scale.Duration);

        float scale = defaultTimeScale;
        foreach (TimeScale timeScale in scales) {
            float easedScale = timeScale.Ease == null ? timeScale.Scale : Mathf.Lerp(timeScale.Scale, defaultTimeScale, Mathf.Clamp01(timeScale.Ease.Evaluate(timeScale.Elapsed / timeScale.Duration)));
            scale = Mathf.Min(scale, easedScale);
        }
        Time.timeScale = scale;
    }
}
