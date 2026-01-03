// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with bool fields.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownBoolAsset : ScriptableObject
    {
        [WValueDropDown(true, false)]
        public bool selection = true;

        [WValueDropDown(false, true)]
        public bool alternateSelection = false;
    }
#endif
}
