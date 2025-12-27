namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.EnumToggleButtons
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for WEnumToggleButtons with color key configurations.
    /// </summary>
    internal sealed class OdinEnumToggleButtonsColorKey : SerializedScriptableObject
    {
        [WEnumToggleButtons(colorKey: "default")]
        public SimpleTestEnum defaultColor;

        [WEnumToggleButtons(colorKey: "custom")]
        public SimpleTestEnum customColor;

        [WEnumToggleButtons(colorKey: null)]
        public SimpleTestEnum noColorKey;
    }
#endif
}
