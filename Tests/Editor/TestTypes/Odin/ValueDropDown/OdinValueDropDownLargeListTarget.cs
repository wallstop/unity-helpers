namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValueDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WValueDropDown with large option list to test popup dropdown behavior.
    /// </summary>
    internal sealed class OdinValueDropDownLargeListTarget : SerializedScriptableObject
    {
        [WValueDropDown(typeof(TestLargeListProvider), nameof(TestLargeListProvider.GetLargeList))]
        public int largeListSelection;

        public static class TestLargeListProvider
        {
            public static IEnumerable<int> GetLargeList()
            {
                for (int i = 0; i < 100; i++)
                {
                    yield return i;
                }
            }
        }
    }
#endif
}
