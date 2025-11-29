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
            string[] backup = originalPatterns?.ToArray() ?? Array.Empty<string>();

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
        public void EnsureWButtonCustomColorDefaults_ManualEditSkipsAutoSuggestion()
        {
            const string PaletteKey = "EditorManualWButton";
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty paletteProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            PaletteEntrySnapshot snapshot = CapturePaletteEntrySnapshot(
                paletteProperty,
                PaletteKey,
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
            );
            HashSet<string> skipSnapshot = CloneSkipSet(settings, WButtonSkipFieldName);

            try
            {
                SetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    Color.white,
                    Color.black,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();

                UnityHelpersSettings.RegisterPaletteManualEdit(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors,
                    PaletteKey
                );

                InvokeEnsureMethod(settings, "EnsureWButtonCustomColorDefaults");
                serialized.Update();

                (Color button, Color text) = GetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );

                AssertColorsApproximately(Color.white, button);
                AssertColorsApproximately(Color.black, text);
                HashSet<string> skipSet = GetSkipSet(settings, WButtonSkipFieldName);
                Assert.IsTrue(
                    skipSet != null && skipSet.Contains(PaletteKey),
                    "Manual edit should remain in the skip set."
                );
            }
            finally
            {
                RestorePaletteEntry(
                    paletteProperty,
                    PaletteKey,
                    snapshot,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();
                RestoreSkipSet(settings, WButtonSkipFieldName, skipSnapshot);
            }
        }

        [Test]
        public void EnsureWButtonCustomColorDefaults_SuggestsColorsWithoutManualEdit()
        {
            const string PaletteKey = "EditorAutoWButton";
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty paletteProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            PaletteEntrySnapshot snapshot = CapturePaletteEntrySnapshot(
                paletteProperty,
                PaletteKey,
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
            );
            HashSet<string> skipSnapshot = CloneSkipSet(settings, WButtonSkipFieldName);

            try
            {
                SetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    Color.white,
                    Color.black,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();

                InvokeEnsureMethod(settings, "EnsureWButtonCustomColorDefaults");
                serialized.Update();

                (Color _, Color text) = GetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );

                Assert.IsFalse(
                    ColorsApproximatelyEqual(Color.black, text),
                    "Auto-suggestion should adjust the text color when no manual edit is present."
                );
            }
            finally
            {
                RestorePaletteEntry(
                    paletteProperty,
                    PaletteKey,
                    snapshot,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();
                RestoreSkipSet(settings, WButtonSkipFieldName, skipSnapshot);
            }
        }

        [Test]
        public void EnsureWGroupCustomColorDefaults_ManualEditSkipsAutoSuggestion()
        {
            const string PaletteKey = "EditorManualWGroup";
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty paletteProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
            );
            PaletteEntrySnapshot snapshot = CapturePaletteEntrySnapshot(
                paletteProperty,
                PaletteKey,
                BackgroundColorField,
                GroupTextColorField
            );
            HashSet<string> skipSnapshot = CloneSkipSet(settings, WGroupSkipFieldName);

            try
            {
                SetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    Color.white,
                    Color.black,
                    BackgroundColorField,
                    GroupTextColorField
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();

                UnityHelpersSettings.RegisterPaletteManualEdit(
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors,
                    PaletteKey
                );

                InvokeEnsureMethod(settings, "EnsureWGroupCustomColorDefaults");
                serialized.Update();

                (Color background, Color text) = GetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    BackgroundColorField,
                    GroupTextColorField
                );

                AssertColorsApproximately(Color.white, background);
                AssertColorsApproximately(Color.black, text);
            }
            finally
            {
                RestorePaletteEntry(
                    paletteProperty,
                    PaletteKey,
                    snapshot,
                    BackgroundColorField,
                    GroupTextColorField
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();
                RestoreSkipSet(settings, WGroupSkipFieldName, skipSnapshot);
            }
        }

        [Test]
        public void SerializableDictionaryDrawerCommit_RegistersManualEditForPalette()
        {
            const string PaletteKey = "EditorDrawerPaletteKey";
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty paletteProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            PaletteEntrySnapshot snapshot = CapturePaletteEntrySnapshot(
                paletteProperty,
                PaletteKey,
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
            );
            HashSet<string> skipSnapshot = CloneSkipSet(settings, WButtonSkipFieldName);

            try
            {
                SerializableDictionaryPropertyDrawer drawer = new();
                Type paletteValueType = typeof(UnityHelpersSettings).GetNestedType(
                    "WButtonCustomColor",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                Assert.NotNull(paletteValueType);

                object paletteValue = Activator.CreateInstance(paletteValueType, nonPublic: true);
                FieldInfo buttonField = paletteValueType.GetField(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                FieldInfo textField = paletteValueType.GetField(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText,
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                buttonField.SetValue(paletteValue, Color.cyan);
                textField.SetValue(paletteValue, Color.black);

                SerializableDictionaryPropertyDrawer.PendingEntry pending =
                    drawer.GetOrCreatePendingEntry(
                        paletteProperty,
                        typeof(string),
                        paletteValueType,
                        isSortedDictionary: false
                    );
                pending.key = PaletteKey;
                pending.value = paletteValue;

                SerializedProperty keysProperty = paletteProperty.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Keys
                );
                SerializedProperty valuesProperty = paletteProperty.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Values
                );

                drawer.CommitEntry(
                    keysProperty,
                    valuesProperty,
                    typeof(string),
                    paletteValueType,
                    pending,
                    existingIndex: -1,
                    paletteProperty
                );

                serialized.Update();
                HashSet<string> skipSet = GetSkipSet(settings, WButtonSkipFieldName);
                Assert.IsTrue(
                    skipSet != null && skipSet.Contains(PaletteKey),
                    "Drawer commit should register palette manual edits."
                );
            }
            finally
            {
                RestorePaletteEntry(
                    paletteProperty,
                    PaletteKey,
                    snapshot,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();
                RestoreSkipSet(settings, WButtonSkipFieldName, skipSnapshot);
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

        private const string WButtonSkipFieldName = "wbuttonCustomColorSkipAutoSuggest";
        private const string WGroupSkipFieldName = "wgroupCustomColorSkipAutoSuggest";
        private const string BackgroundColorField = "backgroundColor";
        private const string GroupTextColorField = "textColor";

        private readonly struct PaletteEntrySnapshot
        {
            public PaletteEntrySnapshot(bool exists, Color button, Color text)
            {
                Exists = exists;
                Button = button;
                Text = text;
            }

            public bool Exists { get; }
            public Color Button { get; }
            public Color Text { get; }
        }

        private static PaletteEntrySnapshot CapturePaletteEntrySnapshot(
            SerializedProperty dictionaryProperty,
            string key,
            string buttonField,
            string textField
        )
        {
            (SerializedProperty keys, SerializedProperty values) = GetDictionaryArrays(
                dictionaryProperty
            );
            int index = FindDictionaryIndex(keys, key);
            if (index < 0)
            {
                return new PaletteEntrySnapshot(false, default, default);
            }

            SerializedProperty valueProperty = values.GetArrayElementAtIndex(index);
            Color button =
                valueProperty.FindPropertyRelative(buttonField)?.colorValue ?? Color.clear;
            Color text = valueProperty.FindPropertyRelative(textField)?.colorValue ?? Color.clear;
            return new PaletteEntrySnapshot(true, button, text);
        }

        private static void RestorePaletteEntry(
            SerializedProperty dictionaryProperty,
            string key,
            PaletteEntrySnapshot snapshot,
            string buttonField,
            string textField
        )
        {
            if (snapshot.Exists)
            {
                SetPaletteEntryColors(
                    dictionaryProperty,
                    key,
                    snapshot.Button,
                    snapshot.Text,
                    buttonField,
                    textField
                );
            }
            else
            {
                RemovePaletteEntry(dictionaryProperty, key);
            }
        }

        private static void SetPaletteEntryColors(
            SerializedProperty dictionaryProperty,
            string key,
            Color buttonColor,
            Color textColor,
            string buttonField,
            string textField
        )
        {
            (SerializedProperty keys, SerializedProperty values) = GetDictionaryArrays(
                dictionaryProperty
            );
            int index = FindDictionaryIndex(keys, key);
            if (index < 0)
            {
                index = keys.arraySize;
                keys.InsertArrayElementAtIndex(index);
                values.InsertArrayElementAtIndex(index);
                keys.GetArrayElementAtIndex(index).stringValue = key;
            }

            SerializedProperty valueProperty = values.GetArrayElementAtIndex(index);
            valueProperty.FindPropertyRelative(buttonField).colorValue = buttonColor;
            valueProperty.FindPropertyRelative(textField).colorValue = textColor;
        }

        private static (Color Button, Color Text) GetPaletteEntryColors(
            SerializedProperty dictionaryProperty,
            string key,
            string buttonField,
            string textField
        )
        {
            (SerializedProperty keys, SerializedProperty values) = GetDictionaryArrays(
                dictionaryProperty
            );
            int index = FindDictionaryIndex(keys, key);
            Assert.GreaterOrEqual(index, 0, $"Palette entry '{key}' was not found.");

            SerializedProperty valueProperty = values.GetArrayElementAtIndex(index);
            return (
                valueProperty.FindPropertyRelative(buttonField).colorValue,
                valueProperty.FindPropertyRelative(textField).colorValue
            );
        }

        private static void RemovePaletteEntry(SerializedProperty dictionaryProperty, string key)
        {
            (SerializedProperty keys, SerializedProperty values) = GetDictionaryArrays(
                dictionaryProperty
            );
            int index = FindDictionaryIndex(keys, key);
            if (index < 0)
            {
                return;
            }

            keys.DeleteArrayElementAtIndex(index);
            values.DeleteArrayElementAtIndex(index);
        }

        private static (SerializedProperty Keys, SerializedProperty Values) GetDictionaryArrays(
            SerializedProperty dictionaryProperty
        )
        {
            SerializedProperty keys = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty values = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            return (keys, values);
        }

        private static int FindDictionaryIndex(SerializedProperty keysProperty, string targetKey)
        {
            for (int index = 0; index < keysProperty.arraySize; index++)
            {
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                if (
                    string.Equals(
                        keyProperty.stringValue,
                        targetKey,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return index;
                }
            }

            return -1;
        }

        private static void InvokeEnsureMethod(UnityHelpersSettings settings, string methodName)
        {
            MethodInfo method = typeof(UnityHelpersSettings).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.NotNull(method, $"Unable to resolve method '{methodName}'.");
            method.Invoke(settings, null);
        }

        private static HashSet<string> CloneSkipSet(UnityHelpersSettings settings, string fieldName)
        {
            HashSet<string> existing = GetSkipSet(settings, fieldName);
            return existing != null
                ? new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase)
                : null;
        }

        private static HashSet<string> GetSkipSet(UnityHelpersSettings settings, string fieldName)
        {
            FieldInfo field = typeof(UnityHelpersSettings).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.NotNull(field, $"Unable to resolve field '{fieldName}'.");
            return field.GetValue(settings) as HashSet<string>;
        }

        private static void RestoreSkipSet(
            UnityHelpersSettings settings,
            string fieldName,
            HashSet<string> snapshot
        )
        {
            FieldInfo field = typeof(UnityHelpersSettings).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.NotNull(field, $"Unable to resolve field '{fieldName}'.");
            if (snapshot == null)
            {
                field.SetValue(settings, null);
                return;
            }

            field.SetValue(
                settings,
                new HashSet<string>(snapshot, StringComparer.OrdinalIgnoreCase)
            );
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance
                && Mathf.Abs(a.g - b.g) <= tolerance
                && Mathf.Abs(a.b - b.b) <= tolerance
                && Mathf.Abs(a.a - b.a) <= tolerance;
        }
    }
}
#endif
