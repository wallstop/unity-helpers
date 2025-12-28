// MIT License - Copyright (c) 2023 Eli Pinkerton
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
