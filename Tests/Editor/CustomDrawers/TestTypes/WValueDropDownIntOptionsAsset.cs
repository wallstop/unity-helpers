namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class WValueDropDownIntOptionsAsset : ScriptableObject
    {
        [WValueDropDown(10, 20, 30)]
        public int selection = 10;
    }
#endif
}
