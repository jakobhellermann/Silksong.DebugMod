#if HK1578
using System.Collections.Generic;
using UnityEngine;

// TODO(port/1.5.78): stub for the TimeManager of 1.5.12620
// ReSharper disable once CheckNamespace
internal static class TimeManager
{
    private static readonly List<TimeControlInstance> instances = [];

    public static float TimeScale
    {
        get => TimeController.GenericTimeScale;
        set
        {
            TimeController.GenericTimeScale = value;
            UpdateTimeScale();
        }
    }

    public sealed class TimeControlInstance
    {
        public enum Type
        {
            Multiplicative,
        }

        public float timeScale;

        public float TimeScale
        {
            get => timeScale;
            set
            {
                timeScale = value;
                UpdateTimeScale();
            }
        }

        public TimeControlInstance(float timeScale, Type type)
        {
            this.timeScale = timeScale;
            instances.Add(this);
            UpdateTimeScale();
        }

        public void Release()
        {
            instances.Remove(this);
            UpdateTimeScale();
        }
    }

    private static void UpdateTimeScale()
    {
        float scale = TimeController.SlowMotionTimeScale * TimeController.PauseTimeScale
            * TimeController.PlatformBackgroundTimeScale * TimeController.GenericTimeScale;
        foreach (TimeControlInstance instance in instances)
        {
            scale *= instance.timeScale;
        }
        Time.timeScale = scale < 0.01f ? 0f : scale;
    }
}
#endif
