using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TimeManager : MonoBehaviour {
    private sealed class TimedScale {
        public float Scale;
        public float Duration;
        public float Elapsed;
        public float UnderlyingScale;
        public float CurrentScale;
        public AnimationCurve Ease;
    }

    private static TimeManager instance;

    [SerializeField, Min(0f)] private float defaultTimeScale = 1f;

    private readonly Stack<TimedScale> scales = new();
    private int freezeCount;
    private float activeScale;

    private static TimeManager Instance {
        get {
            if (instance != null) return instance;

            instance = FindAnyObjectByType<TimeManager>();
            if (instance == null) {
                instance = new GameObject(nameof(TimeManager)).AddComponent<TimeManager>();
            }

            return instance;
        }
    }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        activeScale = defaultTimeScale;
        Apply();
    }

    private void OnDestroy() {
        if (instance != this) return;

        Time.timeScale = defaultTimeScale;
        instance = null;
    }

    public static void Freeze() {
        TimeManager manager = Instance;
        manager.freezeCount++;
        manager.Apply();
    }

    public static void Resume() {
        if (instance == null || instance.freezeCount == 0) return;

        instance.freezeCount--;
        instance.Apply();
    }

    public static void Push(float scale, float duration, AnimationCurve ease) {
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0f) {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f) {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        TimeManager manager = Instance;
        if (scale == 0f && ease == null && manager.scales.Count > 0) {
            TimedScale top = manager.scales.Peek();
            if (top.Scale == 0f && top.Ease == null) {
                // Overlapping hits should extend one freeze instead of stacking freezes.
                top.Duration = Mathf.Max(top.Duration, top.Elapsed + duration);
                return;
            }
        }

        float underlyingScale = manager.activeScale;
        float initialBlend = ease == null ? 0f : Mathf.Clamp01(ease.Evaluate(0f));
        float initialScale = Mathf.Lerp(scale, underlyingScale, initialBlend);
        manager.scales.Push(new TimedScale {
            Scale = scale,
            Duration = duration,
            UnderlyingScale = underlyingScale,
            CurrentScale = initialScale,
            Ease = ease,
        });
        manager.activeScale = initialScale;
        manager.Apply();
    }

    private void Update() {
        if (freezeCount > 0 || scales.Count == 0) return;

        TimedScale top = scales.Peek();
        top.Elapsed += Time.unscaledDeltaTime;

        if (top.Elapsed >= top.Duration) {
            scales.Pop();
            activeScale = scales.Count > 0 ? scales.Peek().CurrentScale : defaultTimeScale;
        } else {
            float progress = top.Elapsed / top.Duration;
            float blend = top.Ease == null ? 0f : Mathf.Clamp01(top.Ease.Evaluate(progress));
            top.CurrentScale = Mathf.Lerp(top.Scale, top.UnderlyingScale, blend);
            activeScale = top.CurrentScale;
        }

        Apply();
    }

    private void Apply() {
        Time.timeScale = freezeCount > 0 ? 0f : activeScale;
    }
}
