namespace UnityHelpers.Core.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using Extension;

    public static class ReflectionHelpers
    {
        private static readonly Dictionary<Type, Func<int, Array>> ArrayCreators = new();
        private static readonly Dictionary<Type, Func<IList>> ListCreators = new();
        private static readonly Dictionary<Type, Func<int, IList>> ListWithCapacityCreators = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<int, Array> GetOrCreateArrayCreator(Type type)
        {
            return ArrayCreators.GetOrAdd(type, elementType => GetArrayCreator(elementType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array CreateArray(Type type, int length)
        {
            return GetOrCreateArrayCreator(type).Invoke(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList CreateList(Type elementType, int length)
        {
            return ListWithCapacityCreators
                .GetOrAdd(elementType, type => GetListWithCapacityCreator(type))
                .Invoke(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList CreateList(Type elementType)
        {
            return ListCreators.GetOrAdd(elementType, type => GetListCreator(type)).Invoke();
        }

        public static Action<object, object> CreateFieldSetter(Type type, FieldInfo field)
        {
#if WEB_GL
            return field.SetValue;
#else
            DynamicMethod dynamicMethod = new(
                $"SetField{field.Name}",
                null,
                new[] { typeof(object), typeof(object) },
                type.Module,
                true
            );

            ILGenerator il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0); // Load the object (arg0)
            il.Emit(OpCodes.Castclass, type); // Cast to the actual type

            il.Emit(OpCodes.Ldarg_1); // Load the value (arg1)
            il.Emit(
                field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                field.FieldType
            ); // Cast for reference types
            // Unbox if it's a value type
            il.Emit(OpCodes.Stfld, field); // Set the field
            il.Emit(OpCodes.Ret); // Return
            return (Action<object, object>)
                dynamicMethod.CreateDelegate(typeof(Action<object, object>));
#endif
        }

        public static Func<int, Array> GetArrayCreator(Type elementType)
        {
#if WEB_GL
            return size => Array.CreateInstance(elementType, size);
#else

            DynamicMethod dynamicMethod = new DynamicMethod(
                $"CreateArray{elementType.Namespace}",
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
            DynamicMethod dynamicMethod = new DynamicMethod(
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
            DynamicMethod dynamicMethod = new DynamicMethod(
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
