namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class CollapsibleDefaultAsset : ScriptableObject
    {
        [WGroup("DefaultGroup", collapsible: true)]
        public int first;
    }
}
