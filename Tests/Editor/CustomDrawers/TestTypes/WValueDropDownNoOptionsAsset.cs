namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class WValueDropDownNoOptionsAsset : ScriptableObject
    {
        [WValueDropDown(
            typeof(WValueDropDownEmptySource),
            nameof(WValueDropDownEmptySource.GetEmptyOptions)
        )]
        public int unspecified;
    }
#endif
}
