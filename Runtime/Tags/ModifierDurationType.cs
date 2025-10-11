namespace WallstopStudios.UnityHelpers.Tags
{
    using System;

    /// <summary>
    /// Specifies how long an effect's modifications should persist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enum determines the lifecycle of an applied effect:
    /// - Instant effects apply once and modify the base value permanently
    /// - Duration effects are temporary and automatically removed after a specified time
    /// - Infinite effects persist until manually removed
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create a temporary speed boost that lasts 5 seconds
    /// var speedBoost = ScriptableObject.CreateInstance&lt;AttributeEffect&gt;();
    /// speedBoost.durationType = ModifierDurationType.Duration;
    /// speedBoost.duration = 5f;
    ///
    /// // Create a permanent stat increase
    /// var permanentBonus = ScriptableObject.CreateInstance&lt;AttributeEffect&gt;();
    /// permanentBonus.durationType = ModifierDurationType.Instant;
    ///
    /// // Create a buff that lasts until dispelled
    /// var buff = ScriptableObject.CreateInstance&lt;AttributeEffect&gt;();
    /// buff.durationType = ModifierDurationType.Infinite;
    /// </code>
    /// </para>
    /// </remarks>
    public enum ModifierDurationType
    {
        /// <summary>
        /// Invalid/uninitialized value. Do not use.
        /// </summary>
        [Obsolete("Please use a valid value.")]
        None = 0,

        /// <summary>
        /// The effect applies immediately and permanently modifies the base attribute value.
        /// Cannot be removed later. No EffectHandle is created.
        /// </summary>
        /// <example>
        /// Use for permanent stat increases, level-up bonuses, or consumable items
        /// that permanently enhance a character.
        /// </example>
        Instant = 1,

        /// <summary>
        /// The effect is temporary and automatically expires after the specified duration.
        /// Creates an EffectHandle that can be manually removed before expiration.
        /// </summary>
        /// <example>
        /// Use for temporary buffs, debuffs, potion effects, or timed power-ups.
        /// Duration is specified in the <see cref="AttributeEffect.duration"/> field.
        /// </example>
        Duration = 2,

        /// <summary>
        /// The effect persists indefinitely until manually removed.
        /// Creates an EffectHandle that must be explicitly removed.
        /// </summary>
        /// <example>
        /// Use for equipment bonuses, persistent auras, or status effects
        /// that should remain active until a specific condition is met.
        /// </example>
        Infinite = 3,
    }
}
