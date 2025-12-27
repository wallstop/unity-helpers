namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.StringInList
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for StringInList attribute with string field type.
    /// </summary>
    internal sealed class OdinStringInListStringFieldTarget : SerializedScriptableObject
    {
        [StringInList("Easy", "Normal", "Hard", "Expert")]
        public string selectedDifficulty;
    }
#endif
}
