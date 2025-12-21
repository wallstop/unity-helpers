namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test host for grouped palette dictionary with WGroup attribute.
    /// </summary>
    public sealed class GroupedPaletteHost : ScriptableObject
    {
        [WGroup("Palette", displayName: "Palette Colors", autoIncludeCount: 1, collapsible: true)]
        public GroupPaletteDictionary palette = new();
    }
}
