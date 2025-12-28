// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WNotNull attribute with custom messages.
    /// </summary>
    internal sealed class WNotNullCustomMessageTestAsset : ScriptableObject
    {
        [WNotNull("Player prefab is required for spawning")]
        public GameObject playerPrefab;

        [WNotNull(WNotNullMessageType.Error, "Audio source must be assigned for sound effects")]
        public AudioSource audioSource;

        [WNotNull(WNotNullMessageType.Warning, "Optional warning message")]
        public Transform optionalTransform;
    }
#endif
}
