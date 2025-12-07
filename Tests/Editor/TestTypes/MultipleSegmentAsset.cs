namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class MultipleSegmentAsset : ScriptableObject
    {
        [WGroup("Segments", autoIncludeCount: 0)]
        public int first;

        public int outside;

        [WGroup("Segments", autoIncludeCount: 1)]
        public int second;

        public int third;
    }
}
