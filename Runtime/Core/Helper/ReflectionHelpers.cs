#if !((UNITY_WEBGL && !UNITY_EDITOR) || ENABLE_IL2CPP)
#define EMIT_DYNAMIC_IL
#define SUPPORT_EXPRESSION_COMPILE
#endif

namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
#if UNITY_EDITOR
    using UnityEditor;
#endif
#if EMIT_DYNAMIC_IL
    using System.Reflection.Emit;
#endif

#if !SINGLE_THREADED
    using System.Collections.Concurrent;
#else
    using Extension;
#endif

    public delegate void FieldSetter<TInstance, in TValue>(ref TInstance instance, TValue value);

    /// <summary>
    /// High-performance reflection helpers for field/property access, method/constructor invocation,
    /// dynamic collection creation, and type/attribute scanning with caching and optional IL emission.
    /// </summary>
    /// <remarks>
    /// - Uses expression compilation or dynamic IL where supported; falls back to reflection otherwise.
    /// - Caches generated delegates to avoid per-call reflection overhead.
    /// - Designed for hot paths (serialization, UI binding, ECS-style systems).
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// // Get and set a field without repeated reflection costs
    /// public sealed class Player { public int Score; }
    /// var field = typeof(Player).GetField("Score");
    /// var getter = ReflectionHelpers.GetFieldGetter(field);   // object -> object
    /// var setter = ReflectionHelpers.GetFieldSetter(field);   // (object, object) -> void
    /// var p = new Player();
    /// setter(p, 42);
    /// UnityEngine.Debug.Log(getter(p)); // 42
    /// ]]></code>
    /// </example>
    public static partial class ReflectionHelpers
    {
        // Cache for type resolution by name
#if !SINGLE_THREADED
        private static readonly ConcurrentDictionary<string, Type> TypeResolutionCache = new(
            StringComparer.Ordinal
        );
#else
        private static readonly Dictionary<string, Type> TypeResolutionCache = new(
            StringComparer.Ordinal
        );
#endif
#if SINGLE_THREADED
        private static readonly Dictionary<Type, Func<int, Array>> ArrayCreators = new();
        private static readonly Dictionary<Type, Func<IList>> ListCreators = new();
        private static readonly Dictionary<Type, Func<int, IList>> ListWithCapacityCreators = new();
        private static readonly Dictionary<Type, Func<int, object>> HashSetWithCapacityCreators =
            new();
        private static readonly Dictionary<Type, Action<object>> HashSetClearers = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<int, Array> GetArrayCreatorCached(Type type)
        {
            if (!ArrayCreators.TryGetValue(type, out Func<int, Array> factory))
            {
                factory = GetArrayCreator(type);
                ArrayCreators[type] = factory;
            }

            return factory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<int, IList> GetListWithCapacityCreatorCached(Type elementType)
        {
            if (!ListWithCapacityCreators.TryGetValue(elementType, out Func<int, IList> factory))
            {
                factory = GetListWithCapacityCreator(elementType);
                ListWithCapacityCreators[elementType] = factory;
            }

            return factory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<IList> GetListCreatorCached(Type elementType)
        {
            if (!ListCreators.TryGetValue(elementType, out Func<IList> factory))
            {
                factory = GetListCreator(elementType);
                ListCreators[elementType] = factory;
            }

            return factory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<int, object> GetHashSetWithCapacityCreatorCached(Type elementType)
        {
            if (
                !HashSetWithCapacityCreators.TryGetValue(elementType, out Func<int, object> factory)
            )
            {
                factory = GetHashSetWithCapacityCreator(elementType);
                HashSetWithCapacityCreators[elementType] = factory;
            }

            return factory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Action<object> GetHashSetClearerCached(Type elementType)
        {
            if (!HashSetClearers.TryGetValue(elementType, out Action<object> clearer))
            {
                clearer = GetHashSetClearer(elementType);
                HashSetClearers[elementType] = clearer;
            }

            return clearer;
        }
#else
        private static readonly ConcurrentDictionary<Type, Func<int, Array>> ArrayCreators = new();
        private static readonly ConcurrentDictionary<Type, Func<IList>> ListCreators = new();
        private static readonly ConcurrentDictionary<
            Type,
            Func<int, IList>
        > ListWithCapacityCreators = new();
        private static readonly ConcurrentDictionary<
            Type,
            Func<int, object>
        > HashSetWithCapacityCreators = new();
        private static readonly ConcurrentDictionary<Type, Action<object>> HashSetClearers = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<int, Array> GetArrayCreatorCached(Type type)
        {
            return ArrayCreators.GetOrAdd(type, static elementType => GetArrayCreator(elementType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<int, IList> GetListWithCapacityCreatorCached(Type elementType)
        {
            return ListWithCapacityCreators.GetOrAdd(
                elementType,
                static type => GetListWithCapacityCreator(type)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<IList> GetListCreatorCached(Type elementType)
        {
            return ListCreators.GetOrAdd(elementType, static type => GetListCreator(type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<int, object> GetHashSetWithCapacityCreatorCached(Type elementType)
        {
            return HashSetWithCapacityCreators.GetOrAdd(
                elementType,
                static type => GetHashSetWithCapacityCreator(type)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Action<object> GetHashSetClearerCached(Type elementType)
        {
            return HashSetClearers.GetOrAdd(elementType, static type => GetHashSetClearer(type));
        }
#endif

        private static readonly bool CanCompileExpressions = CheckExpressionCompilationSupport();
        private static readonly bool DynamicIlSupported = CheckDynamicIlSupport();
        private static bool? _ExpressionCapabilityOverride;
        private static bool? _DynamicIlCapabilityOverride;

        internal static bool ExpressionsEnabled =>
            _ExpressionCapabilityOverride ?? CanCompileExpressions;

        internal static bool DynamicIlEnabled => _DynamicIlCapabilityOverride ?? DynamicIlSupported;

        internal static IDisposable OverrideReflectionCapabilities(
            bool? expressions,
            bool? dynamicIl
        )
        {
            bool? previousExpressions = _ExpressionCapabilityOverride;
            bool? previousDynamicIl = _DynamicIlCapabilityOverride;
            _ExpressionCapabilityOverride = expressions;
            _DynamicIlCapabilityOverride = dynamicIl;
            return new CapabilityOverrideScope(previousExpressions, previousDynamicIl);
        }

        private sealed class CapabilityOverrideScope : IDisposable
        {
            private readonly bool? _previousExpressions;
            private readonly bool? _previousDynamicIl;
            private bool _disposed;

            internal CapabilityOverrideScope(bool? expressions, bool? dynamicIl)
            {
                _previousExpressions = expressions;
                _previousDynamicIl = dynamicIl;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _ExpressionCapabilityOverride = _previousExpressions;
                _DynamicIlCapabilityOverride = _previousDynamicIl;
                _disposed = true;
            }
        }

#if SINGLE_THREADED
        private static readonly Dictionary<
            (Type type, string name, BindingFlags flags),
            FieldInfo
        > FieldLookup = new();
        private static readonly Dictionary<
            (Type type, string name, BindingFlags flags),
            PropertyInfo
        > PropertyLookup = new();
        private static readonly Dictionary<
            (Type type, string sig, BindingFlags flags),
            MethodInfo
        > MethodLookup = new();
#else
        private static readonly ConcurrentDictionary<
            (Type type, string name, BindingFlags flags),
            FieldInfo
        > FieldLookup = new();
        private static readonly ConcurrentDictionary<
            (Type type, string name, BindingFlags flags),
            PropertyInfo
        > PropertyLookup = new();
        private static readonly ConcurrentDictionary<
            (Type type, string sig, BindingFlags flags),
            MethodInfo
        > MethodLookup = new();
#endif

        /// <summary>
        /// Tries to get an attribute of type <typeparamref name="T"/> and indicates whether it is present.
        /// Safe: returns false on any reflection errors.
        /// </summary>
        /// <param name="provider">Any member, type, or assembly supporting attributes.</param>
        /// <param name="attribute">Output attribute instance if found, otherwise default.</param>
        /// <param name="inherit">Whether to search base types.</param>
        /// <returns>True if attribute exists; otherwise false.</returns>
        /// <example>
        /// <code><![CDATA[
        /// if (typeof(MyComponent).IsAttributeDefined(out ObsoleteAttribute attr))
        /// {
        ///     UnityEngine.Debug.Log($"Marked obsolete: {attr.Message}");
        /// }
        /// ]]></code>
        /// </example>
        public static bool IsAttributeDefined<T>(
            this ICustomAttributeProvider provider,
            out T attribute,
            bool inherit = true
        )
            where T : Attribute
        {
            try
            {
                Type type = typeof(T);
                if (provider.IsDefined(type, inherit))
                {
                    object[] attributes = provider.GetCustomAttributes(type, inherit);
                    if (attributes.Length == 0)
                    {
                        attribute = default;
                        return false;
                    }
                    attribute = Unsafe.As<T>(attributes[0]);
                    return attribute != null;
                }
            }
            catch
            {
                // Swallow
            }
            attribute = default;
            return false;
        }

        /// <summary>
        /// Loads all public static properties whose property type is exactly <typeparamref name="T"/>,
        /// keyed by property name (case-insensitive).
        /// </summary>
        /// <remarks>
        /// Use when enumerating well-known static instances or singletons of a type.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// // Finds all: public static T SomeProperty { get; }
        /// var props = ReflectionHelpers.LoadStaticPropertiesForType<MyType>();
        /// foreach (var kvp in props) UnityEngine.Debug.Log($"{kvp.Key} -> {kvp.Value}");
        /// ]]></code>
        /// </example>
        public static Dictionary<string, PropertyInfo> LoadStaticPropertiesForType<T>()
        {
            Type type = typeof(T);
            return type.GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(property => property.PropertyType == type)
                .ToDictionary(
                    property => property.Name,
                    property => property,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        /// <summary>
        /// Loads all public static fields whose field type is exactly <typeparamref name="T"/>,
        /// keyed by field name (case-insensitive).
        /// </summary>
        /// <remarks>
        /// Use when mapping constant instances or static registries.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// var fields = ReflectionHelpers.LoadStaticFieldsForType<MyType>();
        /// // Access FieldInfo directly to get or set values
        /// ]]></code>
        /// </example>
        public static Dictionary<string, FieldInfo> LoadStaticFieldsForType<T>()
        {
            Type type = typeof(T);
            return type.GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(field => field.FieldType == type)
                .ToDictionary(
                    field => field.Name,
                    field => field,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        /// <summary>
        /// Creates a new array instance of element <paramref name="type"/> with the specified length.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// Array ints = ReflectionHelpers.CreateArray(typeof(int), 16); // int[16]
        /// ]]></code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array CreateArray(Type type, int length)
        {
            return GetArrayCreatorCached(type).Invoke(length);
        }

        /// <summary>
        /// Creates a new <see cref="List{T}"/> instance for <paramref name="elementType"/> with the specified capacity.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// IList list = ReflectionHelpers.CreateList(typeof(string), 128); // List<string> with Capacity=128
        /// ]]></code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList CreateList(Type elementType, int length)
        {
            return GetListWithCapacityCreatorCached(elementType).Invoke(length);
        }

        /// <summary>
        /// Creates a new <see cref="List{T}"/> instance for <paramref name="elementType"/>.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// IList list = ReflectionHelpers.CreateList(typeof(UnityEngine.Vector3));
        /// ]]></code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList CreateList(Type elementType)
        {
            return GetListCreatorCached(elementType).Invoke();
        }

        // Test helpers to avoid reflection in tests when asserting cache state
        internal static bool IsFieldGetterCached(FieldInfo field)
        {
            return DelegateFactory.IsFieldGetterCached(field);
        }

        internal static bool IsFieldSetterCached(FieldInfo field)
        {
            return DelegateFactory.IsFieldSetterCached(field);
        }

        internal static void ClearFieldGetterCache()
        {
            DelegateFactory.ClearFieldGetterCache();
        }

        internal static void ClearFieldSetterCache()
        {
            DelegateFactory.ClearFieldSetterCache();
        }

        internal static void ClearPropertyCache()
        {
            DelegateFactory.ClearPropertyCache();
        }

        internal static void ClearMethodCache()
        {
            DelegateFactory.ClearMethodCache();
        }

        internal static void ClearConstructorCache()
        {
            DelegateFactory.ClearConstructorCache();
        }

        internal static bool TryGetDelegateStrategy(
            Delegate delegateInstance,
            out ReflectionDelegateStrategy strategy
        )
        {
            return DelegateFactory.TryGetStrategy(delegateInstance, out strategy);
        }

        /// <summary>
        /// Builds a cached delegate that returns the value of a field as <see cref="object"/>.
        /// Supports instance and static fields.
        /// </summary>
        /// <param name="field">Field to read.</param>
        /// <returns>Delegate: <c>object instance =&gt; object value</c></returns>
        /// <example>
        /// <code><![CDATA[
        /// var fi = typeof(Player).GetField("Score");
        /// var getter = ReflectionHelpers.GetFieldGetter(fi);
        /// object value = getter(myPlayer);
        /// ]]></code>
        /// </example>
        public static Func<object, object> GetFieldGetter(FieldInfo field)
        {
            return DelegateFactory.GetFieldGetter(field);
        }

#if EMIT_DYNAMIC_IL
        private static Func<object, object> BuildFieldGetterIL(FieldInfo field)
        {
            DynamicMethod dynamicMethod = new(
                $"Get{field.DeclaringType.Name}_{field.Name}",
                typeof(object),
                new[] { typeof(object) },
                field.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(
                field.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                field.DeclaringType
            );

            il.Emit(OpCodes.Ldfld, field);

            if (field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
            }

            il.Emit(OpCodes.Ret);

            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }
#endif

        /// <summary>
        /// Builds a cached delegate that returns the value of a property as <see cref="object"/>.
        /// Supports static and instance properties.
        /// </summary>
        /// <param name="property">Property to read.</param>
        /// <returns>Delegate: <c>object instanceOrNull =&gt; object value</c></returns>
        /// <example>
        /// <code><![CDATA[
        /// var pi = typeof(Settings).GetProperty("Instance"); // static property
        /// var getter = ReflectionHelpers.GetPropertyGetter(pi);
        /// object inst = getter(null);
        /// ]]></code>
        /// </example>
        public static Func<object, object> GetPropertyGetter(PropertyInfo property)
        {
            return DelegateFactory.GetPropertyGetter(property);
        }

#if EMIT_DYNAMIC_IL
        private static Func<object, object> BuildPropertyGetterIL(PropertyInfo property)
        {
            MethodInfo getMethod = property.GetGetMethod(true);

            DynamicMethod dynamicMethod = new(
                $"Get{property.DeclaringType.Name}_{property.Name}",
                typeof(object),
                new[] { typeof(object) },
                property.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            if (getMethod.IsStatic)
            {
                il.Emit(OpCodes.Call, getMethod);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                    property.DeclaringType
                );
                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt,
                    getMethod
                );
            }

            if (property.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Box, property.PropertyType);
            }

            il.Emit(OpCodes.Ret);

            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }
#endif

        /// <summary>
        /// Builds a strongly-typed property getter delegate.
        /// Supports static and instance properties; for static, the instance argument is ignored.
        /// </summary>
        /// <typeparam name="TInstance">Declaring type or compatible type.</typeparam>
        /// <typeparam name="TValue">Expected property value type.</typeparam>
        /// <param name="property">Property to read.</param>
        /// <returns>Delegate: <c>TInstance instance =&gt; TValue value</c></returns>
        /// <example>
        /// <code><![CDATA[
        /// var pi = typeof(TestPropertyClass).GetProperty(nameof(TestPropertyClass.InstanceProperty));
        /// var getter = ReflectionHelpers.GetPropertyGetter<TestPropertyClass, int>(pi);
        /// var obj = new TestPropertyClass { InstanceProperty = 123 };
        /// int value = getter(obj); // 123
        /// ]]></code>
        /// </example>
        public static Func<TInstance, TValue> GetPropertyGetter<TInstance, TValue>(
            PropertyInfo property
        )
        {
            return DelegateFactory.GetPropertyGetterTyped<TInstance, TValue>(property);
        }

        /// <summary>
        /// Builds a boxed setter delegate for a property.
        /// Supports instance and static properties. Throws if property has no setter.
        /// </summary>
        /// <param name="property">Property to set.</param>
        /// <returns>Delegate: <c>(object instanceOrNull, object value) =&gt; void</c></returns>
        /// <example>
        /// <code><![CDATA[
        /// var pi = typeof(TestPropertyClass).GetProperty(nameof(TestPropertyClass.InstanceProperty));
        /// var setter = ReflectionHelpers.GetPropertySetter(pi);
        /// object obj = new TestPropertyClass();
        /// setter(obj, 321);
        /// ]]></code>
        /// </example>
        public static Action<object, object> GetPropertySetter(PropertyInfo property)
        {
            MethodInfo setMethod = property.GetSetMethod(true);
            if (setMethod == null)
            {
                throw new ArgumentException(
                    $"Property {property?.Name} has no setter",
                    nameof(property)
                );
            }
            return DelegateFactory.GetPropertySetter(property);
        }

        public static Action<TInstance, TValue> GetPropertySetter<TInstance, TValue>(
            PropertyInfo property
        )
        {
            MethodInfo setMethod = property.GetSetMethod(true);
            if (setMethod == null)
            {
                throw new ArgumentException(
                    $"Property {property?.Name} has no setter",
                    nameof(property)
                );
            }
            return DelegateFactory.GetPropertySetterTyped<TInstance, TValue>(property);
        }

        public static Action<TValue> GetStaticPropertySetter<TValue>(PropertyInfo property)
        {
            MethodInfo setMethod = property.GetSetMethod(true);
            if (setMethod == null || !setMethod.IsStatic)
            {
                throw new ArgumentException(
                    $"Property {property?.Name} must be static and have a setter",
                    nameof(property)
                );
            }
            return DelegateFactory.GetStaticPropertySetterTyped<TValue>(property);
        }

        public static Func<object, object[], object> GetIndexerGetter(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }
            ParameterInfo[] indices = property.GetIndexParameters();
            if (indices == null || indices.Length == 0)
            {
                throw new ArgumentException("Property is not an indexer", nameof(property));
            }
            MethodInfo getter = property.GetGetMethod(true);
            if (getter == null)
            {
                throw new ArgumentException("Indexer has no getter", nameof(property));
            }
            return DelegateFactory.GetIndexerGetter(property);
        }

        public static Action<object, object, object[]> GetIndexerSetter(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }
            ParameterInfo[] indices = property.GetIndexParameters();
            if (indices == null || indices.Length == 0)
            {
                throw new ArgumentException("Property is not an indexer", nameof(property));
            }
            MethodInfo setter = property.GetSetMethod(true);
            if (setter == null)
            {
                throw new ArgumentException("Indexer has no setter", nameof(property));
            }
            return DelegateFactory.GetIndexerSetter(property);
        }

#if EMIT_DYNAMIC_IL
        private static Action<object, object> BuildPropertySetterIL(PropertyInfo property)
        {
            MethodInfo setMethod = property.GetSetMethod(true);
            DynamicMethod dynamicMethod = new(
                $"SetProperty{property.DeclaringType.Name}_{property.Name}",
                typeof(void),
                new[] { typeof(object), typeof(object) },
                property.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            if (setMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(
                    property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                    property.PropertyType
                );
                il.Emit(OpCodes.Call, setMethod);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                    property.DeclaringType
                );

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(
                    property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                    property.PropertyType
                );

                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt,
                    setMethod
                );
            }
            il.Emit(OpCodes.Ret);

            return (Action<object, object>)
                dynamicMethod.CreateDelegate(typeof(Action<object, object>));
        }
#endif

        /// <summary>
        /// Returns a compiled parameterless constructor delegate for a given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type with a public parameterless constructor.</param>
        /// <returns>Delegate: <c>() =&gt; object instance</c></returns>
        /// <exception cref="ArgumentNullException">If <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentException">If the type lacks a parameterless constructor.</exception>
        /// <example>
        /// <code><![CDATA[
        /// var ctor = ReflectionHelpers.GetParameterlessConstructor(typeof(List<int>));
        /// object list = ctor();
        /// ]]></code>
        /// </example>
        public static Func<object> GetParameterlessConstructor(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                throw new ArgumentException(
                    $"Type {type.FullName} does not have a parameterless constructor"
                );
            }
            return DelegateFactory.GetParameterlessConstructor(ctor);
        }

        /// <summary>
        /// Builds a cached delegate that returns the value of a static field as <see cref="object"/>.
        /// </summary>
        /// <param name="field">Static field to read.</param>
        /// <returns>Delegate: <c>() =&gt; object value</c></returns>
        /// <exception cref="ArgumentException">If <paramref name="field"/> is not static.</exception>
        public static Func<object> GetStaticFieldGetter(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }
            return DelegateFactory.GetStaticFieldGetter(field);
        }

#if EMIT_DYNAMIC_IL
        private static Func<object> BuildStaticFieldGetterIL(FieldInfo field)
        {
            DynamicMethod dynamicMethod = new(
                $"Get{field.DeclaringType.Name}_{field.Name}",
                typeof(object),
                Type.EmptyTypes,
                field.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, field);
            if (field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
        }
#endif

        /// <summary>
        /// Builds a strongly-typed field getter delegate for instance fields.
        /// </summary>
        public static Func<TInstance, TValue> GetFieldGetter<TInstance, TValue>(FieldInfo field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }
            return DelegateFactory.GetFieldGetterTyped<TInstance, TValue>(field);
        }

        /// <summary>
        /// Builds a cached delegate that returns the value of a static property as TValue.
        /// </summary>
        /// <summary>
        /// Builds a strongly-typed static property getter.
        /// </summary>
        /// <typeparam name="TValue">Property value type.</typeparam>
        /// <param name="property">Static property to read.</param>
        /// <returns>Delegate: <c>() =&gt; TValue</c></returns>
        public static Func<TValue> GetStaticPropertyGetter<TValue>(PropertyInfo property)
        {
            return DelegateFactory.GetStaticPropertyGetterTyped<TValue>(property);
        }

        /// <summary>
        /// Builds a cached delegate that returns the value of a static field as TValue.
        /// </summary>
        /// <summary>
        /// Builds a strongly-typed static field getter.
        /// </summary>
        /// <typeparam name="TValue">Field value type.</typeparam>
        /// <param name="field">Static field.</param>
        /// <returns>Delegate: <c>() =&gt; TValue</c></returns>
        /// <exception cref="ArgumentException">If the field is not static.</exception>
        public static Func<TValue> GetStaticFieldGetter<TValue>(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }
            return DelegateFactory.GetStaticFieldGetterTyped<TValue>(field);
        }

        /// <summary>
        /// Builds a strongly-typed field setter for instance fields.
        /// </summary>
        public static FieldSetter<TInstance, TValue> GetFieldSetter<TInstance, TValue>(
            FieldInfo field
        )
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }
            return DelegateFactory.GetFieldSetterTyped<TInstance, TValue>(field);
        }

        /// <summary>
        /// Builds a delegate that sets a static field to a value (boxed types supported).
        /// </summary>
        /// <summary>
        /// Builds a delegate that sets a static field to a value.
        /// </summary>
        /// <typeparam name="TValue">Field value type.</typeparam>
        /// <param name="field">Static field to write.</param>
        /// <returns>Delegate: <c>(TValue value) =&gt; void</c></returns>
        /// <exception cref="ArgumentException">If the field is not static.</exception>
        public static Action<TValue> GetStaticFieldSetter<TValue>(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }
            return DelegateFactory.GetStaticFieldSetterTyped<TValue>(field);
        }

        /// <summary>
        /// Builds a field setter for instance fields with boxed parameters (object instance, object value).
        /// </summary>
        /// <summary>
        /// Builds a field setter for fields with boxed parameters.
        /// Supports instance and static fields.
        /// </summary>
        /// <param name="field">Field to write.</param>
        /// <returns>Delegate: <c>(object instance, object value) =&gt; void</c></returns>
        public static Action<object, object> GetFieldSetter(FieldInfo field)
        {
            return DelegateFactory.GetFieldSetter(field);
        }

#if EMIT_DYNAMIC_IL
        private static Action<object, object> BuildFieldSetterIL(FieldInfo field)
        {
            DynamicMethod dynamicMethod = new(
                $"SetField{field.DeclaringType.Name}_{field.Name}",
                null,
                new[] { typeof(object), typeof(object) },
                field.DeclaringType.Module,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(
                field.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                field.DeclaringType
            );
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(
                field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                field.FieldType
            );
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<object, object>)
                dynamicMethod.CreateDelegate(typeof(Action<object, object>));
        }
#endif

        /// <summary>
        /// Builds a static field setter with boxed parameter (object value).
        /// </summary>
        /// <summary>
        /// Builds a static field setter with boxed parameter.
        /// </summary>
        /// <param name="field">Static field to write.</param>
        /// <returns>Delegate: <c>(object value) =&gt; void</c></returns>
        /// <exception cref="ArgumentException">If the field is not static.</exception>
        public static Action<object> GetStaticFieldSetter(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }
            return DelegateFactory.GetStaticFieldSetter(field);
        }

#if EMIT_DYNAMIC_IL
        private static Action<object> BuildStaticFieldSetterIL(FieldInfo field)
        {
            DynamicMethod dynamicMethod = new(
                $"SetFieldStatic{field.DeclaringType.Name}_{field.Name}",
                null,
                new[] { typeof(object) },
                field.DeclaringType.Module,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(
                field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                field.FieldType
            );
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<object>)dynamicMethod.CreateDelegate(typeof(Action<object>));
        }
#endif

        /// <summary>
        /// Gets (or caches) an array creator function for the given element type and length.
        /// </summary>
        /// <summary>
        /// Gets (or caches) an array creator function for the given element type.
        /// </summary>
        /// <param name="elementType">Array element type.</param>
        /// <returns>Delegate: <c>(int length) =&gt; Array</c></returns>
        public static Func<int, Array> GetArrayCreator(Type elementType)
        {
#if !EMIT_DYNAMIC_IL
            return size =>
            {
                if (size < 0)
                {
                    // Match IL newarr behavior which throws OverflowException for negative lengths
                    throw new OverflowException("Array length must be non-negative.");
                }
                return Array.CreateInstance(elementType, size);
            };
#else
            DynamicMethod dynamicMethod = new(
                $"CreateArray{elementType.Name}",
                typeof(Array), // Return type: Array
                new[] { typeof(int) }, // Parameter: int (size)
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // Load the array size
            il.Emit(OpCodes.Newarr, elementType); // Create a new array of 'type'
            il.Emit(OpCodes.Ret); // Return the array
            return (Func<int, Array>)dynamicMethod.CreateDelegate(typeof(Func<int, Array>));
#endif
        }

        /// <summary>
        /// Gets (or caches) a List&lt;T&gt; creator function for the given element type.
        /// </summary>
        /// <summary>
        /// Gets (or caches) a <see cref="List{T}"/> creator function for the given element type.
        /// </summary>
        /// <param name="elementType">List element type.</param>
        /// <returns>Delegate: <c>() =&gt; IList</c> where the instance is a <c>List&lt;T&gt;</c>.</returns>
        public static Func<IList> GetListCreator(Type elementType)
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
#if !EMIT_DYNAMIC_IL
            return () => (IList)Activator.CreateInstance(listType);
#else
            DynamicMethod dynamicMethod = new(
                $"CreateList{listType.Name}",
                typeof(IList), // Return type: IList
                Type.EmptyTypes, // No parameters
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            ConstructorInfo constructor = listType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"Type {listType} does not have a parameterless constructor."
                );
            }

            il.Emit(OpCodes.Newobj, constructor); // Call List<T> constructor
            il.Emit(OpCodes.Ret); // Return the instance
            return (Func<IList>)dynamicMethod.CreateDelegate(typeof(Func<IList>));
#endif
        }

        /// <summary>
        /// Gets (or caches) a List&lt;T&gt; creator function with capacity for the given element type.
        /// </summary>
        /// <summary>
        /// Gets (or caches) a <see cref="List{T}"/> creator function with capacity for the given element type.
        /// </summary>
        /// <param name="elementType">List element type.</param>
        /// <returns>Delegate: <c>(int capacity) =&gt; IList</c> where the instance is a <c>List&lt;T&gt;</c>.</returns>
        public static Func<int, IList> GetListWithCapacityCreator(Type elementType)
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
#if !EMIT_DYNAMIC_IL
            return capacity => (IList)Activator.CreateInstance(listType, capacity);
#else
            DynamicMethod dynamicMethod = new(
                $"CreateListWithCapacity{listType.Name}",
                typeof(IList), // Return type: IList
                new[] { typeof(int) }, // Parameter: int (size)
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            ConstructorInfo constructor = listType.GetConstructor(new[] { typeof(int) });
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"Type {listType} does not have a constructor accepting an int."
                );
            }

            il.Emit(OpCodes.Ldarg_0); // Load capacity argument
            il.Emit(OpCodes.Newobj, constructor); // Call List<T>(int capacity) constructor
            il.Emit(OpCodes.Ret); // Return the instance
            return (Func<int, IList>)dynamicMethod.CreateDelegate(typeof(Func<int, IList>));
#endif
        }

        /// <summary>
        /// Creates a <see cref="HashSet{T}"/> instance for the given element type with capacity.
        /// </summary>
        /// <param name="elementType">Element type for the set.</param>
        /// <param name="capacity">Initial capacity.</param>
        /// <returns>A boxed <c>HashSet&lt;T&gt;</c> as <see cref="object"/>.</returns>
        /// <example>
        /// <code><![CDATA[
        /// object set = ReflectionHelpers.CreateHashSet(typeof(string), 64); // HashSet<string>
        /// ]]></code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CreateHashSet(Type elementType, int capacity)
        {
            return GetHashSetWithCapacityCreatorCached(elementType).Invoke(capacity);
        }

        /// <summary>
        /// Gets (or caches) a <see cref="HashSet{T}"/> creator with capacity for the given element type.
        /// </summary>
        /// <param name="elementType">Element type for the set.</param>
        /// <returns>Delegate: <c>(int capacity) =&gt; object</c> producing a boxed <c>HashSet&lt;T&gt;</c>.</returns>
        public static Func<int, object> GetHashSetWithCapacityCreator(Type elementType)
        {
            Type hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
#if !EMIT_DYNAMIC_IL
            return capacity => Activator.CreateInstance(hashSetType, capacity);
#else
            DynamicMethod dynamicMethod = new(
                $"CreateHashSetWithCapacity{hashSetType.Name}",
                typeof(object), // Return type: object
                new[] { typeof(int) }, // Parameter: int (capacity)
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            ConstructorInfo constructor = hashSetType.GetConstructor(new[] { typeof(int) });
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"Type {hashSetType} does not have a constructor accepting an int."
                );
            }

            il.Emit(OpCodes.Ldarg_0); // Load capacity argument
            il.Emit(OpCodes.Newobj, constructor); // Call HashSet<T>(int capacity) constructor
            il.Emit(OpCodes.Ret); // Return the instance
            return (Func<int, object>)dynamicMethod.CreateDelegate(typeof(Func<int, object>));
#endif
        }

        /// <summary>
        /// Gets (or caches) an adder delegate for <c>HashSet&lt;T&gt;.Add</c>.
        /// </summary>
        /// <param name="elementType">Element type for the target <c>HashSet&lt;T&gt;</c>.</param>
        /// <returns>Delegate: <c>(object hashSet, object item) =&gt; void</c></returns>
        /// <example>
        /// <code><![CDATA[
        /// object set = ReflectionHelpers.CreateHashSet(typeof(int), 0);
        /// var add = ReflectionHelpers.GetHashSetAdder(typeof(int));
        /// add(set, 5);
        /// ]]></code>
        /// </example>
        public static Action<object, object> GetHashSetAdder(Type elementType)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            Type hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
            MethodInfo addMethod = hashSetType.GetMethod("Add", new[] { elementType });
            if (addMethod == null)
            {
                throw new ArgumentException(
                    $"Type {hashSetType} does not have an Add method accepting {elementType}.",
                    nameof(elementType)
                );
            }

#if !EMIT_DYNAMIC_IL
            return CreateCompiledHashSetAdder(hashSetType, elementType, addMethod)
                ?? (
                    (set, value) =>
                    {
                        // Mirror cast/unbox behavior to surface InvalidCastException for mismatches
                        if (value == null)
                        {
                            if (
                                elementType.IsValueType
                                && Nullable.GetUnderlyingType(elementType) == null
                            )
                            {
                                throw new InvalidCastException(
                                    $"Object of type 'null' cannot be converted to type '{elementType}'."
                                );
                            }
                        }
                        else if (!elementType.IsInstanceOfType(value))
                        {
                            throw new InvalidCastException(
                                $"Object of type '{value.GetType()}' cannot be converted to type '{elementType}'."
                            );
                        }

                        addMethod.Invoke(set, new[] { value });
                    }
                );
#else
            try
            {
                DynamicMethod dynamicMethod = new(
                    $"AddToHashSet{hashSetType.Name}",
                    typeof(void),
                    new[] { typeof(object), typeof(object) },
                    hashSetType.Module,
                    true
                );

                ILGenerator il = dynamicMethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, hashSetType);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(
                    elementType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                    elementType
                );
                il.EmitCall(OpCodes.Callvirt, addMethod, null);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);

                return (Action<object, object>)
                    dynamicMethod.CreateDelegate(typeof(Action<object, object>));
            }
            catch
            {
                return CreateCompiledHashSetAdder(hashSetType, elementType, addMethod)
                    ?? ((set, value) => addMethod.Invoke(set, new[] { value }));
            }
#endif
        }

        public static Action<object> GetHashSetClearer(Type elementType)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            return HashSetClearers.GetOrAdd(
                elementType,
                static type =>
                {
                    Type hashSetType = typeof(HashSet<>).MakeGenericType(type);
                    MethodInfo clearMethod = hashSetType.GetMethod("Clear", Type.EmptyTypes);
                    if (clearMethod == null)
                    {
                        return _ => { };
                    }
#if SUPPORT_EXPRESSION_COMPILE
                    if (ExpressionsEnabled)
                    {
                        try
                        {
                            ParameterExpression target = Expression.Parameter(
                                typeof(object),
                                "set"
                            );
                            UnaryExpression cast = Expression.Convert(target, hashSetType);
                            MethodCallExpression call = Expression.Call(cast, clearMethod);
                            return Expression.Lambda<Action<object>>(call, target).Compile();
                        }
                        catch
                        {
                            // Fall through to reflection fallback
                        }
                    }
#endif
                    MethodInfo closedMethod = clearMethod;
                    return set => closedMethod.Invoke(set, Array.Empty<object>());
                }
            );
        }

        /// <summary>
        /// Invokes an instance method using a cached invoker; avoids per-call reflection overhead.
        /// </summary>
        /// <param name="method">The instance method to invoke.</param>
        /// <param name="instance">The target instance.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>The return value from the method, or null for void.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object InvokeMethod(
            MethodInfo method,
            object instance,
            params object[] parameters
        )
        {
            return DelegateFactory.GetMethodInvoker(method).Invoke(instance, parameters);
        }

        /// <summary>
        /// Invokes a static method using a cached invoker; avoids per-call reflection overhead.
        /// </summary>
        /// <param name="method">The static method to invoke.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>The return value from the method, or null for void.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object InvokeStaticMethod(MethodInfo method, params object[] parameters)
        {
            return DelegateFactory.GetStaticMethodInvoker(method).Invoke(parameters);
        }

        /// <summary>
        /// Creates an instance using a constructor via a cached delegate.
        /// </summary>
        /// <param name="constructor">Constructor to invoke.</param>
        /// <param name="parameters">Constructor parameters.</param>
        /// <returns>Created instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CreateInstance(ConstructorInfo constructor, params object[] parameters)
        {
            return DelegateFactory.GetConstructorInvoker(constructor).Invoke(parameters);
        }

        /// <summary>
        /// Constructs an instance using a cached constructor invoker and returns it as T.
        /// </summary>
        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> using the best-matching constructor.
        /// </summary>
        /// <param name="parameters">Constructor parameters.</param>
        /// <typeparam name="T">Type to create.</typeparam>
        /// <returns>New instance of <typeparamref name="T"/>.</returns>
        /// <example>
        /// <code><![CDATA[
        /// var p = ReflectionHelpers.CreateInstance<Player>(name, level);
        /// ]]></code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreateInstance<T>(params object[] parameters)
        {
            Type type = typeof(T);
            Type[] parameterTypes =
                parameters?.Select(p => p?.GetType()).ToArray() ?? Type.EmptyTypes;
            ConstructorInfo constructor = type.GetConstructor(parameterTypes);
            if (constructor == null)
            {
                throw new ArgumentException($"No matching constructor found for type {type.Name}");
            }
            return (T)CreateInstance(constructor, parameters);
        }

        /// <summary>
        /// Constructs an instance of a closed generic type built from the given definition and arguments.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreateGenericInstance<T>(
            Type genericTypeDefinition,
            Type[] genericArguments,
            params object[] parameters
        )
        {
            Type constructedType = genericTypeDefinition.MakeGenericType(genericArguments);
            Type[] parameterTypes =
                parameters?.Select(p => p?.GetType()).ToArray() ?? Type.EmptyTypes;
            ConstructorInfo constructor = constructedType.GetConstructor(parameterTypes);
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"No matching constructor found for type {constructedType.Name}"
                );
            }
            return (T)CreateInstance(constructor, parameters);
        }

        /// <summary>
        /// Gets (or caches) a fast method invoker for instance methods to avoid reflection per call.
        /// </summary>
        public static Func<object, object[], object> GetMethodInvoker(MethodInfo method)
        {
            if (method.IsStatic)
            {
                throw new ArgumentException(
                    "Use GetStaticMethodInvoker for static methods",
                    nameof(method)
                );
            }
            return DelegateFactory.GetMethodInvoker(method);
        }

        /// <summary>
        /// Gets (or caches) a fast method invoker for static methods to avoid reflection per call.
        /// </summary>
        public static Func<object[], object> GetStaticMethodInvoker(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                throw new ArgumentException("Method must be static", nameof(method));
            }
            return DelegateFactory.GetStaticMethodInvoker(method);
        }

        /// <summary>
        /// Gets (or caches) a fast constructor invoker delegate that accepts an object[] of arguments.
        /// </summary>
        public static Func<object[], object> GetConstructor(ConstructorInfo constructor)
        {
            return DelegateFactory.GetConstructorInvoker(constructor);
        }

        /// <summary>
        /// Gets (or caches) a strongly-typed static method invoker with two parameters to avoid object[] allocations.
        /// Signature: Func&lt;T1, T2, TReturn&gt;
        /// </summary>
        /// <typeparam name="T1">First parameter type.</typeparam>
        /// <typeparam name="T2">Second parameter type.</typeparam>
        /// <typeparam name="TReturn">Return type.</typeparam>
        /// <param name="method">Static method info.</param>
        /// <returns>Compiled delegate matching the method signature.</returns>
        public static Func<T1, T2, TReturn> GetStaticMethodInvoker<T1, T2, TReturn>(
            MethodInfo method
        )
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (!method.IsStatic)
            {
                throw new ArgumentException("Method must be static", nameof(method));
            }

            ParameterInfo[] ps = method.GetParameters();
            if (
                ps.Length != 2
                || ps[0].ParameterType != typeof(T1)
                || ps[1].ParameterType != typeof(T2)
            )
            {
                throw new ArgumentException("Method signature does not match <T1,T2,TReturn>.");
            }

            return DelegateFactory.GetStaticMethodInvokerTyped<T1, T2, TReturn>(method);
        }

        public static Func<int, T[]> GetArrayCreator<T>()
        {
            return length => new T[length];
        }

        public static Func<IList> GetListCreator<T>()
        {
            return () => new List<T>();
        }

        public static Func<int, IList> GetListWithCapacityCreator<T>()
        {
            return capacity => new List<T>(capacity);
        }

        public static Func<int, HashSet<T>> GetHashSetWithCapacityCreator<T>()
        {
            return capacity => new HashSet<T>(capacity);
        }

        public static HashSet<T> CreateHashSet<T>(int capacity)
        {
            return new HashSet<T>(capacity);
        }

        public static Action<HashSet<T>, T> GetHashSetAdder<T>()
        {
            return (set, value) => set.Add(value);
        }

        public static object CreateDictionary(Type keyType, Type valueType, int capacity)
        {
            if (keyType == null || valueType == null)
            {
                throw new ArgumentNullException();
            }
            Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
#if !EMIT_DYNAMIC_IL
            return Activator.CreateInstance(dictType, capacity);
#else
            DynamicMethod dm = new(
                $"CreateDict_{dictType.Name}",
                typeof(object),
                new[] { typeof(int) },
                true
            );
            ILGenerator il = dm.GetILGenerator();
            ConstructorInfo ctor = dictType.GetConstructor(new[] { typeof(int) });
            if (ctor == null)
            {
                throw new ArgumentException($"Type {dictType} has no (int) constructor");
            }
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<int, object>)) is Func<int, object> f
                ? f(capacity)
                : Activator.CreateInstance(dictType, capacity);
