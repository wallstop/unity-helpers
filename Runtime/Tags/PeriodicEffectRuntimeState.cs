namespace WallstopStudios.UnityHelpers.Tags
{
    using UnityEngine;

    /// <summary>
    /// Runtime tracking for a periodic effect definition.
    /// </summary>
    internal sealed class PeriodicEffectRuntimeState
    {
        internal bool IsComplete => definition.maxTicks > 0 && ExecutedTicks >= definition.maxTicks;

        internal float NextTickTime { get; private set; }

        internal int ExecutedTicks { get; private set; }

        internal readonly PeriodicEffectDefinition definition;
        internal readonly float interval;

        internal PeriodicEffectRuntimeState(PeriodicEffectDefinition definition, float startTime)
        {
            this.definition = definition;
            ExecutedTicks = 0;
            float clampedInterval = Mathf.Max(0.01f, definition.interval);
            interval = clampedInterval;
            NextTickTime = startTime + Mathf.Max(0f, definition.initialDelay);
        }

        internal bool TryConsumeTick(float currentTime)
        {
            if (currentTime < NextTickTime || IsComplete)
            {
                return false;
            }

            ++ExecutedTicks;
            NextTickTime = currentTime + interval;
            return true;
        }
    }
}
