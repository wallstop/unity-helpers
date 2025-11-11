namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Draws a serialized property as a dropdown populated from fixed values or a method provider.
    /// Provides strongly-typed constructors for common primitives, enums, and numeric types.
    /// </summary>
    public sealed class ValueDropdownAttribute : PropertyAttribute
    {
        private const string AttributeName = "ValueDropdownAttribute";
        private static readonly Func<object[]> EmptyFactory = () => Array.Empty<object>();
        private readonly Func<object[]> _getOptions;

        public ValueDropdownAttribute(params bool[] options)
            : this(typeof(bool), Wrap(DropdownValueProvider<bool>.FromList(options))) { }

        public ValueDropdownAttribute(params char[] options)
            : this(typeof(char), Wrap(DropdownValueProvider<char>.FromList(options))) { }

        public ValueDropdownAttribute(params string[] options)
            : this(typeof(string), Wrap(DropdownValueProvider<string>.FromList(options))) { }

        public ValueDropdownAttribute(params sbyte[] options)
            : this(typeof(sbyte), Wrap(DropdownValueProvider<sbyte>.FromList(options))) { }

        public ValueDropdownAttribute(params byte[] options)
            : this(typeof(byte), Wrap(DropdownValueProvider<byte>.FromList(options))) { }

        public ValueDropdownAttribute(params short[] options)
            : this(typeof(short), Wrap(DropdownValueProvider<short>.FromList(options))) { }

        public ValueDropdownAttribute(params ushort[] options)
            : this(typeof(ushort), Wrap(DropdownValueProvider<ushort>.FromList(options))) { }

        public ValueDropdownAttribute(params int[] options)
            : this(typeof(int), Wrap(DropdownValueProvider<int>.FromList(options))) { }

        public ValueDropdownAttribute(params uint[] options)
            : this(typeof(uint), Wrap(DropdownValueProvider<uint>.FromList(options))) { }

        public ValueDropdownAttribute(params long[] options)
            : this(typeof(long), Wrap(DropdownValueProvider<long>.FromList(options))) { }

        public ValueDropdownAttribute(params ulong[] options)
            : this(typeof(ulong), Wrap(DropdownValueProvider<ulong>.FromList(options))) { }

        public ValueDropdownAttribute(params float[] options)
            : this(typeof(float), Wrap(DropdownValueProvider<float>.FromList(options))) { }

        public ValueDropdownAttribute(params double[] options)
            : this(typeof(double), Wrap(DropdownValueProvider<double>.FromList(options))) { }

        public ValueDropdownAttribute(Type providerType, string methodName)
            : this(
                ResolveProviderFactory(providerType, methodName, out Type resolvedValueType),
                resolvedValueType
            ) { }

        /// <summary>
        /// Initializes the attribute with an inline list of values.
        /// </summary>
        /// <param name="valueType">Target value type for the decorated property.</param>
        /// <param name="options">One or more selectable values compatible with <paramref name="valueType"/>.</param>
        public ValueDropdownAttribute(Type valueType, params object[] options)
            : this(
                valueType ?? typeof(object),
                DropdownValueProvider.FromList(valueType, options, AttributeName)
            ) { }

        /// <summary>
        /// Initializes the attribute using a method provider.
        /// </summary>
        /// <param name="providerType">Type containing the static provider method.</param>
        /// <param name="methodName">Parameterless static method returning an array or IEnumerable of compatible values.</param>
        /// <param name="valueType">Target value type for the decorated property.</param>
        public ValueDropdownAttribute(Type providerType, string methodName, Type valueType)
            : this(
                valueType ?? typeof(object),
                DropdownValueProvider.FromMethod(providerType, methodName, valueType, AttributeName)
            ) { }

        private ValueDropdownAttribute(Type valueType, Func<object[]> optionFactory)
        {
            ValueType = valueType ?? typeof(object);
            _getOptions = optionFactory ?? EmptyFactory;
        }

        private ValueDropdownAttribute(Func<object[]> optionFactory, Type valueType)
            : this(valueType ?? typeof(object), optionFactory) { }

        /// <summary>
        /// Gets the target value type represented by the dropdown options.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Retrieves the dropdown options.
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
