namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;

    public sealed class TypeNameSorter : IComparer<Type>
    {
        public static readonly TypeNameSorter Instance = new();

        private TypeNameSorter() { }

        public int Compare(Type x, Type y)
        {
            return string.Compare(x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
