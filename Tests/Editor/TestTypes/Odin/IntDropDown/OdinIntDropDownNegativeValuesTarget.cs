namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.IntDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for IntDropDown attribute with negative integer values.
    /// </summary>
    internal sealed class OdinIntDropDownNegativeValuesTarget : SerializedScriptableObject
    {
        [IntDropDown(-10, -5, 0, 5, 10)]
        public int selectedValue;
    }
#endif
}
