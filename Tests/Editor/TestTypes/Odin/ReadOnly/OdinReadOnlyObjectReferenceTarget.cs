namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WReadOnly attribute on object reference field with Odin Inspector.
    /// </summary>
    internal sealed class OdinReadOnlyObjectReferenceTarget : SerializedScriptableObject
    {
        [WReadOnly]
        public OdinReferencedObject readOnlyObject;
    }
#endif
}
