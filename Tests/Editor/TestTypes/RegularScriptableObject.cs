namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// A regular ScriptableObject (not a singleton) for testing that detection returns false.
    /// </summary>
    internal sealed class RegularScriptableObject : ScriptableObject
    {
        public SerializableDictionary<string, string> dictionary = new();
        public SerializableHashSet<int> set = new();
    }
}
