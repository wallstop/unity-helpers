// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class StringInListTypeMismatchAsset : ScriptableObject
    {
        [StringInList("A", "B", "C")]
        public float floatFieldWithStringInList = 0f;

        [StringInList("A", "B", "C")]
        public bool boolFieldWithStringInList = false;

        [StringInList("A", "B", "C")]
        public Vector3 vector3FieldWithStringInList = Vector3.zero;
    }
#endif
}
