// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with char fields.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownCharAsset : ScriptableObject
    {
        [WValueDropDown('A', 'B', 'C')]
        public char selection = 'A';

        [WValueDropDown('X', 'Y', 'Z')]
        public char alternateSelection = 'X';
    }
#endif
}
