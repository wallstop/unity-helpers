// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class WValueDropDownFloatAsset : ScriptableObject
    {
        [WValueDropDown(typeof(WValueDropDownSource), nameof(WValueDropDownSource.GetFloatValues))]
        public float selection = 1f;

        [WValueDropDown(typeof(WValueDropDownSource), nameof(WValueDropDownSource.GetDoubleValues))]
        public double preciseSelection = 2d;
    }
#endif
}
