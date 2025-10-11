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
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
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
    public static class ReflectionHelpers
    {
#if SINGLE_THREADED
        private static readonly Dictionary<Type, Func<int, Array>> ArrayCreators = new();
        private static readonly Dictionary<Type, Func<IList>> ListCreators = new();
        private static readonly Dictionary<Type, Func<int, IList>> ListWithCapacityCreators = new();
        private static readonly Dictionary<Type, Func<int, object>> HashSetWithCapacityCreators =
            new();
        private static readonly Dictionary<FieldInfo, Func<object, object>> FieldGetterCache =
            new();
        private static readonly Dictionary<FieldInfo, Action<object, object>> FieldSetterCache =
            new();
        private static readonly Dictionary<FieldInfo, Func<object>> StaticFieldGetterCache = new();
        private static readonly Dictionary<FieldInfo, Action<object>> StaticFieldSetterCache =
            new();
        private static readonly Dictionary<PropertyInfo, Func<object, object>> PropertyGetterCache =
            new();
        private static readonly Dictionary<
            PropertyInfo,
            Action<object, object>
        > PropertySetterCache = new();
        private static readonly Dictionary<
            MethodInfo,
            Func<object, object[], object>
        > MethodInvokers = new();
        private static readonly Dictionary<
            MethodInfo,
            Func<object[], object>
        > StaticMethodInvokers = new();
        private static readonly Dictionary<ConstructorInfo, Func<object[], object>> Constructors =
            new();
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
        private static readonly ConcurrentDictionary<
            FieldInfo,
            Func<object, object>
        > FieldGetterCache = new();
        private static readonly ConcurrentDictionary<
            FieldInfo,
            Action<object, object>
        > FieldSetterCache = new();
        private static readonly ConcurrentDictionary<
            FieldInfo,
            Func<object>
        > StaticFieldGetterCache = new();
        private static readonly ConcurrentDictionary<
            FieldInfo,
            Action<object>
        > StaticFieldSetterCache = new();
        private static readonly ConcurrentDictionary<
            PropertyInfo,
            Func<object, object>
        > PropertyGetterCache = new();
        private static readonly ConcurrentDictionary<
            PropertyInfo,
            Action<object, object>
        > PropertySetterCache = new();
        private static readonly ConcurrentDictionary<
            MethodInfo,
            Func<object, object[], object>
        > MethodInvokers = new();
        private static readonly ConcurrentDictionary<
            MethodInfo,
            Func<object[], object>
        > StaticMethodInvokers = new();
        private static readonly ConcurrentDictionary<
            ConstructorInfo,
            Func<object[], object>
        > Constructors = new();

        // Cache for typed static method invokers with 0-4 parameters to avoid object[] allocations
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticInvoker2 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticInvoker0 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticInvoker1 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticInvoker3 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticInvoker4 =
            new();

        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticAction0 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticAction1 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticAction2 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticAction3 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedStaticAction4 =
            new();

        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceInvoker0 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceInvoker1 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceInvoker2 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceInvoker3 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceInvoker4 =
            new();

        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceAction0 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceAction1 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceAction2 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceAction3 =
            new();
        private static readonly ConcurrentDictionary<MethodInfo, Delegate> TypedInstanceAction4 =
            new();
#endif

        private static readonly bool CanCompileExpressions = CheckExpressionCompilationSupport();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Creates a new array instance of element <paramref name="type"/> with the specified length.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// Array ints = ReflectionHelpers.CreateArray(typeof(int), 16); // int[16]
        /// ]]></code>
        /// </example>
        public static Array CreateArray(Type type, int length)
        {
            return ArrayCreators
                // ReSharper disable once ConvertClosureToMethodGroup
                .GetOrAdd(type, elementType => GetArrayCreator(elementType))
                .Invoke(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Creates a new <see cref="List{T}"/> instance for <paramref name="elementType"/> with the specified capacity.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// IList list = ReflectionHelpers.CreateList(typeof(string), 128); // List<string> with Capacity=128
        /// ]]></code>
        /// </example>
        public static IList CreateList(Type elementType, int length)
        {
            return ListWithCapacityCreators
                // ReSharper disable once ConvertClosureToMethodGroup
                .GetOrAdd(elementType, type => GetListWithCapacityCreator(type))
                .Invoke(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Creates a new <see cref="List{T}"/> instance for <paramref name="elementType"/>.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// IList list = ReflectionHelpers.CreateList(typeof(UnityEngine.Vector3));
        /// ]]></code>
        /// </example>
        public static IList CreateList(Type elementType)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return ListCreators.GetOrAdd(elementType, type => GetListCreator(type)).Invoke();
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
            return FieldGetterCache.GetOrAdd(field, f =>
#if !EMIT_DYNAMIC_IL
                    CreateCompiledFieldGetter(f)
#else
                    BuildFieldGetterIL(f)
#endif
            );
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
            return PropertyGetterCache.GetOrAdd(property, p =>
#if !EMIT_DYNAMIC_IL
                    CreateCompiledPropertyGetter(p)
#else
                    BuildPropertyGetterIL(p)
#endif
            );
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
#if !EMIT_DYNAMIC_IL
            return Getter;
            TValue Getter(TInstance instance)
            {
                return (TValue)property.GetValue(instance);
            }
#else
            MethodInfo getMethod = property.GetGetMethod(true);
            if (getMethod == null)
            {
                throw new ArgumentException(
                    $"Property {property.Name} has no getter",
                    nameof(property)
                );
            }

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

            if (property.PropertyType.IsValueType)
            {
                if (!typeof(TValue).IsValueType)
                {
                    il.Emit(OpCodes.Box, property.PropertyType);
                }
            }
            else
            {
                if (typeof(TValue).IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, typeof(TValue));
                }
                else if (typeof(TValue) != property.PropertyType)
                {
                    il.Emit(OpCodes.Castclass, typeof(TValue));
                }
            }

            il.Emit(OpCodes.Ret);
            return (Func<TInstance, TValue>)
                dynamicMethod.CreateDelegate(typeof(Func<TInstance, TValue>));
#endif
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
            return PropertySetterCache.GetOrAdd(property, p =>
#if !EMIT_DYNAMIC_IL
                    ((obj, value) => p.SetValue(obj, value))
#else
                    BuildPropertySetterIL(p)
#endif
            );
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
#if !EMIT_DYNAMIC_IL
            return (instance, value) => property.SetValue(instance, value);
#else
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
                if (property.PropertyType != typeof(TValue))
                {
                    il.Emit(
                        property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                        property.PropertyType
                    );
                }
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
                if (property.PropertyType != typeof(TValue))
                {
                    il.Emit(
                        property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                        property.PropertyType
                    );
                }

                il.Emit(
                    property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt,
                    setMethod
                );
            }
            il.Emit(OpCodes.Ret);
            return (Action<TInstance, TValue>)
                dynamicMethod.CreateDelegate(typeof(Action<TInstance, TValue>));
#endif
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
#if !EMIT_DYNAMIC_IL
            return value => property.SetValue(null, value);
#else
            DynamicMethod dynamicMethod = new(
                $"SetStaticProperty_{property.DeclaringType.Name}_{property.Name}",
                typeof(void),
                new[] { typeof(TValue) },
                property.DeclaringType,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (property.PropertyType != typeof(TValue))
            {
                il.Emit(
                    property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                    property.PropertyType
                );
            }
            il.Emit(OpCodes.Call, setMethod);
            il.Emit(OpCodes.Ret);
            return (Action<TValue>)dynamicMethod.CreateDelegate(typeof(Action<TValue>));
#endif
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
#if !EMIT_DYNAMIC_IL
            if (!CanCompileExpressions)
            {
                return (instance, indexArgs) => property.GetValue(instance, indexArgs);
            }
            var inst = Expression.Parameter(typeof(object), "instance");
            var args = Expression.Parameter(typeof(object[]), "args");
            Expression target = property.DeclaringType.IsValueType
                ? Expression.Unbox(inst, property.DeclaringType)
                : Expression.Convert(inst, property.DeclaringType);
            var indexExprs = indices
                .Select((p, i) =>
                    p.ParameterType.IsValueType
                        ? Expression.Unbox(Expression.ArrayIndex(args, Expression.Constant(i)), p.ParameterType)
                        : Expression.Convert(
                            Expression.ArrayIndex(args, Expression.Constant(i)),
                            p.ParameterType
                        )
                )
                .ToArray();
            Expression call = Expression.Property(target, property, indexExprs);
            Expression ret = property.PropertyType.IsValueType
                ? Expression.Convert(call, typeof(object))
                : call;
            return Expression
                .Lambda<Func<object, object[], object>>(ret, inst, args)
                .Compile();
#else
            MethodInfo getter = property.GetGetMethod(true);
            if (getter == null)
            {
                throw new ArgumentException("Indexer has no getter", nameof(property));
            }
            DynamicMethod dm = new(
                $"GetIndexer_{property.DeclaringType.Name}_{property.Name}",
                typeof(object),
                new[] { typeof(object), typeof(object[]) },
                property.DeclaringType,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(
                property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                property.DeclaringType
            );
            for (int i = 0; i < indices.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                Type pt = indices[i].ParameterType;
                il.Emit(pt.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, pt);
            }
            il.Emit(property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, getter);
            if (property.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Box, property.PropertyType);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object, object[], object>)
                dm.CreateDelegate(typeof(Func<object, object[], object>));
#endif
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
#if !EMIT_DYNAMIC_IL
            if (!CanCompileExpressions)
            {
                return (instance, value, indexArgs) => property.SetValue(instance, value, indexArgs);
            }
            var inst = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");
            var args = Expression.Parameter(typeof(object[]), "args");
            Expression target = property.DeclaringType.IsValueType
                ? Expression.Unbox(inst, property.DeclaringType)
                : Expression.Convert(inst, property.DeclaringType);
            var indexExprs = indices
                .Select((p, i) =>
                    p.ParameterType.IsValueType
                        ? Expression.Unbox(Expression.ArrayIndex(args, Expression.Constant(i)), p.ParameterType)
                        : Expression.Convert(
                            Expression.ArrayIndex(args, Expression.Constant(i)),
                            p.ParameterType
                        )
                )
                .ToArray();
            Expression valExpr = property.PropertyType.IsValueType
                ? Expression.Unbox(value, property.PropertyType)
                : Expression.Convert(value, property.PropertyType);
            Expression call = Expression.Call(target, setter, indexExprs.Append(valExpr));
            return Expression
                .Lambda<Action<object, object, object[]>>(call, inst, value, args)
                .Compile();
#else
            DynamicMethod dm = new(
                $"SetIndexer_{property.DeclaringType.Name}_{property.Name}",
                typeof(void),
                new[] { typeof(object), typeof(object), typeof(object[]) },
                property.DeclaringType,
                true
            );
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(
                property.DeclaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass,
                property.DeclaringType
            );
            for (int i = 0; i < indices.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                Type pt = indices[i].ParameterType;
                il.Emit(pt.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, pt);
            }
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(
                property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                property.PropertyType
            );
            il.Emit(property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, setter);
            il.Emit(OpCodes.Ret);
            return (Action<object, object, object[]>)
                dm.CreateDelegate(typeof(Action<object, object, object[]>));
#endif
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
#if !EMIT_DYNAMIC_IL
            return () => Activator.CreateInstance(type);
#else
            DynamicMethod dynamicMethod = new(
                $"New_{type.Name}",
                typeof(object),
                Type.EmptyTypes,
                type,
                true
            );
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
#endif
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
            return StaticFieldGetterCache.GetOrAdd(field, f =>
#if !EMIT_DYNAMIC_IL
                    CreateCompiledStaticFieldGetter(f)
#else
                    BuildStaticFieldGetterIL(f)
#endif
            );
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
#if !EMIT_DYNAMIC_IL
            return Getter;
            TValue Getter(TInstance instance)
            {
                return (TValue)field.GetValue(instance);
            }
#else
            DynamicMethod dynamicMethod = new(
                $"GetGeneric{field.DeclaringType.Name}_{field.Name}",
                typeof(TValue),
                new[] { typeof(TInstance) },
                field.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            if (!field.IsStatic)
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
            else
            {
                il.Emit(OpCodes.Ldsfld, field);
            }

            if (field.FieldType.IsValueType)
            {
                if (!typeof(TValue).IsValueType)
                {
                    il.Emit(OpCodes.Box, field.FieldType);
                }
            }
            else
            {
                if (typeof(TValue).IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, typeof(TValue));
                }
                else if (typeof(TValue) != field.FieldType)
                {
                    il.Emit(OpCodes.Castclass, typeof(TValue));
                }
            }

            il.Emit(OpCodes.Ret);
            return (Func<TInstance, TValue>)
                dynamicMethod.CreateDelegate(typeof(Func<TInstance, TValue>));
#endif
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
            MethodInfo getMethod = property.GetGetMethod(true);
#if !EMIT_DYNAMIC_IL
            return Getter;
            TValue Getter()
            {
                // Use null for instance, null for indexer args for static properties
                return (TValue)property.GetValue(null, null);
            }
#else
            DynamicMethod dynamicMethod = new(
                $"GetStatic_{property.DeclaringType.Name}_{property.Name}",
                typeof(TValue),
                Type.EmptyTypes,
                property.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Call, getMethod);

            Type actualType = property.PropertyType;
            Type targetType = typeof(TValue);

            if (actualType != targetType)
            {
                if (actualType.IsValueType)
                {
                    il.Emit(OpCodes.Box, actualType);
                    if (targetType != typeof(object))
                    {
                        il.Emit(OpCodes.Castclass, targetType);
                    }
                }
                else
                {
                    il.Emit(
                        targetType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                        targetType
                    );
                }
            }

            il.Emit(OpCodes.Ret);
            return (Func<TValue>)dynamicMethod.CreateDelegate(typeof(Func<TValue>));
#endif
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

#if !EMIT_DYNAMIC_IL
            return Getter;
            TValue Getter()
            {
                return (TValue)field.GetValue(null);
            }
#else
            DynamicMethod dynamicMethod = new(
                $"GetStatic_{field.DeclaringType.Name}_{field.Name}",
                typeof(TValue),
                Type.EmptyTypes,
                field.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldsfld, field);

            Type actualType = field.FieldType;
            Type targetType = typeof(TValue);

            if (actualType != targetType)
            {
                if (actualType.IsValueType)
                {
                    il.Emit(OpCodes.Box, actualType);
                    if (targetType != typeof(object))
                    {
                        il.Emit(OpCodes.Castclass, targetType);
                    }
                }
                else
                {
                    il.Emit(
                        targetType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                        targetType
                    );
                }
            }

            il.Emit(OpCodes.Ret);
            return (Func<TValue>)dynamicMethod.CreateDelegate(typeof(Func<TValue>));
#endif
        }

        /// <summary>
        /// Builds a strongly-typed field setter for instance fields.
        /// </summary>
        public static FieldSetter<TInstance, TValue> GetFieldSetter<TInstance, TValue>(
            FieldInfo field
        )
        {
#if !EMIT_DYNAMIC_IL
            return Setter;
            void Setter(ref TInstance instance, TValue newValue)
            {
                object value = instance;
                field.SetValue(value, newValue);
                instance = (TInstance)value;
            }
#else
            Type instanceType = field.DeclaringType;
            Type valueType = field.FieldType;

            DynamicMethod dynamicMethod = new(
                $"SetFieldGeneric{field.DeclaringType.Name}_{field.Name}",
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(void),
                new[] { instanceType.MakeByRefType(), valueType },
                field.Module,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (!instanceType.IsValueType)
            {
                il.Emit(OpCodes.Ldind_Ref);
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            Type delegateType = typeof(FieldSetter<,>).MakeGenericType(instanceType, valueType);
            return (FieldSetter<TInstance, TValue>)dynamicMethod.CreateDelegate(delegateType);
#endif
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
#if !EMIT_DYNAMIC_IL
            return Setter;
            void Setter(TValue newValue)
            {
                field.SetValue(null, newValue);
            }
#else
            DynamicMethod dynamicMethod = new(
                $"SetFieldGenericStatic{field.DeclaringType.Name}_{field.Name}",
                typeof(void),
                new[] { typeof(TValue) },
                field.Module,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);

            return (Action<TValue>)dynamicMethod.CreateDelegate(typeof(Action<TValue>));
#endif
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
            return FieldSetterCache.GetOrAdd(field, f =>
#if !EMIT_DYNAMIC_IL
                    CreateCompiledFieldSetter(f)
#else
                    BuildFieldSetterIL(f)
#endif
            );
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
            return StaticFieldSetterCache.GetOrAdd(field, f =>
#if !EMIT_DYNAMIC_IL
                    CreateCompiledStaticFieldSetter(f)
#else
                    BuildStaticFieldSetterIL(f)
#endif
            );
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
            return size => Array.CreateInstance(elementType, size);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        public static object CreateHashSet(Type elementType, int capacity)
        {
            return HashSetWithCapacityCreators
                // ReSharper disable once ConvertClosureToMethodGroup
                .GetOrAdd(elementType, type => GetHashSetWithCapacityCreator(type))
                .Invoke(capacity);
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
                ?? ((set, value) => addMethod.Invoke(set, new[] { value }));
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

        /// <summary>
        /// Invokes an instance method using a cached invoker; avoids per-call reflection overhead.
        /// </summary>
        /// <param name="method">The instance method to invoke.</param>
        /// <param name="instance">The target instance.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>The return value from the method, or null for void.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Invokes an instance method using a cached delegate when possible.
        /// </summary>
        /// <param name="method">Instance method to invoke.</param>
        /// <param name="instance">Target instance.</param>
        /// <param name="parameters">Method parameters (optional).</param>
        /// <returns>Boxed return value or null for void methods.</returns>
        public static object InvokeMethod(
            MethodInfo method,
            object instance,
            params object[] parameters
        )
        {
            return MethodInvokers
                .GetOrAdd(method, methodInfo => GetMethodInvoker(methodInfo))
                .Invoke(instance, parameters);
        }

        /// <summary>
        /// Invokes a static method using a cached invoker; avoids per-call reflection overhead.
        /// </summary>
        /// <param name="method">The static method to invoke.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>The return value from the method, or null for void.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Invokes a static method using a cached delegate when possible.
        /// </summary>
        /// <param name="method">Static method.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>Boxed return value or null for void methods.</returns>
        public static object InvokeStaticMethod(MethodInfo method, params object[] parameters)
        {
            return StaticMethodInvokers
                .GetOrAdd(method, methodInfo => GetStaticMethodInvoker(methodInfo))
                .Invoke(parameters);
        }

        /// <summary>
        /// Constructs an instance using a cached constructor invoker.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Creates an instance using a constructor via a cached delegate.
        /// </summary>
        /// <param name="constructor">Constructor to invoke.</param>
        /// <param name="parameters">Constructor parameters.</param>
        /// <returns>Created instance.</returns>
        public static object CreateInstance(ConstructorInfo constructor, params object[] parameters)
        {
            return Constructors
                .GetOrAdd(constructor, ctor => GetConstructor(ctor))
                .Invoke(parameters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Constructs an instance using a cached constructor invoker and returns it as T.
        /// </summary>
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

#if !EMIT_DYNAMIC_IL
            return CreateCompiledMethodInvoker(method);
#else
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
#endif
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

#if !EMIT_DYNAMIC_IL
            return CreateCompiledStaticMethodInvoker(method);
#else
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
#endif
        }

        /// <summary>
        /// Gets (or caches) a fast constructor invoker delegate that accepts an object[] of arguments.
        /// </summary>
        public static Func<object[], object> GetConstructor(ConstructorInfo constructor)
        {
#if !EMIT_DYNAMIC_IL
            return CreateCompiledConstructor(constructor);
#else
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
#endif
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

#if SINGLE_THREADED
            if (!TypedStaticInvoker2.TryGetValue(method, out Delegate del))
            {
                del = BuildTypedStaticInvoker2<T1, T2, TReturn>(method);
                TypedStaticInvoker2[method] = del;
            }
            return (Func<T1, T2, TReturn>)del;
#else
            Delegate del = TypedStaticInvoker2.GetOrAdd(
                method,
                m => BuildTypedStaticInvoker2<T1, T2, TReturn>(m)
            );
            return (Func<T1, T2, TReturn>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedStaticInvoker0.TryGetValue(method, out Delegate del))
            {
                del = BuildTypedStaticInvoker0<TReturn>(method);
                TypedStaticInvoker0[method] = del;
            }
            return (Func<TReturn>)del;
#else
            Delegate del = TypedStaticInvoker0.GetOrAdd(
                method,
                m => BuildTypedStaticInvoker0<TReturn>(m)
            );
            return (Func<TReturn>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedStaticInvoker1.TryGetValue(method, out Delegate del))
            {
                del = BuildTypedStaticInvoker1<T1, TReturn>(method);
                TypedStaticInvoker1[method] = del;
            }
            return (Func<T1, TReturn>)del;
#else
            Delegate del = TypedStaticInvoker1.GetOrAdd(
                method,
                m => BuildTypedStaticInvoker1<T1, TReturn>(m)
            );
            return (Func<T1, TReturn>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedStaticInvoker3.TryGetValue(method, out Delegate del))
            {
                del = BuildTypedStaticInvoker3<T1, T2, T3, TReturn>(method);
                TypedStaticInvoker3[method] = del;
            }
            return (Func<T1, T2, T3, TReturn>)del;
#else
            Delegate del = TypedStaticInvoker3.GetOrAdd(
                method,
                m => BuildTypedStaticInvoker3<T1, T2, T3, TReturn>(m)
            );
            return (Func<T1, T2, T3, TReturn>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedStaticInvoker4.TryGetValue(method, out Delegate del))
            {
                del = BuildTypedStaticInvoker4<T1, T2, T3, T4, TReturn>(method);
                TypedStaticInvoker4[method] = del;
            }
            return (Func<T1, T2, T3, T4, TReturn>)del;
#else
            Delegate del = TypedStaticInvoker4.GetOrAdd(
                method,
                m => BuildTypedStaticInvoker4<T1, T2, T3, T4, TReturn>(m)
            );
            return (Func<T1, T2, T3, T4, TReturn>)del;
#endif
        }

        public static Action GetStaticActionInvoker(MethodInfo method)
        {
            ValidateStaticActionSignature(method);
#if SINGLE_THREADED
            if (!TypedStaticAction0.TryGetValue(method, out Delegate del))
            {
                del = BuildStaticActionInvoker0(method);
                TypedStaticAction0[method] = del;
            }
            return (Action)del;
#else
            Delegate del = TypedStaticAction0.GetOrAdd(method, m => BuildStaticActionInvoker0(m));
            return (Action)del;
#endif
        }

        public static Action<T1> GetStaticActionInvoker<T1>(MethodInfo method)
        {
            ValidateStaticActionSignature(method, typeof(T1));
#if SINGLE_THREADED
            if (!TypedStaticAction1.TryGetValue(method, out Delegate del))
            {
                del = BuildStaticActionInvoker1<T1>(method);
                TypedStaticAction1[method] = del;
            }
            return (Action<T1>)del;
#else
            Delegate del = TypedStaticAction1.GetOrAdd(
                method,
                m => BuildStaticActionInvoker1<T1>(m)
            );
            return (Action<T1>)del;
#endif
        }

        public static Action<T1, T2> GetStaticActionInvoker<T1, T2>(MethodInfo method)
        {
            ValidateStaticActionSignature(method, typeof(T1), typeof(T2));
#if SINGLE_THREADED
            if (!TypedStaticAction2.TryGetValue(method, out Delegate del))
            {
                del = BuildStaticActionInvoker2<T1, T2>(method);
                TypedStaticAction2[method] = del;
            }
            return (Action<T1, T2>)del;
#else
            Delegate del = TypedStaticAction2.GetOrAdd(
                method,
                m => BuildStaticActionInvoker2<T1, T2>(m)
            );
            return (Action<T1, T2>)del;
#endif
        }

        public static Action<T1, T2, T3> GetStaticActionInvoker<T1, T2, T3>(MethodInfo method)
        {
            ValidateStaticActionSignature(method, typeof(T1), typeof(T2), typeof(T3));
#if SINGLE_THREADED
            if (!TypedStaticAction3.TryGetValue(method, out Delegate del))
            {
                del = BuildStaticActionInvoker3<T1, T2, T3>(method);
                TypedStaticAction3[method] = del;
            }
            return (Action<T1, T2, T3>)del;
#else
            Delegate del = TypedStaticAction3.GetOrAdd(
                method,
                m => BuildStaticActionInvoker3<T1, T2, T3>(m)
            );
            return (Action<T1, T2, T3>)del;
#endif
        }

        public static Action<T1, T2, T3, T4> GetStaticActionInvoker<T1, T2, T3, T4>(
            MethodInfo method
        )
        {
            ValidateStaticActionSignature(method, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
#if SINGLE_THREADED
            if (!TypedStaticAction4.TryGetValue(method, out Delegate del))
            {
                del = BuildStaticActionInvoker4<T1, T2, T3, T4>(method);
                TypedStaticAction4[method] = del;
            }
            return (Action<T1, T2, T3, T4>)del;
#else
            Delegate del = TypedStaticAction4.GetOrAdd(
                method,
                m => BuildStaticActionInvoker4<T1, T2, T3, T4>(m)
            );
            return (Action<T1, T2, T3, T4>)del;
#endif
        }

        public static Func<TInstance, TReturn> GetInstanceMethodInvoker<TInstance, TReturn>(
            MethodInfo method
        )
        {
            ValidateInstanceFuncSignature<TInstance, TReturn>(method, Type.EmptyTypes);
#if SINGLE_THREADED
            if (!TypedInstanceInvoker0.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceInvoker0<TInstance, TReturn>(method);
                TypedInstanceInvoker0[method] = del;
            }
            return (Func<TInstance, TReturn>)del;
#else
            Delegate del = TypedInstanceInvoker0.GetOrAdd(
                method,
                m => BuildInstanceInvoker0<TInstance, TReturn>(m)
            );
            return (Func<TInstance, TReturn>)del;
#endif
        }

        public static Func<TInstance, T1, TReturn> GetInstanceMethodInvoker<TInstance, T1, TReturn>(
            MethodInfo method
        )
        {
            ValidateInstanceFuncSignature<TInstance, TReturn>(method, new[] { typeof(T1) });
#if SINGLE_THREADED
            if (!TypedInstanceInvoker1.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceInvoker1<TInstance, T1, TReturn>(method);
                TypedInstanceInvoker1[method] = del;
            }
            return (Func<TInstance, T1, TReturn>)del;
#else
            Delegate del = TypedInstanceInvoker1.GetOrAdd(
                method,
                m => BuildInstanceInvoker1<TInstance, T1, TReturn>(m)
            );
            return (Func<TInstance, T1, TReturn>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedInstanceInvoker2.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceInvoker2<TInstance, T1, T2, TReturn>(method);
                TypedInstanceInvoker2[method] = del;
            }
            return (Func<TInstance, T1, T2, TReturn>)del;
#else
            Delegate del = TypedInstanceInvoker2.GetOrAdd(
                method,
                m => BuildInstanceInvoker2<TInstance, T1, T2, TReturn>(m)
            );
            return (Func<TInstance, T1, T2, TReturn>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedInstanceInvoker3.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceInvoker3<TInstance, T1, T2, T3, TReturn>(method);
                TypedInstanceInvoker3[method] = del;
            }
            return (Func<TInstance, T1, T2, T3, TReturn>)del;
#else
            Delegate del = TypedInstanceInvoker3.GetOrAdd(
                method,
                m => BuildInstanceInvoker3<TInstance, T1, T2, T3, TReturn>(m)
            );
            return (Func<TInstance, T1, T2, T3, TReturn>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedInstanceInvoker4.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceInvoker4<TInstance, T1, T2, T3, T4, TReturn>(method);
                TypedInstanceInvoker4[method] = del;
            }
            return (Func<TInstance, T1, T2, T3, T4, TReturn>)del;
#else
            Delegate del = TypedInstanceInvoker4.GetOrAdd(
                method,
                m => BuildInstanceInvoker4<TInstance, T1, T2, T3, T4, TReturn>(m)
            );
            return (Func<TInstance, T1, T2, T3, T4, TReturn>)del;
#endif
        }

        public static Action<TInstance> GetInstanceActionInvoker<TInstance>(MethodInfo method)
        {
            ValidateInstanceActionSignature<TInstance>(method, Type.EmptyTypes);
#if SINGLE_THREADED
            if (!TypedInstanceAction0.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceActionInvoker0<TInstance>(method);
                TypedInstanceAction0[method] = del;
            }
            return (Action<TInstance>)del;
#else
            Delegate del = TypedInstanceAction0.GetOrAdd(
                method,
                m => BuildInstanceActionInvoker0<TInstance>(m)
            );
            return (Action<TInstance>)del;
#endif
        }

        public static Action<TInstance, T1> GetInstanceActionInvoker<TInstance, T1>(
            MethodInfo method
        )
        {
            ValidateInstanceActionSignature<TInstance>(method, new[] { typeof(T1) });
#if SINGLE_THREADED
            if (!TypedInstanceAction1.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceActionInvoker1<TInstance, T1>(method);
                TypedInstanceAction1[method] = del;
            }
            return (Action<TInstance, T1>)del;
#else
            Delegate del = TypedInstanceAction1.GetOrAdd(
                method,
                m => BuildInstanceActionInvoker1<TInstance, T1>(m)
            );
            return (Action<TInstance, T1>)del;
#endif
        }

        public static Action<TInstance, T1, T2> GetInstanceActionInvoker<TInstance, T1, T2>(
            MethodInfo method
        )
        {
            ValidateInstanceActionSignature<TInstance>(method, new[] { typeof(T1), typeof(T2) });
#if SINGLE_THREADED
            if (!TypedInstanceAction2.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceActionInvoker2<TInstance, T1, T2>(method);
                TypedInstanceAction2[method] = del;
            }
            return (Action<TInstance, T1, T2>)del;
#else
            Delegate del = TypedInstanceAction2.GetOrAdd(
                method,
                m => BuildInstanceActionInvoker2<TInstance, T1, T2>(m)
            );
            return (Action<TInstance, T1, T2>)del;
#endif
        }

        public static Action<TInstance, T1, T2, T3> GetInstanceActionInvoker<TInstance, T1, T2, T3>(
            MethodInfo method
        )
        {
            ValidateInstanceActionSignature<TInstance>(
                method,
                new[] { typeof(T1), typeof(T2), typeof(T3) }
            );
#if SINGLE_THREADED
            if (!TypedInstanceAction3.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceActionInvoker3<TInstance, T1, T2, T3>(method);
                TypedInstanceAction3[method] = del;
            }
            return (Action<TInstance, T1, T2, T3>)del;
#else
            Delegate del = TypedInstanceAction3.GetOrAdd(
                method,
                m => BuildInstanceActionInvoker3<TInstance, T1, T2, T3>(m)
            );
            return (Action<TInstance, T1, T2, T3>)del;
#endif
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
#if SINGLE_THREADED
            if (!TypedInstanceAction4.TryGetValue(method, out Delegate del))
            {
                del = BuildInstanceActionInvoker4<TInstance, T1, T2, T3, T4>(method);
                TypedInstanceAction4[method] = del;
            }
            return (Action<TInstance, T1, T2, T3, T4>)del;
#else
            Delegate del = TypedInstanceAction4.GetOrAdd(
                method,
                m => BuildInstanceActionInvoker4<TInstance, T1, T2, T3, T4>(m)
            );
            return (Action<TInstance, T1, T2, T3, T4>)del;
#endif
        }

        private static Delegate BuildTypedStaticInvoker2<T1, T2, TReturn>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Func<T1, T2, TReturn>));
            }
            catch
            {
                // Fallback to expression compilation
                var p1 = Expression.Parameter(typeof(T1), "a");
                var p2 = Expression.Parameter(typeof(T2), "b");
                MethodCallExpression call = Expression.Call(method, p1, p2);
                return Expression
                    .Lambda<Func<T1, T2, TReturn>>(call, p1, p2)
                    .Compile();
            }
#else
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
#endif
        }

        private static Delegate BuildTypedStaticInvoker0<TReturn>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Func<TReturn>));
            }
            catch
            {
                var call = Expression.Call(method);
                return Expression.Lambda<Func<TReturn>>(call).Compile();
            }
#else
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
#endif
        }

        private static Delegate BuildTypedStaticInvoker1<T1, TReturn>(MethodInfo method)
        {
#if !EMIT_DYNAMIC_IL
            try
            {
                return method.CreateDelegate(typeof(Func<T1, TReturn>));
            }
            catch
            {
                var a = Expression.Parameter(typeof(T1), "a");
                var call = Expression.Call(method, a);
                return Expression.Lambda<Func<T1, TReturn>>(call, a).Compile();
            }
#else
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
#endif
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
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var call = Expression.Call(method, a, b, c);
                return Expression
                    .Lambda<Func<T1, T2, T3, TReturn>>(call, a, b, c)
                    .Compile();
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
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var d = Expression.Parameter(typeof(T4), "d");
                var call = Expression.Call(method, a, b, c, d);
                return Expression
                    .Lambda<Func<T1, T2, T3, T4, TReturn>>(call, a, b, c, d)
                    .Compile();
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
            try { return method.CreateDelegate(typeof(Action)); }
            catch { var call = Expression.Call(method); return Expression.Lambda<Action>(call).Compile(); }
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
            try { return method.CreateDelegate(typeof(Action<T1>)); }
            catch
            {
                var a = Expression.Parameter(typeof(T1), "a");
                var call = Expression.Call(method, a);
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
            try { return method.CreateDelegate(typeof(Action<T1, T2>)); }
            catch
            {
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var call = Expression.Call(method, a, b);
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
            try { return method.CreateDelegate(typeof(Action<T1, T2, T3>)); }
            catch
            {
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var call = Expression.Call(method, a, b, c);
                return Expression
                    .Lambda<Action<T1, T2, T3>>(call, a, b, c)
                    .Compile();
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
            try { return method.CreateDelegate(typeof(Action<T1, T2, T3, T4>)); }
            catch
            {
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var d = Expression.Parameter(typeof(T4), "d");
                var call = Expression.Call(method, a, b, c, d);
                return Expression
                    .Lambda<Action<T1, T2, T3, T4>>(call, a, b, c, d)
                    .Compile();
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
            try { return method.CreateDelegate(typeof(Func<TInstance, TReturn>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var call = Expression.Call(inst, method);
                return Expression.Lambda<Func<TInstance, TReturn>>(call, inst).Compile();
            }
        }
        private static Delegate BuildInstanceInvoker1<TInstance, T1, TReturn>(MethodInfo method)
        {
            try { return method.CreateDelegate(typeof(Func<TInstance, T1, TReturn>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var call = Expression.Call(inst, method, a);
                return Expression.Lambda<Func<TInstance, T1, TReturn>>(call, inst, a).Compile();
            }
        }
        private static Delegate BuildInstanceInvoker2<TInstance, T1, T2, TReturn>(MethodInfo method)
        {
            try { return method.CreateDelegate(typeof(Func<TInstance, T1, T2, TReturn>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var call = Expression.Call(inst, method, a, b);
                return Expression
                    .Lambda<Func<TInstance, T1, T2, TReturn>>(call, inst, a, b)
                    .Compile();
            }
        }
        private static Delegate BuildInstanceInvoker3<TInstance, T1, T2, T3, TReturn>(
            MethodInfo method
        )
        {
            try { return method.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, TReturn>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var call = Expression.Call(inst, method, a, b, c);
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
                return method.CreateDelegate(
                    typeof(Func<TInstance, T1, T2, T3, T4, TReturn>)
                );
            }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var d = Expression.Parameter(typeof(T4), "d");
                var call = Expression.Call(inst, method, a, b, c, d);
                return Expression
                    .Lambda<Func<TInstance, T1, T2, T3, T4, TReturn>>(call, inst, a, b, c, d)
                    .Compile();
            }
        }

        private static Delegate BuildInstanceActionInvoker0<TInstance>(MethodInfo method)
        {
            try { return method.CreateDelegate(typeof(Action<TInstance>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var call = Expression.Call(inst, method);
                return Expression.Lambda<Action<TInstance>>(call, inst).Compile();
            }
        }
        private static Delegate BuildInstanceActionInvoker1<TInstance, T1>(MethodInfo method)
        {
            try { return method.CreateDelegate(typeof(Action<TInstance, T1>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var call = Expression.Call(inst, method, a);
                return Expression.Lambda<Action<TInstance, T1>>(call, inst, a).Compile();
            }
        }
        private static Delegate BuildInstanceActionInvoker2<TInstance, T1, T2>(MethodInfo method)
        {
            try { return method.CreateDelegate(typeof(Action<TInstance, T1, T2>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var call = Expression.Call(inst, method, a, b);
                return Expression.Lambda<Action<TInstance, T1, T2>>(call, inst, a, b).Compile();
            }
        }
        private static Delegate BuildInstanceActionInvoker3<TInstance, T1, T2, T3>(
            MethodInfo method
        )
        {
            try { return method.CreateDelegate(typeof(Action<TInstance, T1, T2, T3>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var call = Expression.Call(inst, method, a, b, c);
                return Expression
                    .Lambda<Action<TInstance, T1, T2, T3>>(call, inst, a, b, c)
                    .Compile();
            }
        }
        private static Delegate BuildInstanceActionInvoker4<TInstance, T1, T2, T3, T4>(
            MethodInfo method
        )
        {
            try { return method.CreateDelegate(typeof(Action<TInstance, T1, T2, T3, T4>)); }
            catch
            {
                var inst = Expression.Parameter(typeof(TInstance), "instance");
                var a = Expression.Parameter(typeof(T1), "a");
                var b = Expression.Parameter(typeof(T2), "b");
                var c = Expression.Parameter(typeof(T3), "c");
                var d = Expression.Parameter(typeof(T4), "d");
                var call = Expression.Call(inst, method, a, b, c, d);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldarg_S, (short)4);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldarg_S, (short)4);
            il.Emit(typeof(TInstance).IsValueType ? OpCodes.Call : OpCodes.Callvirt, method);
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

#if !EMIT_DYNAMIC_IL
            return CreateCompiledParameterlessConstructor<T>(constructor);
#else
            DynamicMethod dynamicMethod = new(
                $"CreateParameterless{type.Name}",
                typeof(T),
                Type.EmptyTypes,
                type,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);

            return (Func<T>)dynamicMethod.CreateDelegate(typeof(Func<T>));
#endif
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

#if !EMIT_DYNAMIC_IL
            return CreateCompiledGenericParameterlessConstructor<T>(constructedType, constructor);
#else
            DynamicMethod dynamicMethod = new(
                $"CreateGenericParameterless{constructedType.Name}",
                typeof(T),
                Type.EmptyTypes,
                constructedType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructor);
            if (constructedType.IsValueType && typeof(T) == typeof(object))
            {
                il.Emit(OpCodes.Box, constructedType);
            }
            il.Emit(OpCodes.Ret);

            return (Func<T>)dynamicMethod.CreateDelegate(typeof(Func<T>));
#endif
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
#else
        private static readonly ConcurrentDictionary<
            Type,
            Func<object, bool>
        > EnabledPropertyGetters = new();
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
            if (!CanCompileExpressions)
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
            Func<object, bool> enabledGetter = EnabledPropertyGetters.GetOrAdd(
                componentType,
                inputType => BuildEnabledPropertyGetter(inputType)
            );

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
            if (!CanCompileExpressions)
            {
                return null;
            }

            try
            {
                ParameterExpression setParam = Expression.Parameter(typeof(object), "hashSet");
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

                Expression castSet = Expression.Convert(setParam, hashSetType);
                Expression castValue = elementType.IsValueType
                    ? Expression.Convert(valueParam, elementType)
                    : Expression.Convert(valueParam, elementType);

                MethodCallExpression call = Expression.Call(castSet, addMethod, castValue);
                Expression body =
                    addMethod.ReturnType == typeof(void)
                        ? (Expression)call
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
            if (!CanCompileExpressions)
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
            if (!CanCompileExpressions)
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
            if (!CanCompileExpressions)
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

        private static Func<T> CreateCompiledParameterlessConstructor<T>(
            ConstructorInfo constructor
        )
        {
            if (!CanCompileExpressions)
            {
                return CreateDelegateParameterlessConstructor<T>(constructor)
                    ?? (() => (T)Activator.CreateInstance(typeof(T)));
            }

            try
            {
                Expression newExpression = Expression.New(constructor);
                return Expression.Lambda<Func<T>>(newExpression).Compile();
            }
            catch
            {
                return () => (T)Activator.CreateInstance(typeof(T));
            }
        }

        private static Func<T> CreateCompiledGenericParameterlessConstructor<T>(
            Type constructedType,
            ConstructorInfo constructor
        )
        {
            if (!CanCompileExpressions)
            {
                return () => (T)Activator.CreateInstance(constructedType);
            }

            try
            {
                Expression newExpression = Expression.New(constructor);
                Expression convertExpression =
                    constructedType.IsValueType && typeof(T) == typeof(object)
                        ? Expression.Convert(newExpression, typeof(object))
                        : newExpression;

                return Expression.Lambda<Func<T>>(convertExpression).Compile();
            }
            catch
            {
                return () => (T)Activator.CreateInstance(constructedType);
            }
        }

        private static Func<object, object> CreateCompiledFieldGetter(FieldInfo field)
        {
            if (!CanCompileExpressions)
            {
                return CreateDelegateFieldGetter(field) ?? field.GetValue;
            }

            try
            {
                ParameterExpression instanceParam = Expression.Parameter(
                    typeof(object),
                    "instance"
                );

                Expression instanceExpression = field.DeclaringType.IsValueType
                    ? Expression.Unbox(instanceParam, field.DeclaringType)
                    : Expression.Convert(instanceParam, field.DeclaringType);

                Expression fieldExpression = Expression.Field(instanceExpression, field);

                Expression returnExpression = field.FieldType.IsValueType
                    ? Expression.Convert(fieldExpression, typeof(object))
                    : fieldExpression;

                return Expression
                    .Lambda<Func<object, object>>(returnExpression, instanceParam)
                    .Compile();
            }
            catch
            {
                return field.GetValue;
            }
        }

        private static Func<object, object> CreateCompiledPropertyGetter(PropertyInfo property)
        {
            if (!CanCompileExpressions)
            {
                return CreateDelegatePropertyGetter(property) ?? property.GetValue;
            }

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

                Expression instanceExpression = property.DeclaringType.IsValueType
                    ? Expression.Unbox(instanceParam, property.DeclaringType)
                    : Expression.Convert(instanceParam, property.DeclaringType);

                Expression propertyExpression = Expression.Property(instanceExpression, property);

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

        private static Func<object> CreateCompiledStaticFieldGetter(FieldInfo field)
        {
            if (!CanCompileExpressions)
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
            if (!CanCompileExpressions)
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
            if (!CanCompileExpressions)
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

        private static Func<object, object> CreateDelegateFieldGetter(FieldInfo field)
        {
            try
            {
                if (field.IsStatic)
                {
                    // For static fields, create a simple wrapper
                    return instance => field.GetValue(null);
                }

                // For instance fields, we can't easily create delegates, so use optimized wrapper
                return instance => field.GetValue(instance);
            }
            catch
            {
                return null;
            }
        }

        private static Func<object, object> CreateDelegatePropertyGetter(PropertyInfo property)
        {
            try
            {
                MethodInfo getMethod = property.GetGetMethod(true);
                if (getMethod == null)
                {
                    return null;
                }

                if (getMethod.IsStatic)
                {
                    Type funcType = typeof(Func<>).MakeGenericType(property.PropertyType);
                    Delegate getter = Delegate.CreateDelegate(funcType, getMethod);
                    return instance => getter.DynamicInvoke();
                }
                else
                {
                    Type funcType = typeof(Func<,>).MakeGenericType(
                        property.DeclaringType,
                        property.PropertyType
                    );
                    Delegate getter = Delegate.CreateDelegate(funcType, getMethod);
                    return instance => getter.DynamicInvoke(instance);
                }
            }
            catch
            {
                return null;
            }
        }

        private static Func<object, object> CreateGenericFieldGetter(FieldInfo field)
        {
            try
            {
                // For now, just use direct field access - it's already reasonably fast
                if (field.IsStatic)
                {
                    return instance => field.GetValue(null);
                }

                return instance => field.GetValue(instance);
            }
            catch
            {
                return null;
            }
        }

        private static Type GetActionType(Type[] parameterTypes)
        {
            switch (parameterTypes.Length)
            {
                case 0:
                    return typeof(Action);
                case 1:
                    return typeof(Action<>).MakeGenericType(parameterTypes);
                case 2:
                    return typeof(Action<,>).MakeGenericType(parameterTypes);
                case 3:
                    return typeof(Action<,,>).MakeGenericType(parameterTypes);
                case 4:
                    return typeof(Action<,,,>).MakeGenericType(parameterTypes);
                default:
                    return null;
            }
        }

        private static Type GetFuncType(Type[] typeArgs)
        {
            switch (typeArgs.Length)
            {
                case 1:
                    return typeof(Func<>).MakeGenericType(typeArgs);
                case 2:
                    return typeof(Func<,>).MakeGenericType(typeArgs);
                case 3:
                    return typeof(Func<,,>).MakeGenericType(typeArgs);
                case 4:
                    return typeof(Func<,,,>).MakeGenericType(typeArgs);
                case 5:
                    return typeof(Func<,,,,>).MakeGenericType(typeArgs);
                default:
                    return null;
            }
        }
    }
}