#endif
        }

        public static Func<int, object> GetDictionaryWithCapacityCreator(
            Type keyType,
            Type valueType
        )
        {
            if (keyType == null || valueType == null)
            {
                throw new ArgumentNullException();
            }
            Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
#if !EMIT_DYNAMIC_IL
            return capacity => Activator.CreateInstance(dictType, capacity);
#else
            DynamicMethod dm = new(
                $"CreateDict_{dictType.Name}",
                typeof(object),
                new[] { typeof(int) },
                true
            );
            ILGenerator il = dm.GetILGenerator();
            ConstructorInfo ctor = dictType.GetConstructor(new[] { typeof(int) });
            if (ctor == null)
            {
                throw new ArgumentException($"Type {dictType} has no (int) constructor");
            }
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            return (Func<int, object>)dm.CreateDelegate(typeof(Func<int, object>));
#endif
        }

        public static Func<int, Dictionary<TKey, TValue>> GetDictionaryCreator<TKey, TValue>()
        {
            return capacity => new Dictionary<TKey, TValue>(capacity);
        }

        public static Func<TReturn> GetStaticMethodInvoker<TReturn>(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (!method.IsStatic)
            {
                throw new ArgumentException("Method must be static", nameof(method));
            }
            if (method.GetParameters().Length != 0 || method.ReturnType != typeof(TReturn))
            {
                throw new ArgumentException("Method signature does not match <TReturn>.");
            }
            return DelegateFactory.GetStaticMethodInvokerTyped<TReturn>(method);
        }

        public static Func<T1, TReturn> GetStaticMethodInvoker<T1, TReturn>(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (!method.IsStatic)
            {
                throw new ArgumentException("Method must be static", nameof(method));
            }
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(T1))
            {
                throw new ArgumentException("Method signature does not match <T1,TReturn>.");
            }
            if (ps.Any(p => p.ParameterType.IsByRef))
            {
                throw new NotSupportedException(
                    "ref/out parameters are not supported in typed invokers"
                );
            }
            return DelegateFactory.GetStaticMethodInvokerTyped<T1, TReturn>(method);
        }

        public static Func<T1, T2, T3, TReturn> GetStaticMethodInvoker<T1, T2, T3, TReturn>(
            MethodInfo method
        )
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (!method.IsStatic)
            {
                throw new ArgumentException("Method must be static", nameof(method));
            }
            ParameterInfo[] ps = method.GetParameters();
            if (
                ps.Length != 3
                || ps[0].ParameterType != typeof(T1)
                || ps[1].ParameterType != typeof(T2)
                || ps[2].ParameterType != typeof(T3)
            )
            {
                throw new ArgumentException("Method signature does not match <T1,T2,T3,TReturn>.");
            }
            if (ps.Any(p => p.ParameterType.IsByRef))
            {
                throw new NotSupportedException(
                    "ref/out parameters are not supported in typed invokers"
                );
            }
            return DelegateFactory.GetStaticMethodInvokerTyped<T1, T2, T3, TReturn>(method);
        }

        public static Func<T1, T2, T3, T4, TReturn> GetStaticMethodInvoker<T1, T2, T3, T4, TReturn>(
            MethodInfo method
        )
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (!method.IsStatic)
            {
                throw new ArgumentException("Method must be static", nameof(method));
            }
            ParameterInfo[] ps = method.GetParameters();
            if (
                ps.Length != 4
                || ps[0].ParameterType != typeof(T1)
                || ps[1].ParameterType != typeof(T2)
                || ps[2].ParameterType != typeof(T3)
                || ps[3].ParameterType != typeof(T4)
            )
            {
                throw new ArgumentException(
                    "Method signature does not match <T1,T2,T3,T4,TReturn>."
                );
            }
            if (ps.Any(p => p.ParameterType.IsByRef))
            {
                throw new NotSupportedException(
                    "ref/out parameters are not supported in typed invokers"
                );
            }
            return DelegateFactory.GetStaticMethodInvokerTyped<T1, T2, T3, T4, TReturn>(method);
        }

        public static Action GetStaticActionInvoker(MethodInfo method)
        {
            ValidateStaticActionSignature(method);
            return DelegateFactory.GetStaticActionInvokerTyped(method);
        }

        public static Action<T1> GetStaticActionInvoker<T1>(MethodInfo method)
        {
            ValidateStaticActionSignature(method, typeof(T1));
            return DelegateFactory.GetStaticActionInvokerTyped<T1>(method);
        }

        public static Action<T1, T2> GetStaticActionInvoker<T1, T2>(MethodInfo method)
        {
            ValidateStaticActionSignature(method, typeof(T1), typeof(T2));
            return DelegateFactory.GetStaticActionInvokerTyped<T1, T2>(method);
        }

        public static Action<T1, T2, T3> GetStaticActionInvoker<T1, T2, T3>(MethodInfo method)
        {
            ValidateStaticActionSignature(method, typeof(T1), typeof(T2), typeof(T3));
            return DelegateFactory.GetStaticActionInvokerTyped<T1, T2, T3>(method);
        }

        public static Action<T1, T2, T3, T4> GetStaticActionInvoker<T1, T2, T3, T4>(
            MethodInfo method
        )
        {
            ValidateStaticActionSignature(method, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
            return DelegateFactory.GetStaticActionInvokerTyped<T1, T2, T3, T4>(method);
        }

        public static Func<TInstance, TReturn> GetInstanceMethodInvoker<TInstance, TReturn>(
            MethodInfo method
        )
        {
            ValidateInstanceFuncSignature<TInstance, TReturn>(method, Type.EmptyTypes);
            return DelegateFactory.GetInstanceMethodInvokerTyped<TInstance, TReturn>(method);
        }

        public static Func<TInstance, T1, TReturn> GetInstanceMethodInvoker<TInstance, T1, TReturn>(
            MethodInfo method
        )
        {
            ValidateInstanceFuncSignature<TInstance, TReturn>(method, new[] { typeof(T1) });
            return DelegateFactory.GetInstanceMethodInvokerTyped<TInstance, T1, TReturn>(method);
        }

        public static Func<TInstance, T1, T2, TReturn> GetInstanceMethodInvoker<
            TInstance,
            T1,
            T2,
            TReturn
        >(MethodInfo method)
        {
            ValidateInstanceFuncSignature<TInstance, TReturn>(
                method,
                new[] { typeof(T1), typeof(T2) }
            );
            return DelegateFactory.GetInstanceMethodInvokerTyped<TInstance, T1, T2, TReturn>(
                method
            );
        }

        public static Func<TInstance, T1, T2, T3, TReturn> GetInstanceMethodInvoker<
            TInstance,
            T1,
            T2,
            T3,
            TReturn
        >(MethodInfo method)
        {
            ValidateInstanceFuncSignature<TInstance, TReturn>(
                method,
                new[] { typeof(T1), typeof(T2), typeof(T3) }
            );
            return DelegateFactory.GetInstanceMethodInvokerTyped<TInstance, T1, T2, T3, TReturn>(
                method
            );
        }

        public static Func<TInstance, T1, T2, T3, T4, TReturn> GetInstanceMethodInvoker<
            TInstance,
            T1,
            T2,
            T3,
            T4,
            TReturn
        >(MethodInfo method)
        {
            ValidateInstanceFuncSignature<TInstance, TReturn>(
                method,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }
            );
            return DelegateFactory.GetInstanceMethodInvokerTyped<
                TInstance,
                T1,
                T2,
                T3,
                T4,
                TReturn
            >(method);
        }

        public static Action<TInstance> GetInstanceActionInvoker<TInstance>(MethodInfo method)
        {
            ValidateInstanceActionSignature<TInstance>(method, Type.EmptyTypes);
            return DelegateFactory.GetInstanceActionInvokerTyped<TInstance>(method);
        }

        public static Action<TInstance, T1> GetInstanceActionInvoker<TInstance, T1>(
            MethodInfo method
        )
        {
            ValidateInstanceActionSignature<TInstance>(method, new[] { typeof(T1) });
            return DelegateFactory.GetInstanceActionInvokerTyped<TInstance, T1>(method);
        }

        public static Action<TInstance, T1, T2> GetInstanceActionInvoker<TInstance, T1, T2>(
            MethodInfo method
        )
        {
            ValidateInstanceActionSignature<TInstance>(method, new[] { typeof(T1), typeof(T2) });
            return DelegateFactory.GetInstanceActionInvokerTyped<TInstance, T1, T2>(method);
        }

        public static Action<TInstance, T1, T2, T3> GetInstanceActionInvoker<TInstance, T1, T2, T3>(
            MethodInfo method
        )
        {
            ValidateInstanceActionSignature<TInstance>(
                method,
                new[] { typeof(T1), typeof(T2), typeof(T3) }
            );
            return DelegateFactory.GetInstanceActionInvokerTyped<TInstance, T1, T2, T3>(method);
        }

        public static Action<TInstance, T1, T2, T3, T4> GetInstanceActionInvoker<
            TInstance,
            T1,
            T2,
            T3,
            T4
        >(MethodInfo method)
        {
            ValidateInstanceActionSignature<TInstance>(
                method,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }
            );
            return DelegateFactory.GetInstanceActionInvokerTyped<TInstance, T1, T2, T3, T4>(method);
        }

        private static Delegate BuildTypedStaticInvoker2<T1, T2, TReturn>(MethodInfo method)
        {
            if (ExpressionsEnabled)
            {
                try
                {
                    ParameterExpression p1 = Expression.Parameter(typeof(T1), "a");
                    ParameterExpression p2 = Expression.Parameter(typeof(T2), "b");
                    MethodCallExpression call = Expression.Call(method, p1, p2);
                    return Expression.Lambda<Func<T1, T2, TReturn>>(call, p1, p2).Compile();
                }
                catch
                {
                    // continue to alternative strategies
                }
            }

#if EMIT_DYNAMIC_IL
            if (DynamicIlEnabled)
            {
                try
                {
                    DynamicMethod dm = new(
                        $"InvokeStatic2_{method.DeclaringType?.Name}_{method.Name}",
                        typeof(TReturn),
                        new[] { typeof(T1), typeof(T2) },
                        method.Module,
                        true
                    );
                    ILGenerator il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, method);
                    il.Emit(OpCodes.Ret);
                    return dm.CreateDelegate(typeof(Func<T1, T2, TReturn>));
                }
                catch
                {
                    // ignore and fall back to reflection
                }
            }
