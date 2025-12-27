namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;

    /// <summary>
    /// A referenced object used by OdinReadOnlyObjectReferenceTarget for testing.
    /// </summary>
    internal sealed class OdinReferencedObject : SerializedScriptableObject
    {
        public int value;
    }
#endif
}
