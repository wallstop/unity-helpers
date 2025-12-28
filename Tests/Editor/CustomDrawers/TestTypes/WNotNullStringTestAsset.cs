// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WNotNull attribute with string fields.
    /// </summary>
    internal sealed class WNotNullStringTestAsset : ScriptableObject
    {
        [WNotNull]
        public string requiredString;

        [WNotNull(WNotNullMessageType.Error, "Configuration name is required")]
        public string configName;
    }
#endif
}
