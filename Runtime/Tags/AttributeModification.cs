namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using Core.Extension;
    using Core.Helper;

    [Serializable]
    public struct AttributeModification : IEquatable<AttributeModification>
    {
        [StringInList(typeof(AttributeUtilities), nameof(AttributeUtilities.GetAllAttributeNames))]
        public string attribute;

        public ModificationAction action;
        public float value;

        public override string ToString()
        {
            return this.ToJson();
        }

        public static bool operator !=(AttributeModification lhs, AttributeModification rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(AttributeModification lhs, AttributeModification rhs)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return string.Equals(lhs.attribute, rhs.attribute)
                && lhs.action == rhs.action
                && lhs.value == rhs.value;
        }

        public override bool Equals(object obj)
        {
            return obj is AttributeModification other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Objects.HashCode(attribute, action, value);
        }

        public bool Equals(AttributeModification other)
        {
            return string.Equals(attribute, other.attribute, StringComparison.Ordinal)
                && action == other.action
                && value.Equals(other.value);
        }
    }
}
