// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropDownVeryLargeOptionsAsset : ScriptableObject
    {
        [IntDropDown(
            typeof(IntDropDownLargeSource),
            nameof(IntDropDownLargeSource.GetVeryLargeOptions)
        )]
        public int selection = 100;
    }
#endif
}
