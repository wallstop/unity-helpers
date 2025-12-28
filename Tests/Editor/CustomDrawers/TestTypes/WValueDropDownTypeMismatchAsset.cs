// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class WValueDropDownTypeMismatchAsset : ScriptableObject
    {
        [FormerlySerializedAs("vector2FieldWithDropdown")]
        [WValueDropDown(1, 2, 3)]
        public Vector2 vector2FieldWithDropDown = Vector2.zero;

        [FormerlySerializedAs("boolFieldWithDropdown")]
        [WValueDropDown("A", "B", "C")]
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public bool boolFieldWithDropDown = false;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        [FormerlySerializedAs("colorFieldWithDropdown")]
        [WValueDropDown(1.5f, 2.5f, 3.5f)]
        public Color colorFieldWithDropDown = Color.white;
    }
#endif
}
