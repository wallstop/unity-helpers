#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Settings
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;

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

        [Test]
        public void WButtonSettingsClampToBounds()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.WButtonPageSize;
            int originalHistorySize = settings.WButtonHistorySize;
            try
            {
                settings.WButtonPageSize = UnityHelpersSettings.MinPageSize - 100;
                settings.WButtonHistorySize = UnityHelpersSettings.MinWButtonHistorySize - 5;

                Assert.That(
                    UnityHelpersSettings.GetWButtonPageSize(),
                    Is.EqualTo(UnityHelpersSettings.MinPageSize)
                );
                Assert.That(
                    UnityHelpersSettings.GetWButtonHistorySize(),
                    Is.EqualTo(UnityHelpersSettings.MinWButtonHistorySize)
                );

                settings.WButtonPageSize = UnityHelpersSettings.MaxPageSize + 250;
                settings.WButtonHistorySize = UnityHelpersSettings.MaxWButtonHistorySize + 25;

                Assert.That(
                    UnityHelpersSettings.GetWButtonPageSize(),
                    Is.EqualTo(UnityHelpersSettings.MaxPageSize)
                );
                Assert.That(
                    UnityHelpersSettings.GetWButtonHistorySize(),
                    Is.EqualTo(UnityHelpersSettings.MaxWButtonHistorySize)
                );
            }
            finally
            {
                settings.WButtonPageSize = originalPageSize;
                settings.WButtonHistorySize = originalHistorySize;
            }
        }

        [Test]
        public void ResolveWButtonPaletteFallsBackToDefault()
        {
            UnityHelpersSettings.WButtonPaletteEntry defaultEntry =
                UnityHelpersSettings.ResolveWButtonPalette(
                    UnityHelpersSettings.DefaultWButtonColorKey
                );
            UnityHelpersSettings.WButtonPaletteEntry missingEntry =
                UnityHelpersSettings.ResolveWButtonPalette("NonExistentColorKey");

            Assert.That(missingEntry.ButtonColor, Is.EqualTo(defaultEntry.ButtonColor));
            Assert.That(missingEntry.TextColor, Is.EqualTo(defaultEntry.TextColor));
        }

        [Test]
        public void LegacyPaletteMigratesIntoCustomColors()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serialized = new(settings);
            serialized.Update();

            SerializedProperty legacyPalette = serialized.FindProperty(
                "legacyWButtonPriorityColors"
            );
            SerializedProperty customPalette = serialized.FindProperty("wbuttonCustomColors");
            SerializedProperty keys = customPalette.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty values = customPalette.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            List<(string Key, Color Button, Color Text)> originalEntries = new(keys.arraySize);
            for (int index = 0; index < keys.arraySize; index++)
            {
                SerializedProperty keyProperty = keys.GetArrayElementAtIndex(index);
                SerializedProperty valueProperty = values.GetArrayElementAtIndex(index);
                SerializedProperty buttonColorProperty = valueProperty.FindPropertyRelative(
                    "buttonColor"
                );
                SerializedProperty textColorProperty = valueProperty.FindPropertyRelative(
                    "textColor"
                );
                originalEntries.Add(
                    (
                        keyProperty.stringValue,
                        buttonColorProperty.colorValue,
                        textColorProperty.colorValue
                    )
                );
            }

            List<(string Key, Color Button, Color Text)> legacyEntries = new(
                legacyPalette != null ? legacyPalette.arraySize : 0
            );
            if (legacyPalette != null)
            {
                for (int index = 0; index < legacyPalette.arraySize; index++)
                {
                    SerializedProperty element = legacyPalette.GetArrayElementAtIndex(index);
                    SerializedProperty keyProperty = element.FindPropertyRelative("priority");
                    SerializedProperty buttonColorProperty = element.FindPropertyRelative(
                        "buttonColor"
                    );
                    SerializedProperty textColorProperty = element.FindPropertyRelative(
                        "textColor"
                    );
                    legacyEntries.Add(
                        (
                            keyProperty.stringValue,
                            buttonColorProperty.colorValue,
                            textColorProperty.colorValue
                        )
                    );
                }
            }

            Color legacyButton = new(0.1f, 0.6f, 0.9f, 1f);
            Color expectedText =
                WallstopStudios.UnityHelpers.Editor.Utils.WButton.WButtonColorUtility.GetReadableTextColor(
                    legacyButton
                );

            try
            {
                if (legacyPalette != null)
                {
                    legacyPalette.arraySize = 1;
                    SerializedProperty element = legacyPalette.GetArrayElementAtIndex(0);
                    element.FindPropertyRelative("priority").stringValue = "LegacyKey";
                    element.FindPropertyRelative("buttonColor").colorValue = legacyButton;
                    element.FindPropertyRelative("textColor").colorValue = Color.clear;
                }

                keys.arraySize = 0;
                values.arraySize = 0;

                serialized.ApplyModifiedPropertiesWithoutUndo();

                MethodInfo onEnable = typeof(UnityHelpersSettings).GetMethod(
                    "OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                onEnable.Invoke(settings, null);

                UnityHelpersSettings.WButtonPaletteEntry migrated =
                    UnityHelpersSettings.ResolveWButtonPalette("LegacyKey");
                Assert.That(migrated.ButtonColor, Is.EqualTo(legacyButton));
                Assert.That(migrated.TextColor, Is.EqualTo(expectedText));
            }
            finally
            {
                serialized.Update();

                if (legacyPalette != null)
                {
                    legacyPalette.arraySize = legacyEntries.Count;
                    for (int index = 0; index < legacyEntries.Count; index++)
                    {
                        SerializedProperty element = legacyPalette.GetArrayElementAtIndex(index);
                        (string Key, Color Button, Color Text) originalLegacy = legacyEntries[
                            index
                        ];
                        element.FindPropertyRelative("priority").stringValue = originalLegacy.Key;
                        element.FindPropertyRelative("buttonColor").colorValue =
                            originalLegacy.Button;
                        element.FindPropertyRelative("textColor").colorValue = originalLegacy.Text;
                    }
                }

                keys.arraySize = originalEntries.Count;
                values.arraySize = originalEntries.Count;
                for (int index = 0; index < originalEntries.Count; index++)
                {
                    (string Key, Color Button, Color Text) original = originalEntries[index];
                    SerializedProperty keyProperty = keys.GetArrayElementAtIndex(index);
                    keyProperty.stringValue = original.Key;
                    SerializedProperty valueProperty = values.GetArrayElementAtIndex(index);
                    valueProperty.FindPropertyRelative("buttonColor").colorValue = original.Button;
                    valueProperty.FindPropertyRelative("textColor").colorValue = original.Text;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }
        }

        [Test]
        public void WButtonPlacementDefaultsToTop()
        {
            Assert.That(
                UnityHelpersSettings.GetWButtonActionsPlacement(),
                Is.EqualTo(UnityHelpersSettings.WButtonActionsPlacement.Top)
            );
        }

        [Test]
        public void WButtonFoldoutBehaviorDefaultsToStartExpanded()
        {
            Assert.That(
                UnityHelpersSettings.GetWButtonFoldoutBehavior(),
                Is.EqualTo(UnityHelpersSettings.WButtonFoldoutBehavior.StartExpanded)
            );
        }

        [Test]
        public void FoldoutSpeedDefaultsMatchConstants()
        {
            Assert.That(
                UnityHelpersSettings.GetWButtonFoldoutSpeed(),
                Is.EqualTo(UnityHelpersSettings.DefaultFoldoutSpeed)
            );
            Assert.That(
                UnityHelpersSettings.GetSerializableDictionaryFoldoutSpeed(),
                Is.EqualTo(UnityHelpersSettings.DefaultFoldoutSpeed)
            );
            Assert.That(
                UnityHelpersSettings.GetSerializableSortedDictionaryFoldoutSpeed(),
                Is.EqualTo(UnityHelpersSettings.DefaultFoldoutSpeed)
            );
        }

        [Test]
        public void FoldoutTweensEnabledByDefault()
        {
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenWButtonFoldouts());
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts());
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts());
        }

        [Test]
        public void FoldoutTweenTogglesAffectBehavior()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serialized = new(settings);
            SerializedProperty wbuttonTweenProperty = serialized.FindProperty(
                "wbuttonFoldoutTweenEnabled"
            );
            SerializedProperty dictionaryTweenProperty = serialized.FindProperty(
                "serializableDictionaryFoldoutTweenEnabled"
            );
            SerializedProperty sortedDictionaryTweenProperty = serialized.FindProperty(
                "serializableSortedDictionaryFoldoutTweenEnabled"
            );

            bool originalWButtonValue = wbuttonTweenProperty.boolValue;
            bool originalDictionaryValue = dictionaryTweenProperty.boolValue;
            bool originalSortedValue = sortedDictionaryTweenProperty.boolValue;

            try
            {
                wbuttonTweenProperty.boolValue = false;
                dictionaryTweenProperty.boolValue = false;
                sortedDictionaryTweenProperty.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsFalse(UnityHelpersSettings.ShouldTweenWButtonFoldouts());
                Assert.IsFalse(UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts());
                Assert.IsFalse(
                    UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts()
                );
            }
            finally
            {
                SerializedObject restore = new(settings);
                restore.FindProperty("wbuttonFoldoutTweenEnabled").boolValue = originalWButtonValue;
                restore.FindProperty("serializableDictionaryFoldoutTweenEnabled").boolValue =
                    originalDictionaryValue;
                restore.FindProperty("serializableSortedDictionaryFoldoutTweenEnabled").boolValue =
                    originalSortedValue;
                restore.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }
        }
    }
}
#endif
