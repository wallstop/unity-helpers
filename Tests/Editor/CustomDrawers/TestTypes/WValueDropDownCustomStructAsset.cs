namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Custom equatable struct for testing WValueDropDown with arbitrary types.
    /// </summary>
    [Serializable]
    internal struct TestEquatableStruct : IEquatable<TestEquatableStruct>
    {
        [SerializeField]
        private int _id;

        [SerializeField]
        private string _name;

        public int Id => _id;
        public string Name => _name ?? string.Empty;

        public TestEquatableStruct(int id, string name)
        {
            _id = id;
            _name = name ?? string.Empty;
        }

        public bool Equals(TestEquatableStruct other)
        {
            return _id == other._id && string.Equals(_name, other._name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TestEquatableStruct other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_id * 397)
                    ^ (_name != null ? StringComparer.Ordinal.GetHashCode(_name) : 0);
            }
        }

        public override string ToString()
        {
            return $"{_name} ({_id})";
        }

        public static bool operator ==(TestEquatableStruct left, TestEquatableStruct right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TestEquatableStruct left, TestEquatableStruct right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Test asset for WValueDropDown attribute with custom equatable struct fields.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownCustomStructAsset : ScriptableObject
    {
        [WValueDropDown(
            typeof(WValueDropDownCustomStructSource),
            nameof(WValueDropDownCustomStructSource.GetStructOptions)
        )]
        public TestEquatableStruct selectedStruct;

        [WValueDropDown(nameof(GetInstanceStructOptions), typeof(TestEquatableStruct))]
        public TestEquatableStruct instanceSelectedStruct;

        public List<TestEquatableStruct> dynamicStructs = new();

        public IEnumerable<TestEquatableStruct> GetInstanceStructOptions()
        {
            return dynamicStructs;
        }
    }

    /// <summary>
    /// Static provider for custom struct dropdown options.
    /// </summary>
    internal static class WValueDropDownCustomStructSource
    {
        private static readonly TestEquatableStruct[] StructOptions = new[]
        {
            new TestEquatableStruct(1, "First"),
            new TestEquatableStruct(2, "Second"),
            new TestEquatableStruct(3, "Third"),
            new TestEquatableStruct(4, "Fourth"),
        };

        public static IEnumerable<TestEquatableStruct> GetStructOptions()
        {
            return StructOptions;
        }
    }
#endif
}
