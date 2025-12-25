namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class CollapsibleExplicitCollapsedAsset : ScriptableObject
    {
        [WGroup("ExplicitCollapsed", collapsible: true, startCollapsed: true)]
        public int first;
    }
}
