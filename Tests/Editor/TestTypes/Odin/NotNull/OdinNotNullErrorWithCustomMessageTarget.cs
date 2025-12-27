namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute with Error type and custom message with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullErrorWithCustomMessageTarget : SerializedScriptableObject
    {
        [WNotNull(WNotNullMessageType.Error, "Critical: Audio source must be assigned!")]
        public AudioSource notNullErrorCustom;
    }
#endif
}
