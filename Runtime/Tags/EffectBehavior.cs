namespace WallstopStudios.UnityHelpers.Tags
{
    using UnityEngine;

    /// <summary>
    /// Base class for authoring custom effect behaviours.
    /// Instances are cloned per applied handle so derived classes can keep state.
    /// </summary>
    public abstract class EffectBehavior : ScriptableObject
    {
        /// <summary>
        /// Invoked once when the effect handle becomes active.
        /// </summary>
        /// <param name="context">Runtime context for the effect instance.</param>
        public virtual void OnApply(EffectBehaviorContext context) { }

        /// <summary>
        /// Invoked every frame while the effect remains active.
        /// </summary>
        /// <param name="context">Runtime context for the effect instance.</param>
        public virtual void OnTick(EffectBehaviorContext context) { }

        /// <summary>
        /// Invoked after a periodic tick has been processed for the owning effect.
        /// </summary>
        /// <param name="context">Runtime context for the effect instance.</param>
        /// <param name="tickContext">Information about the specific periodic tick.</param>
        public virtual void OnPeriodicTick(
            EffectBehaviorContext context,
            PeriodicEffectTickContext tickContext
        ) { }

        /// <summary>
        /// Invoked when the effect handle is removed or expires.
        /// </summary>
        /// <param name="context">Runtime context for the effect instance.</param>
        public virtual void OnRemove(EffectBehaviorContext context) { }
    }

    /// <summary>
    /// Immutable runtime data passed to behaviour callbacks.
    /// </summary>
    public readonly struct EffectBehaviorContext
    {
        /// <summary>
        /// Gets the effect asset backing the handle.
        /// </summary>
        public AttributeEffect Effect => handle.effect;

        /// <summary>
        /// Gets the GameObject targeted by the effect handler.
        /// </summary>
        public GameObject Target => handler.gameObject;

        /// <summary>
        /// Gets the deltaTime used for the current invocation. For <see cref="EffectBehavior.OnApply"/>,
        /// this value is zero.
        /// </summary>
        public readonly float deltaTime;

        /// <summary>
        /// Gets the handler managing the effect.
        /// </summary>
        public readonly EffectHandler handler;

        /// <summary>
        /// Gets the handle associated with this behaviour invocation.
        /// </summary>
        public readonly EffectHandle handle;

        public EffectBehaviorContext(EffectHandler handler, EffectHandle handle, float deltaTime)
        {
            this.handler = handler;
            this.handle = handle;
            this.deltaTime = deltaTime;
        }
    }

    /// <summary>
    /// Details about a specific periodic tick that just executed.
    /// </summary>
    public readonly struct PeriodicEffectTickContext
    {
        /// <summary>
        /// Gets the periodic definition that produced this tick.
        /// </summary>
        public readonly PeriodicEffectDefinition definition;

        /// <summary>
        /// Gets the number of ticks executed so far, including this one.
        /// </summary>
        public readonly int executedTicks;

        /// <summary>
        /// Gets the timestamp when the tick occurred.
        /// </summary>
        public readonly float currentTime;

        public PeriodicEffectTickContext(
            PeriodicEffectDefinition definition,
            int executedTicks,
            float currentTime
        )
        {
            this.definition = definition;
            this.executedTicks = executedTicks;
            this.currentTime = currentTime;
        }
    }
}
