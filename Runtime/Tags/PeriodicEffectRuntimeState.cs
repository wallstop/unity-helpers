namespace WallstopStudios.UnityHelpers.Tags
{
    using UnityEngine;

    /// <summary>
    /// Runtime tracking for a periodic effect definition.
    /// </summary>
    internal sealed class PeriodicEffectRuntimeState
    {
        internal PeriodicEffectRuntimeState(PeriodicEffectDefinition definition, float startTime)
        {
            Definition = definition;
            ExecutedTicks = 0;
            float clampedInterval = Mathf.Max(0.01f, definition.interval);
            Interval = clampedInterval;
            NextTickTime = startTime + Mathf.Max(0f, definition.initialDelay);
        }

        internal PeriodicEffectDefinition Definition { get; }

        internal float Interval { get; }

        internal float NextTickTime { get; private set; }

        internal int ExecutedTicks { get; private set; }

        internal bool IsComplete => Definition.maxTicks > 0 && ExecutedTicks >= Definition.maxTicks;

        internal bool TryConsumeTick(float currentTime)
        {
            if (currentTime < NextTickTime || IsComplete)
            {
                return false;
            }

            ++ExecutedTicks;
            NextTickTime = currentTime + Interval;
            return true;
        }
    }
}
