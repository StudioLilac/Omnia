using System.Collections.Generic;
using Utils;
using UnityEngine;

public sealed class TimeManager : PersistentSingleton<TimeManager> {
    private sealed class TimedScale {
        public float Scale;
        public float Duration;
        public float Elapsed;
        public AnimationCurve Ease;
    }

    private static TimeManager instance;

    private float defaultTimeScale = 1f;

    private readonly List<TimedScale> scales = new();
    private readonly HashSet<object> freezeSources = new();

    private bool IsFrozen => freezeSources.Count > 0;

    private float CurrentScale {
        get {
            if (scales.Count == 0) return defaultTimeScale;

            float scale = float.PositiveInfinity;
            foreach (TimedScale timedScale in scales) {
                float progress = timedScale.Elapsed / timedScale.Duration;
                float blend = timedScale.Ease == null ? 0f : Mathf.Clamp01(timedScale.Ease.Evaluate(progress));
                scale = Mathf.Min(scale, Mathf.Lerp(timedScale.Scale, defaultTimeScale, blend));
            }

            return scale;
        }
    }

    private static TimeManager Manager {
        get {
            if (instance != null) return instance;

            instance = FindAnyObjectByType<TimeManager>();
            if (instance == null) {
                instance = new GameObject(nameof(TimeManager)).AddComponent<TimeManager>();
            }

            return instance;
        }
    }

    protected override void OnAwake() {
        instance = this;
        Apply();
    }

    private void OnDestroy() {
        if (instance != this) return;

        Time.timeScale = defaultTimeScale;
        instance = null;
    }

    public static void Freeze(object source) {
        if (source == null) return;

        TimeManager manager = Manager;
        if (!manager.freezeSources.Add(source)) return;

        manager.Apply();
    }

    public static void Unfreeze(object source) {
        if (instance == null || source == null || !instance.freezeSources.Remove(source)) return;

        instance.Apply();
    }

    public static void Push(float scale, float duration, AnimationCurve ease) {
        TimeManager manager = Manager;
        if (scale == 0f && ease == null && manager.scales.Count > 0) {
            TimedScale top = manager.scales[^1];
            if (top.Scale == 0f && top.Ease == null) {
                // Overlapping hits should extend one freeze instead of stacking freezes.
                top.Duration = Mathf.Max(top.Duration, top.Elapsed + duration);
                return;
            }
        }

        manager.scales.Add(new TimedScale {
            Scale = scale,
            Duration = duration,
            Ease = ease,
        });
        manager.Apply();
    }

    private void Update() {
        if (IsFrozen || scales.Count == 0) return;

        for (int i = scales.Count - 1; i >= 0; i--) {
            scales[i].Elapsed += Time.unscaledDeltaTime;

            if (scales[i].Elapsed >= scales[i].Duration) {
                scales.RemoveAt(i);
            }
        }

        Apply();
    }

    private void Apply() {
        Time.timeScale = IsFrozen ? 0f : CurrentScale;
    }
}
