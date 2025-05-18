namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public sealed class UnityObjectNameComparer<T> : IComparer<T>
        where T : UnityEngine.Object
    {
        public static readonly UnityObjectNameComparer<T> Instance = new();

        private UnityObjectNameComparer() { }

        public int Compare(T x, T y)
        {
            if (x == y)
            {
                return 0;
            }

            if (y == null)
            {
                return 1;
            }

            if (x == null)
            {
                return -1;
            }

            int comparison = string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
            if (comparison != 0)
            {
                return comparison;
            }

#if UNITY_EDITOR
            comparison = string.Compare(
                AssetDatabase.GetAssetOrScenePath(x),
                AssetDatabase.GetAssetOrScenePath(y),
                StringComparison.OrdinalIgnoreCase
            );
#endif
            if (comparison == 0)
            {
                return x.GetInstanceID().CompareTo(y.GetInstanceID());
            }

            return comparison;
        }
    }
}
