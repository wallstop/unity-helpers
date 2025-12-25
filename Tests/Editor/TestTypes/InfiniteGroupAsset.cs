namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class InfiniteGroupAsset : ScriptableObject
    {
        [WGroup("Stream", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
        public string start;

        public string mid;

        [WGroupEnd]
        public string terminator;

        public string trailing;
    }
}
