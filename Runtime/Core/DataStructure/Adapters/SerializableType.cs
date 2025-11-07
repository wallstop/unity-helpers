namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Unity serializable wrapper for <see cref="Type"/> that supports JSON and ProtoBuf.
    /// </summary>
    [Serializable]
    [ProtoContract]
    [JsonConverter(typeof(SerializableTypeJsonConverter))]
    public struct SerializableType
        : IEquatable<SerializableType>,
            ISerializationCallbackReceiver,
            ISerializable
    {
        [SerializeField]
        [ProtoMember(1)]
        [StringInList(
            typeof(SerializableTypeCatalog),
            nameof(SerializableTypeCatalog.GetAssemblyQualifiedNames)
        )]
        private string _assemblyQualifiedName;

        [NonSerialized]
        [ProtoIgnore]
        [JsonIgnore]
        private Type _cachedType;

        [NonSerialized]
        [ProtoIgnore]
        [JsonIgnore]
        private bool _resolutionAttempted;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableType"/> struct.
        /// </summary>
        /// <param name="type">Type to wrap.</param>
        public SerializableType(Type type)
        {
            _assemblyQualifiedName = NormalizeTypeName(type);
            _cachedType = type;
            _resolutionAttempted = true;
        }

        private SerializableType(SerializationInfo info, StreamingContext context)
        {
            _assemblyQualifiedName = info.GetString(nameof(_assemblyQualifiedName)) ?? string.Empty;
            _cachedType = SerializableTypeCatalog.Resolve(_assemblyQualifiedName);
            _resolutionAttempted = true;
        }

        /// <summary>
        /// Gets a value indicating whether this instance does not reference a type.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(_assemblyQualifiedName);

        /// <summary>
        /// Gets the stored assembly qualified type name.
        /// </summary>
        public string AssemblyQualifiedName =>
            string.IsNullOrEmpty(_assemblyQualifiedName) ? string.Empty : _assemblyQualifiedName;

        /// <summary>
        /// Gets a user-friendly display name for the wrapped type.
        /// </summary>
        public string DisplayName
        {
            get
            {
                Type resolved = GetResolvedType();
                if (resolved == null)
                {
                    return string.IsNullOrEmpty(_assemblyQualifiedName)
                        ? SerializableTypeCatalog.NoneDisplayName
                        : $"{SerializableTypeCatalog.NoneDisplayName}: {_assemblyQualifiedName}";
                }

                return SerializableTypeCatalog.GetDisplayName(resolved);
            }
        }

        /// <summary>
        /// Gets the resolved <see cref="Type"/> instance, or <c>null</c> when unresolved.
        /// </summary>
        public Type Value => GetResolvedType();

        /// <summary>
        /// Assigns the wrapper to the provided <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type to store.</param>
        public void SetType(Type type)
        {
            _assemblyQualifiedName = NormalizeTypeName(type);
            _cachedType = type;
            _resolutionAttempted = true;
        }

        /// <summary>
        /// Attempts to retrieve the wrapped <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Receives the resolved type if available.</param>
        /// <returns>True when a type is available and could be resolved.</returns>
        public bool TryGetValue(out Type type)
        {
            type = GetResolvedType();
            return type != null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return DisplayName;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return IsEmpty;
            }

            if (obj is SerializableType serializableType)
            {
                return Equals(serializableType);
            }

            if (obj is Type type)
            {
                return EqualsType(type);
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(SerializableType other)
        {
            string leftName = string.IsNullOrEmpty(_assemblyQualifiedName)
                ? string.Empty
                : _assemblyQualifiedName;
            string rightName = string.IsNullOrEmpty(other._assemblyQualifiedName)
                ? string.Empty
                : other._assemblyQualifiedName;
            return string.Equals(leftName, rightName, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        /// <summary>
        /// Compares the wrapped type against a <see cref="Type"/> instance.
        /// </summary>
        /// <param name="other">Type to compare.</param>
        /// <returns>True when both represent the same type or both are empty.</returns>
        public bool EqualsType(Type other)
        {
            if (other == null)
            {
                return string.IsNullOrEmpty(_assemblyQualifiedName);
            }

            string normalized = NormalizeTypeName(other);
            return string.Equals(_assemblyQualifiedName, normalized, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(_assemblyQualifiedName))
            {
                return 0;
            }

            return StringComparer.Ordinal.GetHashCode(_assemblyQualifiedName);
        }

        /// <summary>
        /// Equality comparison between two <see cref="SerializableType"/> instances.
        /// </summary>
        public static bool operator ==(SerializableType left, SerializableType right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality comparison between two <see cref="SerializableType"/> instances.
        /// </summary>
        public static bool operator !=(SerializableType left, SerializableType right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Equality comparison between a <see cref="SerializableType"/> and a <see cref="Type"/>.
        /// </summary>
        public static bool operator ==(SerializableType left, Type right)
        {
            Type leftType = left.Value;
            if (right == null)
            {
                return leftType == null;
            }

            if (leftType != null)
            {
                return leftType == right;
            }

            return left.EqualsType(right);
        }

        /// <summary>
        /// Inequality comparison between a <see cref="SerializableType"/> and a <see cref="Type"/>.
        /// </summary>
        public static bool operator !=(SerializableType left, Type right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Equality comparison between a <see cref="Type"/> and a <see cref="SerializableType"/>.
        /// </summary>
        public static bool operator ==(Type left, SerializableType right)
        {
            Type rightType = right.Value;
            if (left == null)
            {
                return rightType == null;
            }

            if (rightType != null)
            {
                return rightType == left;
            }

            return right.EqualsType(left);
        }

        /// <summary>
        /// Inequality comparison between a <see cref="Type"/> and a <see cref="SerializableType"/>.
        /// </summary>
        public static bool operator !=(Type left, SerializableType right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Implicitly converts the wrapper to a <see cref="Type"/>.
        /// </summary>
        public static implicit operator Type(SerializableType value)
        {
            return value.Value;
        }

        /// <summary>
        /// Creates a wrapper from the provided <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type to wrap.</param>
        /// <returns>The constructed <see cref="SerializableType"/>.</returns>
        public static SerializableType FromType(Type type)
        {
            return new SerializableType(type);
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_cachedType != null)
            {
                _assemblyQualifiedName = NormalizeTypeName(_cachedType);
            }
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _cachedType = SerializableTypeCatalog.Resolve(_assemblyQualifiedName);
            _resolutionAttempted = true;
        }

        /// <inheritdoc/>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_assemblyQualifiedName), _assemblyQualifiedName);
        }

        private Type GetResolvedType()
        {
            if (_cachedType != null)
            {
                return _cachedType;
            }

            if (string.IsNullOrEmpty(_assemblyQualifiedName))
            {
                _resolutionAttempted = true;
                return null;
            }

            if (!_resolutionAttempted)
            {
                _cachedType = SerializableTypeCatalog.Resolve(_assemblyQualifiedName);
                _resolutionAttempted = true;
            }
            else if (_cachedType == null)
            {
                _cachedType = SerializableTypeCatalog.Resolve(_assemblyQualifiedName);
            }

            return _cachedType;
        }

        internal static SerializableType FromSerializedName(string serialized)
        {
            SerializableType value = default;
            value._assemblyQualifiedName = string.IsNullOrEmpty(serialized)
                ? string.Empty
                : serialized;
            value._cachedType = SerializableTypeCatalog.Resolve(value._assemblyQualifiedName);
            value._resolutionAttempted = true;
            return value;
        }

        internal static string NormalizeTypeName(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            string assemblyQualifiedName = type.AssemblyQualifiedName;
            if (!string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return assemblyQualifiedName;
            }

            string fullName = type.FullName;
            if (!string.IsNullOrEmpty(fullName))
            {
                Assembly assembly = type.Assembly;
                string assemblyName = assembly != null ? assembly.GetName().Name : string.Empty;
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    return $"{fullName}, {assemblyName}";
                }

                return fullName;
            }

            string name = type.Name;
            if (!string.IsNullOrEmpty(name))
            {
                Assembly assembly = type.Assembly;
                string assemblyName = assembly != null ? assembly.GetName().Name : string.Empty;
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    return $"{name}, {assemblyName}";
                }

                return name;
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Catalog utilities for <see cref="SerializableType"/>.
    /// </summary>
    public static class SerializableTypeCatalog
    {
        internal const string NoneDisplayName = "<None>";

        private static readonly object SyncRoot = new();
        private static SerializableTypeDescriptor[] _descriptors;
        private static Dictionary<string, SerializableTypeDescriptor> _descriptorByName;
        private static string[] _assemblyQualifiedNames;
        private static readonly Dictionary<string, SerializableTypeDescriptor[]> FilterCache = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Resolves a type from an assembly qualified name.
        /// </summary>
        /// <param name="assemblyQualifiedName">Assembly qualified name to resolve.</param>
        /// <returns>The resolved type when found; otherwise, <c>null</c>.</returns>
        public static Type Resolve(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return null;
            }

            EnsureCache();

            if (
                _descriptorByName.TryGetValue(
                    assemblyQualifiedName,
                    out SerializableTypeDescriptor descriptor
                )
            )
            {
                return descriptor.Type;
            }

            Type direct = Type.GetType(assemblyQualifiedName, false);
            if (direct != null)
            {
                return direct;
            }

            string fullName = ExtractFullName(assemblyQualifiedName);
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            Type fallback = ResolveByFullName(fullName);
            return fallback;
        }

        /// <summary>
        /// Provides a display name for a type using catalog formatting.
        /// </summary>
        /// <param name="type">Type to format.</param>
        /// <returns>A user-friendly display name.</returns>
        public static string GetDisplayName(Type type)
        {
            if (type == null)
            {
                return NoneDisplayName;
            }

            EnsureCache();

            string normalized = SerializableType.NormalizeTypeName(type);
            if (
                !string.IsNullOrEmpty(normalized)
                && _descriptorByName.TryGetValue(
                    normalized,
                    out SerializableTypeDescriptor descriptor
                )
            )
            {
                return descriptor.DisplayName;
            }

            return FormatDisplayName(type);
        }

        /// <summary>
        /// Gets the assembly qualified names used by the inspector list.
        /// </summary>
        /// <returns>An array of assembly qualified names including the none entry.</returns>
        public static string[] GetAssemblyQualifiedNames()
        {
            EnsureCache();
            return _assemblyQualifiedNames;
        }

        /// <summary>
        /// Retrieves filtered descriptors matching the provided search term.
        /// </summary>
        /// <param name="search">Search term to apply.</param>
        /// <returns>A cached filtered descriptor array.</returns>
        public static IReadOnlyList<SerializableTypeDescriptor> GetFilteredDescriptors(
            string search
        )
        {
            EnsureCache();

            string key = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim();
            if (FilterCache.TryGetValue(key, out SerializableTypeDescriptor[] cached))
            {
                return cached;
            }

            if (string.IsNullOrEmpty(key))
            {
                FilterCache[key] = _descriptors;
                return _descriptors;
            }

            List<SerializableTypeDescriptor> filtered = new();
            for (int index = 0; index < _descriptors.Length; index++)
            {
                SerializableTypeDescriptor descriptor = _descriptors[index];
                if (descriptor.Matches(key))
                {
                    filtered.Add(descriptor);
                }
            }

            SerializableTypeDescriptor[] result = filtered.ToArray();
            FilterCache[key] = result;
            return result;
        }

        private static string FormatDisplayName(Type type)
        {
            if (type == null)
            {
                return NoneDisplayName;
            }

            StringBuilder builder = new();
            AppendTypeName(builder, type);

            Assembly assembly = type.Assembly;
            if (assembly != null)
            {
                AssemblyName assemblyName = assembly.GetName();
                if (assemblyName != null)
                {
                    builder.Append(" (");
                    builder.Append(assemblyName.Name);
                    builder.Append(')');
                }
            }

            return builder.ToString();
        }

        private static void EnsureCache()
        {
            if (_descriptors != null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_descriptors != null)
                {
                    return;
                }

                List<SerializableTypeDescriptor> descriptors = new()
                {
                    new SerializableTypeDescriptor(null, string.Empty, NoneDisplayName),
                };

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
                {
                    Assembly assembly = assemblies[assemblyIndex];
                    if (assembly == null || assembly.IsDynamic)
                    {
                        continue;
                    }

                    Type[] exportedTypes = GetAssemblyTypes(assembly);
                    for (int typeIndex = 0; typeIndex < exportedTypes.Length; typeIndex++)
                    {
                        Type type = exportedTypes[typeIndex];
                        if (type == null)
                        {
                            continue;
                        }

                        string normalized = SerializableType.NormalizeTypeName(type);
                        if (string.IsNullOrEmpty(normalized))
                        {
                            continue;
                        }

                        descriptors.Add(
                            new SerializableTypeDescriptor(
                                type,
                                normalized,
                                FormatDisplayName(type)
                            )
                        );
                    }
                }

                descriptors.Sort(
                    static (left, right) =>
                        string.Compare(
                            left.DisplayName,
                            right.DisplayName,
                            StringComparison.Ordinal
                        )
                );

                _descriptors = descriptors.ToArray();

                _descriptorByName = new Dictionary<string, SerializableTypeDescriptor>(
                    _descriptors.Length,
                    StringComparer.Ordinal
                );

                for (int index = 0; index < _descriptors.Length; index++)
                {
                    SerializableTypeDescriptor descriptor = _descriptors[index];
                    if (
                        !_descriptorByName.ContainsKey(descriptor.AssemblyQualifiedName)
                        && !string.IsNullOrEmpty(descriptor.AssemblyQualifiedName)
                    )
                    {
                        _descriptorByName.Add(descriptor.AssemblyQualifiedName, descriptor);
                    }
                }

                _assemblyQualifiedNames = new string[_descriptors.Length];
                for (int index = 0; index < _descriptors.Length; index++)
                {
                    _assemblyQualifiedNames[index] = _descriptors[index].AssemblyQualifiedName;
                }

                FilterCache[string.Empty] = _descriptors;
            }
        }

        private static Type[] GetAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                if (exception.Types != null)
                {
                    List<Type> result = new();
                    for (int index = 0; index < exception.Types.Length; index++)
                    {
                        Type type = exception.Types[index];
                        if (type != null)
                        {
                            result.Add(type);
                        }
                    }

                    return result.ToArray();
                }

                return Array.Empty<Type>();
            }
        }

        private static Type ResolveByFullName(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Assembly assembly = assemblies[assemblyIndex];
                if (assembly == null || assembly.IsDynamic)
                {
                    continue;
                }

                Type type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string ExtractFullName(string assemblyQualifiedName)
        {
            int commaIndex = assemblyQualifiedName.IndexOf(',');
            if (commaIndex < 0)
            {
                return assemblyQualifiedName;
            }

            return assemblyQualifiedName.Substring(0, commaIndex).Trim();
        }

        private static void AppendTypeName(StringBuilder builder, Type type)
        {
            if (type == null)
            {
                return;
            }

            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                AppendTypeName(builder, elementType);
                builder.Append('[');
                builder.Append(']');
                return;
            }

            if (type.IsGenericType)
            {
                string fullName = type.FullName ?? type.Name;
                if (string.IsNullOrEmpty(fullName))
                {
                    fullName = type.Name;
                }

                int backtickIndex = fullName.IndexOf('`');
                if (backtickIndex >= 0)
                {
                    fullName = fullName.Substring(0, backtickIndex);
                }

                builder.Append(fullName.Replace('+', '.'));
                builder.Append('<');

                Type[] arguments = type.GetGenericArguments();
                for (int index = 0; index < arguments.Length; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(", ");
                    }

                    AppendTypeName(builder, arguments[index]);
                }

                builder.Append('>');
                return;
            }

            if (type.IsGenericParameter)
            {
                builder.Append(type.Name);
                return;
            }

            string name = type.FullName ?? type.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
            }

            builder.Append(name.Replace('+', '.'));
        }

        /// <summary>
        /// Descriptor for inspector presentation.
        /// </summary>
        public readonly struct SerializableTypeDescriptor
        {
            public SerializableTypeDescriptor(
                Type type,
                string assemblyQualifiedName,
                string displayName
            )
            {
                Type = type;
                AssemblyQualifiedName = assemblyQualifiedName ?? string.Empty;
                DisplayName = displayName ?? string.Empty;
            }

            public Type Type { get; }

            public string AssemblyQualifiedName { get; }

            public string DisplayName { get; }

            internal bool Matches(string search)
            {
                if (string.IsNullOrEmpty(search))
                {
                    return true;
                }

                if (
                    DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || AssemblyQualifiedName.IndexOf(search, StringComparison.OrdinalIgnoreCase)
                        >= 0
                )
                {
                    return true;
                }

                return false;
            }
        }
    }

    internal sealed class SerializableTypeJsonConverter : JsonConverter<SerializableType>
    {
        public override SerializableType Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("SerializableType expects a JSON string.");
            }

            string serialized = reader.GetString();
            return SerializableType.FromSerializedName(serialized);
        }

        public override void Write(
            Utf8JsonWriter writer,
            SerializableType value,
            JsonSerializerOptions options
        )
        {
            if (value.IsEmpty)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.AssemblyQualifiedName);
        }
    }
}
