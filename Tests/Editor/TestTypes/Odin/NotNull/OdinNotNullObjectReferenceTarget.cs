namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute on object references with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullObjectReferenceTarget : SerializedScriptableObject
    {
        [WNotNull]
        public OdinNotNullReferencedObject notNullObject;
    }
#endif
}
