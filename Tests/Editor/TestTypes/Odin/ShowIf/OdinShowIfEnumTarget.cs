namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ShowIf
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for WShowIf enum condition tests with Odin Inspector.
    /// </summary>
    internal sealed class OdinShowIfEnumTarget : SerializedScriptableObject
    {
        public TestModeEnum testMode;

        [WShowIf(nameof(testMode), expectedValues: new object[] { TestModeEnum.ModeA })]
        public int dependentField;
    }
#endif
}
