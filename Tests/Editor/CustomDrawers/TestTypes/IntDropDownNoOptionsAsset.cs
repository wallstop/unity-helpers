namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class IntDropDownNoOptionsAsset : ScriptableObject
    {
        [IntDropDown(
            typeof(IntDropDownEmptySource),
            nameof(IntDropDownEmptySource.GetEmptyOptions)
        )]
        public int unspecified;
    }
#endif
}
