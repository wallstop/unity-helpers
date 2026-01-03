// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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