#endif

            try
            {
                return method.CreateDelegate(typeof(Func<T1, T2, TReturn>));
            }
            catch
            {
                return new Func<T1, T2, TReturn>(
                    (a, b) => (TReturn)method.Invoke(null, new object[] { a, b })
                );
            }
        }

        private static Delegate BuildTypedStaticInvoker0<TReturn>(MethodInfo method)
        {
            if (ExpressionsEnabled)
            {
                try
                {
                    MethodCallExpression call = Expression.Call(method);
                    return Expression.Lambda<Func<TReturn>>(call).Compile();
                }
                catch
                {
                    // Fall through to alternative strategies
                }
            }

#if EMIT_DYNAMIC_IL
            if (DynamicIlEnabled)
            {
                try
                {
                    DynamicMethod dm = new(
                        $"InvokeStatic0_{method.DeclaringType?.Name}_{method.Name}",
                        typeof(TReturn),
                        Type.EmptyTypes,
                        method.Module,
                        true
                    );
                    ILGenerator il = dm.GetILGenerator();
                    il.Emit(OpCodes.Call, method);
                    il.Emit(OpCodes.Ret);
                    return dm.CreateDelegate(typeof(Func<TReturn>));
                }
                catch
                {
                    // Ignore and fall back to reflection
                }
            }
#endif

            try
            {
                return method.CreateDelegate(typeof(Func<TReturn>));
            }
            catch
            {
                return new Func<TReturn>(() => (TReturn)method.Invoke(null, Array.Empty<object>()));
            }
        }

        private static Delegate BuildTypedStaticInvoker1<T1, TReturn>(MethodInfo method)
        {
            if (ExpressionsEnabled)
            {
                try
                {
                    ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                    MethodCallExpression call = Expression.Call(method, a);
                    return Expression.Lambda<Func<T1, TReturn>>(call, a).Compile();
                }
                catch
                {
                    // Continue to alternative strategies
                }
            }

#if EMIT_DYNAMIC_IL
            if (DynamicIlEnabled)
            {
                try
                {
                    DynamicMethod dm = new(
                        $"InvokeStatic1_{method.DeclaringType?.Name}_{method.Name}",
                        typeof(TReturn),
                        new[] { typeof(T1) },
                        method.Module,
                        true
                    );
                    ILGenerator il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, method);
                    il.Emit(OpCodes.Ret);
                    return dm.CreateDelegate(typeof(Func<T1, TReturn>));
                }
                catch
                {
                    // fall through
                }
            }
