namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Authoring data for a periodic modifier bundle that executes on an interval while an effect is active.
    /// </summary>
    [Serializable]
    public sealed class PeriodicEffectDefinition
    {
        /// <summary>
        /// Optional label shown in tooling to help distinguish multiple periodic definitions.
        /// </summary>
        public string name;

        /// <summary>
        /// Time (seconds) before the first tick fires after the effect is applied.
        /// </summary>
        [Min(0f)]
        public float initialDelay;

        /// <summary>
        /// Interval (seconds) between ticks once the first tick has executed.
        /// </summary>
        [Min(0.01f)]
        public float interval = 1f;

        /// <summary>
        /// Maximum number of ticks to execute. Zero or negative means unlimited ticks.
        /// </summary>
        [Min(0)]
        public int maxTicks;

        /// <summary>
        /// Attribute modifications applied each time the tick fires.
        /// </summary>
        public List<AttributeModification> modifications = new();
    }
}
