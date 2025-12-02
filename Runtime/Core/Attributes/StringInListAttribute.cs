namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Inspector attribute that constrains a string field to a set of allowed values.
    /// Use when a serialized string should only contain known tokens (scene names, animation states, localization keys, etc.).
    /// </summary>
    /// <remarks>
    /// Supports fixed lists, static providers, or instance providers resolved on the decorated component/asset.
    /// The associated PropertyDrawer can render a dropdown with search, pagination, and autocomplete.
    /// </remarks>
    /// <example>
    /// Inline list:
    /// <code>
    /// [StringInList("Easy", "Normal", "Hard")]
    /// public string difficulty;
    /// </code>
    /// Static provider:
    /// <code>
    /// [StringInList(typeof(DialogKeys), nameof(DialogKeys.GetAllKeys))]
    /// public string dialogKey;
    /// </code>
    /// Instance provider:
    /// <code>
    /// [StringInList(nameof(BuildStateList))]
    /// public string currentState;
    ///
    /// private IEnumerable&lt;string&gt; BuildStateList() => cachedStates;
    /// </code>
    /// </example>
    public sealed class StringInListAttribute : PropertyAttribute
    {
        private const string AttributeName = "StringInListAttribute";
        private static readonly string[] Empty = Array.Empty<string>();

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

        private readonly Func<object, string[]> _getStringList;
        private readonly bool _requiresInstanceContext;
        private readonly string _instanceMethodName;
        private readonly Dictionary<Type, InstanceProviderEntry> _instanceMethodCache;

        internal Type ProviderType { get; }

        internal string ProviderMethodName { get; }

        /// <summary>
        /// Uses a fixed list of allowed strings.
        /// Ideal for short, stable option sets baked into the class or created through constants.
        /// </summary>
        /// <param name="list">List of legal string values. The array is captured by reference.</param>
        public StringInListAttribute(params string[] list)
        {
            string[] captured = list ?? Array.Empty<string>();
            _getStringList = _ => captured;
            ProviderType = null;
            ProviderMethodName = null;
        }

        /// <summary>
        /// Uses a static method on a type to obtain the allowed strings.
        /// </summary>
        /// <remarks>
        /// The provider method must be parameterless, static, and return either <see cref="string"/>[] or <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="string"/>.
        /// This is useful for binding existing data sources (project settings, addressables keys, etc.) without duplicating lists.
        /// </remarks>
        /// <param name="type">Type that defines the static method that supplies the values.</param>
        /// <param name="methodName">Static method name returning string[].</param>
        public StringInListAttribute(Type type, string methodName)
        {
            Func<string[]> provider = DropdownValueProvider<string>.FromMethod(
                type,
                methodName,
                AttributeName
            );
            _getStringList = _ => provider();
            ProviderType = type;
            ProviderMethodName = methodName;
        }

        /// <summary>
        /// Uses a method on the decorated object's type to obtain the allowed strings.
        /// The method can be instance or static, must be parameterless, and return either string[] or IEnumerable&lt;string&gt;.
        /// </summary>
        /// <param name="methodName">Method name declared on the target object's type.</param>
        public StringInListAttribute(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                Debug.LogError($"{AttributeName}: Method name cannot be null or empty.");
                _getStringList = _ => Empty;
                return;
            }

            _requiresInstanceContext = true;
            _instanceMethodName = methodName;
            _instanceMethodCache = new Dictionary<Type, InstanceProviderEntry>();
            _getStringList = ResolveInstanceMethodValues;
            ProviderType = null;
            ProviderMethodName = methodName;
        }

        /// <summary>
        /// Retrieves the allowed string options for the decorated field without any context.
        /// Note: when the attribute targets an instance method, this returns an empty array.
        /// </summary>
        public string[] List => _getStringList?.Invoke(null) ?? Empty;

        /// <summary>
        /// Retrieves the allowed string options for the supplied context object.
        /// </summary>
        /// <param name="context">The object declaring the field/property. Required for instance providers.</param>
        /// <returns>Resolved option list (never null).</returns>
        public string[] GetOptions(object context)
        {
            if (_requiresInstanceContext && context == null)
            {
                return Empty;
            }

            return _getStringList?.Invoke(context) ?? Empty;
        }

        private string[] ResolveInstanceMethodValues(object context)
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
            if (
                returnType != typeof(string[])
                && !typeof(IEnumerable<string>).IsAssignableFrom(returnType)
            )
            {
                Debug.LogError(
                    $"{AttributeName}: Method '{providerType.FullName}.{method.Name}' must return string[] or IEnumerable<string>."
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

        private static string[] ConvertResult(object result, Type providerType, string methodName)
        {
            if (result == null)
            {
                return Empty;
            }

            if (result is string[] array)
            {
                return array;
            }

            if (result is IEnumerable<string> enumerable)
            {
                using PooledResource<List<string>> valuesLease = Buffers<string>.List.Get(
                    out List<string> values
                );
                {
                    foreach (string entry in enumerable)
                    {
                        values.Add(entry);
                    }

                    if (values.Count == 0)
                    {
                        return Empty;
                    }

                    return values.ToArray();
                }
            }

            Debug.LogError(
                $"{AttributeName}: Method '{providerType.FullName}.{methodName}' returned incompatible type '{result.GetType().FullName}'. Expected string[] or IEnumerable<string>."
            );
            return Empty;
        }
    }
}
