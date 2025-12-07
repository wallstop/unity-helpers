namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class ColorKeyAsset : ScriptableObject
    {
        [WGroup("PaletteGroup", colorKey: "TestPalette_WGroup", hideHeader: true)]
        public float value;
    }
}