#endif

            try
            {
                return method.CreateDelegate(typeof(Func<T1, TReturn>));
            }
            catch
            {
                return new Func<T1, TReturn>(arg =>
                    (TReturn)method.Invoke(null, new object[] { arg })
                );
            }
        }

        private static Delegate BuildTypedStaticInvoker3<T1, T2, T3, TReturn>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Func<T1, T2, T3, TReturn>));
            }
            catch
            {
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                MethodCallExpression call = Expression.Call(method, a, b, c);
                return Expression.Lambda<Func<T1, T2, T3, TReturn>>(call, a, b, c).Compile();
            }
#else
            DynamicMethod dm = new(
                $"InvokeStatic3_{method.DeclaringType?.Name}_{method.Name}",
                typeof(TReturn),
                new[] { typeof(T1), typeof(T2), typeof(T3) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<T1, T2, T3, TReturn>));
#endif
        }

        private static Delegate BuildTypedStaticInvoker4<T1, T2, T3, T4, TReturn>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Func<T1, T2, T3, T4, TReturn>));
            }
            catch
            {
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                ParameterExpression d = Expression.Parameter(typeof(T4), "d");
                MethodCallExpression call = Expression.Call(method, a, b, c, d);
                return Expression.Lambda<Func<T1, T2, T3, T4, TReturn>>(call, a, b, c, d).Compile();
            }
#else
            DynamicMethod dm = new(
                $"InvokeStatic4_{method.DeclaringType?.Name}_{method.Name}",
                typeof(TReturn),
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<T1, T2, T3, T4, TReturn>));
#endif
        }

        private static Delegate BuildStaticActionInvoker0(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Action));
            }
            catch
            {
                MethodCallExpression call = Expression.Call(method);
                return Expression.Lambda<Action>(call).Compile();
            }
#else
            DynamicMethod dm = new(
                $"InvokeStaticA0_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                Type.EmptyTypes,
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action));
#endif
        }

        private static Delegate BuildStaticActionInvoker1<T1>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Action<T1>));
            }
            catch
            {
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                MethodCallExpression call = Expression.Call(method, a);
                return Expression.Lambda<Action<T1>>(call, a).Compile();
            }
#else
            DynamicMethod dm = new(
                $"InvokeStaticA1_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(T1) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<T1>));
#endif
        }

        private static Delegate BuildStaticActionInvoker2<T1, T2>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Action<T1, T2>));
            }
            catch
            {
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                MethodCallExpression call = Expression.Call(method, a, b);
                return Expression.Lambda<Action<T1, T2>>(call, a, b).Compile();
            }
#else
            DynamicMethod dm = new(
                $"InvokeStaticA2_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(T1), typeof(T2) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<T1, T2>));
#endif
        }

        private static Delegate BuildStaticActionInvoker3<T1, T2, T3>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Action<T1, T2, T3>));
            }
            catch
            {
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                MethodCallExpression call = Expression.Call(method, a, b, c);
                return Expression.Lambda<Action<T1, T2, T3>>(call, a, b, c).Compile();
            }
#else
            DynamicMethod dm = new(
                $"InvokeStaticA3_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(T1), typeof(T2), typeof(T3) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<T1, T2, T3>));
#endif
        }

        private static Delegate BuildStaticActionInvoker4<T1, T2, T3, T4>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Action<T1, T2, T3, T4>));
            }
            catch
            {
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                ParameterExpression d = Expression.Parameter(typeof(T4), "d");
                MethodCallExpression call = Expression.Call(method, a, b, c, d);
                return Expression.Lambda<Action<T1, T2, T3, T4>>(call, a, b, c, d).Compile();
            }
#else
            DynamicMethod dm = new(
                $"InvokeStaticA4_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<T1, T2, T3, T4>));
#endif
        }

        private static void ValidateStaticActionSignature(
            MethodInfo method,
            params Type[] parameters
        )
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (!method.IsStatic)
            {
                throw new ArgumentException("Method must be static", nameof(method));
            }
            if (method.ReturnType != typeof(void))
            {
                throw new ArgumentException("Action invoker requires void return type");
            }
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Any(p => p.ParameterType.IsByRef))
            {
                throw new NotSupportedException(
                    "ref/out parameters are not supported in typed invokers"
                );
            }
            if (
                ps.Length != parameters.Length
                || ps.Where((t, i) => t.ParameterType != parameters[i]).Any()
            )
            {
                throw new ArgumentException("Method signature does not match Action parameters");
            }
        }

        private static void ValidateInstanceFuncSignature<TInstance, TReturn>(
            MethodInfo method,
            Type[] parameterTypes
        )
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (method.IsStatic)
            {
                throw new ArgumentException("Method must be instance", nameof(method));
            }
            if (method.ReturnType != typeof(TReturn))
            {
                throw new ArgumentException("Return type mismatch");
            }
            if (!typeof(TInstance).IsAssignableFrom(method.DeclaringType))
            {
                throw new ArgumentException("Instance type mismatch");
            }
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Any(p => p.ParameterType.IsByRef))
            {
                throw new NotSupportedException(
                    "ref/out parameters are not supported in typed invokers"
                );
            }
            if (
                ps.Length != parameterTypes.Length
                || ps.Where((t, i) => t.ParameterType != parameterTypes[i]).Any()
            )
            {
                throw new ArgumentException("Method parameters mismatch");
            }
        }

        private static void ValidateInstanceActionSignature<TInstance>(
            MethodInfo method,
            Type[] parameterTypes
        )
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            if (method.IsStatic)
            {
                throw new ArgumentException("Method must be instance", nameof(method));
            }
            if (method.ReturnType != typeof(void))
            {
                throw new ArgumentException("Return type must be void for Action invoker");
            }
            if (!typeof(TInstance).IsAssignableFrom(method.DeclaringType))
            {
                throw new ArgumentException("Instance type mismatch");
            }
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Any(p => p.ParameterType.IsByRef))
            {
                throw new NotSupportedException(
                    "ref/out parameters are not supported in typed invokers"
                );
            }
            if (
                ps.Length != parameterTypes.Length
                || ps.Where((t, i) => t.ParameterType != parameterTypes[i]).Any()
            )
            {
                throw new ArgumentException("Method parameters mismatch");
            }
        }

