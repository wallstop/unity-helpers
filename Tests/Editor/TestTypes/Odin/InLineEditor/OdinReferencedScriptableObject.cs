namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR

    using Sirenix.OdinInspector;

    /// <summary>
    /// A referenced ScriptableObject used by inline editor tests.
    /// </summary>
    internal sealed class OdinReferencedScriptableObject : SerializedScriptableObject
    {
        public int intValue;

        public string stringValue;

        public float floatValue;

        public bool boolValue;
    }

#endif
}
