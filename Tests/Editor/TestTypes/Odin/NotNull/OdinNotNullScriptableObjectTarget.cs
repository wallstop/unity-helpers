// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute on ScriptableObject with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullScriptableObjectTarget : SerializedScriptableObject
    {
        [WNotNull]
        public GameObject notNullObject;
    }
#endif
}
