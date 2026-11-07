#if HK1578_OR_OLDER
using System.Collections.Generic;
using UnityEngine;

// TODO(port/1.5.78): stub for the TimeManager of 1.5.12620
// ReSharper disable once CheckNamespace
internal static class TimeManager
{
    private static readonly List<TimeControlInstance> instances = [];

#if HK1221_OR_OLDER
    // TODO(port/1.2.2.1): the game writes Time.timeScale directly
    private static float baseTimeScale = 1f;

    public static float TimeScale
    {
        get => baseTimeScale;
        set
        {
            baseTimeScale = value;
            UpdateTimeScale();
        }
    }
#else
    public static float TimeScale
    {
        get => TimeController.GenericTimeScale;
        set
        {
            TimeController.GenericTimeScale = value;
            UpdateTimeScale();
        }
    }
#endif

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
#if HK1221_OR_OLDER
        float scale = baseTimeScale;
#else
        float scale = TimeController.SlowMotionTimeScale * TimeController.PauseTimeScale
            * TimeController.PlatformBackgroundTimeScale * TimeController.GenericTimeScale;
#endif
        foreach (TimeControlInstance instance in instances)
        {
            scale *= instance.timeScale;
        }
        Time.timeScale = scale < 0.01f ? 0f : scale;
    }
}
#endif
