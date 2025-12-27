namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WReadOnly attribute on ScriptableObject with Odin Inspector.
    /// </summary>
    internal sealed class OdinReadOnlyScriptableObjectTarget : SerializedScriptableObject
    {
        [WReadOnly]
        public string readOnlyField;

        [WReadOnly]
        public int readOnlyIntField;
    }
#endif
}
