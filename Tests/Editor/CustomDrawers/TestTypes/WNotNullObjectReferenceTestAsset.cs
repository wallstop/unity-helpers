// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WNotNull attribute with object reference fields.
    /// </summary>
    internal sealed class WNotNullObjectReferenceTestAsset : ScriptableObject
    {
        [WNotNull]
        public GameObject requiredGameObject;

        [WNotNull]
        public Transform requiredTransform;

        [WNotNull]
        public ScriptableObject requiredScriptableObject;

        [WNotNull]
        public Material requiredMaterial;
    }
#endif
}
