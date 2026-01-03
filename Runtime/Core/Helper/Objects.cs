// MIT License - Copyright (c) 2023 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Utilities for null checks (including UnityEngine.Object overloads) and deterministic hash code composition.
    /// </summary>
    public static class Objects
    {
        /// <summary>
        /// Unity-aware null check for UnityEngine.Object types (handles destroyed objects returning true for == null).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Null<T>(T instance)
            where T : UnityEngine.Object
        {
            return instance == null;
        }

        /// <summary>
        /// Hybrid null check for boxed or unknown objects (handles UnityEngine.Object special null semantics).
        /// </summary>
        public static bool Null(object instance)
        {
            if (instance is null)
            {
                return true;
            }

            if (instance is UnityEngine.Object unityObject)
            {
                return unityObject == null;
            }

            return false;
        }

        /// <summary>
        /// Unity-aware not-null check for UnityEngine.Object types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotNull<T>(T instance)
            where T : UnityEngine.Object
        {
            return instance != null;
        }

        /// <summary>
        /// Hybrid not-null check for boxed or unknown objects.
        /// </summary>
        public static bool NotNull(object instance)
        {
            return !Null(instance);
        }

        /// <summary>
        /// Combines hash codes for a span of values into a deterministic composite hash.
        /// </summary>
        public static int SpanHashCode<T>(ReadOnlySpan<T> values)
        {
            if (values.IsEmpty)
            {
                return 0;
            }

            DeterministicHashBuilder hash = default;
            foreach (ref readonly T value in values)
            {
                hash.Add(value);
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines one value into a deterministic hash.
        /// </summary>
        public static int HashCode<T1>(T1 param1)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines two values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2>(T1 param1, T2 param2)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3>(T1 param1, T2 param2, T3 param3)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            return hash.ToHashCode();
        }

        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13,
            T14 param14
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            hash.Add(param14);
            return hash.ToHashCode();
        }

        public static int HashCode<
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T7,
            T8,
            T9,
            T10,
            T11,
            T12,
            T13,
            T14,
            T15
        >(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13,
            T14 param14,
            T15 param15
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            hash.Add(param14);
            hash.Add(param15);
            return hash.ToHashCode();
        }

        public static int HashCode<
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T7,
            T8,
            T9,
            T10,
            T11,
            T12,
            T13,
            T14,
            T15,
            T16
        >(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13,
            T14 param14,
            T15 param15,
            T16 param16
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            hash.Add(param14);
            hash.Add(param15);
            hash.Add(param16);
            return hash.ToHashCode();
        }

        public static int HashCode<
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T7,
            T8,
            T9,
            T10,
            T11,
            T12,
            T13,
            T14,
            T15,
            T16,
            T17
        >(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13,
            T14 param14,
            T15 param15,
            T16 param16,
            T17 param17
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            hash.Add(param14);
            hash.Add(param15);
            hash.Add(param16);
            hash.Add(param17);
            return hash.ToHashCode();
        }

        public static int HashCode<
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T7,
            T8,
            T9,
            T10,
            T11,
            T12,
            T13,
            T14,
            T15,
            T16,
            T17,
            T18
        >(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13,
            T14 param14,
            T15 param15,
            T16 param16,
            T17 param17,
            T18 param18
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            hash.Add(param14);
            hash.Add(param15);
            hash.Add(param16);
            hash.Add(param17);
            hash.Add(param18);
            return hash.ToHashCode();
        }

        public static int HashCode<
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T7,
            T8,
            T9,
            T10,
            T11,
            T12,
            T13,
            T14,
            T15,
            T16,
            T17,
            T18,
            T19
        >(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13,
            T14 param14,
            T15 param15,
            T16 param16,
            T17 param17,
            T18 param18,
            T19 param19
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            hash.Add(param14);
            hash.Add(param15);
            hash.Add(param16);
            hash.Add(param17);
            hash.Add(param18);
            hash.Add(param19);
            return hash.ToHashCode();
        }

        public static int HashCode<
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T7,
            T8,
            T9,
            T10,
            T11,
            T12,
            T13,
            T14,
            T15,
            T16,
            T17,
            T18,
            T19,
            T20
        >(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8,
            T9 param9,
            T10 param10,
            T11 param11,
            T12 param12,
            T13 param13,
            T14 param14,
            T15 param15,
            T16 param16,
            T17 param17,
            T18 param18,
            T19 param19,
            T20 param20
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            hash.Add(param9);
            hash.Add(param10);
            hash.Add(param11);
            hash.Add(param12);
            hash.Add(param13);
            hash.Add(param14);
            hash.Add(param15);
            hash.Add(param16);
            hash.Add(param17);
            hash.Add(param18);
            hash.Add(param19);
            hash.Add(param20);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines hash codes for all elements in an enumerable (with optimized paths for common collection types).
        /// </summary>
        public static int EnumerableHashCode<T>(IEnumerable<T> enumerable)
        {
            if (ReferenceEquals(enumerable, null))
            {
                return 0;
            }

            DeterministicHashBuilder hash = default;
            switch (enumerable)
            {
                case IReadOnlyList<T> list:
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        hash.Add(list[i]);
                    }

                    break;
                }
                case HashSet<T> hashSet:
                {
                    foreach (T item in hashSet)
                    {
                        hash.Add(item);
                    }

                    break;
                }
                case Queue<T> queue:
                {
                    foreach (T item in queue)
                    {
                        hash.Add(item);
                    }

                    break;
                }
                case Stack<T> stack:
                {
                    foreach (T item in stack)
                    {
                        hash.Add(item);
                    }

                    break;
                }
                case SortedSet<T> sortedSet:
                {
                    foreach (T item in sortedSet)
                    {
                        hash.Add(item);
                    }

                    break;
                }
                default:
                {
                    foreach (T item in enumerable)
                    {
                        hash.Add(item);
                    }

                    break;
                }
            }

            return hash.ToHashCode();
        }

        // Lightweight deterministic hash accumulator using FNV-1a mixing.
        private struct DeterministicHashBuilder
        {
            private const uint Seed = 2166136261u;
            private const uint Prime = 16777619u;

            private uint _hash;
            private bool _hasContribution;
            private bool _hasNonNullContribution;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<T>(T value)
            {
                uint valueHash = TypeTraits<T>.GetValueHash(value, out bool hasNonNullValue);

                if (!_hasContribution)
                {
                    // Defer seeding until the first value is observed so empty hashes stay at 0.
                    _hash = Seed;
                    _hasContribution = true;
                }

                _hash ^= valueHash;
                _hash *= Prime;

                if (hasNonNullValue)
                {
                    _hasNonNullContribution = true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int ToHashCode()
            {
                if (!_hasContribution || !_hasNonNullContribution)
                {
                    return 0;
                }

                return unchecked((int)_hash);
            }
        }

        private static class TypeTraits<T>
        {
            private const uint NullSentinel = 0x9E3779B9u;

            private static readonly bool IsReferenceType = !typeof(T).IsValueType;
            private static readonly bool IsObjectType = typeof(T) == typeof(object);
            private static readonly bool IsUnityObject =
                typeof(UnityEngine.Object).IsAssignableFrom(typeof(T));
            private static readonly EqualityComparer<T> EqualityComparer =
                EqualityComparer<T>.Default;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint GetValueHash(T value, out bool hasNonNullValue)
            {
                if (!IsReferenceType)
                {
                    hasNonNullValue = true;
                    return unchecked((uint)EqualityComparer.GetHashCode(value));
                }

                if (IsObjectType)
                {
                    return GetBoxedObjectHash(value, out hasNonNullValue);
                }

                if (IsUnityObject)
                {
                    return GetUnityObjectHash(value, out hasNonNullValue);
                }

                if (ReferenceEquals(value, null))
                {
                    hasNonNullValue = false;
                    return NullSentinel;
                }

                hasNonNullValue = true;
                return unchecked((uint)EqualityComparer.GetHashCode(value));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint GetUnityObjectHash(T value, out bool hasNonNullValue)
            {
                T local = value;
                UnityEngine.Object unityObject = Unsafe.As<T, UnityEngine.Object>(ref local);

                if (unityObject == null)
                {
                    hasNonNullValue = false;
                    return NullSentinel;
                }

                hasNonNullValue = true;
                return unchecked((uint)unityObject.GetHashCode());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint GetBoxedObjectHash(T value, out bool hasNonNullValue)
            {
                object boxed = value;

                if (boxed is UnityEngine.Object unityObject)
                {
                    if (unityObject == null)
                    {
                        hasNonNullValue = false;
                        return NullSentinel;
                    }

                    hasNonNullValue = true;
                    return unchecked((uint)unityObject.GetHashCode());
                }

                if (boxed is null)
                {
                    hasNonNullValue = false;
                    return NullSentinel;
                }

                hasNonNullValue = true;
                return unchecked((uint)EqualityComparer<object>.Default.GetHashCode(boxed));
            }
        }
    }
}
