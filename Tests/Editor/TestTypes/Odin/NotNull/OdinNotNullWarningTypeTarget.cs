namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute with Warning message type with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullWarningTypeTarget : SerializedScriptableObject
    {
        [WNotNull(WNotNullMessageType.Warning)]
        public GameObject notNullWarning;
    }
#endif
}
