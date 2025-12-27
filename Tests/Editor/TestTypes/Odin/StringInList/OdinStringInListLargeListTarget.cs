namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.StringInList
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for StringInList attribute with large options list (pagination testing).
    /// </summary>
    internal sealed class OdinStringInListLargeListTarget : SerializedScriptableObject
    {
        [StringInList(
            typeof(TestLargeStringListProvider),
            nameof(TestLargeStringListProvider.GetLargeList)
        )]
        public string largeListSelection;

        public static class TestLargeStringListProvider
        {
            public static IEnumerable<string> GetLargeList()
            {
                for (int i = 0; i < 200; i++)
                {
                    yield return $"Item_{i:D3}";
                }
            }
        }
    }
#endif
}
