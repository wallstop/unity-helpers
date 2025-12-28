// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropDownInstanceMethodAsset : ScriptableObject
    {
        public List<int> dynamicValues = new();

        [IntDropDown(nameof(GetDynamicValues))]
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public int selection;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        internal IEnumerable<int> GetDynamicValues()
        {
            return dynamicValues;
        }
    }
#endif
}
