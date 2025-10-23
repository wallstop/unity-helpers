namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Text.Json.Serialization;
    using Core.Extension;
    using Core.Helper;
    using ProtoBuf;

    /// <summary>
    /// Declarative change to an <see cref="Attribute"/> value (add, multiply, or override).
    /// Forms the stat‑modification payload inside an <see cref="AttributeEffect"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Problems solved:
    /// - Non‑destructive stat changes that can be added/removed per effect instance
    /// - Clear stacking rules via action ordering
    /// - Works with both permanent (Instant) and temporary (Duration/Infinite) effects
    /// </para>
    /// <para>
    /// Stacking and order: Modifications are applied in this order across a target attribute:
    /// 1) Addition (value += x) → 2) Multiplication (value *= x) → 3) Override (value = x).
    /// This means Overrides always win last; use with care.
    /// </para>
    /// <para>
    /// Addressing: The <see cref="attribute"/> field names an <see cref="AttributesComponent"/> field of type
    /// <see cref="Attribute"/>. Misspelled or missing names are ignored at runtime to keep effects robust.
    /// Use the Attribute Metadata Cache generator to populate editor dropdowns and avoid typos.
    /// </para>
    /// <para>
    /// Examples:
    /// <code>
    /// // +50 flat Health
    /// new AttributeModification { attribute = "Health", action = ModificationAction.Addition, value = 50f };
    ///
    /// // +50% Speed (i.e., multiply by 1.5)
    /// new AttributeModification { attribute = "Speed", action = ModificationAction.Multiplication, value = 1.5f };
    ///
    /// // Set Defense to 0 (hard override)
    /// new AttributeModification { attribute = "Defense", action = ModificationAction.Override, value = 0f };
    /// </code>
    /// </para>
    /// <para>
    /// Tips:
    /// - Prefer Addition for small buffs/debuffs; prefer Multiplication for % changes.
    /// - Avoid frequent Overrides unless you intend to fully clamp a value.
    /// - Use negative Addition values to subtract; use Multiplication < 1.0 for % reductions.
    /// </para>
    /// </remarks>
    [Serializable]
    [ProtoContract]
    public struct AttributeModification : IEquatable<AttributeModification>
    {
        /// <summary>
        /// The name of the attribute to modify. This should match a field name in an <see cref="AttributesComponent"/> subclass.
        /// </summary>
        [StringInList(typeof(AttributeUtilities), nameof(AttributeUtilities.GetAllAttributeNames))]
        [ProtoMember(1)]
        public string attribute;

        /// <summary>
        /// The type of modification action to perform (Addition, Multiplication, or Override).
        /// </summary>
        [ProtoMember(2)]
        public ModificationAction action;

        /// <summary>
        /// The value to use for the modification. Interpretation depends on the <see cref="action"/>:
        /// <para>- Addition: The amount to add (can be negative for subtraction)</para>
        /// <para>- Multiplication: The multiplier to apply (e.g., 1.5 for +50%, 0.5 for -50%)</para>
        /// <para>- Override: The new absolute value to set</para>
        /// </summary>
        [ProtoMember(3)]
        public float value;

        [JsonConstructor]
        public AttributeModification(string attribute, ModificationAction action, float value)
        {
            this.attribute = attribute;
            this.action = action;
            this.value = value;
        }

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
