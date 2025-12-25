#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Serializable class for nested object testing.
    /// </summary>
    [Serializable]
    internal sealed class NestedData
    {
        public int value;
        public string name;
        public List<int> numbers = new();
    }

    /// <summary>
    /// Nested serializable structure for deep nesting tests.
    /// </summary>
    [Serializable]
    internal sealed class DeepNestedData
    {
        public int depth;
        public NestedData child = new();
    }

    /// <summary>
    /// Serializable wrapper for nested list testing (Unity can't directly serialize List of Lists).
    /// </summary>
    [Serializable]
    internal sealed class IntListWrapper
    {
        public List<int> values = new();
    }
}
#endif
