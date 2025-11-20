namespace WallstopStudios.UnityHelpers.Editor.Utils.WButton
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Concurrent;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    internal static class WButtonValueUtility
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> ParameterlessFactoryCache =
            new();
        private static readonly ConcurrentDictionary<Type, byte> UnsupportedFactoryTypes = new();

        internal static object CloneValue(object value)
        {
            if (value is Array array)
            {
                return array.Clone();
            }

            if (value is AnimationCurve curve)
            {
                AnimationCurve clone = new(curve.keys)
                {
                    preWrapMode = curve.preWrapMode,
                    postWrapMode = curve.postWrapMode,
                };
                return clone;
            }

            return value;
        }

        internal static bool ValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left is not Array leftArray || right is not Array rightArray)
            {
                return left.Equals(right);
            }

            if (leftArray.Length != rightArray.Length)
            {
                return false;
            }

            for (int index = 0; index < leftArray.Length; index++)
            {
                object leftElement = leftArray.GetValue(index);
                object rightElement = rightArray.GetValue(index);
                if (!ValuesEqual(leftElement, rightElement))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TryCreateInstance(Type type, out object value)
        {
            value = null;
            if (type == null)
            {
                return false;
            }

            if (ParameterlessFactoryCache.TryGetValue(type, out Func<object> cached))
            {
                value = cached();
                return value != null;
            }

            if (UnsupportedFactoryTypes.ContainsKey(type))
            {
                return false;
            }

            try
            {
                Func<object> factory = ReflectionHelpers.GetParameterlessConstructor(type);
                ParameterlessFactoryCache[type] = factory;
                value = factory();
                return value != null;
            }
            catch (ArgumentException)
            {
                UnsupportedFactoryTypes[type] = 0;
                return false;
            }
        }
    }
#endif
}
