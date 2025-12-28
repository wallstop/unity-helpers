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
    internal sealed class StringInListInstanceMethodAsset : ScriptableObject
    {
        public List<string> dynamicValues = new();

        [StringInList(nameof(GetDynamicValues))]
        public string selection;

        internal IEnumerable<string> GetDynamicValues()
        {
            return dynamicValues;
        }
    }
#endif
}
