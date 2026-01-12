// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Custom enum comparer to avoid boxing on dictionary lookups
// Use when using enums as dictionary keys

namespace WallstopStudios.UnityHelpers.Examples
{
    using System.Collections.Generic;

    // Without custom comparer: 4.5MB for 128K lookups (boxing per lookup)
    // With custom comparer: Zero allocation

    public struct MyEnumComparer : IEqualityComparer<MyEnum>
    {
        public bool Equals(MyEnum x, MyEnum y) => x == y;

        public int GetHashCode(MyEnum obj) => (int)obj;
    }

    // Example enum
    public enum MyEnum
    {
        [System.Obsolete("Use a specific enum value.", true)]
        None = 0,
        Option1 = 1,
        Option2 = 2,
        Option3 = 3,
    }

    // Usage example
    public static class EnumDictionaryExample
    {
        // Zero allocation lookups
        public static Dictionary<MyEnum, string> CreateDict()
        {
            return new Dictionary<MyEnum, string>(new MyEnumComparer());
        }

        // Alternative: Cast to int
        public static Dictionary<int, string> CreateIntKeyedDict()
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();
            dict[(int)MyEnum.Option1] = "value";
            return dict;
        }
    }
}
