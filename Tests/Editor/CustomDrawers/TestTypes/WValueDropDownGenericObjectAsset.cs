// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with generic UnityEngine.Object fields.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownGenericObjectAsset : ScriptableObject
    {
        /// <summary>
        /// The test objects that will be used as dropdown options.
        /// </summary>
        public List<UnityEngine.Object> availableObjects = new List<UnityEngine.Object>();

        /// <summary>
        /// Generic object reference field.
        /// </summary>
        [WValueDropDown(nameof(GetAvailableObjects), typeof(UnityEngine.Object))]
        public UnityEngine.Object selectedObject;

        /// <summary>
        /// Returns the available objects from this asset's list.
        /// </summary>
        public IEnumerable<UnityEngine.Object> GetAvailableObjects()
        {
            return availableObjects;
        }
    }
#endif
}
