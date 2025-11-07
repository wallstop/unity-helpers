namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Unity-serializable alternative to <see cref="Nullable{T}"/> that supports ProtoBuf and JSON.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    [Serializable]
    [ProtoContract]
    [JsonConverter(typeof(SerializableNullableJsonConverterFactory))]
    public struct SerializableNullable<T>
        : IEquatable<SerializableNullable<T>>,
            IEquatable<T?>,
            IEquatable<T>,
            ISerializationCallbackReceiver,
            ISerializable
        where T : struct
    {
        [SerializeField]
        [ProtoMember(1)]
        private bool _hasValue;

        [SerializeField]
        [ProtoMember(2, IsRequired = false)]
        [WShowIf(nameof(_hasValue))]
        private T _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableNullable{T}"/> struct.
        /// </summary>
        /// <param name="value">Value to assign.</param>
        public SerializableNullable(T value)
        {
            _hasValue = true;
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableNullable{T}"/> struct.
        /// </summary>
        /// <param name="value">Nullable value to copy.</param>
        public SerializableNullable(T? value)
        {
            _hasValue = value.HasValue;
            _value = value.GetValueOrDefault();
        }

        private SerializableNullable(SerializationInfo info, StreamingContext context)
        {
            _hasValue = info.GetBoolean(nameof(_hasValue));
            if (_hasValue)
            {
                object boxed = info.GetValue(nameof(_value), typeof(T));
                _value = boxed != null ? (T)boxed : default;
            }
            else
            {
                _value = default;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the instance currently stores a value.
        /// </summary>
        public bool HasValue => _hasValue;

        /// <summary>
        /// Gets the stored value, throwing when the value is absent.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    throw new InvalidOperationException("Nullable object must have a value.");
                }

                return _value;
            }
        }

        /// <summary>
        /// Attempts to copy the stored value.
        /// </summary>
        /// <param name="result">Receives the stored value when present.</param>
        /// <returns>True when a value is available.</returns>
        public bool TryGetValue(out T result)
        {
            if (_hasValue)
            {
                result = _value;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Clears the stored value.
        /// </summary>
        public void Clear()
        {
            _hasValue = false;
            _value = default;
        }

        /// <summary>
        /// Assigns a new value.
        /// </summary>
        /// <param name="newValue">Value to assign.</param>
        public void SetValue(T newValue)
        {
            _hasValue = true;
            _value = newValue;
        }

        /// <summary>
        /// Returns the stored value or the underlying type default.
        /// </summary>
        public T GetValueOrDefault()
        {
            return _value;
        }

        /// <summary>
        /// Returns the stored value or a provided default.
        /// </summary>
        /// <param name="defaultValue">Fallback value.</param>
        public T GetValueOrDefault(T defaultValue)
        {
            return _hasValue ? _value : defaultValue;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return _hasValue ? _value.ToString() : string.Empty;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (!_hasValue)
            {
                return 0;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return comparer.GetHashCode(_value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is SerializableNullable<T> otherNullable)
            {
                return Equals(otherNullable);
            }

            if (obj is T otherValue)
            {
                return Equals(otherValue);
            }

            return obj == null && !_hasValue;
        }

        /// <inheritdoc/>
        public bool Equals(SerializableNullable<T> other)
        {
            if (_hasValue != other._hasValue)
            {
                return false;
            }

            if (!_hasValue)
            {
                return true;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return comparer.Equals(_value, other._value);
        }

        /// <inheritdoc/>
        public bool Equals(T? other)
        {
            if (_hasValue != other.HasValue)
            {
                return false;
            }

            if (!_hasValue)
            {
                return true;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return comparer.Equals(_value, other.GetValueOrDefault());
        }

        /// <inheritdoc/>
        public bool Equals(T other)
        {
            if (!_hasValue)
            {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return comparer.Equals(_value, other);
        }

        /// <summary>
        /// Implicit conversion from value to nullable wrapper.
        /// </summary>
        public static implicit operator SerializableNullable<T>(T value)
        {
            SerializableNullable<T> wrapper = new(value);
            return wrapper;
        }

        /// <summary>
        /// Implicit conversion from <see cref="Nullable{T}"/> to wrapper.
        /// </summary>
        public static implicit operator SerializableNullable<T>(T? value)
        {
            SerializableNullable<T> wrapper = new(value);
            return wrapper;
        }

        /// <summary>
        /// Implicit conversion to <see cref="Nullable{T}"/>.
        /// </summary>
        public static implicit operator T?(SerializableNullable<T> value)
        {
            if (!value._hasValue)
            {
                return null;
            }

            return value._value;
        }

        /// <summary>
        /// Explicit conversion to the underlying value.
        /// </summary>
        public static explicit operator T(SerializableNullable<T> value)
        {
            return value.Value;
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (!_hasValue)
            {
                _value = default;
            }
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (!_hasValue)
            {
                _value = default;
            }
        }

        /// <inheritdoc/>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_hasValue), _hasValue);
            if (_hasValue)
            {
                info.AddValue(nameof(_value), _value, typeof(T));
            }
        }

        internal static class SerializedPropertyNames
        {
            internal const string HasValue = nameof(_hasValue);
            internal const string Value = nameof(_value);
        }
    }

    internal static class SerializableNullableSerializedPropertyNames
    {
        internal const string HasValue = SerializableNullable<int>.SerializedPropertyNames.HasValue;
        internal const string Value = SerializableNullable<int>.SerializedPropertyNames.Value;
    }

    internal sealed class SerializableNullableJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            Type genericType = typeToConvert.GetGenericTypeDefinition();
            return genericType == typeof(SerializableNullable<>);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            Type[] arguments = type.GetGenericArguments();
            Type valueType = arguments[0];
            Type converterType = typeof(SerializableNullableJsonConverter<>).MakeGenericType(
                valueType
            );
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(converterType);
            return converter;
        }
    }

    internal sealed class SerializableNullableJsonConverter<T>
        : JsonConverter<SerializableNullable<T>>
        where T : struct
    {
        public override SerializableNullable<T> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                SerializableNullable<T> wrapper = default;
                wrapper.Clear();
                return wrapper;
            }

            T deserialized = JsonSerializer.Deserialize<T>(ref reader, options);
            SerializableNullable<T> result = new(deserialized);
            return result;
        }

        public override void Write(
            Utf8JsonWriter writer,
            SerializableNullable<T> value,
            JsonSerializerOptions options
        )
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
