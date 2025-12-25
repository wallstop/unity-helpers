namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    public sealed class TweenAnimationTestStringIntDictionary
        : SerializableDictionary<string, int> { }

    [Serializable]
    public sealed class TweenAnimationTestSortedStringIntDictionary
        : SerializableSortedDictionary<string, int> { }

    [Serializable]
    public sealed class TweenAnimationTestIntSet : SerializableHashSet<int> { }

    [Serializable]
    public sealed class TweenAnimationTestSortedIntSet : SerializableSortedSet<int> { }

    public sealed class TweenAnimationSimpleDictionaryHost : ScriptableObject
    {
        public TweenAnimationTestStringIntDictionary dictionary = new();
    }

    public sealed class TweenAnimationSimpleSortedDictionaryHost : ScriptableObject
    {
        public TweenAnimationTestSortedStringIntDictionary sortedDictionary = new();
    }

    public sealed class TweenAnimationSimpleSetHost : ScriptableObject
    {
        public TweenAnimationTestIntSet set = new();
    }

    public sealed class TweenAnimationSimpleSortedSetHost : ScriptableObject
    {
        public TweenAnimationTestSortedIntSet sortedSet = new();
    }
}
