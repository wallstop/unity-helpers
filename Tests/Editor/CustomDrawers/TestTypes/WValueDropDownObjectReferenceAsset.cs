// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with object reference fields.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownObjectReferenceAsset : ScriptableObject
    {
        /// <summary>
        /// The test ScriptableObjects that will be used as dropdown options.
        /// </summary>
        public List<WValueDropDownTestScriptableObject> availableOptions =
            new List<WValueDropDownTestScriptableObject>();

        /// <summary>
        /// Object reference field using instance method provider.
        /// </summary>
        [WValueDropDown(
            nameof(GetAvailableScriptableObjects),
            typeof(WValueDropDownTestScriptableObject)
        )]
        public WValueDropDownTestScriptableObject selectedObject;

        /// <summary>
        /// Object reference field using static method provider.
        /// </summary>
        [WValueDropDown(
            typeof(WValueDropDownObjectReferenceSource),
            nameof(WValueDropDownObjectReferenceSource.GetStaticScriptableObjects)
        )]
        public WValueDropDownTestScriptableObject staticSelectedObject;

        /// <summary>
        /// Returns the available ScriptableObjects from this asset's list.
        /// </summary>
        public IEnumerable<WValueDropDownTestScriptableObject> GetAvailableScriptableObjects()
        {
            return availableOptions;
        }
    }
#endif
}
