namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute on ScriptableObject field references with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullScriptableObjectFieldTarget : SerializedScriptableObject
    {
        [WNotNull]
        public OdinNotNullReferencedScriptableObject notNullScriptableObject;
    }
#endif
}
