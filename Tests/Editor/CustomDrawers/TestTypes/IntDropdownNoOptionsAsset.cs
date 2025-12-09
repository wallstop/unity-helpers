namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropdownNoOptionsAsset : ScriptableObject
    {
        [IntDropDown(
            typeof(IntDropdownEmptySource),
            nameof(IntDropdownEmptySource.GetEmptyOptions)
        )]
        public int unspecified;
    }
#endif
}
