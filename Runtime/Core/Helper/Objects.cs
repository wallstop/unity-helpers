namespace UnityHelpers.Core.Helper
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using Object = System.Object;

    public static partial class Objects
    {
        private const int HashBase = 839;
        private const int HashMultiplier = 4021;

        public static T FromWeakReference<T>(WeakReference weakReference) where T : class
        {
            object empty = weakReference.Target;
            return (T)empty;
        }

        public static bool Null(UnityEngine.Object instance)
        {
            return instance == null;
        }

        public static bool Null(Object instance)
        {
            return instance == null;
        }

        public static bool NotNull(UnityEngine.Object instance)
        {
            return instance != null;
        }

        public static bool NotNull(Object instance)
        {
            return instance != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NullSafeHashCode<T>(T param)
        {
            Type type = typeof(T);
            if (type.IsValueType)
            {
                return param.GetHashCode();
            }

            return param == null ? type.GetHashCode() : param.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1>(T1 param1) where T1 : unmanaged
        {
            unchecked
            {
                return HashBase * HashMultiplier + param1.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2>(T1 param1, T2 param2) 
            where T1 : unmanaged 
            where T2 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1) * HashMultiplier + param2.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3>(T1 param1, T2 param2, T3 param3)
            where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2) * HashMultiplier + param3.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4)
            where T1 : unmanaged 
            where T2 : unmanaged 
            where T3 : unmanaged 
            where T4 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3) * HashMultiplier + param4.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
            where T1 : unmanaged 
            where T2 : unmanaged 
            where T3 : unmanaged 
            where T4 : unmanaged 
            where T5 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4) * HashMultiplier + param5.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6) 
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5) *
                       HashMultiplier + param6.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7) 
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6) *
                       HashMultiplier + param7.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8) 
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7) *
                       HashMultiplier + param8.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8) *
                    HashMultiplier + param9.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10: unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8, param9) *
                    HashMultiplier + param10.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10 : unmanaged
            where T11 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10) *
                    HashMultiplier + param11.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10 : unmanaged
            where T11 : unmanaged
            where T12 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11) *
                    HashMultiplier + param12.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10 : unmanaged
            where T11 : unmanaged
            where T12 : unmanaged
            where T13 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12) *
                    HashMultiplier + param13.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10 : unmanaged
            where T11 : unmanaged
            where T12 : unmanaged
            where T13 : unmanaged
            where T14 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13) *
                    HashMultiplier + param14.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10 : unmanaged
            where T11 : unmanaged
            where T12 : unmanaged
            where T13 : unmanaged
            where T14 : unmanaged
            where T15 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14) *
                    HashMultiplier + param15.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueTypeHashCode<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 param1, T2 param2, T3 param3, T4 param4,
            T5 param5, T6 param6, T7 param7, T8 param8, T9 param9, T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15, T16 param16)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10 : unmanaged
            where T11 : unmanaged
            where T12 : unmanaged
            where T13 : unmanaged
            where T14 : unmanaged
            where T15 : unmanaged
            where T16 : unmanaged
        {
            unchecked
            {
                return ValueTypeHashCode(param1, param2, param3, param4, param5, param6, param7, param8, param9, param10, param11, param12, param13, param14, param15) *
                    HashMultiplier + param16.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1>(T1 param1)
        {
            unchecked
            {
                return HashBase * NullSafeHashCode(param1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2>(T1 param1, T2 param2)
        {
            unchecked
            {
                return HashCode(param1) * HashMultiplier + NullSafeHashCode(param2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3>(T1 param1, T2 param2, T3 param3)
        {
            unchecked
            {
                return HashCode(param1, param2) * HashMultiplier + NullSafeHashCode(param3);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4)
        {
            unchecked
            {
                return HashCode(param1, param2, param3) * HashMultiplier + NullSafeHashCode(param4);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3, T4, T5>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            unchecked
            {
                return HashCode(param1, param2, param3, param4) * HashMultiplier + NullSafeHashCode(param5);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3, T4, T5, T6>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5,
            T6 param6)
        {
            unchecked
            {
                return HashCode(param1, param2, param3, param4, param5) * HashMultiplier + NullSafeHashCode(param6);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3, T4, T5, T6, T7>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5,
            T6 param6, T7 param7)
        {
            unchecked
            {
                return HashCode(param1, param2, param3, param4, param5, param6) *
                       HashMultiplier + NullSafeHashCode(param7);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5,
            T6 param6, T7 param7, T8 param8)
        {
            unchecked
            {
                return HashCode(param1, param2, param3, param4, param5, param6, param7) *
                    HashMultiplier + NullSafeHashCode(param8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EnumerableHashCode(IEnumerable enumerable)
        {
            if (ReferenceEquals(enumerable, null))
            {
                return 0;
            }
            unchecked
            {
                int hash = HashBase;
                IEnumerator walker = enumerable.GetEnumerator();
                while (walker.MoveNext())
                {
                    hash = hash * HashMultiplier + NullSafeHashCode(walker.Current);
                }

                return hash;
            }
        }
    }
}
