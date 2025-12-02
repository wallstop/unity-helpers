#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Extensions;
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
        public void SerializableTypeIgnorePatternsCanBeAppendedAndReset()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty patterns = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.SerializableTypeIgnorePatterns
            );
            Assert.NotNull(patterns);

            patterns.ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();

            SerializedProperty element = patterns.AppendArrayElement();
            element
                .FindPropertyRelative(
                    UnityHelpersSettings.SerializedPropertyNames.SerializableTypePattern
                )
                .stringValue = "^CustomPattern$";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            serialized.Update();
            Assert.That(patterns.arraySize, Is.EqualTo(1));

            patterns.ClearArray();
            IReadOnlyList<string> defaults = SerializableTypeCatalog.GetDefaultIgnorePatterns();
            for (int index = 0; index < defaults.Count; index++)
            {
                SerializedProperty defaultElement = patterns.AppendArrayElement();
                defaultElement
                    .FindPropertyRelative(
                        UnityHelpersSettings.SerializedPropertyNames.SerializableTypePattern
                    )
                    .stringValue = defaults[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();

            Assert.That(patterns.arraySize, Is.EqualTo(defaults.Count));
        }

        [Test]
        public void SerializableCollectionFoldoutDefaultsAreConfigurable()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            bool originalDictionarySetting = settings.SerializableDictionaryStartCollapsed;
            bool originalSetSetting = settings.SerializableSetStartCollapsed;

            try
            {
                settings.SerializableDictionaryStartCollapsed = false;
                settings.SerializableSetStartCollapsed = false;

                Assert.IsFalse(UnityHelpersSettings.ShouldStartSerializableDictionaryCollapsed());
                Assert.IsFalse(UnityHelpersSettings.ShouldStartSerializableSetCollapsed());

                settings.SerializableDictionaryStartCollapsed = true;
                settings.SerializableSetStartCollapsed = true;

                Assert.IsTrue(UnityHelpersSettings.ShouldStartSerializableDictionaryCollapsed());
                Assert.IsTrue(UnityHelpersSettings.ShouldStartSerializableSetCollapsed());
            }
            finally
            {
                settings.SerializableDictionaryStartCollapsed = originalDictionarySetting;
                settings.SerializableSetStartCollapsed = originalSetSetting;
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
            HashSet<string> skipSnapshot = CloneSkipSet(settings.WButtonCustomColorSkipAutoSuggest);

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

                settings.EnsureWButtonCustomColorDefaults();
                serialized.Update();

                (Color button, Color text) = GetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );

                AssertColorsApproximately(Color.white, button);
                AssertColorsApproximately(Color.black, text);
                HashSet<string> skipSet = settings.WButtonCustomColorSkipAutoSuggest;
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
                settings.WButtonCustomColorSkipAutoSuggest = CloneSkipSet(skipSnapshot);
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
            HashSet<string> skipSnapshot = CloneSkipSet(settings.WButtonCustomColorSkipAutoSuggest);

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

                settings.EnsureWButtonCustomColorDefaults();
                serialized.Update();

                (Color button, Color text) = GetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorButton,
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColorText
                );

                Assert.IsFalse(
                    ColorsApproximatelyEqual(Color.white, button),
                    "Auto-suggestion should replace the placeholder button color."
                );
                Color expectedText = WButtonColorUtility.GetReadableTextColor(button);
                Assert.IsTrue(
                    ColorsApproximatelyEqual(expectedText, text),
                    $"Auto-suggested text color should match readability for {button}. Expected {expectedText}, observed {text}."
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
                settings.WButtonCustomColorSkipAutoSuggest = CloneSkipSet(skipSnapshot);
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
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
            );
            HashSet<string> skipSnapshot = CloneSkipSet(settings.WGroupCustomColorSkipAutoSuggest);

            try
            {
                SetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    Color.white,
                    Color.black,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();

                UnityHelpersSettings.RegisterPaletteManualEdit(
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors,
                    PaletteKey
                );

                settings.EnsureWGroupCustomColorDefaults();
                serialized.Update();

                (Color background, Color text) = GetPaletteEntryColors(
                    paletteProperty,
                    PaletteKey,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
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
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();
                settings.WGroupCustomColorSkipAutoSuggest = CloneSkipSet(skipSnapshot);
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
            HashSet<string> skipSnapshot = CloneSkipSet(settings.WButtonCustomColorSkipAutoSuggest);

            try
            {
                SerializableDictionaryPropertyDrawer drawer = new();
                Type paletteValueType = typeof(UnityHelpersSettings.WButtonCustomColor);
                UnityHelpersSettings.WButtonCustomColor paletteValue = new()
                {
                    ButtonColor = Color.cyan,
                    TextColor = Color.black,
                };

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
                    PaletteKey,
                    paletteValue,
                    dictionaryProperty: paletteProperty,
                    existingIndex: -1
                );

                serialized.Update();
                HashSet<string> skipSet = settings.WButtonCustomColorSkipAutoSuggest;
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
                settings.WButtonCustomColorSkipAutoSuggest = CloneSkipSet(skipSnapshot);
            }
        }

        [Test]
        public void SerializableDictionaryDrawerCommitRegistersGroupPaletteManualEdit()
        {
            const string PaletteKey = "EditorDrawerGroupPaletteKey";
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty paletteProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
            );
            PaletteEntrySnapshot snapshot = CapturePaletteEntrySnapshot(
                paletteProperty,
                PaletteKey,
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
            );
            HashSet<string> skipSnapshot = CloneSkipSet(settings.WGroupCustomColorSkipAutoSuggest);

            try
            {
                SerializableDictionaryPropertyDrawer drawer = new();
                Type paletteValueType = typeof(UnityHelpersSettings.WGroupCustomColor);
                UnityHelpersSettings.WGroupCustomColor paletteValue = new()
                {
                    BackgroundColor = Color.magenta,
                    TextColor = Color.white,
                };

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
                    PaletteKey,
                    paletteValue,
                    dictionaryProperty: paletteProperty,
                    existingIndex: -1
                );

                serialized.Update();
                HashSet<string> skipSet = settings.WGroupCustomColorSkipAutoSuggest;
                Assert.IsTrue(
                    skipSet != null && skipSet.Contains(PaletteKey),
                    "Drawer commit should register group palette manual edits."
                );
            }
            finally
            {
                RestorePaletteEntry(
                    paletteProperty,
                    PaletteKey,
                    snapshot,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();
                settings.WGroupCustomColorSkipAutoSuggest = CloneSkipSet(skipSnapshot);
            }
        }

        [Test]
        public void PaletteSortButtonOrdersProjectSettingsKeysLexically()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            using SerializedObject serialized = new SerializedObject(settings);
            serialized.Update();

            SerializedProperty paletteProperty = serialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
            );
            PaletteDictionarySnapshot snapshot = CapturePaletteDictionarySnapshot(
                paletteProperty,
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
            );

            try
            {
                PaletteDictionaryEntrySnapshot[] testEntries =
                {
                    new PaletteDictionaryEntrySnapshot(
                        "SortZeta",
                        new Color(0.9f, 0.2f, 0.2f, 1f),
                        Color.white
                    ),
                    new PaletteDictionaryEntrySnapshot(
                        "SortAlpha",
                        new Color(0.2f, 0.6f, 0.3f, 1f),
                        Color.black
                    ),
                    new PaletteDictionaryEntrySnapshot(
                        "SortMid",
                        new Color(0.2f, 0.3f, 0.8f, 1f),
                        Color.white
                    ),
                };

                OverwritePaletteDictionary(
                    paletteProperty,
                    testEntries,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializableDictionaryPropertyDrawer drawer = new();
                SerializedProperty keysProperty = paletteProperty.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Keys
                );
                SerializedProperty valuesProperty = paletteProperty.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Values
                );
                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(paletteProperty);
                pagination.selectedIndex = 0;
                ReorderableList list = drawer.GetOrCreateList(paletteProperty);

                drawer.SortDictionaryEntries(
                    paletteProperty,
                    keysProperty,
                    valuesProperty,
                    typeof(string),
                    typeof(UnityHelpersSettings.WGroupCustomColor),
                    Comparison,
                    pagination,
                    list
                );

                serialized.Update();
                string[] expectedOrder = { "SortAlpha", "SortMid", "SortZeta" };
                string[] serializedKeys = ExtractDictionaryKeys(keysProperty);
                List<string> filtered = serializedKeys
                    .Where(key => key != null && key.StartsWith("Sort", StringComparison.Ordinal))
                    .ToList();
                CollectionAssert.AreEqual(
                    expectedOrder,
                    filtered,
                    "Custom palette keys should be sorted lexically."
                );

                int firstCustomIndex = Array.IndexOf(serializedKeys, expectedOrder[0]);
                int lastCustomIndex = Array.IndexOf(serializedKeys, expectedOrder[^1]);
                Assert.GreaterOrEqual(
                    firstCustomIndex,
                    0,
                    "Sorted custom keys should remain in the serialized arrays."
                );
                Assert.GreaterOrEqual(
                    lastCustomIndex,
                    firstCustomIndex,
                    "Custom palette keys should remain contiguous after sorting."
                );
                Assert.AreEqual(
                    expectedOrder.Length - 1,
                    lastCustomIndex - firstCustomIndex,
                    "Sorted custom palette keys should occupy a single contiguous range."
                );

                string[] reservedKeys =
                {
                    UnityHelpersSettings.WGroupLegacyColorKey,
                    UnityHelpersSettings.WGroupLightThemeColorKey,
                    UnityHelpersSettings.WGroupDarkThemeColorKey,
                };
                foreach (string reserved in reservedKeys)
                {
                    Assert.Contains(
                        reserved,
                        serializedKeys,
                        $"Reserved palette key '{reserved}' should remain serialized."
                    );
                }
            }
            finally
            {
                RestorePaletteDictionary(
                    paletteProperty,
                    snapshot,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground,
                    UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
                );
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            int Comparison(object left, object right)
            {
                string leftKey = left as string ?? left?.ToString();
                string rightKey = right as string ?? right?.ToString();
                return string.CompareOrdinal(leftKey, rightKey);
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

        private sealed class PaletteDictionarySnapshot
        {
            public PaletteDictionarySnapshot(IReadOnlyList<PaletteDictionaryEntrySnapshot> entries)
            {
                Entries = entries ?? Array.Empty<PaletteDictionaryEntrySnapshot>();
            }

            public IReadOnlyList<PaletteDictionaryEntrySnapshot> Entries { get; }
        }

        private readonly struct PaletteDictionaryEntrySnapshot
        {
            public PaletteDictionaryEntrySnapshot(string key, Color button, Color text)
            {
                Key = key;
                Button = button;
                Text = text;
            }

            public string Key { get; }
            public Color Button { get; }
            public Color Text { get; }
        }

        private static PaletteDictionarySnapshot CapturePaletteDictionarySnapshot(
            SerializedProperty dictionaryProperty,
            string buttonField,
            string textField
        )
        {
            (SerializedProperty keysProperty, SerializedProperty valuesProperty) =
                GetDictionaryArrays(dictionaryProperty);
            if (keysProperty == null || valuesProperty == null)
            {
                return new PaletteDictionarySnapshot(Array.Empty<PaletteDictionaryEntrySnapshot>());
            }

            List<PaletteDictionaryEntrySnapshot> entries = new(keysProperty.arraySize);
            for (int index = 0; index < keysProperty.arraySize; index++)
            {
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(index);
                string key = keyProperty.stringValue;
                Color button =
                    valueProperty.FindPropertyRelative(buttonField)?.colorValue ?? Color.clear;
                Color text =
                    valueProperty.FindPropertyRelative(textField)?.colorValue ?? Color.clear;
                entries.Add(new PaletteDictionaryEntrySnapshot(key, button, text));
            }

            return new PaletteDictionarySnapshot(entries);
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

        private static void RestorePaletteDictionary(
            SerializedProperty dictionaryProperty,
            PaletteDictionarySnapshot snapshot,
            string buttonField,
            string textField
        )
        {
            IReadOnlyList<PaletteDictionaryEntrySnapshot> entries =
                snapshot?.Entries ?? Array.Empty<PaletteDictionaryEntrySnapshot>();
            OverwritePaletteDictionary(dictionaryProperty, entries, buttonField, textField);
        }

        private static void OverwritePaletteDictionary(
            SerializedProperty dictionaryProperty,
            IReadOnlyList<PaletteDictionaryEntrySnapshot> entries,
            string buttonField,
            string textField
        )
        {
            (SerializedProperty keysProperty, SerializedProperty valuesProperty) =
                GetDictionaryArrays(dictionaryProperty);
            if (keysProperty == null || valuesProperty == null)
            {
                return;
            }

            keysProperty.ClearArray();
            valuesProperty.ClearArray();

            if (entries == null || entries.Count == 0)
            {
                return;
            }

            keysProperty.arraySize = entries.Count;
            valuesProperty.arraySize = entries.Count;

            for (int index = 0; index < entries.Count; index++)
            {
                PaletteDictionaryEntrySnapshot entry = entries[index];
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                keyProperty.stringValue = entry.Key ?? string.Empty;

                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(index);
                SerializedProperty backgroundProperty = valueProperty.FindPropertyRelative(
                    buttonField
                );
                if (backgroundProperty != null)
                {
                    backgroundProperty.colorValue = entry.Button;
                }

                SerializedProperty textProperty = valueProperty.FindPropertyRelative(textField);
                if (textProperty != null)
                {
                    textProperty.colorValue = entry.Text;
                }
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

        private static string[] ExtractDictionaryKeys(SerializedProperty keysProperty)
        {
            if (keysProperty == null)
            {
                return Array.Empty<string>();
            }

            int count = Mathf.Max(0, keysProperty.arraySize);
            string[] keys = new string[count];
            for (int index = 0; index < count; index++)
            {
                keys[index] = keysProperty.GetArrayElementAtIndex(index).stringValue;
            }

            return keys;
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

        private static HashSet<string> CloneSkipSet(HashSet<string> source)
        {
            return source != null
                ? new HashSet<string>(source, StringComparer.OrdinalIgnoreCase)
                : null;
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
