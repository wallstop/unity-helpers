namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropdownLargeOptionsAsset : ScriptableObject
    {
        [IntDropDown(
            typeof(IntDropdownLargeSource),
            nameof(IntDropdownLargeSource.GetLargeOptions)
        )]
        public int selection = 100;
    }
#endif
}
