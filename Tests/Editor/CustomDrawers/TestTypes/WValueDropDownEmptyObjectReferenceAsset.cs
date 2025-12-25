namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with empty object reference options.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownEmptyObjectReferenceAsset : ScriptableObject
    {
        /// <summary>
        /// Object reference field with empty options.
        /// </summary>
        [WValueDropDown(
            typeof(WValueDropDownObjectReferenceSource),
            nameof(WValueDropDownObjectReferenceSource.GetEmptyScriptableObjects)
        )]
        public WValueDropDownTestScriptableObject emptySelection;
    }
#endif
}
