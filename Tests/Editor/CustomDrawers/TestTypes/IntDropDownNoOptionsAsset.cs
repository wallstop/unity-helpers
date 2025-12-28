// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropDownNoOptionsAsset : ScriptableObject
    {
        [IntDropDown(
            typeof(IntDropDownEmptySource),
            nameof(IntDropDownEmptySource.GetEmptyOptions)
        )]
        public int unspecified;
    }
#endif
}
