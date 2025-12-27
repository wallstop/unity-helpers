namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute with Error message type with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullErrorTypeTarget : SerializedScriptableObject
    {
        [WNotNull(WNotNullMessageType.Error)]
        public GameObject notNullError;
    }
#endif
}
