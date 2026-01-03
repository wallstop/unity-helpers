// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class CollapsibleAsset : ScriptableObject
    {
        [WGroup("ToggleGroup", autoIncludeCount: 1, collapsible: true, startCollapsed: true)]
        public int first;

        public int second;
    }
}
