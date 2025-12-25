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
