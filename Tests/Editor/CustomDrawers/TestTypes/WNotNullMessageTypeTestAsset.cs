// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WNotNull attribute with custom message types.
    /// </summary>
    internal sealed class WNotNullMessageTypeTestAsset : ScriptableObject
    {
        [WNotNull(WNotNullMessageType.Warning)]
        public GameObject warningField;

        [WNotNull(WNotNullMessageType.Error)]
        public GameObject errorField;

        [WNotNull]
        public GameObject defaultField;
    }
#endif
}
