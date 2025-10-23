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
        public EffectBehaviorContext(EffectHandler handler, EffectHandle handle, float deltaTime)
        {
            Handler = handler;
            Handle = handle;
            DeltaTime = deltaTime;
        }

        /// <summary>
        /// Gets the handler managing the effect.
        /// </summary>
        public EffectHandler Handler { get; }

        /// <summary>
        /// Gets the handle associated with this behaviour invocation.
        /// </summary>
        public EffectHandle Handle { get; }

        /// <summary>
        /// Gets the effect asset backing the handle.
        /// </summary>
        public AttributeEffect Effect => Handle.effect;

        /// <summary>
        /// Gets the GameObject targeted by the effect handler.
        /// </summary>
        public GameObject Target => Handler.gameObject;

        /// <summary>
        /// Gets the deltaTime used for the current invocation. For <see cref="EffectBehavior.OnApply"/>,
        /// this value is zero.
        /// </summary>
        public float DeltaTime { get; }
    }

    /// <summary>
    /// Details about a specific periodic tick that just executed.
    /// </summary>
    public readonly struct PeriodicEffectTickContext
    {
        public PeriodicEffectTickContext(
            PeriodicEffectDefinition definition,
            int executedTicks,
            float currentTime
        )
        {
            Definition = definition;
            ExecutedTicks = executedTicks;
            CurrentTime = currentTime;
        }

        /// <summary>
        /// Gets the periodic definition that produced this tick.
        /// </summary>
        public PeriodicEffectDefinition Definition { get; }

        /// <summary>
        /// Gets the number of ticks executed so far, including this one.
        /// </summary>
        public int ExecutedTicks { get; }

        /// <summary>
        /// Gets the timestamp when the tick occurred.
        /// </summary>
        public float CurrentTime { get; }
    }
}
