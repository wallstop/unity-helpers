namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropdownInstanceMethodAsset : ScriptableObject
    {
        public List<int> dynamicValues = new();

        [IntDropDown(nameof(GetDynamicValues))]
        public int selection;

        internal IEnumerable<int> GetDynamicValues()
        {
            return dynamicValues;
        }
    }
#endif
}
