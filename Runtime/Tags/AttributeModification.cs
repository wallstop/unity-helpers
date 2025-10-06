namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using Core.Extension;
    using Core.Helper;

    /// <summary>
    /// Represents a single modification to be applied to an attribute.
    /// Modifications define how an attribute's value should be changed using a specific action and value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Modifications are applied in a specific order based on their <see cref="ModificationAction"/>:
    /// 1. Addition - Adds or subtracts from the value
    /// 2. Multiplication - Scales the value
    /// 3. Override - Completely replaces the value
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create a modification that adds 50 to a Health attribute
    /// var healthBoost = new AttributeModification
    /// {
    ///     attribute = "Health",
    ///     action = ModificationAction.Addition,
    ///     value = 50f
    /// };
    ///
    /// // Create a modification that multiplies Speed by 1.5 (150% speed)
    /// var speedBoost = new AttributeModification
    /// {
    ///     attribute = "Speed",
    ///     action = ModificationAction.Multiplication,
    ///     value = 1.5f
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public struct AttributeModification : IEquatable<AttributeModification>
    {
        /// <summary>
        /// The name of the attribute to modify. This should match a field name in an <see cref="AttributesComponent"/> subclass.
        /// </summary>
        [StringInList(typeof(AttributeUtilities), nameof(AttributeUtilities.GetAllAttributeNames))]
        public string attribute;

        /// <summary>
        /// The type of modification action to perform (Addition, Multiplication, or Override).
        /// </summary>
        public ModificationAction action;

        /// <summary>
        /// The value to use for the modification. Interpretation depends on the <see cref="action"/>:
        /// <para>- Addition: The amount to add (can be negative for subtraction)</para>
        /// <para>- Multiplication: The multiplier to apply (e.g., 1.5 for +50%, 0.5 for -50%)</para>
        /// <para>- Override: The new absolute value to set</para>
        /// </summary>
        public float value;

        /// <summary>
        /// Converts this modification to a JSON string representation.
        /// </summary>
        /// <returns>A JSON string representing this modification.</returns>
        public override string ToString()
        {
            return this.ToJson();
        }

        /// <summary>
        /// Determines whether two attribute modifications are not equal.
        /// </summary>
        /// <param name="lhs">The first modification to compare.</param>
        /// <param name="rhs">The second modification to compare.</param>
        /// <returns><c>true</c> if the modifications are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(AttributeModification lhs, AttributeModification rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Determines whether two attribute modifications are equal.
        /// </summary>
        /// <param name="lhs">The first modification to compare.</param>
        /// <param name="rhs">The second modification to compare.</param>
        /// <returns><c>true</c> if the modifications are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(AttributeModification lhs, AttributeModification rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Determines whether this modification equals the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> if the object is an AttributeModification with equal values; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AttributeModification other && Equals(other);
        }

        /// <summary>
        /// Determines whether this modification equals another modification by comparing all fields.
        /// </summary>
        /// <param name="other">The modification to compare with.</param>
        /// <returns><c>true</c> if all fields are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(AttributeModification other)
        {
            return string.Equals(attribute, other.attribute, StringComparison.Ordinal)
                && action == other.action
                && value.Equals(other.value);
        }

        /// <summary>
        /// Returns the hash code for this modification.
        /// </summary>
        /// <returns>A hash code combining the attribute name, action, and value.</returns>
        public override int GetHashCode()
        {
            return Objects.HashCode(attribute, action, value);
        }
    }
}
