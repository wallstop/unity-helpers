namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with Material fields.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownMaterialAsset : ScriptableObject
    {
        /// <summary>
        /// Material dropdown using static provider.
        /// </summary>
        [WValueDropDown(
            typeof(WValueDropDownObjectReferenceSource),
            nameof(WValueDropDownObjectReferenceSource.GetStaticMaterials)
        )]
        public Material selectedMaterial;
    }
#endif
}
