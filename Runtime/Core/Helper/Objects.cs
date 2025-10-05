namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public static class Objects
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Null<T>(T instance)
            where T : UnityEngine.Object
        {
            return instance == null;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotNull<T>(T instance)
            where T : UnityEngine.Object
        {
            return instance != null;
        }

        public static bool NotNull(object instance)
        {
            return !Null(instance);
        }

        public static int HashCode<T>(ReadOnlySpan<T> values)
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

        public static int HashCode<T1>(T1 param1)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            return hash.ToHashCode();
        }

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

                if (IsUnityObject)
                {
                    UnityEngine.Object unityObject = value as UnityEngine.Object;
                    if (unityObject == null)
                    {
                        hasNonNullValue = false;
                        return NullSentinel;
                    }

                    hasNonNullValue = true;
                    return unchecked((uint)unityObject.GetHashCode());
                }

                if (ReferenceEquals(value, null))
                {
                    hasNonNullValue = false;
                    return NullSentinel;
                }

                hasNonNullValue = true;
                return unchecked((uint)EqualityComparer.GetHashCode(value));
            }
        }
    }
}
