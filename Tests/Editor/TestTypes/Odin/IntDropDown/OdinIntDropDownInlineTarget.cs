namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.IntDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for IntDropDown attribute with multiple inline frame rate options.
    /// </summary>
    internal sealed class OdinIntDropDownInlineTarget : SerializedScriptableObject
    {
        [IntDropDown(30, 60, 90, 120, 144)]
        public int selectedFrameRate;
    }
#endif
}
