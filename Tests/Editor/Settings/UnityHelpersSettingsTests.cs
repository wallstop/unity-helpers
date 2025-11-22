#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
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
                using SerializedObject serializedSettings = new SerializedObject(settings);
                SerializedProperty patternsProperty = serializedSettings.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.SerializableTypeIgnorePatterns
                );
                patternsProperty.ClearArray();
                patternsProperty.InsertArrayElementAtIndex(0);
                SerializedProperty patternElement = patternsProperty.GetArrayElementAtIndex(0);
                patternElement
                    .FindPropertyRelative(
                        UnityHelpersSettings.SerializedPropertyNames.SerializableTypePattern
                    )
                    .stringValue = "^System\\.Int32$";
                SerializedProperty initializedProperty = serializedSettings.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.SerializableTypePatternsInitialized
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
                using SerializedObject restore = new SerializedObject(settings);
                SerializedProperty patternsProperty = restore.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.SerializableTypeIgnorePatterns
                );
                patternsProperty.ClearArray();
                for (int index = 0; index < backup.Length; index++)
                {
                    patternsProperty.InsertArrayElementAtIndex(index);
                    SerializedProperty element = patternsProperty.GetArrayElementAtIndex(index);
                    element
                        .FindPropertyRelative(
                            UnityHelpersSettings.SerializedPropertyNames.SerializableTypePattern
                        )
                        .stringValue = backup[index];
                }

                SerializedProperty initializedProperty = restore.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.SerializableTypePatternsInitialized
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
        public void SerializableDictionaryPageSizeClampsToBounds()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalDictionaryPageSize = settings.SerializableDictionaryPageSize;
            try
            {
                settings.SerializableDictionaryPageSize = UnityHelpersSettings.MinPageSize - 1;
                Assert.That(
                    UnityHelpersSettings.GetSerializableDictionaryPageSize(),
                    Is.EqualTo(UnityHelpersSettings.MinPageSize)
                );

                settings.SerializableDictionaryPageSize =
                    UnityHelpersSettings.MaxSerializableDictionaryPageSize + 25;
                Assert.That(
                    UnityHelpersSettings.GetSerializableDictionaryPageSize(),
                    Is.EqualTo(UnityHelpersSettings.MaxSerializableDictionaryPageSize)
                );
            }
            finally
            {
                settings.SerializableDictionaryPageSize = originalDictionaryPageSize;
            }
        }

        [Test]
        public void DictionaryAndSetPaginationSettingsRemainIndependent()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalDictionaryPageSize = settings.SerializableDictionaryPageSize;
            int originalSetPageSize = settings.SerializableSetPageSize;

            try
            {
                int dictionarySize = UnityHelpersSettings.MinPageSize + 6;
                int setSize = UnityHelpersSettings.MaxPageSize - 10;

                settings.SerializableDictionaryPageSize = dictionarySize;
                settings.SerializableSetPageSize = setSize;

                Assert.That(
                    UnityHelpersSettings.GetSerializableDictionaryPageSize(),
                    Is.EqualTo(dictionarySize)
                );
                Assert.That(UnityHelpersSettings.GetSerializableSetPageSize(), Is.EqualTo(setSize));
            }
            finally
            {
                settings.SerializableDictionaryPageSize = originalDictionaryPageSize;
                settings.SerializableSetPageSize = originalSetPageSize;
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
            UnityHelpersSettings.WButtonPaletteEntry lightThemeEntry =
                UnityHelpersSettings.ResolveWButtonPalette(
                    UnityHelpersSettings.WButtonLightThemeColorKey
                );
            UnityHelpersSettings.WButtonPaletteEntry darkThemeEntry =
                UnityHelpersSettings.ResolveWButtonPalette(
                    UnityHelpersSettings.WButtonDarkThemeColorKey
                );

            Color expectedButton = EditorGUIUtility.isProSkin
                ? new Color(0.35f, 0.35f, 0.35f, 1f)
                : new Color(0.78f, 0.78f, 0.78f, 1f);
            Color expectedText = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            Color expectedLightButton = new(0.78f, 0.78f, 0.78f, 1f);
            Color expectedDarkButton = new(0.35f, 0.35f, 0.35f, 1f);

            AssertColorsApproximately(expectedButton, defaultEntry.ButtonColor);
            AssertColorsApproximately(expectedText, defaultEntry.TextColor);
            AssertColorsApproximately(expectedButton, missingEntry.ButtonColor);
            AssertColorsApproximately(expectedText, missingEntry.TextColor);
            AssertColorsApproximately(expectedLightButton, lightThemeEntry.ButtonColor);
            AssertColorsApproximately(Color.black, lightThemeEntry.TextColor);
            AssertColorsApproximately(expectedDarkButton, darkThemeEntry.ButtonColor);
            AssertColorsApproximately(Color.white, darkThemeEntry.TextColor);
            Assert.IsTrue(
                UnityHelpersSettings.HasWButtonPaletteColorKey(
                    UnityHelpersSettings.WButtonLightThemeColorKey
                )
            );
            Assert.IsTrue(
                UnityHelpersSettings.HasWButtonPaletteColorKey(
                    UnityHelpersSettings.WButtonDarkThemeColorKey
                )
            );
            Assert.IsTrue(
                UnityHelpersSettings.HasWButtonPaletteColorKey(
                    UnityHelpersSettings.WButtonLegacyColorKey
                )
            );
        }

        [Test]
        public void LegacyPaletteMigratesIntoCustomColors()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty legacyPalette = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.LegacyWButtonPriorityColors
            );
            SerializedProperty customPalette = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
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
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton
                );
                SerializedProperty textColorProperty = valueProperty.FindPropertyRelative(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
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
                    SerializedProperty keyProperty = element.FindPropertyRelative(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonPriority
                    );
                    SerializedProperty buttonColorProperty = element.FindPropertyRelative(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton
                    );
                    SerializedProperty textColorProperty = element.FindPropertyRelative(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
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
                    element
                        .FindPropertyRelative(
                            UnityHelpersSettings.SerializedPropertyNames.WButtonPriority
                        )
                        .stringValue = "LegacyKey";
                    element
                        .FindPropertyRelative(
                            UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton
                        )
                        .colorValue = legacyButton;
                    element
                        .FindPropertyRelative(
                            UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                        )
                        .colorValue = Color.clear;
                }

                keys.arraySize = 0;
                values.arraySize = 0;

                serialized.ApplyModifiedPropertiesWithoutUndo();

                settings.OnEnable();

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
                        element
                            .FindPropertyRelative(
                                UnityHelpersSettings.SerializedPropertyNames.WButtonPriority
                            )
                            .stringValue = originalLegacy.Key;
                        element
                            .FindPropertyRelative(
                                UnityHelpersSettings
                                    .SerializedPropertyNames
                                    .WButtonCustomColorButton
                            )
                            .colorValue = originalLegacy.Button;
                        element
                            .FindPropertyRelative(
                                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                            )
                            .colorValue = originalLegacy.Text;
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
                    valueProperty
                        .FindPropertyRelative(
                            UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton
                        )
                        .colorValue = original.Button;
                    valueProperty
                        .FindPropertyRelative(
                            UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                        )
                        .colorValue = original.Text;
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
            Assert.That(
                UnityHelpersSettings.GetSerializableSetFoldoutSpeed(),
                Is.EqualTo(UnityHelpersSettings.DefaultFoldoutSpeed)
            );
            Assert.That(
                UnityHelpersSettings.GetSerializableSortedSetFoldoutSpeed(),
                Is.EqualTo(UnityHelpersSettings.DefaultFoldoutSpeed)
            );
        }

        [Test]
        public void FoldoutTweensEnabledByDefault()
        {
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenWButtonFoldouts());
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts());
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts());
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableSetFoldouts());
            Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableSortedSetFoldouts());
        }

        [Test]
        public void FoldoutTweenDefaultsRepairWhenUninitialized()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty wbuttonTweenProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonFoldoutTweenEnabled
            );
            SerializedProperty dictionaryTweenProperty = serialized.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableDictionaryFoldoutTweenEnabled
            );
            SerializedProperty sortedDictionaryTweenProperty = serialized.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableSortedDictionaryFoldoutTweenEnabled
            );
            SerializedProperty setTweenProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.SerializableSetFoldoutTweenEnabled
            );
            SerializedProperty sortedSetTweenProperty = serialized.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableSortedSetFoldoutTweenEnabled
            );
            SerializedProperty initializedProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.FoldoutTweenSettingsInitialized
            );

            bool originalWButton = wbuttonTweenProperty.boolValue;
            bool originalDictionary = dictionaryTweenProperty.boolValue;
            bool originalSorted = sortedDictionaryTweenProperty.boolValue;
            bool originalSet = setTweenProperty.boolValue;
            bool originalSortedSet = sortedSetTweenProperty.boolValue;
            bool originalInitialized = initializedProperty.boolValue;

            try
            {
                wbuttonTweenProperty.boolValue = false;
                dictionaryTweenProperty.boolValue = false;
                sortedDictionaryTweenProperty.boolValue = false;
                setTweenProperty.boolValue = false;
                sortedSetTweenProperty.boolValue = false;
                initializedProperty.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                settings.OnEnable();

                Assert.IsTrue(UnityHelpersSettings.ShouldTweenWButtonFoldouts());
                Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts());
                Assert.IsTrue(
                    UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts()
                );
                Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableSetFoldouts());
                Assert.IsTrue(UnityHelpersSettings.ShouldTweenSerializableSortedSetFoldouts());

                using SerializedObject verification = new SerializedObject(settings);
                verification.Update();
                Assert.IsTrue(
                    verification
                        .FindProperty(
                            UnityHelpersSettings
                                .SerializedPropertyNames
                                .FoldoutTweenSettingsInitialized
                        )
                        .boolValue
                );
            }
            finally
            {
                using SerializedObject restore = new SerializedObject(settings);
                restore
                    .FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonFoldoutTweenEnabled
                    )
                    .boolValue = originalWButton;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableDictionaryFoldoutTweenEnabled
                    )
                    .boolValue = originalDictionary;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableSortedDictionaryFoldoutTweenEnabled
                    )
                    .boolValue = originalSorted;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableSetFoldoutTweenEnabled
                    )
                    .boolValue = originalSet;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableSortedSetFoldoutTweenEnabled
                    )
                    .boolValue = originalSortedSet;
                restore
                    .FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.FoldoutTweenSettingsInitialized
                    )
                    .boolValue = originalInitialized;
                restore.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }
        }

        [Test]
        public void FoldoutTweenTogglesAffectBehavior()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty wbuttonTweenProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonFoldoutTweenEnabled
            );
            SerializedProperty dictionaryTweenProperty = serialized.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableDictionaryFoldoutTweenEnabled
            );
            SerializedProperty sortedDictionaryTweenProperty = serialized.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableSortedDictionaryFoldoutTweenEnabled
            );
            SerializedProperty setTweenProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.SerializableSetFoldoutTweenEnabled
            );
            SerializedProperty sortedSetTweenProperty = serialized.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableSortedSetFoldoutTweenEnabled
            );

            bool originalWButtonValue = wbuttonTweenProperty.boolValue;
            bool originalDictionaryValue = dictionaryTweenProperty.boolValue;
            bool originalSortedValue = sortedDictionaryTweenProperty.boolValue;
            bool originalSetValue = setTweenProperty.boolValue;
            bool originalSortedSetValue = sortedSetTweenProperty.boolValue;

            try
            {
                wbuttonTweenProperty.boolValue = false;
                dictionaryTweenProperty.boolValue = false;
                sortedDictionaryTweenProperty.boolValue = false;
                setTweenProperty.boolValue = false;
                sortedSetTweenProperty.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsFalse(UnityHelpersSettings.ShouldTweenWButtonFoldouts());
                Assert.IsFalse(UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts());
                Assert.IsFalse(
                    UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts()
                );
                Assert.IsFalse(UnityHelpersSettings.ShouldTweenSerializableSetFoldouts());
                Assert.IsFalse(UnityHelpersSettings.ShouldTweenSerializableSortedSetFoldouts());
            }
            finally
            {
                using SerializedObject restore = new SerializedObject(settings);
                restore
                    .FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonFoldoutTweenEnabled
                    )
                    .boolValue = originalWButtonValue;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableDictionaryFoldoutTweenEnabled
                    )
                    .boolValue = originalDictionaryValue;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableSortedDictionaryFoldoutTweenEnabled
                    )
                    .boolValue = originalSortedValue;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableSetFoldoutTweenEnabled
                    )
                    .boolValue = originalSetValue;
                restore
                    .FindProperty(
                        UnityHelpersSettings
                            .SerializedPropertyNames
                            .SerializableSortedSetFoldoutTweenEnabled
                    )
                    .boolValue = originalSortedSetValue;
                restore.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }
        }

        [Test]
        public void WGroupAutoIncludeConfigurationClampsToBounds()
        {
            UnityHelpersSettings.WGroupAutoIncludeConfiguration original =
                UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();

            try
            {
                UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                    UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                    1000
                );

                UnityHelpersSettings.WGroupAutoIncludeConfiguration configuration =
                    UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();

                Assert.AreEqual(
                    UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                    configuration.Mode
                );
                Assert.AreEqual(
                    UnityHelpersSettings.MaxWGroupAutoIncludeRowCount,
                    configuration.RowCount
                );
            }
            finally
            {
                UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                    original.Mode,
                    original.RowCount
                );
            }
        }

        [Test]
        public void ResolveWGroupColorKeyEnsuresPaletteEntry()
        {
            const string PaletteKey = "EditorTestGroupPaletteKey";
            string resolved = UnityHelpersSettings.EnsureWGroupColorKey(PaletteKey);
            Assert.IsNotNull(resolved);
            Assert.IsTrue(UnityHelpersSettings.HasWGroupPaletteColorKey(resolved));
            UnityHelpersSettings.WGroupPaletteEntry entry =
                UnityHelpersSettings.ResolveWGroupPalette(resolved);
            Assert.Greater(entry.BackgroundColor.a, 0f);
        }

        [Test]
        public void ResolveWGroupPaletteFallsBackToDefault()
        {
            UnityHelpersSettings.WGroupPaletteEntry groupEntry =
                UnityHelpersSettings.ResolveWGroupPalette(null);
            Color expectedBackground = EditorGUIUtility.isProSkin
                ? new Color(0.215f, 0.215f, 0.215f, 1f)
                : new Color(0.90f, 0.90f, 0.90f, 1f);
            Color expectedText = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            AssertColorsApproximately(expectedBackground, groupEntry.BackgroundColor);
            AssertColorsApproximately(expectedText, groupEntry.TextColor);
        }

        [Test]
        public void EnsureWFoldoutGroupColorKeyRegistersPalette()
        {
            const string PaletteKey = "EditorTestFoldoutPaletteKey";
            string resolved = UnityHelpersSettings.EnsureWFoldoutGroupColorKey(PaletteKey);
            Assert.IsNotNull(resolved);
            UnityHelpersSettings.WFoldoutGroupPaletteEntry entry =
                UnityHelpersSettings.ResolveWFoldoutGroupPalette(resolved);
            Assert.Greater(entry.BackgroundColor.a, 0f);
        }

        [Test]
        public void ResolveWFoldoutGroupPaletteFallsBackToDefault()
        {
            UnityHelpersSettings.WFoldoutGroupPaletteEntry entry =
                UnityHelpersSettings.ResolveWFoldoutGroupPalette(null);
            Color expectedBackground = EditorGUIUtility.isProSkin
                ? new Color(0.215f, 0.215f, 0.215f, 1f)
                : new Color(0.90f, 0.90f, 0.90f, 1f);
            Color expectedText = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            AssertColorsApproximately(expectedBackground, entry.BackgroundColor);
            AssertColorsApproximately(expectedText, entry.TextColor);
        }

        [Test]
        public void WFoldoutTweenSettingsRoundTripThroughSerializedObject()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            bool originalEnabled = UnityHelpersSettings.ShouldTweenWFoldoutGroups();
            float originalSpeed = UnityHelpersSettings.GetWFoldoutGroupTweenSpeed();

            bool newEnabled = !originalEnabled;
            float newSpeed =
                Math.Abs(originalSpeed - UnityHelpersSettings.MinFoldoutSpeed) < 0.01f
                    ? UnityHelpersSettings.MaxFoldoutSpeed
                    : UnityHelpersSettings.MinFoldoutSpeed;

            try
            {
                using SerializedObject serialized = new SerializedObject(settings);
                SerializedProperty enabledProperty = serialized.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WFoldoutGroupTweenEnabled
                );
                SerializedProperty speedProperty = serialized.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WFoldoutGroupTweenSpeed
                );

                enabledProperty.boolValue = newEnabled;
                speedProperty.floatValue = newSpeed;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                settings.SaveSettings();

                Assert.AreEqual(
                    newEnabled,
                    UnityHelpersSettings.ShouldTweenWFoldoutGroups(),
                    "ShouldTweenWFoldoutGroups should reflect serialized toggle changes."
                );
                Assert.That(
                    UnityHelpersSettings.GetWFoldoutGroupTweenSpeed(),
                    Is.EqualTo(
                        Mathf.Clamp(
                            newSpeed,
                            UnityHelpersSettings.MinFoldoutSpeed,
                            UnityHelpersSettings.MaxFoldoutSpeed
                        )
                    )
                );
            }
            finally
            {
                using SerializedObject restore = new SerializedObject(settings);
                restore
                    .FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WFoldoutGroupTweenEnabled
                    )
                    .boolValue = originalEnabled;
                restore
                    .FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WFoldoutGroupTweenSpeed
                    )
                    .floatValue = originalSpeed;
                restore.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }
        }

        [Test]
        public void WGroupAndFoldoutDictionariesPreserveCustomEntries()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            string wgroupKey = "TestGroupPaletteKey_Settings";
            string wfoldoutKey = "TestFoldoutPaletteKey_Settings";

            try
            {
                string resolvedGroup = UnityHelpersSettings.EnsureWGroupColorKey(wgroupKey);
                string resolvedFoldout = UnityHelpersSettings.EnsureWFoldoutGroupColorKey(
                    wfoldoutKey
                );

                UnityHelpersSettings.WGroupPaletteEntry groupPalette =
                    UnityHelpersSettings.ResolveWGroupPalette(resolvedGroup);
                UnityHelpersSettings.WFoldoutGroupPaletteEntry foldoutPalette =
                    UnityHelpersSettings.ResolveWFoldoutGroupPalette(resolvedFoldout);

                Assert.IsTrue(
                    UnityHelpersSettings.HasWGroupPaletteColorKey(resolvedGroup),
                    "WGroup palette should contain the user supplied key."
                );
                Assert.IsTrue(
                    UnityHelpersSettings.HasWFoldoutGroupPaletteColorKey(resolvedFoldout),
                    "WFoldout palette should contain the user supplied key."
                );
                Assert.Greater(groupPalette.BackgroundColor.a, 0f);
                Assert.Greater(foldoutPalette.BackgroundColor.a, 0f);
            }
            finally
            {
                using SerializedObject serialized = new SerializedObject(settings);
                SerializedProperty groupDictionary = serialized.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
                );
                RemoveDictionaryEntry(groupDictionary, wgroupKey);
                SerializedProperty foldoutDictionary = serialized.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WFoldoutGroupCustomColors
                );
                RemoveDictionaryEntry(foldoutDictionary, wfoldoutKey);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }
        }

        private static void AssertColorsApproximately(
            Color expected,
            Color actual,
            float tolerance = 0.01f
        )
        {
            Assert.That(Mathf.Abs(expected.r - actual.r), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(expected.g - actual.g), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(expected.b - actual.b), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(expected.a - actual.a), Is.LessThanOrEqualTo(tolerance));
        }

        private static void RemoveDictionaryEntry(SerializedProperty dictionaryProperty, string key)
        {
            if (dictionaryProperty == null || !dictionaryProperty.isArray)
            {
                return;
            }

            for (int index = dictionaryProperty.arraySize - 1; index >= 0; index--)
            {
                SerializedProperty element = dictionaryProperty.GetArrayElementAtIndex(index);
                SerializedProperty keyProperty = element.FindPropertyRelative("Key");
                if (
                    keyProperty != null
                    && string.Equals(
                        keyProperty.stringValue,
                        key,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    dictionaryProperty.DeleteArrayElementAtIndex(index);
                }
            }
        }
    }
}
#endif
