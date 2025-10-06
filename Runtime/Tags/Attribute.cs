namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Core.Extension;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Represents a dynamic numeric attribute that supports temporary modifications through effects.
    /// Attributes maintain a base value and automatically calculate a current value by applying all active modifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a flexible system for game attributes (like health, speed, damage, etc.) that can be
    /// temporarily or permanently modified. Modifications are applied in a specific order based on their action type:
    /// Addition, then Multiplication, then Override.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create an attribute with base value of 100
    /// Attribute health = new Attribute(100f);
    ///
    /// // Apply a modification (e.g., +20 health from a buff)
    /// health.ApplyAttributeModification(new AttributeModification
    /// {
    ///     action = ModificationAction.Addition,
    ///     value = 20f
    /// }, effectHandle);
    ///
    /// // Current value is now 120
    /// float currentHealth = health.CurrentValue;
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    [ProtoContract]
    public sealed class Attribute
        : IEquatable<Attribute>,
            IEquatable<float>,
            IComparable<Attribute>,
            IComparable<float>
    {
        /// <summary>
        /// Gets the current calculated value of the attribute, including all active modifications.
        /// This value is cached and recalculated only when modifications change.
        /// </summary>
        /// <value>The current value after applying all modifications to the base value.</value>
        public float CurrentValue
        {
            get
            {
#if UNITY_EDITOR
                /*
                    For some reason, there's a bug with loot tables where
                    _currentValueCalculated will be true but the current
                    value is not calculated, so ignore the flag if we're
                    in editor mode, where this happens
                 */
                if (Application.isPlaying)
#endif
                {
                    if (_currentValueCalculated)
                    {
                        return _currentValue;
                    }
                }

                CalculateCurrentValue();
                return _currentValue;
            }
        }

        /// <summary>
        /// Gets the base value of the attribute before any modifications are applied.
        /// </summary>
        /// <value>The unmodified base value.</value>
        public float BaseValue => _baseValue;

        [SerializeField]
        internal float _baseValue;

        [SerializeField]
        private float _currentValue;

        private bool _currentValueCalculated;

        /// <summary>
        /// Recalculates the current value by applying all active modifications to the base value.
        /// Modifications are sorted and applied in order: Addition, Multiplication, then Override.
        /// </summary>
        internal void CalculateCurrentValue()
        {
            float calculatedValue = _baseValue;
            using PooledResource<List<AttributeModification>> modificationBuffer =
                Buffers<AttributeModification>.List.Get();
            List<AttributeModification> modifications = modificationBuffer.resource;
            foreach (
                KeyValuePair<EffectHandle, List<AttributeModification>> entry in _modifications
            )
            {
                foreach (AttributeModification modification in entry.Value)
                {
                    modifications.Add(modification);
                }
            }

            modifications.Sort((a, b) => ((int)a.action).CompareTo((int)b.action));

            foreach (AttributeModification attributeModification in modifications)
            {
                ApplyAttributeModification(attributeModification, ref calculatedValue);
            }

            _currentValue = calculatedValue;
            _currentValueCalculated = true;
        }

        private readonly Dictionary<EffectHandle, List<AttributeModification>> _modifications =
            new();

        /// <summary>
        /// Implicitly converts an Attribute to its current float value.
        /// </summary>
        /// <param name="attribute">The attribute to convert.</param>
        /// <returns>The current value of the attribute.</returns>
        public static implicit operator float(Attribute attribute) => attribute.CurrentValue;

        /// <summary>
        /// Implicitly converts a float value to an Attribute with that base value.
        /// </summary>
        /// <param name="value">The base value for the attribute.</param>
        /// <returns>A new Attribute with the specified base value.</returns>
        public static implicit operator Attribute(float value) => new(value);

        /// <summary>
        /// Initializes a new instance of the <see cref="Attribute"/> class with a base value of 0.
        /// </summary>
        public Attribute()
            : this(0) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Attribute"/> class with the specified base value.
        /// </summary>
        /// <param name="value">The base value for this attribute.</param>
        public Attribute(float value)
        {
            _baseValue = value;
            _currentValueCalculated = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Attribute"/> class for JSON deserialization.
        /// </summary>
        /// <param name="baseValue">The base value for this attribute.</param>
        /// <param name="currentValue">The cached current value.</param>
        [JsonConstructor]
        public Attribute(float baseValue, float currentValue)
        {
            _baseValue = baseValue;
            _currentValue = currentValue;
            _currentValueCalculated = true;
        }

        /// <summary>
        /// Clears the cached current value, forcing it to be recalculated on next access.
        /// </summary>
        public void ClearCache()
        {
            _currentValueCalculated = false;
        }

        /// <summary>
        /// Applies an attribute modification to this attribute.
        /// If a handle is provided, the modification is temporary and can be removed.
        /// If no handle is provided, the modification is permanent and applied directly to the base value.
        /// </summary>
        /// <param name="attributeModification">The modification to apply.</param>
        /// <param name="handle">Optional effect handle for temporary modifications. If null, the modification is permanent.</param>
        public void ApplyAttributeModification(
            AttributeModification attributeModification,
            EffectHandle? handle = null
        )
        {
            // If we don't have a handle, then this is an instant effect, apply it to the base value.
            if (!handle.HasValue)
            {
                ApplyAttributeModification(attributeModification, ref _baseValue);
            }
            else
            {
                _modifications.GetOrAdd(handle.Value).Add(attributeModification);
            }

            CalculateCurrentValue();
        }

        /// <summary>
        /// Removes all modifications associated with the specified effect handle.
        /// </summary>
        /// <param name="handle">The effect handle whose modifications should be removed.</param>
        /// <returns><c>true</c> if modifications were found and removed; otherwise, <c>false</c>.</returns>
        public bool RemoveAttributeModification(EffectHandle handle)
        {
            bool removed = _modifications.Remove(handle);
            if (removed)
            {
                CalculateCurrentValue();
            }

            return removed;
        }

        [OnDeserialized]
        private void AfterDeserialize(StreamingContext streamingContext)
        {
            ClearCache();
        }

        [ProtoAfterDeserialization]
        private void AfterProtoDeserialized()
        {
            ClearCache();
        }

        private static void ApplyAttributeModification(
            AttributeModification attributeModification,
            ref float value
        )
        {
            switch (attributeModification.action)
            {
                case ModificationAction.Addition:
                {
                    value += attributeModification.value;
                    break;
                }
                case ModificationAction.Multiplication:
                {
                    value *= attributeModification.value;
                    break;
                }
                case ModificationAction.Override:
                {
                    value = attributeModification.value;
                    break;
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(attributeModification.action),
                        (int)attributeModification.action,
                        typeof(ModificationAction)
                    );
                }
            }
        }

        /// <summary>
        /// Determines whether this attribute is equal to another attribute by comparing their current values.
        /// </summary>
        /// <param name="other">The attribute to compare with.</param>
        /// <returns><c>true</c> if the current values are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(Attribute other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null && CurrentValue.Equals(other.CurrentValue);
        }

        /// <summary>
        /// Compares this attribute to another attribute based on their current values.
        /// </summary>
        /// <param name="other">The attribute to compare with.</param>
        /// <returns>
        /// A value less than 0 if this attribute is less than <paramref name="other"/>;
        /// 0 if they are equal;
        /// a value greater than 0 if this attribute is greater than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(Attribute other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }
            return other == null ? 1 : CurrentValue.CompareTo(other.CurrentValue);
        }

        /// <summary>
        /// Compares this attribute's current value to a float value.
        /// </summary>
        /// <param name="other">The float value to compare with.</param>
        /// <returns>
        /// A value less than 0 if this attribute is less than <paramref name="other"/>;
        /// 0 if they are equal;
        /// a value greater than 0 if this attribute is greater than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(float other)
        {
            return CurrentValue.CompareTo(other);
        }

        /// <summary>
        /// Determines whether this attribute is equal to the specified object.
        /// Supports comparison with Attribute and numeric types.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object other)
        {
            switch (other)
            {
                case Attribute attribute:
                {
                    return Equals(attribute);
                }
                case float attribute:
                {
                    return Equals(attribute);
                }
                case double attribute:
                {
                    return Equals((float)attribute);
                }
                case int attribute:
                {
                    return Equals((float)attribute);
                }
                case long attribute:
                {
                    return Equals((float)attribute);
                }
                case short attribute:
                {
                    return Equals((float)attribute);
                }
                case uint attribute:
                {
                    return Equals((float)attribute);
                }
                case ulong attribute:
                {
                    return Equals((float)attribute);
                }
                case ushort attribute:
                {
                    return Equals((float)attribute);
                }
                case byte attribute:
                {
                    return Equals((float)attribute);
                }
                case sbyte attribute:
                {
                    return Equals((float)attribute);
                }
                default:
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines whether this attribute's current value equals the specified float value.
        /// </summary>
        /// <param name="other">The float value to compare with.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(float other)
        {
            return CurrentValue.Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this attribute.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        /// <summary>
        /// Converts this attribute to its string representation using the current value.
        /// </summary>
        /// <returns>A string representation of the current value in invariant culture format.</returns>
        public override string ToString()
        {
            return ((float)this).ToString(CultureInfo.InvariantCulture);
        }
    }
}
