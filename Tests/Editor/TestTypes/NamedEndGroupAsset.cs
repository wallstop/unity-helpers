namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class NamedEndGroupAsset : ScriptableObject
    {
        [WGroup("Alpha", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
        public int alphaStart;

        public int alphaMid;

        [WGroup("Beta", autoIncludeCount: 1)]
        public int betaStart;

        public int betaTail;

        [WGroupEnd("Alpha")]
        public int alphaStop;

        public int alphaOutside;
    }
}
