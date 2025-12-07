using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    internal sealed class ToggleDropdownAsset : ScriptableObject
    {
        [WEnumToggleButtons]
        [WValueDropDown(typeof(DropdownProvider), nameof(DropdownProvider.GetModes))]
        public string mode;
    }

    internal static class DropdownProvider
    {
        internal static string[] GetModes()
        {
            return new[] { "Alpha", "Beta", "Gamma" };
        }
    }
}
