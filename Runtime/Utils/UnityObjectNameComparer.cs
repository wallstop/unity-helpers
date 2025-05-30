namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public sealed class UnityObjectNameComparer<T> : IComparer<T>
        where T : UnityEngine.Object
    {
        private static readonly Regex TrailingNumberRegex = new(
            @"^(.*?)(\d+)$",
            RegexOptions.Compiled
        );
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

            int comparison = CompareNatural(x.name, y.name);
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

        private static int CompareNatural(string nameA, string nameB)
        {
            Match mA = TrailingNumberRegex.Match(nameA);
            Match mB = TrailingNumberRegex.Match(nameB);

            bool hasNumberA = mA.Success;
            bool hasNumberB = mB.Success;

            // If both have trailing numbers, compare prefix then numeric
            if (hasNumberA && hasNumberB)
            {
                string prefixA = mA.Groups[1].Value;
                string prefixB = mB.Groups[1].Value;

                int prefixCompare = StringComparer.OrdinalIgnoreCase.Compare(prefixA, prefixB);
                if (prefixCompare != 0)
                {
                    return prefixCompare;
                }

                // same prefix → compare parsed integers
                int numA = int.Parse(mA.Groups[2].Value);
                int numB = int.Parse(mB.Groups[2].Value);
                return numA.CompareTo(numB);
            }
            // If only one has a trailing number, treat the one without number as coming first

            if (hasNumberA)
            {
                return 1; // B (no number) comes before A
            }

            if (hasNumberB)
            {
                return -1; // A (no number) comes before B
            }
            // Neither has a trailing number → pure string compare
            return StringComparer.OrdinalIgnoreCase.Compare(nameA, nameB);
        }
    }
}
