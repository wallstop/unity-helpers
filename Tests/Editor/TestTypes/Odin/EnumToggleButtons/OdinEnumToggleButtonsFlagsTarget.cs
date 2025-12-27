namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.EnumToggleButtons
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for WEnumToggleButtons with flags enum on SerializedScriptableObject.
    /// </summary>
    internal sealed class OdinEnumToggleButtonsFlagsTarget : SerializedScriptableObject
    {
        [WEnumToggleButtons]
        public TestFlagsEnum flags;
    }
#endif
}
