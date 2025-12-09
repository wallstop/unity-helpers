namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class WValueDropDownTypeMismatchAsset : ScriptableObject
    {
        [WValueDropDown(1, 2, 3)]
        public Vector2 vector2FieldWithDropdown = Vector2.zero;

        [WValueDropDown("A", "B", "C")]
        public bool boolFieldWithDropdown = false;

        [WValueDropDown(1.5f, 2.5f, 3.5f)]
        public Color colorFieldWithDropdown = Color.white;
    }
#endif
}
