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

    [Serializable]
    [ProtoContract]
    public sealed class Attribute
        : IEquatable<Attribute>,
            IEquatable<float>,
            IComparable<Attribute>,
            IComparable<float>
    {
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

        public float BaseValue => _baseValue;

        [SerializeField]
        internal float _baseValue;

        [SerializeField]
        private float _currentValue;

        private bool _currentValueCalculated;

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

        public static implicit operator float(Attribute attribute) => attribute.CurrentValue;

        public static implicit operator Attribute(float value) => new(value);

        public Attribute()
            : this(0) { }

        public Attribute(float value)
        {
            _baseValue = value;
            _currentValueCalculated = false;
        }

        [JsonConstructor]
        public Attribute(float baseValue, float currentValue)
        {
            _baseValue = baseValue;
            _currentValue = currentValue;
            _currentValueCalculated = true;
        }

        public void ClearCache()
        {
            _currentValueCalculated = false;
        }

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

        public bool Equals(Attribute other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null && CurrentValue.Equals(other.CurrentValue);
        }

        public int CompareTo(Attribute other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }
            return other == null ? 1 : CurrentValue.CompareTo(other.CurrentValue);
        }

        public int CompareTo(float other)
        {
            return CurrentValue.CompareTo(other);
        }

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

        public bool Equals(float other)
        {
            return CurrentValue.Equals(other);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return ((float)this).ToString(CultureInfo.InvariantCulture);
        }
    }
}
