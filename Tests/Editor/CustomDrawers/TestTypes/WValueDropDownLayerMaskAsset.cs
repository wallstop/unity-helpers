namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with LayerMask.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownLayerMaskAsset : ScriptableObject
    {
        [WValueDropDown(
            typeof(WValueDropDownUnityTypesSource),
            nameof(WValueDropDownUnityTypesSource.GetLayerMasks)
        )]
        public LayerMask selectedLayerMask;
    }
#endif
}
