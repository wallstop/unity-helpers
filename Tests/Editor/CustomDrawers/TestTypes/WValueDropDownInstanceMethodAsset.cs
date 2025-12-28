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
    internal sealed class WValueDropDownInstanceMethodAsset : ScriptableObject
    {
        public List<int> dynamicValues = new();

        [WValueDropDown(nameof(GetDynamicValues), typeof(int))]
        public int selection;

        internal IEnumerable<int> GetDynamicValues()
        {
            return dynamicValues;
        }
    }
#endif
}
