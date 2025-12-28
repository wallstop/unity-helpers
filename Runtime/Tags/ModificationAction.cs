// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tags
{
    /// <summary>
    /// Specifies the type of action to perform when modifying an attribute value.
    /// Actions are applied in a specific order to ensure consistent calculation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When multiple modifications are applied to an attribute, they are processed in this order:
    /// 1. All Addition modifications
    /// 2. All Multiplication modifications
    /// 3. All Override modifications (last override wins)
    /// </para>
    /// <para>
    /// Example calculation with base value 100:
    /// <code>
    /// Base: 100
    /// + Addition(20): 120
    /// + Addition(-10): 110
    /// * Multiplication(1.5): 165
    /// * Multiplication(0.8): 132
    /// = Override(200): 200 (if present, completely replaces the value)
    /// </code>
    /// </para>
    /// </remarks>
    public enum ModificationAction
    {
        /// <summary>
        /// Adds the modification value to the current value.
        /// Use negative values for subtraction.
        /// Applied first in the calculation order.
        /// </summary>
        /// <example>
        /// A value of 20 adds 20 to the attribute.
        /// A value of -10 subtracts 10 from the attribute.
        /// </example>
        Addition = 0,

        /// <summary>
        /// Multiplies the current value by the modification value.
        /// Use values greater than 1 to increase, less than 1 to decrease.
        /// Applied second in the calculation order, after all additions.
        /// </summary>
        /// <example>
        /// A value of 1.5 increases by 50% (150% of original).
        /// A value of 0.5 decreases by 50% (50% of original).
        /// A value of 2.0 doubles the value.
        /// </example>
        Multiplication = 1,

        /// <summary>
        /// Completely replaces the current value with the modification value.
        /// Applied last in the calculation order. If multiple overrides exist, the last one wins.
        /// </summary>
        /// <example>
        /// A value of 999 sets the attribute to exactly 999, ignoring all previous calculations.
        /// </example>
        Override = 2,
    }
}
