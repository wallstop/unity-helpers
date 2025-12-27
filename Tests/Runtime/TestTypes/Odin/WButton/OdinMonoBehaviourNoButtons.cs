namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.WButton
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;

    /// <summary>
    /// Test target for SerializedMonoBehaviour without WButton methods with Odin Inspector.
    /// </summary>
    public sealed class OdinMonoBehaviourNoButtons : SerializedMonoBehaviour
    {
        public int SomeValue;
        public string SomeName;
    }
#endif
}
