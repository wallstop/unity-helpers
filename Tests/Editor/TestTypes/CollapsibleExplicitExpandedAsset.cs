namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class CollapsibleExplicitExpandedAsset : ScriptableObject
    {
        [WGroup(
            "ExplicitExpanded",
            collapsible: true,
            CollapseBehavior = WGroupAttribute.WGroupCollapseBehavior.ForceExpanded
        )]
        public int first;
    }
}
