namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Draws a serialized property as a dropdown populated from fixed values or a static method provider.
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
    /// Provider-based values:
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
    /// </example>
    public sealed class WValueDropDownAttribute : PropertyAttribute
    {
        private const string AttributeName = "WValueDropDownAttribute";
        private static readonly Func<object[]> EmptyFactory = () => Array.Empty<object>();
        private readonly Func<object[]> _getOptions;

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params bool[] options)
            : this(typeof(bool), Wrap(DropdownValueProvider<bool>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params char[] options)
            : this(typeof(char), Wrap(DropdownValueProvider<char>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params string[] options)
            : this(typeof(string), Wrap(DropdownValueProvider<string>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params sbyte[] options)
            : this(typeof(sbyte), Wrap(DropdownValueProvider<sbyte>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params byte[] options)
            : this(typeof(byte), Wrap(DropdownValueProvider<byte>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params short[] options)
            : this(typeof(short), Wrap(DropdownValueProvider<short>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params ushort[] options)
            : this(typeof(ushort), Wrap(DropdownValueProvider<ushort>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params int[] options)
            : this(typeof(int), Wrap(DropdownValueProvider<int>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params uint[] options)
            : this(typeof(uint), Wrap(DropdownValueProvider<uint>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params long[] options)
            : this(typeof(long), Wrap(DropdownValueProvider<long>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params ulong[] options)
            : this(typeof(ulong), Wrap(DropdownValueProvider<ulong>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params float[] options)
            : this(typeof(float), Wrap(DropdownValueProvider<float>.FromList(options))) { }

        /// <inheritdoc cref="WValueDropDownAttribute(Type, object[])" />
        public WValueDropDownAttribute(params double[] options)
            : this(typeof(double), Wrap(DropdownValueProvider<double>.FromList(options))) { }

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
            : this(
                ResolveProviderFactory(providerType, methodName, out Type resolvedValueType),
                resolvedValueType
            ) { }

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
                DropdownValueProvider.FromList(valueType, options, AttributeName)
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
            : this(
                valueType ?? typeof(object),
                DropdownValueProvider.FromMethod(providerType, methodName, valueType, AttributeName)
            ) { }

        private WValueDropDownAttribute(Type valueType, Func<object[]> optionFactory)
        {
            ValueType = valueType ?? typeof(object);
            _getOptions = optionFactory ?? EmptyFactory;
        }

        private WValueDropDownAttribute(Func<object[]> optionFactory, Type valueType)
            : this(valueType ?? typeof(object), optionFactory) { }

        /// <summary>
        /// Gets the effective type for the dropdown values.
        /// When constructors infer provider output, this is the element type returned by the provider.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Retrieves the dropdown entries as boxed objects.
        /// The returned array should be treated as read-only.
        /// </summary>
        public object[] Options => _getOptions();

        private static Func<object[]> Wrap<T>(Func<T[]> provider)
        {
            if (provider == null)
            {
                return EmptyFactory;
            }

            return () =>
            {
                T[] typedValues = provider();
                if (typedValues == null || typedValues.Length == 0)
                {
                    return Array.Empty<object>();
                }

                object[] boxedValues = new object[typedValues.Length];
                for (int index = 0; index < typedValues.Length; index += 1)
                {
                    boxedValues[index] = typedValues[index];
                }

                return boxedValues;
            };
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
