// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WNotNull attribute with multiple field types to test various scenarios.
    /// </summary>
    internal sealed class WNotNullMixedFieldsTestAsset : ScriptableObject
    {
        [WNotNull]
        public GameObject nullableGameObject;

        [WNotNull]
        public string nullableString;

        [WNotNull]
        public Sprite nullableSprite;

        [WNotNull]
        public AudioClip nullableAudioClip;

        public int nonDecoratedIntField;

        public string nonDecoratedStringField;

        public GameObject nonDecoratedGameObject;
    }
#endif
}