#if !EMIT_DYNAMIC_IL
        private static Delegate BuildInstanceInvoker0<TInstance, TReturn>(MethodInfo method)
        {
            try
            {
                return method.CreateDelegate(typeof(Func<TInstance, TReturn>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                MethodCallExpression call = Expression.Call(inst, method);
                return Expression.Lambda<Func<TInstance, TReturn>>(call, inst).Compile();
            }
        }

        private static Delegate BuildInstanceInvoker1<TInstance, T1, TReturn>(MethodInfo method)
        {
            try
            {
                return method.CreateDelegate(typeof(Func<TInstance, T1, TReturn>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                MethodCallExpression call = Expression.Call(inst, method, a);
                return Expression.Lambda<Func<TInstance, T1, TReturn>>(call, inst, a).Compile();
            }
        }

        private static Delegate BuildInstanceInvoker2<TInstance, T1, T2, TReturn>(MethodInfo method)
        {
            try
            {
                return method.CreateDelegate(typeof(Func<TInstance, T1, T2, TReturn>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                MethodCallExpression call = Expression.Call(inst, method, a, b);
                return Expression
                    .Lambda<Func<TInstance, T1, T2, TReturn>>(call, inst, a, b)
                    .Compile();
            }
        }

        private static Delegate BuildInstanceInvoker3<TInstance, T1, T2, T3, TReturn>(
            MethodInfo method
        )
        {
            try
            {
                return method.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, TReturn>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                MethodCallExpression call = Expression.Call(inst, method, a, b, c);
                return Expression
                    .Lambda<Func<TInstance, T1, T2, T3, TReturn>>(call, inst, a, b, c)
                    .Compile();
            }
        }

        private static Delegate BuildInstanceInvoker4<TInstance, T1, T2, T3, T4, TReturn>(
            MethodInfo method
        )
        {
            try
            {
                return method.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, T4, TReturn>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                ParameterExpression d = Expression.Parameter(typeof(T4), "d");
                MethodCallExpression call = Expression.Call(inst, method, a, b, c, d);
                return Expression
                    .Lambda<Func<TInstance, T1, T2, T3, T4, TReturn>>(call, inst, a, b, c, d)
                    .Compile();
            }
        }

        private static Delegate BuildInstanceActionInvoker0<TInstance>(MethodInfo method)
        {
            try
            {
                return method.CreateDelegate(typeof(Action<TInstance>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                MethodCallExpression call = Expression.Call(inst, method);
                return Expression.Lambda<Action<TInstance>>(call, inst).Compile();
            }
        }

        private static Delegate BuildInstanceActionInvoker1<TInstance, T1>(MethodInfo method)
        {
            try
            {
                return method.CreateDelegate(typeof(Action<TInstance, T1>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                MethodCallExpression call = Expression.Call(inst, method, a);
                return Expression.Lambda<Action<TInstance, T1>>(call, inst, a).Compile();
            }
        }

        private static Delegate BuildInstanceActionInvoker2<TInstance, T1, T2>(MethodInfo method)
        {
            try
            {
                return method.CreateDelegate(typeof(Action<TInstance, T1, T2>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                MethodCallExpression call = Expression.Call(inst, method, a, b);
                return Expression.Lambda<Action<TInstance, T1, T2>>(call, inst, a, b).Compile();
            }
        }

        private static Delegate BuildInstanceActionInvoker3<TInstance, T1, T2, T3>(
            MethodInfo method
        )
        {
            try
            {
                return method.CreateDelegate(typeof(Action<TInstance, T1, T2, T3>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                MethodCallExpression call = Expression.Call(inst, method, a, b, c);
                return Expression
                    .Lambda<Action<TInstance, T1, T2, T3>>(call, inst, a, b, c)
                    .Compile();
            }
        }

        private static Delegate BuildInstanceActionInvoker4<TInstance, T1, T2, T3, T4>(
            MethodInfo method
        )
        {
            try
            {
                return method.CreateDelegate(typeof(Action<TInstance, T1, T2, T3, T4>));
            }
            catch
            {
                ParameterExpression inst = Expression.Parameter(typeof(TInstance), "instance");
                ParameterExpression a = Expression.Parameter(typeof(T1), "a");
                ParameterExpression b = Expression.Parameter(typeof(T2), "b");
                ParameterExpression c = Expression.Parameter(typeof(T3), "c");
                ParameterExpression d = Expression.Parameter(typeof(T4), "d");
                MethodCallExpression call = Expression.Call(inst, method, a, b, c, d);
                return Expression
                    .Lambda<Action<TInstance, T1, T2, T3, T4>>(call, inst, a, b, c, d)
                    .Compile();
            }
        }
#else
        private static Delegate BuildInstanceInvoker0<TInstance, TReturn>(MethodInfo method)
        {
            DynamicMethod dm = new(
                $"InvokeInst0_{method.DeclaringType?.Name}_{method.Name}",
                typeof(TReturn),
                new[] { typeof(TInstance) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<TInstance, TReturn>));
        }

        private static Delegate BuildInstanceInvoker1<TInstance, T1, TReturn>(MethodInfo method)
        {
            DynamicMethod dm = new(
                $"InvokeInst1_{method.DeclaringType?.Name}_{method.Name}",
                typeof(TReturn),
                new[] { typeof(TInstance), typeof(T1) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<TInstance, T1, TReturn>));
        }

        private static Delegate BuildInstanceInvoker2<TInstance, T1, T2, TReturn>(MethodInfo method)
        {
            DynamicMethod dm = new(
                $"InvokeInst2_{method.DeclaringType?.Name}_{method.Name}",
                typeof(TReturn),
                new[] { typeof(TInstance), typeof(T1), typeof(T2) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<TInstance, T1, T2, TReturn>));
        }

        private static Delegate BuildInstanceInvoker3<TInstance, T1, T2, T3, TReturn>(
            MethodInfo method
        )
        {
            DynamicMethod dm = new(
                $"InvokeInst3_{method.DeclaringType?.Name}_{method.Name}",
                typeof(TReturn),
                new[] { typeof(TInstance), typeof(T1), typeof(T2), typeof(T3) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, TReturn>));
        }

        private static Delegate BuildInstanceInvoker4<TInstance, T1, T2, T3, T4, TReturn>(
            MethodInfo method
        )
        {
            DynamicMethod dm = new(
                $"InvokeInst4_{method.DeclaringType?.Name}_{method.Name}",
                typeof(TReturn),
                new[] { typeof(TInstance), typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldarg_S, (byte)4);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldarg_S, (byte)4);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, T4, TReturn>));
        }

        private static Delegate BuildInstanceActionInvoker0<TInstance>(MethodInfo method)
        {
            DynamicMethod dm = new(
                $"InvokeInstA0_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(TInstance) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<TInstance>));
        }

        private static Delegate BuildInstanceActionInvoker1<TInstance, T1>(MethodInfo method)
        {
            DynamicMethod dm = new(
                $"InvokeInstA1_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(TInstance), typeof(T1) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<TInstance, T1>));
        }

        private static Delegate BuildInstanceActionInvoker2<TInstance, T1, T2>(MethodInfo method)
        {
            DynamicMethod dm = new(
                $"InvokeInstA2_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(TInstance), typeof(T1), typeof(T2) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<TInstance, T1, T2>));
        }

        private static Delegate BuildInstanceActionInvoker3<TInstance, T1, T2, T3>(
            MethodInfo method
        )
        {
            DynamicMethod dm = new(
                $"InvokeInstA3_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(TInstance), typeof(T1), typeof(T2), typeof(T3) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<TInstance, T1, T2, T3>));
        }

        private static Delegate BuildInstanceActionInvoker4<TInstance, T1, T2, T3, T4>(
            MethodInfo method
        )
        {
            DynamicMethod dm = new(
                $"InvokeInstA4_{method.DeclaringType?.Name}_{method.Name}",
                typeof(void),
                new[] { typeof(TInstance), typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
                method.Module,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            if (typeof(TInstance).IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldarg_S, (byte)4);
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldarg_S, (byte)4);
                il.Emit(OpCodes.Callvirt, method);
            }
            il.Emit(OpCodes.Ret);
            return dm.CreateDelegate(typeof(Action<TInstance, T1, T2, T3, T4>));
        }
#endif

        /// <summary>
        /// Gets a parameterless constructor delegate for type T, or throws if not present.
        /// </summary>
        /// <summary>
        /// Gets a parameterless constructor delegate for type T, or throws if not present.
        /// </summary>
        public static Func<T> GetParameterlessConstructor<T>()
        {
            Type type = typeof(T);
            ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"Type {type.Name} does not have a parameterless constructor"
                );
            }
            return DelegateFactory.GetParameterlessConstructorTyped<T>(constructor);
        }

        /// <summary>
        /// Gets a parameterless constructor delegate for a closed generic type constructed from the given generic definition and arguments.
        /// </summary>
        public static Func<T> GetGenericParameterlessConstructor<T>(
            Type genericTypeDefinition,
            params Type[] genericArguments
        )
        {
            Type constructedType = genericTypeDefinition.MakeGenericType(genericArguments);
            ConstructorInfo constructor = constructedType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"Type {constructedType.Name} does not have a parameterless constructor"
                );
            }
            return DelegateFactory.GetParameterlessConstructorTyped<T>(constructor);
        }

        /// <summary>
        /// Returns all loaded types across accessible assemblies, swallowing reflection errors.
        /// </summary>
        public static IEnumerable<Type> GetAllLoadedTypes()
        {
            return GetAllLoadedAssemblies()
                .SelectMany(assembly => GetTypesFromAssembly(assembly))
                .Where(type => type != null);
        }

        /// <summary>
        /// Returns all loaded assemblies discoverable by the current AppDomain.
        /// </summary>
        public static IEnumerable<Assembly> GetAllLoadedAssemblies()
        {
            try
            {
                return AppDomain
                    .CurrentDomain.GetAssemblies()
                    .Where(assembly => assembly != null && !assembly.IsDynamic);
            }
            catch
            {
                return Enumerable.Empty<Assembly>();
            }
        }

        /// <summary>
        /// Safely gets all types from the specified assembly, returning an empty array on failure.
        /// </summary>
        public static Type[] GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return Type.EmptyTypes;
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray();
            }
            catch
            {
                return Type.EmptyTypes;
            }
        }

        /// <summary>
        /// Attempts to resolve a type by name using Type.GetType first, then scans loaded assemblies.
        /// Returns null if not found. Results are cached.
        /// </summary>
        public static Type TryResolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            if (TypeResolutionCache.TryGetValue(typeName, out Type cached))
            {
                return cached;
            }

            Type resolved = null;
            try
            {
                resolved = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
            }
            catch
            {
                resolved = null;
            }

            if (resolved == null)
            {
                foreach (Assembly asm in GetAllLoadedAssemblies())
                {
                    try
                    {
                        resolved = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
                        if (resolved != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // swallow and continue
                    }
                }
            }

            TypeResolutionCache[typeName] = resolved;
            return resolved;
        }

        /// <summary>
        /// Gets all loaded types derived from T. In editor, uses TypeCache for speed.
        /// </summary>
        public static IEnumerable<Type> GetTypesDerivedFrom<T>(bool includeAbstract = false)
        {
#if UNITY_EDITOR
            try
            {
                TypeCache.TypeCollection list = TypeCache.GetTypesDerivedFrom<T>();
                return list.Where(t =>
                    t != null && (includeAbstract || (t.IsClass && !t.IsAbstract))
                );
            }
            catch
            {
                // fall through to runtime path
            }
#endif
            Type baseType = typeof(T);
            return GetAllLoadedTypes()
                .Where(t =>
                    t != null
                    && baseType.IsAssignableFrom(t)
                    && (includeAbstract || (t.IsClass && !t.IsAbstract))
                );
        }

        /// <summary>
        /// Gets all loaded types derived from the specified base type. In editor, uses TypeCache for speed.
        /// </summary>
        public static IEnumerable<Type> GetTypesDerivedFrom(
            Type baseType,
            bool includeAbstract = false
        )
        {
            if (baseType == null)
            {
                return Array.Empty<Type>();
            }
#if UNITY_EDITOR
            try
            {
                TypeCache.TypeCollection list = TypeCache.GetTypesDerivedFrom(baseType);
                return list.Where(t =>
                    t != null && (includeAbstract || (t.IsClass && !t.IsAbstract))
                );
            }
            catch
            {
                // fall through
            }
#endif
            return GetAllLoadedTypes()
                .Where(t =>
                    t != null
                    && baseType.IsAssignableFrom(t)
                    && (includeAbstract || (t.IsClass && !t.IsAbstract))
                );
        }

        /// <summary>
        /// Safely gets all types from the assembly with the specified name, if loaded.
        /// </summary>
        public static Type[] GetTypesFromAssemblyName(string assemblyName)
        {
            try
            {
                Assembly assembly = Assembly.Load(assemblyName);
                return GetTypesFromAssembly(assembly);
            }
            catch
            {
                return Type.EmptyTypes;
            }
        }

        /// <summary>
        /// Finds all types with a given attribute across loaded assemblies.
        /// </summary>
        public static IEnumerable<Type> GetTypesWithAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            return GetAllLoadedTypes().Where(type => HasAttributeSafe<TAttribute>(type));
        }

        /// <summary>
        /// Finds all types with a given attribute, using TypeCache in editor when available.
        /// </summary>
        public static IEnumerable<Type> GetTypesWithAttribute<TAttribute>(bool includeAbstract)
            where TAttribute : Attribute
        {
#if UNITY_EDITOR
            try
            {
                TypeCache.TypeCollection types = TypeCache.GetTypesWithAttribute<TAttribute>();
                return types.Where(t =>
                    t != null && (includeAbstract || (t.IsClass && !t.IsAbstract))
                );
            }
            catch
            {
                // fall through
            }
#endif
            return GetAllLoadedTypes()
                .Where(t =>
                    t != null
                    && (includeAbstract || (t.IsClass && !t.IsAbstract))
                    && HasAttributeSafe<TAttribute>(t)
                );
        }

        /// <summary>
        /// Finds all types with a given attribute across loaded assemblies (non-generic overload).
        /// </summary>
        public static IEnumerable<Type> GetTypesWithAttribute(Type attributeType)
        {
            if (attributeType == null || !typeof(Attribute).IsAssignableFrom(attributeType))
            {
                return Enumerable.Empty<Type>();
            }

            return GetAllLoadedTypes().Where(type => HasAttributeSafe(type, attributeType));
        }

        public static IEnumerable<Type> GetComponentTypes(bool includeAbstract = false)
        {
            return GetTypesDerivedFrom(typeof(UnityEngine.Component), includeAbstract);
        }

        public static IEnumerable<Type> GetScriptableObjectTypes(bool includeAbstract = false)
        {
            return GetTypesDerivedFrom(typeof(UnityEngine.ScriptableObject), includeAbstract);
        }

        public static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(
            Type within = null,
            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
        )
            where TAttribute : Attribute
        {
#if UNITY_EDITOR
            try
            {
                TypeCache.MethodCollection methods =
                    TypeCache.GetMethodsWithAttribute<TAttribute>();
                IEnumerable<MethodInfo> filtered = methods;
                if (within != null)
                {
                    filtered = filtered.Where(m => m?.DeclaringType == within);
                }
                return filtered.Where(m => m != null);
            }
            catch
            {
                // fall through
            }
#endif
            if (within != null)
            {
                return SafeGetMethods(within, flags)
                    .Where(m => m != null && HasAttributeSafe<TAttribute>(m));
            }
            return GetAllLoadedTypes()
                .SelectMany(t => SafeGetMethods(t, flags))
                .Where(m => m != null && HasAttributeSafe<TAttribute>(m));
        }

        public static IEnumerable<FieldInfo> GetFieldsWithAttribute<TAttribute>(
            Type within = null,
            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
        )
            where TAttribute : Attribute
        {
#if UNITY_EDITOR
            try
            {
                TypeCache.FieldInfoCollection fields =
                    TypeCache.GetFieldsWithAttribute<TAttribute>();
                IEnumerable<FieldInfo> filtered = fields;
                if (within != null)
                {
                    filtered = filtered.Where(f => f?.DeclaringType == within);
                }
                return filtered.Where(f => f != null);
            }
            catch
            {
                // fall through
            }
#endif
            if (within != null)
            {
                return SafeGetFields(within, flags)
                    .Where(f => f != null && HasAttributeSafe<TAttribute>(f));
            }
            return GetAllLoadedTypes()
                .SelectMany(t => SafeGetFields(t, flags))
                .Where(f => f != null && HasAttributeSafe<TAttribute>(f));
        }

        public static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(
            Type within = null,
            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
        )
            where TAttribute : Attribute
        {
            if (within != null)
            {
                return SafeGetProperties(within, flags)
                    .Where(p => p != null && HasAttributeSafe<TAttribute>(p));
            }
            return GetAllLoadedTypes()
                .SelectMany(t => SafeGetProperties(t, flags))
                .Where(p => p != null && HasAttributeSafe<TAttribute>(p));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<MethodInfo> SafeGetMethods(Type t, BindingFlags flags)
        {
            try
            {
                return t?.GetMethods(flags) ?? Array.Empty<MethodInfo>();
            }
            catch
            {
                return Array.Empty<MethodInfo>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<FieldInfo> SafeGetFields(Type t, BindingFlags flags)
        {
            try
            {
                return t?.GetFields(flags) ?? Array.Empty<FieldInfo>();
            }
            catch
            {
                return Array.Empty<FieldInfo>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<PropertyInfo> SafeGetProperties(Type t, BindingFlags flags)
        {
            try
            {
                return t?.GetProperties(flags) ?? Array.Empty<PropertyInfo>();
            }
            catch
            {
                return Array.Empty<PropertyInfo>();
            }
        }

        public static bool TryGetField(
            Type type,
            string name,
            out FieldInfo field,
            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
        )
        {
            field = null;
            if (type == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            (Type type, string name, BindingFlags flags) key = (type, name, flags);
#if SINGLE_THREADED
            if (!FieldLookup.TryGetValue(key, out field))
            {
                field = type.GetField(name, flags);
                FieldLookup[key] = field;
            }
#else
            field = FieldLookup.GetOrAdd(key, static k => k.type.GetField(k.name, k.flags));
#endif
            return field != null;
        }

        public static bool TryGetProperty(
            Type type,
            string name,
            out PropertyInfo property,
            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
        )
        {
            property = null;
            if (type == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            (Type type, string name, BindingFlags flags) key = (type, name, flags);
#if SINGLE_THREADED
            if (!PropertyLookup.TryGetValue(key, out property))
            {
                property = type.GetProperty(name, flags);
                PropertyLookup[key] = property;
            }
#else
            property = PropertyLookup.GetOrAdd(
                key,
                static k => k.type.GetProperty(k.name, k.flags)
            );
#endif
            return property != null;
        }

        public static bool TryGetMethod(
            Type type,
            string name,
            out MethodInfo method,
            Type[] paramTypes = null,
            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
        )
        {
            method = null;
            if (type == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            string sig = BuildMethodSignatureKey(name, paramTypes);
            (Type type, string sig, BindingFlags flags) key = (type, sig, flags);
#if SINGLE_THREADED
            if (!MethodLookup.TryGetValue(key, out method))
            {
                method =
                    paramTypes == null
                        ? type.GetMethod(name, flags)
                        : type.GetMethod(
                            name,
                            flags,
                            binder: null,
                            types: paramTypes,
                            modifiers: null
                        );
                MethodLookup[key] = method;
            }
#else
            method = MethodLookup.GetOrAdd(
                key,
                static (tuple, state) =>
                {
                    if (state.parameterTypes == null)
                    {
                        return tuple.type.GetMethod(state.methodName, tuple.flags);
                    }
                    return tuple.type.GetMethod(
                        state.methodName,
                        tuple.flags,
                        binder: null,
                        types: state.parameterTypes,
                        modifiers: null
                    );
                },
                (methodName: name, parameterTypes: paramTypes)
            );
#endif
            return method != null;
        }

        private static string BuildMethodSignatureKey(string name, Type[] paramTypes)
        {
            if (paramTypes == null || paramTypes.Length == 0)
            {
                return name + "()";
            }

            return name
                + "("
                + string.Join(",", paramTypes.Select(t => t?.FullName ?? "null"))
                + ")";
        }

        public static bool HasAttributeSafe<TAttribute>(
            ICustomAttributeProvider provider,
            bool inherit = true
        )
            where TAttribute : Attribute
        {
            if (provider == null)
            {
                return false;
            }

            try
            {
                return provider.IsDefined(typeof(TAttribute), inherit);
            }
            catch
            {
                return false;
            }
        }

        public static Action<T> BuildParameterlessInstanceMethodIfExists<T>(string methodName)
        {
            try
            {
                Type type = typeof(T);
                foreach (
                    MethodInfo method in type.GetMethods(
                        BindingFlags.Instance | BindingFlags.Public
                    )
                )
                {
                    if (
                        string.Equals(method.Name, methodName, StringComparison.Ordinal)
                        && method.GetParameters().Length == 0
                    )
                    {
                        return (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), method);
                    }
                }
            }
            catch
            {
                // Swallow
            }
            return null;
        }

#if SINGLE_THREADED
        private static readonly Dictionary<Type, Func<object, bool>> EnabledPropertyGetters = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<object, bool> GetEnabledPropertyGetterCached(Type componentType)
        {
            if (!EnabledPropertyGetters.TryGetValue(componentType, out Func<object, bool> getter))
            {
                getter = BuildEnabledPropertyGetter(componentType);
                EnabledPropertyGetters[componentType] = getter;
            }

            return getter;
        }
#else
        private static readonly ConcurrentDictionary<
            Type,
            Func<object, bool>
        > EnabledPropertyGetters = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<object, bool> GetEnabledPropertyGetterCached(Type componentType)
        {
            return EnabledPropertyGetters.GetOrAdd(
                componentType,
                static type => BuildEnabledPropertyGetter(type)
            );
        }
#endif

        private static Func<object, bool> BuildEnabledPropertyGetter(Type type)
        {
            try
            {
                PropertyInfo property = type.GetProperty(
                    "enabled",
                    BindingFlags.Instance | BindingFlags.Public
                );

                if (property == null || property.PropertyType != typeof(bool))
                {
                    return null;
                }

                MethodInfo getMethod = property.GetGetMethod();
                if (getMethod == null)
                {
                    return null;
                }

#if !EMIT_DYNAMIC_IL
                return CreateCompiledEnabledPropertyGetter(property, type);
#else
                DynamicMethod dynamicMethod = new(
                    $"GetEnabled_{type.Name}",
                    typeof(bool),
                    new[] { typeof(object) },
                    type,
                    true
                );

                ILGenerator il = dynamicMethod.GetILGenerator();

                // Load and cast the instance argument
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(type.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, type);

                // Call the getter
                il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, getMethod);

                il.Emit(OpCodes.Ret);

                return (Func<object, bool>)dynamicMethod.CreateDelegate(typeof(Func<object, bool>));
#endif
            }
            catch
            {
                return null;
            }
        }

        private static Func<object, bool> CreateCompiledEnabledPropertyGetter(
            PropertyInfo property,
            Type type
        )
        {
            if (!ExpressionsEnabled)
            {
                return CreateDelegateEnabledPropertyGetter(property, type)
                    ?? (instance => (bool)property.GetValue(instance));
            }

            try
            {
                MethodInfo getMethod = property.GetGetMethod();
                if (getMethod == null)
                {
                    return instance => (bool)property.GetValue(instance);
                }

                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );

                Expression instanceExpression = type.IsValueType
                    ? Expression.Unbox(instanceParam, type)
                    : Expression.Convert(instanceParam, type);

                Expression propertyExpression = Expression.Property(instanceExpression, property);

                return Expression
                    .Lambda<Func<object, bool>>(propertyExpression, instanceParam)
                    .Compile();
            }
            catch
            {
                return instance => (bool)property.GetValue(instance);
            }
        }

        private static Func<object, bool> CreateDelegateEnabledPropertyGetter(
            PropertyInfo property,
            Type type
        )
        {
            try
            {
                MethodInfo getMethod = property.GetGetMethod();
                if (getMethod == null)
                {
                    return null;
                }

                // Try to create a delegate directly
                Type delegateType = typeof(Func<,>).MakeGenericType(type, typeof(bool));
                Delegate del = Delegate.CreateDelegate(delegateType, null, getMethod, false);
                if (del == null)
                {
                    return null;
                }

                return instance => (bool)del.DynamicInvoke(instance);
            }
            catch
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsComponentEnabled<T>(this T component)
            where T : UnityEngine.Object
        {
            if (component == null)
            {
                return false;
            }

            Type componentType = component.GetType();
            Func<object, bool> enabledGetter = GetEnabledPropertyGetterCached(componentType);

            if (enabledGetter == null)
            {
                return true;
            }

            try
            {
                return enabledGetter(component);
            }
            catch
            {
                return true;
            }
        }

        public static bool IsActiveAndEnabled<T>(this T component)
            where T : UnityEngine.Object
        {
            if (component == null)
            {
                return false;
            }

            bool enabled = component.IsComponentEnabled();
            if (component is UnityEngine.Component c)
            {
                return enabled && c.gameObject != null && c.gameObject.activeInHierarchy;
            }
            return enabled;
        }

        public static bool HasAttributeSafe(
            ICustomAttributeProvider provider,
            Type attributeType,
            bool inherit = true
        )
        {
            if (provider == null || attributeType == null)
            {
                return false;
            }

            try
            {
                return provider.IsDefined(attributeType, inherit);
            }
            catch
            {
                return false;
            }
        }

        public static TAttribute GetAttributeSafe<TAttribute>(
            ICustomAttributeProvider provider,
            bool inherit = true
        )
            where TAttribute : Attribute
        {
            if (provider == null)
            {
                return default;
            }

            try
            {
                if (provider.IsDefined(typeof(TAttribute), inherit))
                {
                    object[] attributes = provider.GetCustomAttributes(typeof(TAttribute), inherit);
                    return attributes.Length > 0 ? attributes[0] as TAttribute : default;
                }
            }
            catch
            {
                // Swallow
            }

            return default;
        }

        public static Attribute GetAttributeSafe(
            ICustomAttributeProvider provider,
            Type attributeType,
            bool inherit = true
        )
        {
            if (provider == null || attributeType == null)
            {
                return null;
            }

            try
            {
                if (provider.IsDefined(attributeType, inherit))
                {
                    object[] attributes = provider.GetCustomAttributes(attributeType, inherit);
                    return attributes.Length > 0 ? attributes[0] as Attribute : null;
                }
            }
            catch
            {
                // Swallow
            }

            return null;
        }

        public static TAttribute[] GetAllAttributesSafe<TAttribute>(
            this ICustomAttributeProvider provider,
            bool inherit = true
        )
            where TAttribute : Attribute
        {
            if (provider == null)
            {
                return Array.Empty<TAttribute>();
            }

            try
            {
                if (provider.IsDefined(typeof(TAttribute), inherit))
                {
                    return provider
                        .GetCustomAttributes(typeof(TAttribute), inherit)
                        .OfType<TAttribute>()
                        .ToArray();
                }
                return Array.Empty<TAttribute>();
            }
            catch
            {
                return Array.Empty<TAttribute>();
            }
        }

        public static Attribute[] GetAllAttributesSafe(
            this ICustomAttributeProvider provider,
            bool inherit = true
        )
        {
            if (provider == null)
            {
                return Array.Empty<Attribute>();
            }

            try
            {
                return provider.GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
            }
            catch
            {
                return Array.Empty<Attribute>();
            }
        }

        public static Attribute[] GetAllAttributesSafe(
            this ICustomAttributeProvider provider,
            Type attributeType,
            bool inherit = true
        )
        {
            if (provider == null || attributeType == null)
            {
                return Array.Empty<Attribute>();
            }

            try
            {
                if (provider.IsDefined(attributeType, inherit))
                {
                    return provider
                        .GetCustomAttributes(attributeType, inherit)
                        .OfType<Attribute>()
                        .ToArray();
                }
                return Array.Empty<Attribute>();
            }
            catch
            {
                return Array.Empty<Attribute>();
            }
        }

        public static Dictionary<string, object> GetAllAttributeValuesSafe(
            this ICustomAttributeProvider provider,
            bool inherit = true
        )
        {
            Dictionary<string, object> result = new();

            foreach (Attribute attr in GetAllAttributesSafe(provider, inherit))
            {
                try
                {
                    Type attrType = attr.GetType();
                    string key = attrType.Name;

                    if (key.EndsWith("Attribute"))
                    {
                        key = key.Substring(0, key.Length - 9);
                    }

                    result.TryAdd(key, attr);
                }
                catch
                {
                    // Skip this attribute if we can't process it
                }
            }

            return result;
        }

        public static MethodInfo[] GetMethodsWithAttributeSafe<TAttribute>(
            this Type type,
            bool inherit = true
        )
            where TAttribute : Attribute
        {
            if (type == null)
            {
                return Array.Empty<MethodInfo>();
            }

            try
            {
                bool localInherit = inherit;
                return type.GetMethods(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                    )
                    .Where(method => HasAttributeSafe<TAttribute>(method, localInherit))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<MethodInfo>();
            }
        }

        public static PropertyInfo[] GetPropertiesWithAttributeSafe<TAttribute>(
            this Type type,
            bool inherit = true
        )
            where TAttribute : Attribute
        {
            if (type == null)
            {
                return Array.Empty<PropertyInfo>();
            }

            try
            {
                bool localInherit = inherit;
                return type.GetProperties(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                    )
                    .Where(property => HasAttributeSafe<TAttribute>(property, localInherit))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<PropertyInfo>();
            }
        }

        public static FieldInfo[] GetFieldsWithAttributeSafe<TAttribute>(
            this Type type,
            bool inherit = true
        )
            where TAttribute : Attribute
        {
            if (type == null)
            {
                return Array.Empty<FieldInfo>();
            }

            try
            {
                bool localInherit = inherit;
                return type.GetFields(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                    )
                    .Where(field => HasAttributeSafe<TAttribute>(field, localInherit))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<FieldInfo>();
            }
        }

        private static bool CheckDynamicIlSupport()
        {
#if !EMIT_DYNAMIC_IL
            return false;
#else
            try
            {
                DynamicMethod dynamicMethod = new(
                    "UnityHelpersIlProbe",
                    typeof(int),
                    Type.EmptyTypes,
                    typeof(ReflectionHelpers),
                    true
                );
                ILGenerator il = dynamicMethod.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);
                _ = ((Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>)))();
                return true;
            }
            catch
            {
                return false;
            }
#endif
        }

#if EMIT_DYNAMIC_IL
        private static Func<object, object[], object> BuildMethodInvokerIL(MethodInfo method)
        {
            try
            {
                DynamicMethod dynamicMethod = new(
                    $"Invoke{method.DeclaringType.Name}_{method.Name}",
                    typeof(object),
                    new[] { typeof(object), typeof(object[]) },
                    method.DeclaringType,
                    true
                );

                ILGenerator il = dynamicMethod.GetILGenerator();
                ParameterInfo[] parameters = method.GetParameters();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(
                    method.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                    method.DeclaringType
                );

                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    Type paramType = parameters[i].ParameterType;
                    if (paramType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, paramType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, paramType);
                    }
                }

                il.Emit(method.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);

                if (method.ReturnType == typeof(void))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (method.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, method.ReturnType);
                }

                il.Emit(OpCodes.Ret);

                return (Func<object, object[], object>)
                    dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
            }
            catch
            {
                return null;
            }
        }

        private static Func<object[], object> BuildStaticMethodInvokerIL(MethodInfo method)
        {
            try
            {
                DynamicMethod dynamicMethod = new(
                    $"InvokeStatic{method.DeclaringType.Name}_{method.Name}",
                    typeof(object),
                    new[] { typeof(object[]) },
                    method.DeclaringType,
                    true
                );

                ILGenerator il = dynamicMethod.GetILGenerator();
                ParameterInfo[] parameters = method.GetParameters();

                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    Type paramType = parameters[i].ParameterType;
                    if (paramType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, paramType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, paramType);
                    }
                }

                il.Emit(OpCodes.Call, method);

                if (method.ReturnType == typeof(void))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (method.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, method.ReturnType);
                }

                il.Emit(OpCodes.Ret);

                return (Func<object[], object>)
                    dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
            }
            catch
            {
                return null;
            }
        }

        private static Func<object[], object> BuildConstructorIL(ConstructorInfo constructor)
        {
            try
            {
                DynamicMethod dynamicMethod = new(
                    $"Create{constructor.DeclaringType.Name}",
                    typeof(object),
                    new[] { typeof(object[]) },
                    constructor.DeclaringType,
                    true
                );

                ILGenerator il = dynamicMethod.GetILGenerator();
                ParameterInfo[] parameters = constructor.GetParameters();

                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    Type paramType = parameters[i].ParameterType;
                    if (paramType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, paramType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, paramType);
                    }
                }

                il.Emit(OpCodes.Newobj, constructor);

                if (constructor.DeclaringType.IsValueType)
                {
                    il.Emit(OpCodes.Box, constructor.DeclaringType);
                }

                il.Emit(OpCodes.Ret);

                return (Func<object[], object>)
                    dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
            }
            catch
            {
                return null;
            }
        }
#endif

        private static bool CheckExpressionCompilationSupport()
        {
#if !SUPPORT_EXPRESSION_COMPILE
            return false;
#else
            try
            {
                // Test if expression compilation works by trying a simple lambda
                Expression<Func<int>> testExpr = () => 42;
                Func<int> compiled = testExpr.Compile();
                return compiled() == 42;
            }
            catch
            {
                return false;
            }
#endif
        }

        private static Action<object, object> CreateCompiledHashSetAdder(
            Type hashSetType,
            Type elementType,
            MethodInfo addMethod
        )
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                ParameterExpression setParam = Expression.Parameter(typeof(object), "hashSet");
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

                Expression castSet = Expression.Convert(setParam, hashSetType);
                Expression castValue = Expression.Convert(valueParam, elementType);

                MethodCallExpression call = Expression.Call(castSet, addMethod, castValue);
                Expression body =
                    addMethod.ReturnType == typeof(void)
                        ? call
                        : Expression.Block(call, Expression.Empty());

                return Expression
                    .Lambda<Action<object, object>>(body, setParam, valueParam)
                    .Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<object, object[], object> CreateCompiledMethodInvoker(MethodInfo method)
        {
            if (!ExpressionsEnabled)
            {
                return CreateDelegateMethodInvoker(method)
                    ?? ((instance, args) => method.Invoke(instance, args));
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );
                ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

                Expression instanceExpression = method.DeclaringType.IsValueType
                    ? Expression.Unbox(instanceParam, method.DeclaringType)
                    : Expression.Convert(instanceParam, method.DeclaringType);

                ParameterInfo[] parameters = method.GetParameters();
                Expression[] paramExpressions = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression argExpression = Expression.ArrayIndex(
                        argsParam,
                        Expression.Constant(i)
                    );
                    Type paramType = parameters[i].ParameterType;
                    paramExpressions[i] = paramType.IsValueType
                        ? Expression.Unbox(argExpression, paramType)
                        : Expression.Convert(argExpression, paramType);
                }

                Expression callExpression = Expression.Call(
                    instanceExpression,
                    method,
                    paramExpressions
                );

                Expression returnExpression =
                    method.ReturnType == typeof(void)
                        ? Expression.Block(callExpression, Expression.Constant(null))
                    : method.ReturnType.IsValueType
                        ? Expression.Convert(callExpression, typeof(object))
                    : callExpression;

                return Expression
                    .Lambda<Func<object, object[], object>>(
                        returnExpression,
                        instanceParam,
                        argsParam
                    )
                    .Compile();
            }
            catch
            {
                return (instance, args) => method.Invoke(instance, args);
            }
        }

        private static Func<object[], object> CreateCompiledStaticMethodInvoker(MethodInfo method)
        {
            if (!ExpressionsEnabled)
            {
                return CreateDelegateStaticMethodInvoker(method)
                    ?? (args => method.Invoke(null, args));
            }

            try
            {
                ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

                ParameterInfo[] parameters = method.GetParameters();
                Expression[] paramExpressions = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression argExpression = Expression.ArrayIndex(
                        argsParam,
                        Expression.Constant(i)
                    );
                    Type paramType = parameters[i].ParameterType;
                    paramExpressions[i] = paramType.IsValueType
                        ? Expression.Unbox(argExpression, paramType)
                        : Expression.Convert(argExpression, paramType);
                }

                Expression callExpression = Expression.Call(method, paramExpressions);

                Expression returnExpression =
                    method.ReturnType == typeof(void)
                        ? Expression.Block(callExpression, Expression.Constant(null))
                    : method.ReturnType.IsValueType
                        ? Expression.Convert(callExpression, typeof(object))
                    : callExpression;

                return Expression
                    .Lambda<Func<object[], object>>(returnExpression, argsParam)
                    .Compile();
            }
            catch
            {
                return args => method.Invoke(null, args);
            }
        }

        private static Func<object[], object> CreateCompiledConstructor(ConstructorInfo constructor)
        {
            if (!ExpressionsEnabled)
            {
                return CreateDelegateConstructor(constructor) ?? (args => constructor.Invoke(args));
            }

            try
            {
                ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

                ParameterInfo[] parameters = constructor.GetParameters();
                Expression[] paramExpressions = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression argExpression = Expression.ArrayIndex(
                        argsParam,
                        Expression.Constant(i)
                    );
                    Type paramType = parameters[i].ParameterType;
                    paramExpressions[i] = paramType.IsValueType
                        ? Expression.Unbox(argExpression, paramType)
                        : Expression.Convert(argExpression, paramType);
                }

                Expression newExpression = Expression.New(constructor, paramExpressions);

                Expression returnExpression = constructor.DeclaringType.IsValueType
                    ? Expression.Convert(newExpression, typeof(object))
                    : newExpression;

                return Expression
                    .Lambda<Func<object[], object>>(returnExpression, argsParam)
                    .Compile();
            }
            catch
            {
                return args => constructor.Invoke(args);
            }
        }

        private static Func<object> CreateCompiledParameterlessConstructor(
            ConstructorInfo constructor
        )
        {
            if (!ExpressionsEnabled)
            {
                return CreateDelegateParameterlessConstructor(constructor)
                    ?? (() => constructor.Invoke(null));
            }

            try
            {
                Expression newExpression = Expression.New(constructor);
                Expression body = constructor.DeclaringType.IsValueType
                    ? Expression.Convert(newExpression, typeof(object))
                    : newExpression;

                return Expression.Lambda<Func<object>>(body).Compile();
            }
            catch
            {
                return CreateDelegateParameterlessConstructor(constructor)
                    ?? (() => constructor.Invoke(null));
            }
        }

        private static Func<T> CreateCompiledParameterlessConstructor<T>(
            ConstructorInfo constructor,
            Type constructedType
        )
        {
            if (!ExpressionsEnabled)
            {
                if (constructedType == typeof(T))
                {
                    return CreateDelegateParameterlessConstructor<T>(constructor)
                        ?? (() => (T)constructor.Invoke(null));
                }

                return () => (T)constructor.Invoke(null);
            }

            try
            {
                Expression newExpression = Expression.New(constructor);
                Expression body =
                    constructedType.IsValueType && typeof(T) != constructedType
                        ? Expression.Convert(newExpression, typeof(T))
                    : constructedType == typeof(T) ? newExpression
                    : Expression.Convert(newExpression, typeof(T));

                return Expression.Lambda<Func<T>>(body).Compile();
            }
            catch
            {
                return () => (T)constructor.Invoke(null);
            }
        }

        private static Func<object, object[], object> CreateCompiledIndexerGetter(
            PropertyInfo property
        )
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );
                ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

                MethodInfo getMethod = property.GetGetMethod(true);
                if (getMethod == null)
                {
                    return null;
                }

                Expression targetExpression = property.DeclaringType.IsValueType
                    ? Expression.Unbox(instanceParam, property.DeclaringType)
                    : Expression.Convert(instanceParam, property.DeclaringType);

                ParameterInfo[] indices = property.GetIndexParameters();
                Expression[] indexExpressions = new Expression[indices.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    Expression element = Expression.ArrayIndex(argsParam, Expression.Constant(i));
                    Type indexType = indices[i].ParameterType;
                    indexExpressions[i] = indexType.IsValueType
                        ? Expression.Unbox(element, indexType)
                        : Expression.Convert(element, indexType);
                }

                Expression propertyExpression = getMethod.IsStatic
                    ? Expression.Property(null, property, indexExpressions)
                    : Expression.Property(targetExpression, property, indexExpressions);

                Expression body = property.PropertyType.IsValueType
                    ? Expression.Convert(propertyExpression, typeof(object))
                    : propertyExpression;

                return Expression
                    .Lambda<Func<object, object[], object>>(body, instanceParam, argsParam)
                    .Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Action<object, object, object[]> CreateCompiledIndexerSetter(
            PropertyInfo property
        )
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                MethodInfo setMethod = property.GetSetMethod(true);
                if (setMethod == null)
                {
                    return null;
                }

                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
                ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

                Expression targetExpression = property.DeclaringType.IsValueType
                    ? Expression.Unbox(instanceParam, property.DeclaringType)
                    : Expression.Convert(instanceParam, property.DeclaringType);

                ParameterInfo[] indices = property.GetIndexParameters();
                Expression[] indexExpressions = new Expression[indices.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    Expression element = Expression.ArrayIndex(argsParam, Expression.Constant(i));
                    Type indexType = indices[i].ParameterType;
                    indexExpressions[i] = indexType.IsValueType
                        ? Expression.Unbox(element, indexType)
                        : Expression.Convert(element, indexType);
                }

                Expression valueExpression = property.PropertyType.IsValueType
                    ? Expression.Unbox(valueParam, property.PropertyType)
                    : Expression.Convert(valueParam, property.PropertyType);

                IEnumerable<Expression> arguments = indexExpressions.Concat(
                    new[] { valueExpression }
                );
                Expression callExpression = setMethod.IsStatic
                    ? Expression.Call(setMethod, arguments)
                    : Expression.Call(targetExpression, setMethod, arguments);

                return Expression
                    .Lambda<Action<object, object, object[]>>(
                        callExpression,
                        instanceParam,
                        valueParam,
                        argsParam
                    )
                    .Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<T> CreateCompiledParameterlessConstructor<T>(
            ConstructorInfo constructor
        )
        {
            return CreateCompiledParameterlessConstructor<T>(
                constructor,
                constructor.DeclaringType
            );
        }

        private static Func<object, object> CreateCompiledFieldGetter(FieldInfo field)
        {
            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );

                Expression instanceExpression =
                    field.IsStatic ? null
                    : field.DeclaringType.IsValueType
                        ? Expression.Unbox(instanceParam, field.DeclaringType)
                    : Expression.Convert(instanceParam, field.DeclaringType);

                Expression fieldExpression = field.IsStatic
                    ? Expression.Field(null, field)
                    : Expression.Field(instanceExpression, field);
                Expression returnExpression = field.FieldType.IsValueType
                    ? Expression.Convert(fieldExpression, typeof(object))
                    : fieldExpression;

                return Expression
                    .Lambda<Func<object, object>>(returnExpression, instanceParam)
                    .Compile();
            }
            catch
            {
                return null;
            }
        }

        private static bool CanInlineAssignment(Type sourceType, Type targetType)
        {
            if (sourceType == targetType)
            {
                return true;
            }

            if (!targetType.IsValueType)
            {
                if (sourceType.IsValueType)
                {
                    return true;
                }

                return targetType.IsAssignableFrom(sourceType);
            }

            if (!sourceType.IsValueType)
            {
                return sourceType == typeof(object);
            }

            return false;
        }

        private static bool CanInlineReturnConversion(Type actualType, Type requestedType)
        {
            if (actualType == requestedType)
            {
                return true;
            }

            if (requestedType.IsValueType)
            {
                if (!actualType.IsValueType)
                {
                    return actualType == typeof(object);
                }

                return false;
            }

            if (actualType.IsValueType)
            {
                return true;
            }

            return requestedType.IsAssignableFrom(actualType);
        }

        private static Func<object, object> CreateCompiledPropertyGetter(PropertyInfo property)
        {
            try
            {
                MethodInfo getMethod = property.GetGetMethod(true);
                if (getMethod == null)
                {
                    return property.GetValue;
                }

                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );
                Expression propertyExpression;

                if (getMethod.IsStatic)
                {
                    propertyExpression = Expression.Property(null, property);
                }
                else
                {
                    Expression instanceExpression = property.DeclaringType.IsValueType
                        ? Expression.Unbox(instanceParam, property.DeclaringType)
                        : Expression.Convert(instanceParam, property.DeclaringType);
                    propertyExpression = Expression.Property(instanceExpression, property);
                }

                Expression returnExpression = property.PropertyType.IsValueType
                    ? Expression.Convert(propertyExpression, typeof(object))
                    : propertyExpression;

                return Expression
                    .Lambda<Func<object, object>>(returnExpression, instanceParam)
                    .Compile();
            }
            catch
            {
                return property.GetValue;
            }
        }

        private static Action<object, object> CreateCompiledPropertySetter(PropertyInfo property)
        {
            if (!ExpressionsEnabled)
            {
                return property.SetValue;
            }

            try
            {
                MethodInfo setMethod = property.GetSetMethod(true);
                if (setMethod == null)
                {
                    return property.SetValue;
                }

                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

                Expression valueExpression = property.PropertyType.IsValueType
                    ? Expression.Unbox(valueParam, property.PropertyType)
                    : Expression.Convert(valueParam, property.PropertyType);

                Expression callExpression;
                if (setMethod.IsStatic)
                {
                    callExpression = Expression.Call(setMethod, valueExpression);
                }
                else
                {
                    Expression instanceExpression = property.DeclaringType.IsValueType
                        ? Expression.Unbox(instanceParam, property.DeclaringType)
                        : Expression.Convert(instanceParam, property.DeclaringType);
                    callExpression = Expression.Call(
                        instanceExpression,
                        setMethod,
                        valueExpression
                    );
                }

                return Expression
                    .Lambda<Action<object, object>>(callExpression, instanceParam, valueParam)
                    .Compile();
            }
            catch
            {
                return property.SetValue;
            }
        }

        private static Func<TInstance, TValue> CreateCompiledTypedFieldGetter<TInstance, TValue>(
            FieldInfo field
        )
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(TInstance),
                    "instance"
                );
                Expression fieldExpression;
                if (field.IsStatic)
                {
                    fieldExpression = Expression.Field(null, field);
                }
                else
                {
                    Expression instanceExpression = PrepareInstanceExpression(
                        instanceParam,
                        field.DeclaringType
                    );
                    fieldExpression = Expression.Field(instanceExpression, field);
                }

                Expression body =
                    field.FieldType == typeof(TValue)
                        ? fieldExpression
                        : Expression.Convert(fieldExpression, typeof(TValue));

                return Expression.Lambda<Func<TInstance, TValue>>(body, instanceParam).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static FieldSetter<TInstance, TValue> CreateCompiledTypedFieldSetter<
            TInstance,
            TValue
        >(FieldInfo field)
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            if (!field.IsStatic && typeof(TInstance) != field.DeclaringType)
            {
                return null;
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(TInstance).MakeByRefType(),
                    "instance"
                );
                ParameterExpression valueParam = Expression.Parameter(typeof(TValue), "value");

                Expression valueExpression =
                    field.FieldType == typeof(TValue)
                        ? valueParam
                        : Expression.Convert(valueParam, field.FieldType);

                Expression assignExpression;
                if (field.IsStatic)
                {
                    assignExpression = Expression.Assign(
                        Expression.Field(null, field),
                        valueExpression
                    );
                }
                else
                {
                    Expression fieldAccess = Expression.Field(instanceParam, field);
                    assignExpression = Expression.Assign(fieldAccess, valueExpression);
                }

                return Expression
                    .Lambda<FieldSetter<TInstance, TValue>>(
                        assignExpression,
                        instanceParam,
                        valueParam
                    )
                    .Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<TValue> CreateCompiledTypedStaticFieldGetter<TValue>(FieldInfo field)
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                Expression body = Expression.Field(null, field);
                if (field.FieldType != typeof(TValue))
                {
                    body = Expression.Convert(body, typeof(TValue));
                }

                return Expression.Lambda<Func<TValue>>(body).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Action<TValue> CreateCompiledTypedStaticFieldSetter<TValue>(FieldInfo field)
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                ParameterExpression valueParam = Expression.Parameter(typeof(TValue), "value");
                Expression valueExpression =
                    field.FieldType == typeof(TValue)
                        ? valueParam
                        : Expression.Convert(valueParam, field.FieldType);
                Expression assignExpression = Expression.Assign(
                    Expression.Field(null, field),
                    valueExpression
                );
                return Expression.Lambda<Action<TValue>>(assignExpression, valueParam).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<TInstance, TValue> CreateCompiledTypedPropertyGetter<TInstance, TValue>(
            PropertyInfo property,
            MethodInfo getMethod
        )
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(TInstance),
                    "instance"
                );

                Expression callExpression;
                if (getMethod.IsStatic)
                {
                    callExpression = Expression.Call(getMethod);
                }
                else
                {
                    Expression instanceExpression = PrepareInstanceExpression(
                        instanceParam,
                        property.DeclaringType
                    );
                    callExpression = Expression.Call(instanceExpression, getMethod);
                }

                Expression body =
                    property.PropertyType == typeof(TValue)
                        ? callExpression
                        : Expression.Convert(callExpression, typeof(TValue));

                return Expression.Lambda<Func<TInstance, TValue>>(body, instanceParam).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Action<TInstance, TValue> CreateCompiledTypedPropertySetter<
            TInstance,
            TValue
        >(PropertyInfo property, MethodInfo setMethod)
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            if (!setMethod.IsStatic && property.DeclaringType.IsValueType)
            {
                return null;
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(TInstance),
                    "instance"
                );
                ParameterExpression valueParam = Expression.Parameter(typeof(TValue), "value");

                Expression valueExpression =
                    property.PropertyType == typeof(TValue)
                        ? valueParam
                        : Expression.Convert(valueParam, property.PropertyType);

                MethodCallExpression callExpression;
                if (setMethod.IsStatic)
                {
                    callExpression = Expression.Call(setMethod, valueExpression);
                }
                else
                {
                    Expression instanceExpression = PrepareInstanceExpression(
                        instanceParam,
                        property.DeclaringType
                    );
                    callExpression = Expression.Call(
                        instanceExpression,
                        setMethod,
                        valueExpression
                    );
                }

                return Expression
                    .Lambda<Action<TInstance, TValue>>(callExpression, instanceParam, valueParam)
                    .Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<TValue> CreateCompiledTypedStaticPropertyGetter<TValue>(
            PropertyInfo property,
            MethodInfo getMethod
        )
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                Expression body = Expression.Call(getMethod);
                if (property.PropertyType != typeof(TValue))
                {
                    body = Expression.Convert(body, typeof(TValue));
                }

                return Expression.Lambda<Func<TValue>>(body).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Action<TValue> CreateCompiledTypedStaticPropertySetter<TValue>(
            PropertyInfo property,
            MethodInfo setMethod
        )
        {
            if (!ExpressionsEnabled)
            {
                return null;
            }

            try
            {
                ParameterExpression valueParam = Expression.Parameter(typeof(TValue), "value");

                Expression valueExpression =
                    property.PropertyType == typeof(TValue)
                        ? valueParam
                        : Expression.Convert(valueParam, property.PropertyType);

                MethodCallExpression callExpression = Expression.Call(setMethod, valueExpression);

                return Expression.Lambda<Action<TValue>>(callExpression, valueParam).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Expression PrepareInstanceExpression(
            ParameterExpression instanceParameter,
            Type declaringType
        )
        {
            if (instanceParameter.Type == declaringType)
            {
                return instanceParameter;
            }

            return Expression.Convert(instanceParameter, declaringType);
        }

#if EMIT_DYNAMIC_IL
        private static void EmitAssignmentConversion(
            ILGenerator il,
            Type sourceType,
            Type targetType
        )
        {
            if (targetType == sourceType)
            {
                return;
            }

            if (targetType.IsValueType)
            {
                if (!sourceType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, targetType);
                }
            }
            else
            {
                if (sourceType.IsValueType)
                {
                    il.Emit(OpCodes.Box, sourceType);
                    if (targetType != typeof(object))
                    {
                        il.Emit(OpCodes.Castclass, targetType);
                    }
                }
                else if (!targetType.IsAssignableFrom(sourceType))
                {
                    il.Emit(OpCodes.Castclass, targetType);
                }
            }
        }

        private static void EmitReturnConversion(
            ILGenerator il,
            Type actualType,
            Type requestedType
        )
        {
            if (requestedType == actualType)
            {
                return;
            }

            if (requestedType.IsValueType)
            {
                if (!actualType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, requestedType);
                }
            }
            else
            {
                if (actualType.IsValueType)
                {
                    il.Emit(OpCodes.Box, actualType);
                    if (requestedType != typeof(object))
                    {
                        il.Emit(OpCodes.Castclass, requestedType);
                    }
                }
                else if (!requestedType.IsAssignableFrom(actualType))
                {
                    il.Emit(OpCodes.Castclass, requestedType);
                }
            }
        }

        private static Func<object> BuildParameterlessConstructorIL(ConstructorInfo constructor)
        {
            DynamicMethod dynamicMethod = new(
                $"New_{constructor.DeclaringType.Name}_Obj",
                typeof(object),
                Type.EmptyTypes,
                constructor.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructor);
            if (constructor.DeclaringType.IsValueType)
            {
                il.Emit(OpCodes.Box, constructor.DeclaringType);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
        }

        private static Func<T> BuildTypedParameterlessConstructorIL<T>(ConstructorInfo constructor)
        {
            DynamicMethod dynamicMethod = new(
                $"New_{constructor.DeclaringType.Name}_{typeof(T).Name}",
                typeof(T),
                Type.EmptyTypes,
                constructor.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructor);

            Type declaringType = constructor.DeclaringType;
            if (declaringType != typeof(T))
            {
                if (declaringType.IsValueType)
                {
                    il.Emit(OpCodes.Box, declaringType);
                    if (typeof(T) != typeof(object))
                    {
                        il.Emit(OpCodes.Castclass, typeof(T));
                    }
                }
                else if (!typeof(T).IsAssignableFrom(declaringType))
                {
                    il.Emit(OpCodes.Castclass, typeof(T));
                }
            }

            il.Emit(OpCodes.Ret);
            return (Func<T>)dynamicMethod.CreateDelegate(typeof(Func<T>));
        }

        private static Func<object, object[], object> BuildIndexerGetterIL(PropertyInfo property)
        {
            MethodInfo getter =
                property.GetGetMethod(true)
                ?? throw new ArgumentException("Indexer has no getter", nameof(property));

            DynamicMethod dynamicMethod = new(
                $"GetIndexer_{property.DeclaringType.Name}_{property.Name}",
                typeof(object),
                new[] { typeof(object), typeof(object[]) },
                property.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            bool isStatic = getter.IsStatic;
            if (!isStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                    property.DeclaringType
                );
            }

            ParameterInfo[] indices = property.GetIndexParameters();
            for (int i = 0; i < indices.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitAssignmentConversion(il, typeof(object), indices[i].ParameterType);
            }

            il.Emit(
                getter.IsStatic ? OpCodes.Call
                    : property.DeclaringType.IsValueType ? OpCodes.Call
                    : OpCodes.Callvirt,
                getter
            );
            EmitReturnConversion(il, property.PropertyType, typeof(object));
            il.Emit(OpCodes.Ret);
            return (Func<object, object[], object>)
                dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
        }

        private static Action<object, object, object[]> BuildIndexerSetterIL(PropertyInfo property)
        {
            MethodInfo setter =
                property.GetSetMethod(true)
                ?? throw new ArgumentException("Indexer has no setter", nameof(property));

            DynamicMethod dynamicMethod = new(
                $"SetIndexer_{property.DeclaringType.Name}_{property.Name}",
                typeof(void),
                new[] { typeof(object), typeof(object), typeof(object[]) },
                property.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            bool isStatic = setter.IsStatic;
            if (!isStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                    property.DeclaringType
                );
            }

            ParameterInfo[] indices = property.GetIndexParameters();
            for (int i = 0; i < indices.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitAssignmentConversion(il, typeof(object), indices[i].ParameterType);
            }

            il.Emit(OpCodes.Ldarg_1);
            EmitAssignmentConversion(il, typeof(object), property.PropertyType);
            il.Emit(
                setter.IsStatic ? OpCodes.Call
                    : property.DeclaringType.IsValueType ? OpCodes.Call
                    : OpCodes.Callvirt,
                setter
            );
            il.Emit(OpCodes.Ret);
            return (Action<object, object, object[]>)
                dynamicMethod.CreateDelegate(typeof(Action<object, object, object[]>));
        }

        private static Func<TInstance, TValue> BuildTypedFieldGetterIL<TInstance, TValue>(
            FieldInfo field
        )
        {
            DynamicMethod dynamicMethod = new(
                $"GetGeneric{field.DeclaringType.Name}_{field.Name}",
                typeof(TValue),
                new[] { typeof(TInstance) },
                field.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();

            if (field.IsStatic)
            {
                il.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                if (typeof(TInstance).IsValueType)
                {
                    il.Emit(OpCodes.Ldarga_S, 0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                }

                if (field.DeclaringType != typeof(TInstance))
                {
                    il.Emit(
                        field.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                        field.DeclaringType
                    );
                }

                il.Emit(OpCodes.Ldfld, field);
            }

            EmitReturnConversion(il, field.FieldType, typeof(TValue));
            il.Emit(OpCodes.Ret);
            return (Func<TInstance, TValue>)
                dynamicMethod.CreateDelegate(typeof(Func<TInstance, TValue>));
        }

        private static FieldSetter<TInstance, TValue> BuildTypedFieldSetterIL<TInstance, TValue>(
            FieldInfo field
        )
        {
            DynamicMethod dynamicMethod = new(
                $"SetFieldGeneric{field.DeclaringType.Name}_{field.Name}",
                typeof(void),
                new[] { typeof(TInstance).MakeByRefType(), typeof(TValue) },
                field.Module,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();

            if (field.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitAssignmentConversion(il, typeof(TValue), field.FieldType);
                il.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                if (typeof(TInstance).IsValueType)
                {
                    il.Emit(OpCodes.Ldarg_0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldind_Ref);
                }

                if (field.DeclaringType != typeof(TInstance))
                {
                    il.Emit(
                        field.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                        field.DeclaringType
                    );
                }

                il.Emit(OpCodes.Ldarg_1);
                EmitAssignmentConversion(il, typeof(TValue), field.FieldType);
                il.Emit(OpCodes.Stfld, field);
            }

            il.Emit(OpCodes.Ret);
            return (FieldSetter<TInstance, TValue>)
                dynamicMethod.CreateDelegate(typeof(FieldSetter<TInstance, TValue>));
        }

        private static Func<TValue> BuildTypedStaticFieldGetterIL<TValue>(FieldInfo field)
        {
            DynamicMethod dynamicMethod = new(
                $"GetStaticTyped{field.DeclaringType.Name}_{field.Name}",
                typeof(TValue),
                Type.EmptyTypes,
                field.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, field);
            EmitReturnConversion(il, field.FieldType, typeof(TValue));
            il.Emit(OpCodes.Ret);
            return (Func<TValue>)dynamicMethod.CreateDelegate(typeof(Func<TValue>));
        }

        private static Action<TValue> BuildTypedStaticFieldSetterIL<TValue>(FieldInfo field)
        {
            DynamicMethod dynamicMethod = new(
                $"SetStaticTyped{field.DeclaringType.Name}_{field.Name}",
                typeof(void),
                new[] { typeof(TValue) },
                field.Module,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            EmitAssignmentConversion(il, typeof(TValue), field.FieldType);
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<TValue>)dynamicMethod.CreateDelegate(typeof(Action<TValue>));
        }

        private static Func<TInstance, TValue> BuildTypedPropertyGetterIL<TInstance, TValue>(
            PropertyInfo property,
            MethodInfo getMethod
        )
        {
            DynamicMethod dynamicMethod = new(
                $"GetGeneric{property.DeclaringType.Name}_{property.Name}",
                typeof(TValue),
                new[] { typeof(TInstance) },
                property.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            if (getMethod.IsStatic)
            {
                il.Emit(OpCodes.Call, getMethod);
            }
            else
            {
                if (typeof(TInstance).IsValueType)
                {
                    il.Emit(OpCodes.Ldarga_S, 0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                }

                if (property.DeclaringType != typeof(TInstance))
                {
                    il.Emit(
                        property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                        property.DeclaringType
                    );
                }

                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt,
                    getMethod
                );
            }

            EmitReturnConversion(il, property.PropertyType, typeof(TValue));

            il.Emit(OpCodes.Ret);
            return (Func<TInstance, TValue>)
                dynamicMethod.CreateDelegate(typeof(Func<TInstance, TValue>));
        }

        private static Action<TInstance, TValue> BuildTypedPropertySetterIL<TInstance, TValue>(
            PropertyInfo property,
            MethodInfo setMethod
        )
        {
            DynamicMethod dynamicMethod = new(
                $"SetPropertyGeneric{property.DeclaringType.Name}_{property.Name}",
                typeof(void),
                new[] { typeof(TInstance), typeof(TValue) },
                property.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();

            if (setMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitAssignmentConversion(il, typeof(TValue), property.PropertyType);
                il.Emit(OpCodes.Call, setMethod);
            }
            else
            {
                if (typeof(TInstance).IsValueType)
                {
                    il.Emit(OpCodes.Ldarga_S, 0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                }
                if (property.DeclaringType != typeof(TInstance))
                {
                    il.Emit(
                        property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                        property.DeclaringType
                    );
                }

                il.Emit(OpCodes.Ldarg_1);
                EmitAssignmentConversion(il, typeof(TValue), property.PropertyType);

                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt,
                    setMethod
                );
            }

            il.Emit(OpCodes.Ret);
            return (Action<TInstance, TValue>)
                dynamicMethod.CreateDelegate(typeof(Action<TInstance, TValue>));
        }

        private static Func<TValue> BuildTypedStaticPropertyGetterIL<TValue>(
            PropertyInfo property,
            MethodInfo getMethod
        )
        {
            DynamicMethod dynamicMethod = new(
                $"GetStaticTyped{property.DeclaringType.Name}_{property.Name}",
                typeof(TValue),
                Type.EmptyTypes,
                property.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Call, getMethod);
            EmitReturnConversion(il, property.PropertyType, typeof(TValue));

            il.Emit(OpCodes.Ret);
            return (Func<TValue>)dynamicMethod.CreateDelegate(typeof(Func<TValue>));
        }

        private static Action<TValue> BuildTypedStaticPropertySetterIL<TValue>(
            PropertyInfo property,
            MethodInfo setMethod
        )
        {
            DynamicMethod dynamicMethod = new(
                $"SetStaticTyped{property.DeclaringType.Name}_{property.Name}",
                typeof(void),
                new[] { typeof(TValue) },
                property.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            EmitAssignmentConversion(il, typeof(TValue), property.PropertyType);
            il.Emit(OpCodes.Call, setMethod);
            il.Emit(OpCodes.Ret);
            return (Action<TValue>)dynamicMethod.CreateDelegate(typeof(Action<TValue>));
        }
#endif

        private static Func<object> CreateCompiledStaticFieldGetter(FieldInfo field)
        {
            if (!ExpressionsEnabled)
            {
                return () => field.GetValue(null);
            }

            try
            {
                Expression fieldExpression = Expression.Field(null, field);

                Expression returnExpression = field.FieldType.IsValueType
                    ? Expression.Convert(fieldExpression, typeof(object))
                    : fieldExpression;

                return Expression.Lambda<Func<object>>(returnExpression).Compile();
            }
            catch
            {
                return () => field.GetValue(null);
            }
        }

        private static Action<object, object> CreateCompiledFieldSetter(FieldInfo field)
        {
            if (!ExpressionsEnabled)
            {
                return field.SetValue;
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

                Expression instanceExpression = field.DeclaringType.IsValueType
                    ? Expression.Unbox(instanceParam, field.DeclaringType)
                    : Expression.Convert(instanceParam, field.DeclaringType);

                Expression valueExpression = field.FieldType.IsValueType
                    ? Expression.Unbox(valueParam, field.FieldType)
                    : Expression.Convert(valueParam, field.FieldType);

                Expression assignExpression = Expression.Assign(
                    Expression.Field(instanceExpression, field),
                    valueExpression
                );

                return Expression
                    .Lambda<Action<object, object>>(assignExpression, instanceParam, valueParam)
                    .Compile();
            }
            catch
            {
                return field.SetValue;
            }
        }

        private static Action<object> CreateCompiledStaticFieldSetter(FieldInfo field)
        {
            if (!ExpressionsEnabled)
            {
                return value => field.SetValue(null, value);
            }

            try
            {
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

                Expression valueExpression = field.FieldType.IsValueType
                    ? Expression.Unbox(valueParam, field.FieldType)
                    : Expression.Convert(valueParam, field.FieldType);

                Expression assignExpression = Expression.Assign(
                    Expression.Field(null, field),
                    valueExpression
                );

                return Expression.Lambda<Action<object>>(assignExpression, valueParam).Compile();
            }
            catch
            {
                return value => field.SetValue(null, value);
            }
        }

        private static Func<object, object[], object> CreateDelegateMethodInvoker(MethodInfo method)
        {
            try
            {
                // For IL2CPP/WebGL, focus on simple optimizations that avoid DynamicInvoke
                // which can be slower than direct reflection in some cases

                ParameterInfo[] parameters = method.GetParameters();

                // Only optimize very simple cases to avoid DynamicInvoke overhead
                if (parameters.Length == 0 && method.IsStatic)
                {
                    if (method.ReturnType == typeof(void))
                    {
                        Action staticAction = (Action)
                            Delegate.CreateDelegate(typeof(Action), method);
                        return (instance, args) =>
                        {
                            staticAction();
                            return null;
                        };
                    }

                    if (method.ReturnType == typeof(int))
                    {
                        Func<int> staticFunc =
                            (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), method);
                        return (instance, args) => staticFunc();
                    }

                    if (method.ReturnType == typeof(string))
                    {
                        Func<string> staticFunc =
                            (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), method);
                        return (instance, args) => staticFunc();
                    }

                    if (method.ReturnType == typeof(bool))
                    {
                        Func<bool> staticFunc =
                            (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method);
                        return (instance, args) => staticFunc();
                    }
                }

                // For most other cases, direct reflection is often faster than DynamicInvoke
                return null;
            }
            catch
            {
                return null; // Fallback to reflection
            }
        }

        private static Func<object[], object> CreateDelegateStaticMethodInvoker(MethodInfo method)
        {
            try
            {
                ParameterInfo[] parameters = method.GetParameters();

                // Only optimize simple static methods with no parameters to avoid DynamicInvoke
                if (parameters.Length == 0)
                {
                    if (method.ReturnType == typeof(void))
                    {
                        Action action = (Action)Delegate.CreateDelegate(typeof(Action), method);
                        return args =>
                        {
                            action();
                            return null;
                        };
                    }

                    if (method.ReturnType == typeof(int))
                    {
                        Func<int> func =
                            (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), method);
                        return args => func();
                    }

                    if (method.ReturnType == typeof(string))
                    {
                        Func<string> func =
                            (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), method);
                        return args => func();
                    }

                    if (method.ReturnType == typeof(bool))
                    {
                        Func<bool> func =
                            (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method);
                        return args => func();
                    }
                }

                // For other cases, reflection is often faster than DynamicInvoke
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static Func<object[], object> CreateDelegateConstructor(ConstructorInfo constructor)
        {
            try
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                Type declaringType = constructor.DeclaringType;

                // For constructors, we can use Activator.CreateInstance with optimizations
                // or create wrapper delegates that call the constructor

                if (parameters.Length == 0)
                {
                    // Use cached Activator.CreateInstance for parameterless constructors
                    return args => Activator.CreateInstance(declaringType);
                }

                if (parameters.Length == 1)
                {
                    Type paramType = parameters[0].ParameterType;
                    return args => Activator.CreateInstance(declaringType, args[0]);
                }

                if (parameters.Length == 2)
                {
                    return args => Activator.CreateInstance(declaringType, args[0], args[1]);
                }

                if (parameters.Length <= 4)
                {
                    // For up to 4 parameters, use Activator.CreateInstance which is reasonably fast
                    return args => Activator.CreateInstance(declaringType, args);
                }

                // For more complex constructors, fallback to reflection
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static Func<T> CreateDelegateParameterlessConstructor<T>(
            ConstructorInfo constructor
        )
        {
            try
            {
                // For parameterless constructors, we can use optimized Activator.CreateInstance
                if (constructor.GetParameters().Length == 0)
                {
                    return () => (T)Activator.CreateInstance(typeof(T));
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static Func<object> CreateDelegateParameterlessConstructor(
            ConstructorInfo constructor
        )
        {
            try
            {
                if (constructor.GetParameters().Length == 0)
                {
                    return () => constructor.Invoke(null);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
