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
    using UnityEditor;
#if !SINGLE_THREADED
    using System.Collections.Concurrent;
#else
    using Extension;
#endif

    public delegate void FieldSetter<TInstance, in TValue>(ref TInstance instance, TValue value);

    /// <summary>
    /// High-performance reflection helpers for field/property access, method/constructor invocation,
    /// and dynamic collection creation with caching and optional IL emission.
    /// </summary>
    /// <remarks>
    /// Uses expression compilation or dynamic IL where supported; falls back to reflection otherwise.
    /// Caches delegates to avoid per-call reflection overhead.
    /// </remarks>
    public static class ReflectionHelpers
    {
#if SINGLE_THREADED
        private static readonly Dictionary<Type, Func<int, Array>> ArrayCreators = new();
        private static readonly Dictionary<Type, Func<IList>> ListCreators = new();
        private static readonly Dictionary<Type, Func<int, IList>> ListWithCapacityCreators = new();
        private static readonly Dictionary<Type, Func<int, object>> HashSetWithCapacityCreators =
            new();
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
#endif

        private static readonly bool CanCompileExpressions = CheckExpressionCompilationSupport();

        /// <summary>
        /// Tries to get an attribute of type <typeparamref name="T"/> and indicates whether it is present.
        /// </summary>
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
        /// Loads all public static properties whose type matches <typeparamref name="T"/> keyed by property name (case-insensitive).
        /// </summary>
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
        /// Loads all public static fields whose type matches <typeparamref name="T"/> keyed by field name (case-insensitive).
        /// </summary>
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
        /// Creates a new array instance of <paramref name="type"/> with the specified length.
        /// </summary>
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
        public static IList CreateList(Type elementType)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return ListCreators.GetOrAdd(elementType, type => GetListCreator(type)).Invoke();
        }

        /// <summary>
        /// Builds a cached delegate that returns the value of an instance field as <see cref="object"/>.
        /// </summary>
        public static Func<object, object> GetFieldGetter(FieldInfo field)
        {
#if !EMIT_DYNAMIC_IL
            return CreateCompiledFieldGetter(field);
#else
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

            // If the field's type is a value type, box it.
            if (field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
            }

            il.Emit(OpCodes.Ret);

            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
#endif
        }

        /// <summary>
        /// Builds a cached delegate that returns the value of a property as <see cref="object"/>.
        /// Supports static and instance properties.
        /// </summary>
        public static Func<object, object> GetPropertyGetter(PropertyInfo property)
        {
#if !EMIT_DYNAMIC_IL
            return CreateCompiledPropertyGetter(property);
#else
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
                // For static properties, don't load any arguments
                il.Emit(OpCodes.Call, getMethod);
            }
            else
            {
                // For instance properties, load and cast the argument
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
#endif
        }

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

        public static Func<object> GetStaticFieldGetter(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }

#if !EMIT_DYNAMIC_IL
            return CreateCompiledStaticFieldGetter(field);
#else
            DynamicMethod dynamicMethod = new(
                $"Get{field.DeclaringType.Name}_{field.Name}",
                typeof(object),
                Type.EmptyTypes, // No parameters for static fields
                field.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            // Load the static field
            il.Emit(OpCodes.Ldsfld, field);

            // If the field's type is a value type, box it.
            if (field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
            }

            il.Emit(OpCodes.Ret);

            return (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
#endif
        }

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

        public static Action<object, object> GetFieldSetter(FieldInfo field)
        {
#if !EMIT_DYNAMIC_IL
            return CreateCompiledFieldSetter(field);
#else
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
#endif
        }

        public static Action<object> GetStaticFieldSetter(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }
#if !EMIT_DYNAMIC_IL
            return CreateCompiledStaticFieldSetter(field);
#else
            DynamicMethod dynamicMethod = new(
                $"SetFieldStatic{field.DeclaringType.Name}_{field.Name}",
                null,
                new[] { typeof(object) },
                field.DeclaringType.Module,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            // Load the new value (argument 0)
            il.Emit(OpCodes.Ldarg_0);
            // Convert the object to the field's type (unbox or cast as needed)
            il.Emit(
                field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                field.FieldType
            );
            // Set the static field
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);

            return (Action<object>)dynamicMethod.CreateDelegate(typeof(Action<object>));
#endif
        }

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

        public static Func<int, IList> GetListWithCapacityCreator(Type elementType)
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
#if !EMIT_DYNAMIC_IL
            return _ => (IList)Activator.CreateInstance(listType);
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
        public static object CreateHashSet(Type elementType, int capacity)
        {
            return HashSetWithCapacityCreators
                // ReSharper disable once ConvertClosureToMethodGroup
                .GetOrAdd(elementType, type => GetHashSetWithCapacityCreator(type))
                .Invoke(capacity);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object InvokeStaticMethod(MethodInfo method, params object[] parameters)
        {
            return StaticMethodInvokers
                .GetOrAdd(method, methodInfo => GetStaticMethodInvoker(methodInfo))
                .Invoke(parameters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CreateInstance(ConstructorInfo constructor, params object[] parameters)
        {
            return Constructors
                .GetOrAdd(constructor, ctor => GetConstructor(ctor))
                .Invoke(parameters);
        }

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

        public static IEnumerable<Type> GetAllLoadedTypes()
        {
            return GetAllLoadedAssemblies()
                .SelectMany(assembly => GetTypesFromAssembly(assembly))
                .Where(type => type != null);
        }

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

        public static IEnumerable<Type> GetTypesWithAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            return GetAllLoadedTypes().Where(type => HasAttributeSafe<TAttribute>(type));
        }

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
