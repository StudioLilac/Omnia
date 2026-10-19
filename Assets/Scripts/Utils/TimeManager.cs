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

    private static TimeManager instance;

    private static float defaultTimeScale = 1f;

    private readonly List<TimeScale> scales = new();
    private readonly HashSet<object> freezeSources = new();

    private bool IsFrozen => freezeSources.Count > 0;

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
    }

    private void OnDestroy() {
        if (instance != this) return;

        Time.timeScale = defaultTimeScale;
        instance = null;
    }

    public static void Freeze(object source) {
        if (source == null) return;
        TimeManager manager = Manager;
        manager.freezeSources.Add(source);
    }

    public static void Unfreeze(object source) {
        if (source == null) return;
        instance?.freezeSources.Remove(source);
    }

    public static void Push(float scale, float duration, AnimationCurve ease) {
        TimeManager manager = Manager;
        manager.scales.Add(new TimeScale {
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
        foreach (TimeScale TimeScale in scales) {
            float easedScale = TimeScale.Ease == null ? TimeScale.Scale : Mathf.Lerp(TimeScale.Scale, defaultTimeScale, Mathf.Clamp01(TimeScale.Ease.Evaluate(TimeScale.Elapsed / TimeScale.Duration)));
            scale = Mathf.Min(scale, easedScale);
        }
        Time.timeScale = scale;
    }
}
