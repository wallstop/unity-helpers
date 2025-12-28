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
    /// Test asset for WValueDropDown with nullable object reference (can select null).
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownNullableObjectReferenceAsset : ScriptableObject
    {
        /// <summary>
        /// The test objects that will be used as dropdown options.
        /// </summary>
        public List<WValueDropDownTestScriptableObject> availableOptions =
            new List<WValueDropDownTestScriptableObject>();

        /// <summary>
        /// Object reference field that may include null in options.
        /// </summary>
        [WValueDropDown(
            nameof(GetOptionsIncludingNull),
            typeof(WValueDropDownTestScriptableObject)
        )]
        public WValueDropDownTestScriptableObject selectedObjectOrNull;

        /// <summary>
        /// Returns the available objects including a null option.
        /// </summary>
        public IEnumerable<WValueDropDownTestScriptableObject> GetOptionsIncludingNull()
        {
            yield return null;
            foreach (WValueDropDownTestScriptableObject option in availableOptions)
            {
                yield return option;
            }
        }
    }
#endif
}
