namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropdownTestAsset : ScriptableObject
    {
        [IntDropDown(5, 10, 15)]
        public int missingValue = 5;

        [IntDropDown(5, 10, 15)]
        public int validValue = 10;
    }
#endif
}
