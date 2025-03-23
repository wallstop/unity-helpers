﻿namespace UnityHelpers.Core.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using Extension;

    public delegate void FieldSetter<TInstance, in TValue>(ref TInstance instance, TValue value);

    public static class ReflectionHelpers
    {
        private static readonly Dictionary<Type, Func<int, Array>> ArrayCreators = new();
        private static readonly Dictionary<Type, Func<IList>> ListCreators = new();
        private static readonly Dictionary<Type, Func<int, IList>> ListWithCapacityCreators = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array CreateArray(Type type, int length)
        {
            return ArrayCreators
                // ReSharper disable once ConvertClosureToMethodGroup
                .GetOrAdd(type, elementType => GetArrayCreator(elementType))
                .Invoke(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList CreateList(Type elementType, int length)
        {
            return ListWithCapacityCreators
                // ReSharper disable once ConvertClosureToMethodGroup
                .GetOrAdd(elementType, type => GetListWithCapacityCreator(type))
                .Invoke(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList CreateList(Type elementType)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return ListCreators.GetOrAdd(elementType, type => GetListCreator(type)).Invoke();
        }

        public static Func<object, object> GetFieldGetter(FieldInfo field)
        {
#if WEB_GL
            return field.GetValue;
#else
            DynamicMethod dynamicMethod = new(
                $"Get{field.DeclaringType.Name}{field.Name}",
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

        public static Func<object> GetStaticFieldGetter(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }

#if WEB_GL
            return () => field.GetValue(null);
#else
            DynamicMethod dynamicMethod = new(
                $"Get{field.DeclaringType.Name}{field.Name}",
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
#if WEB_GL
            return Getter;
            TValue Getter(TInstance instance)
            {
                return (TValue)field.GetValue(instance);
            }
#else
            DynamicMethod dynamicMethod = new(
                $"GetGeneric{field.DeclaringType.Name}{field.Name}",
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

        public static Func<TValue> GetStaticFieldGetter<TValue>(FieldInfo field)
        {
            if (!field.IsStatic)
            {
                throw new ArgumentException(nameof(field));
            }

#if WEB_GL
            return Getter;
            TValue Getter()
            {
                return (TValue)field.GetValue(null);
            }
#else
            DynamicMethod dynamicMethod = new(
                $"GetGenericStatic{field.DeclaringType.Name}{field.Name}",
                typeof(TValue),
                Type.EmptyTypes, // no parameters needed for static fields
                field.DeclaringType,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            // Load the static field.
            il.Emit(OpCodes.Ldsfld, field);

            // Handle conversion from the field type to TValue.
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

            return (Func<TValue>)dynamicMethod.CreateDelegate(typeof(Func<TValue>));
#endif
        }

        public static FieldSetter<TInstance, TValue> GetFieldSetter<TInstance, TValue>(
            FieldInfo field
        )
        {
#if WEB_GL
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
                $"SetFieldGeneric{field.DeclaringType.Name}{field.Name}",
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
#if WEB_GL
            return Setter;
            void Setter(TValue newValue)
            {
                field.SetValue(null, newValue);
            }
#else
            DynamicMethod dynamicMethod = new(
                $"SetFieldGenericStatic{field.DeclaringType.Name}{field.Name}",
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
#if WEB_GL
            return field.SetValue;
#else
            DynamicMethod dynamicMethod = new(
                $"SetField{field.DeclaringType.Name}{field.Name}",
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
#if WEB_GL
            return value => field.SetValue(null, value);
#else
            DynamicMethod dynamicMethod = new(
                $"SetFieldStatic{field.DeclaringType.Name}{field.Name}",
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
#if WEB_GL
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
#if WEB_GL
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
#if WEB_GL
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
    }
}
