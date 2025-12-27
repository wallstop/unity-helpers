namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.WButton
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;

    /// <summary>
    /// Test target for SerializedScriptableObject without WButton methods with Odin Inspector.
    /// </summary>
    internal sealed class OdinScriptableObjectNoButtons : SerializedScriptableObject
    {
        public float SomeFloat;
        public bool SomeBool;
    }
#endif
}
