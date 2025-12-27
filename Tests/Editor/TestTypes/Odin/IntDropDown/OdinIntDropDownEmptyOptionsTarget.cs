namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.IntDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for IntDropDown attribute with an empty options provider.
    /// </summary>
    internal sealed class OdinIntDropDownEmptyOptionsTarget : SerializedScriptableObject
    {
        [IntDropDown(typeof(TestEmptyIntProvider), nameof(TestEmptyIntProvider.GetEmptyOptions))]
        public int emptyDropdown;

        public static class TestEmptyIntProvider
        {
            public static IEnumerable<int> GetEmptyOptions()
            {
                return Array.Empty<int>();
            }
        }
    }
#endif
}
