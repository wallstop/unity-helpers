namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Draws a serialized property as a dropdown populated from fixed values, a static method provider, or an instance method provider.
    /// Supports inline lists, strongly typed primitive overloads, and late-bound providers that return custom structs or reference types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this attribute when a field should only be assigned values from a curated set (difficulty levels, asset references, data-driven enums, etc.).
    /// Inline lists are ideal for short constants, while provider overloads let you mirror external collections without duplicating state.
    /// </para>
    /// </remarks>
    /// <example>
    /// Inline values:
    /// <code>
    /// [WValueDropDown(0, 25, 50, 100)]
    /// public int staminaThreshold;
    /// </code>
    /// Typed inline overload:
    /// <code>
    /// [WValueDropDown(true, false)]
    /// public bool isEnabled;
    /// </code>
    /// Static provider-based values:
    /// <code>
    /// [WValueDropDown(typeof(PowerUpCatalogue), nameof(PowerUpCatalogue.GetAvailablePowerUps))]
    /// public PowerUpDefinition selectedPowerUp;
    ///
    /// private static class PowerUpCatalogue
    /// {
    ///     internal static IEnumerable&lt;PowerUpDefinition&gt; GetAvailablePowerUps()
    ///     {
    ///         return Resources.LoadAll&lt;PowerUpDefinition&gt;(\"PowerUps\");
    ///     }
    /// }
    /// </code>
    /// Instance provider:
    /// <code>
    /// [WValueDropDown(nameof(GetAvailableOptions), typeof(int))]
    /// public int selectedOption;
    ///
    /// private IEnumerable&lt;int&gt; GetAvailableOptions()
    /// {
    ///     return new[] { 1, 2, 3, 4, 5 };
    /// }
    /// </code>
    /// </example>
    public sealed class WValueDropDownAttribute : PropertyAttribute
    {
        private const string AttributeName = "WValueDropDownAttribute";
        private static readonly object[] Empty = Array.Empty<object>();
        private static readonly Func<object, object[]> EmptyFactory = _ => Empty;

        private sealed class InstanceProviderEntry
        {
            internal readonly MethodInfo Method;
            internal readonly bool IsStatic;
            internal readonly Func<object, object[], object> InstanceInvoker;
            internal readonly Func<object[], object> StaticInvoker;

            internal InstanceProviderEntry(
                MethodInfo method,
                Func<object, object[], object> instanceInvoker,
                Func<object[], object> staticInvoker
            )
            {
                Method = method;
                IsStatic = method.IsStatic;
                InstanceInvoker = instanceInvoker;
                StaticInvoker = staticInvoker;
            }

            internal object Invoke(object context)
            {
                return IsStatic
                    ? StaticInvoker?.Invoke(Array.Empty<object>())
                    : InstanceInvoker?.Invoke(context, Array.Empty<object>());
            }
        }

        private readonly Func<object, object[]> _getOptions;
        private readonly bool _requiresInstanceContext;
        private readonly string _instanceMethodName;
        private readonly Dictionary<Type, InstanceProviderEntry> _instanceMethodCache;

        internal Type ProviderType { get; }

        internal string ProviderMethodName { get; }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params bool[] options)
            : this(typeof(bool), WrapStatic(DropdownValueProvider<bool>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params char[] options)
            : this(typeof(char), WrapStatic(DropdownValueProvider<char>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params string[] options)
            : this(typeof(string), WrapStatic(DropdownValueProvider<string>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params sbyte[] options)
            : this(typeof(sbyte), WrapStatic(DropdownValueProvider<sbyte>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params byte[] options)
            : this(typeof(byte), WrapStatic(DropdownValueProvider<byte>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params short[] options)
            : this(typeof(short), WrapStatic(DropdownValueProvider<short>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params ushort[] options)
            : this(typeof(ushort), WrapStatic(DropdownValueProvider<ushort>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params int[] options)
            : this(typeof(int), WrapStatic(DropdownValueProvider<int>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params uint[] options)
            : this(typeof(uint), WrapStatic(DropdownValueProvider<uint>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params long[] options)
            : this(typeof(long), WrapStatic(DropdownValueProvider<long>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params ulong[] options)
            : this(typeof(ulong), WrapStatic(DropdownValueProvider<ulong>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params float[] options)
            : this(typeof(float), WrapStatic(DropdownValueProvider<float>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params double[] options)
            : this(typeof(double), WrapStatic(DropdownValueProvider<double>.FromList(options))) { }

        /// <summary>
        /// Initializes the attribute using a static provider method and infers the option type from its return value.
        /// </summary>
        /// <remarks>
        /// The provider must be parameterless, static, and return an array or <see cref="System.Collections.Generic.IEnumerable{T}"/>.
        /// The inspector queries the provider each time it renders the field, keeping the dropdown synchronised with external data.
        /// </remarks>
        /// <param name="providerType">Type that defines the static provider.</param>
        /// <param name="methodName">Name of the parameterless static method that supplies the dropdown values.</param>
        public WValueDropDownAttribute(Type providerType, string methodName)
        {
            Func<object[]> optionFactory = ResolveProviderFactory(
                providerType,
                methodName,
                out Type resolvedValueType
            );
            ValueType = resolvedValueType ?? typeof(object);
            _getOptions = WrapStaticFactory(optionFactory);
            ProviderType = providerType;
            ProviderMethodName = methodName;
        }

        /// <summary>
        /// Initializes the attribute with an inline list of values.
        /// </summary>
        /// <remarks>
        /// Use this overload for custom types or when you already have an object array. Values are coerced to <paramref name="valueType"/>.
        /// </remarks>
        /// <param name="valueType">Target value type for the decorated property.</param>
        /// <param name="options">One or more selectable values compatible with <paramref name="valueType"/>.</param>
        public WValueDropDownAttribute(Type valueType, params object[] options)
            : this(
                valueType ?? typeof(object),
                WrapStaticFactory(DropdownValueProvider.FromList(valueType, options, AttributeName))
            ) { }

        /// <summary>
        /// Initializes the attribute using a method provider with explicit output type conversion.
        /// </summary>
        /// <remarks>
        /// This overload is useful when the provider returns a type that needs to be converted before appearing in the dropdown (for example, numeric IDs mapped to enums).
        /// </remarks>
        /// <param name="providerType">Type containing the static provider method.</param>
        /// <param name="methodName">Parameterless static method returning an array or enumerable of values.</param>
        /// <param name="valueType">Target value type for the decorated property.</param>
        public WValueDropDownAttribute(Type providerType, string methodName, Type valueType)
        {
            ValueType = valueType ?? typeof(object);
            _getOptions = WrapStaticFactory(
                DropdownValueProvider.FromMethod(providerType, methodName, valueType, AttributeName)
            );
            ProviderType = providerType;
            ProviderMethodName = methodName;
        }

        /// <summary>
        /// Uses a method on the decorated object's type to obtain the allowed values.
        /// The method can be instance or static, must be parameterless, and return an array or IEnumerable.
        /// </summary>
        /// <param name="methodName">Method name declared on the target object's type.</param>
        /// <param name="valueType">Target value type for the decorated property.</param>
        public WValueDropDownAttribute(string methodName, Type valueType)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                Debug.LogError($"{AttributeName}: Method name cannot be null or empty.");
                ValueType = valueType ?? typeof(object);
                _getOptions = EmptyFactory;
                return;
            }

            _requiresInstanceContext = true;
            _instanceMethodName = methodName;
            _instanceMethodCache = new Dictionary<Type, InstanceProviderEntry>();
            _getOptions = ResolveInstanceMethodValues;
            ValueType = valueType ?? typeof(object);
            ProviderType = null;
            ProviderMethodName = methodName;
        }

        private WValueDropDownAttribute(Type valueType, Func<object, object[]> optionFactory)
        {
            ValueType = valueType ?? typeof(object);
            _getOptions = optionFactory ?? EmptyFactory;
        }

        /// <summary>
        /// Gets the effective type for the dropdown values.
        /// When constructors infer provider output, this is the element type returned by the provider.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Retrieves the dropdown entries as boxed objects without any context.
        /// Note: when the attribute targets an instance method, this returns an empty array.
        /// The returned array should be treated as read-only.
        /// </summary>
        public object[] Options => _getOptions?.Invoke(null) ?? Empty;

        /// <summary>
        /// Retrieves the dropdown entries for the supplied context object.
        /// </summary>
        /// <param name="context">The object declaring the field/property. Required for instance providers.</param>
        /// <returns>Resolved option list (never null).</returns>
        public object[] GetOptions(object context)
        {
            if (_requiresInstanceContext && context == null)
            {
                return Empty;
            }

            return _getOptions?.Invoke(context) ?? Empty;
        }

        /// <summary>
        /// Indicates whether this attribute uses an instance method provider.
        /// </summary>
        internal bool RequiresInstanceContext => _requiresInstanceContext;

        private object[] ResolveInstanceMethodValues(object context)
        {
            if (context == null)
            {
                return Empty;
            }

            Type providerType = context.GetType();
            InstanceProviderEntry provider = GetOrResolveInstanceProvider(providerType);
            if (provider == null)
            {
                return Empty;
            }

            object result;
            try
            {
                result = provider.Invoke(context);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{AttributeName}: Invocation of '{providerType.FullName}.{provider.Method.Name}' threw {exception.GetType().Name}."
                );
                return Empty;
            }

            return ConvertResult(
                result,
                provider.Method.DeclaringType ?? providerType,
                provider.Method.Name
            );
        }

        private InstanceProviderEntry GetOrResolveInstanceProvider(Type providerType)
        {
            if (
                _instanceMethodCache != null
                && _instanceMethodCache.TryGetValue(providerType, out InstanceProviderEntry cached)
            )
            {
                return cached;
            }

            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic;
            MethodInfo method = providerType.GetMethod(
                _instanceMethodName,
                flags,
                null,
                Type.EmptyTypes,
                null
            );

            if (method == null)
            {
                Debug.LogError(
                    $"{AttributeName}: Could not locate '{_instanceMethodName}' on {providerType.FullName}."
                );
                CacheProvider(providerType, null);
                return null;
            }

            Type returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                Debug.LogError(
                    $"{AttributeName}: Method '{providerType.FullName}.{method.Name}' must return an array or IEnumerable."
                );
                CacheProvider(providerType, null);
                return null;
            }

            bool isEnumerable =
                returnType.IsArray
                || (
                    returnType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(returnType)
                );

            if (!isEnumerable)
            {
                Debug.LogError(
                    $"{AttributeName}: Method '{providerType.FullName}.{method.Name}' must return an array or IEnumerable."
                );
                CacheProvider(providerType, null);
                return null;
            }

            InstanceProviderEntry entry = method.IsStatic
                ? new InstanceProviderEntry(
                    method,
                    instanceInvoker: null,
                    staticInvoker: ReflectionHelpers.GetStaticMethodInvoker(method)
                )
                : new InstanceProviderEntry(
                    method,
                    ReflectionHelpers.GetMethodInvoker(method),
                    staticInvoker: null
                );

            CacheProvider(providerType, entry);
            return entry;
        }

        private void CacheProvider(Type providerType, InstanceProviderEntry entry)
        {
            if (_instanceMethodCache == null)
            {
                return;
            }

            _instanceMethodCache[providerType] = entry;
        }

        private object[] ConvertResult(object result, Type providerType, string methodName)
        {
            if (result == null)
            {
                return Empty;
            }

            if (result is object[] objectArray)
            {
                return objectArray;
            }

            if (result is Array array)
            {
                object[] boxed = new object[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    boxed[i] = array.GetValue(i);
                }
                return boxed;
            }

            if (result is IEnumerable enumerable)
            {
                List<object> values = new();
                foreach (object entry in enumerable)
                {
                    values.Add(entry);
                }

                if (values.Count == 0)
                {
                    return Empty;
                }

                return values.ToArray();
            }

            Debug.LogError(
                $"{AttributeName}: Method '{providerType.FullName}.{methodName}' returned incompatible type '{result.GetType().FullName}'. Expected an array or IEnumerable."
            );
            return Empty;
        }

        private static Func<object, object[]> WrapStatic<T>(Func<T[]> provider)
        {
            if (provider == null)
            {
                return EmptyFactory;
            }

            T[] cachedTypedValues = null;
            object[] cachedBoxedValues = null;

            return _ =>
            {
                T[] typedValues = provider();
                if (typedValues == null || typedValues.Length == 0)
                {
                    return Empty;
                }

                if (ReferenceEquals(typedValues, cachedTypedValues) && cachedBoxedValues != null)
                {
                    return cachedBoxedValues;
                }

                object[] boxedValues = new object[typedValues.Length];
                for (int index = 0; index < typedValues.Length; index += 1)
                {
                    boxedValues[index] = typedValues[index];
                }

                cachedTypedValues = typedValues;
                cachedBoxedValues = boxedValues;

                return boxedValues;
            };
        }

        private static Func<object, object[]> WrapStaticFactory(Func<object[]> provider)
        {
            if (provider == null)
            {
                return EmptyFactory;
            }

            return _ => provider();
        }

        private static Func<object[]> ResolveProviderFactory(
            Type providerType,
            string methodName,
            out Type resolvedValueType
        )
        {
            return DropdownValueProvider.FromMethod(
                providerType,
                methodName,
                AttributeName,
                out resolvedValueType
            );
        }
    }
}
