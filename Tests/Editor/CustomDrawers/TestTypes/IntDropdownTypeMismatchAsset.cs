namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropdownTypeMismatchAsset : ScriptableObject
    {
        [IntDropDown(1, 2, 3)]
        public string stringFieldWithIntDropdown = string.Empty;

        [IntDropDown(1, 2, 3)]
        public float floatFieldWithIntDropdown = 0f;

        [IntDropDown(1, 2, 3)]
        public bool boolFieldWithIntDropdown = false;
    }
#endif
}
