namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// A test ScriptableSingleton for verifying ScriptableSingleton detection and save behavior.
    /// Uses FilePath attribute to control where the singleton is stored.
    /// </summary>
    [FilePath("Temp/TestScriptableSingleton.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class TestScriptableSingleton : ScriptableSingleton<TestScriptableSingleton>
    {
        public SerializableDictionary<string, string> dictionary = new();
        public SerializableHashSet<int> set = new();
        public int saveCallCount;

        /// <summary>
        /// Tracks whether Save was called, for testing purposes.
        /// </summary>
        internal void TrackSave()
        {
            saveCallCount++;
        }

        /// <summary>
        /// Resets the singleton state for testing.
        /// </summary>
        internal void ResetForTest()
        {
            dictionary.Clear();
            set.Clear();
            saveCallCount = 0;
        }
    }

    /// <summary>
    /// Another test ScriptableSingleton to verify the detection works with different generic type parameters.
    /// </summary>
    [FilePath(
        "Temp/AnotherTestScriptableSingleton.asset",
        FilePathAttribute.Location.ProjectFolder
    )]
    internal sealed class AnotherTestScriptableSingleton
        : ScriptableSingleton<AnotherTestScriptableSingleton>
    {
        public SerializableDictionary<int, string> intStringDictionary = new();
        public SerializableSortedSet<string> sortedSet = new();
    }

    /// <summary>
    /// A derived class from a ScriptableSingleton to test multi-level inheritance detection.
    /// This tests that the detection properly walks the type hierarchy.
    /// </summary>
    internal abstract class BaseTestSingleton<T> : ScriptableSingleton<T>
        where T : ScriptableObject
    {
        public string baseValue;
    }

    /// <summary>
    /// Concrete derived singleton that tests inheritance chain detection.
    /// </summary>
    [FilePath(
        "Temp/DerivedTestScriptableSingleton.asset",
        FilePathAttribute.Location.ProjectFolder
    )]
    internal sealed class DerivedTestScriptableSingleton
        : BaseTestSingleton<DerivedTestScriptableSingleton>
    {
        public SerializableDictionary<string, int> derivedDictionary = new();
    }

    /// <summary>
    /// A regular ScriptableObject (not a singleton) for testing that detection returns false.
    /// </summary>
    internal sealed class RegularScriptableObject : ScriptableObject
    {
        public SerializableDictionary<string, string> dictionary = new();
        public SerializableHashSet<int> set = new();
    }

    /// <summary>
    /// A MonoBehaviour for testing that detection returns false.
    /// </summary>
    internal sealed class RegularMonoBehaviour : MonoBehaviour
    {
        public SerializableDictionary<string, string> dictionary = new();
        public SerializableHashSet<int> set = new();
    }

    /// <summary>
    /// ScriptableSingleton with complex nested serializable types.
    /// </summary>
    [FilePath("Temp/ComplexScriptableSingleton.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ComplexScriptableSingleton
        : ScriptableSingleton<ComplexScriptableSingleton>
    {
        public SerializableDictionary<string, SingletonComplexValue> complexDictionary = new();
        public SerializableHashSet<SingletonSetElement> complexSet = new();

        internal void ResetForTest()
        {
            complexDictionary.Clear();
            complexSet.Clear();
        }
    }

    /// <summary>
    /// Complex value type for testing nested serialization in ScriptableSingleton tests.
    /// </summary>
    [Serializable]
    internal sealed class SingletonComplexValue
    {
        public string name;
        public int count;
        public Color color;
    }

    /// <summary>
    /// Complex set element for testing nested serialization in ScriptableSingleton tests.
    /// </summary>
    [Serializable]
    internal sealed class SingletonSetElement : IEquatable<SingletonSetElement>
    {
        public string id;
        public float value;

        public bool Equals(SingletonSetElement other)
        {
            if (other is null)
            {
                return false;
            }
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SingletonSetElement);
        }

        public override int GetHashCode()
        {
            return id?.GetHashCode() ?? 0;
        }
    }
}
