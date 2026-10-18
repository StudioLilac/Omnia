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

    private string DescribeState(string action) {
        StringBuilder message = new StringBuilder()
            .Append("[TimeManager] ").Append(action)
            .Append(" | frame=").Append(Time.frameCount)
            .Append(" | frozen=").Append(IsFrozen)
            .Append(" | subscribers=").Append(freezeSources.Count)
            .Append(" | timeScale=").Append(Time.timeScale)
            .Append(" | scales=[");

        for (int i = 0; i < scales.Count; i++) {
            if (i > 0) message.Append(", ");

            TimeScale timeScale = scales[i];
            message.Append('#').Append(i)
                .Append(" {scale=").Append(timeScale.Scale)
                .Append(", elapsed=").Append(timeScale.Elapsed)
                .Append('/').Append(timeScale.Duration)
                .Append(", ease=").Append(timeScale.Ease == null ? "none" : "curve")
                .Append('}');
        }

        return message.Append(']').ToString();
    }

    public void Freeze(object source) {
        if (source == null) return;
        if (freezeSources.Add(source)) Debug.Log(DescribeState("Freeze"), this);
    }

    public void Unfreeze(object source) {
        if (source == null) return;
        if (freezeSources.Remove(source)) Debug.Log(DescribeState("Unfreeze"), this);
    }

    public void Push(float scale, float duration, AnimationCurve ease) {
        scales.Add(new TimeScale {
            Scale = scale,
            Duration = duration,
            Ease = ease,
        });
        Debug.Log(DescribeState("Push"), this);
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
        if (expiredCount > 0) Debug.Log(DescribeState($"Expired {expiredCount} scale(s)"), this);
    }
}
