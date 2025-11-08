#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Settings
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    public sealed class UnityHelpersSettingsTests
    {
        [Test]
        public void SaveSettingsPropagatesRegexConfiguration()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            IReadOnlyList<string> originalPatterns = settings.GetSerializableTypeIgnorePatterns();
            bool wasConfigured = !ReferenceEquals(
                SerializableTypeCatalog.GetActiveIgnorePatterns(),
                SerializableTypeCatalog.GetDefaultIgnorePatterns()
            );
            string[] backup = originalPatterns?.ToArray() ?? System.Array.Empty<string>();

            try
            {
                SerializedObject serializedSettings = new(settings);
                SerializedProperty patternsProperty = serializedSettings.FindProperty(
                    "serializableTypeIgnorePatterns"
                );
                patternsProperty.ClearArray();
                patternsProperty.InsertArrayElementAtIndex(0);
                SerializedProperty patternElement = patternsProperty.GetArrayElementAtIndex(0);
                patternElement.FindPropertyRelative("pattern").stringValue = "^System\\.Int32$";
                SerializedProperty initializedProperty = serializedSettings.FindProperty(
                    "serializableTypePatternsInitialized"
                );
                initializedProperty.boolValue = true;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();

                settings.SaveSettings();

                Assert.IsTrue(
                    SerializableTypeCatalog.ShouldSkipType(typeof(int)),
                    "Configured regex should exclude System.Int32 from the catalog."
                );
                Assert.IsFalse(
                    SerializableTypeCatalog.ShouldSkipType(typeof(string)),
                    "Other types should remain available when only System.Int32 is ignored."
                );
            }
            finally
            {
                SerializedObject restore = new(settings);
                SerializedProperty patternsProperty = restore.FindProperty(
                    "serializableTypeIgnorePatterns"
                );
                patternsProperty.ClearArray();
                for (int index = 0; index < backup.Length; index++)
                {
                    patternsProperty.InsertArrayElementAtIndex(index);
                    SerializedProperty element = patternsProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("pattern").stringValue = backup[index];
                }

                SerializedProperty initializedProperty = restore.FindProperty(
                    "serializableTypePatternsInitialized"
                );
                initializedProperty.boolValue = true;
                restore.ApplyModifiedPropertiesWithoutUndo();

                settings.SaveSettings();

                SerializableTypeCatalog.ConfigureTypeNameIgnorePatterns(
                    wasConfigured ? backup : null
                );
            }
        }
    }
}
#endif
