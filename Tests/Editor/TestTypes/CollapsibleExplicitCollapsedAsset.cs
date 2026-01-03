// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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
