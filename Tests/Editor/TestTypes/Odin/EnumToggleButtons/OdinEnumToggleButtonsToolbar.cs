namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.EnumToggleButtons
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for WEnumToggleButtons with select all/none toolbar configurations.
    /// </summary>
    internal sealed class OdinEnumToggleButtonsToolbar : SerializedScriptableObject
    {
        [WEnumToggleButtons(showSelectAll: true, showSelectNone: true)]
        public TestFlagsEnum bothButtons;

        [WEnumToggleButtons(showSelectAll: true, showSelectNone: false)]
        public TestFlagsEnum selectAllOnly;

        [WEnumToggleButtons(showSelectAll: false, showSelectNone: true)]
        public TestFlagsEnum selectNoneOnly;

        [WEnumToggleButtons(showSelectAll: false, showSelectNone: false)]
        public TestFlagsEnum noButtons;
    }
#endif
}
