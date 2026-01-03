// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute on Transform references with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullTransformTarget : SerializedScriptableObject
    {
        [WNotNull]
        public Transform notNullTransform;
    }
#endif
}
