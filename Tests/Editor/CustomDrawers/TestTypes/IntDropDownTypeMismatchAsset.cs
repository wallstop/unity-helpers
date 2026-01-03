// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropDownTypeMismatchAsset : ScriptableObject
    {
        [FormerlySerializedAs("stringFieldWithIntDropdown")]
        [IntDropDown(1, 2, 3)]
        public string stringFieldWithIntDropDown = string.Empty;

        [FormerlySerializedAs("floatFieldWithIntDropdown")]
        [IntDropDown(1, 2, 3)]
        public float floatFieldWithIntDropDown = 0f;

        [FormerlySerializedAs("boolFieldWithIntDropdown")]
        [IntDropDown(1, 2, 3)]
        public bool boolFieldWithIntDropDown = false;
    }
#endif
}
