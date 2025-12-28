// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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
