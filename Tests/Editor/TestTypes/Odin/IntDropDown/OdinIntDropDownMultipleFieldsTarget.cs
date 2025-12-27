namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.IntDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for multiple IntDropDown fields on the same ScriptableObject.
    /// </summary>
    internal sealed class OdinIntDropDownMultipleFieldsTarget : SerializedScriptableObject
    {
        [IntDropDown(1, 2, 3)]
        public int firstDropdown;

        [IntDropDown(10, 20, 30)]
        public int secondDropdown;

        [IntDropDown(100, 200, 300)]
        public int thirdDropdown;
    }
#endif
}
