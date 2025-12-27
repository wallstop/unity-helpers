namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.ReadOnly
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WReadOnly attribute on MonoBehaviour with Odin Inspector.
    /// </summary>
    public sealed class OdinReadOnlyMonoBehaviourTarget : SerializedMonoBehaviour
    {
        [WReadOnly]
        public string readOnlyField;

        [WReadOnly]
        public float readOnlyFloatField;
    }
#endif
}
