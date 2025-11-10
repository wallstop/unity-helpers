namespace WallstopStudios.UnityHelpers.Editor.Settings
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.WButton;

    /// <summary>
    /// Project-wide configuration surface for Unity Helpers editor tooling.
    /// </summary>
    /// <remarks>
    /// Currently exposes pagination defaults for <see cref="CustomDrawers.StringInListDrawer"/> and companion editor tooling (SerializableSet, WButton trays, duplicate highlighting).
    /// </remarks>
    [FilePath(
        "ProjectSettings/UnityHelpersSettings.asset",
        FilePathAttribute.Location.ProjectFolder
    )]
    public sealed class UnityHelpersSettings : ScriptableSingleton<UnityHelpersSettings>
    {
        public const int MinPageSize = 5;
        public const int MaxPageSize = 500;
        public const int DefaultStringInListPageSize = 25;
        public const int DefaultSerializableSetPageSize = 15;
        public const int DefaultWButtonPageSize = 6;
        public const int DefaultWButtonHistorySize = 5;
        public const int MinWButtonHistorySize = 1;
        public const int MaxWButtonHistorySize = 10;
        public const string DefaultWButtonPriority = "Default";
        public const int DefaultDuplicateTweenCycles = 3;
        private static readonly Color DefaultPriorityButtonColor = new(0.243f, 0.525f, 0.988f, 1f);

        public readonly struct WButtonPaletteEntry
        {
            public WButtonPaletteEntry(Color buttonColor, Color textColor)
            {
                ButtonColor = buttonColor;
                TextColor = textColor;
            }

            public Color ButtonColor { get; }

            public Color TextColor { get; }
        }

        public enum DuplicateRowAnimationMode
        {
            [System.Obsolete(
                "Disable duplicate duplicate-row animations only when required.",
                false
            )]
            None = 0,
            Static = 1,
            Tween = 2,
        }

        [SerializeField]
        [Tooltip("Maximum number of entries shown per page for StringInList dropdowns.")]
        private int stringInListPageSize = DefaultStringInListPageSize;

        [SerializeField]
        [Tooltip(
            "Maximum number of entries shown per page when drawing SerializableHashSet/SerializableSortedSet inspectors."
        )]
        private int serializableSetPageSize = DefaultSerializableSetPageSize;

        [SerializeField]
        [Tooltip("Maximum number of WButton actions displayed per page in inspector trays.")]
        private int wbuttonPageSize = DefaultWButtonPageSize;

        [SerializeField]
        [Tooltip("Number of recent invocation results retained per WButton method.")]
        private int wbuttonHistorySize = DefaultWButtonHistorySize;

        [SerializeField]
        [Tooltip(
            "Controls how duplicate entries are emphasized inside SerializableDictionary inspectors."
        )]
        private DuplicateRowAnimationMode duplicateRowAnimationMode =
            DuplicateRowAnimationMode.Tween;

        [SerializeField]
        [Tooltip(
            "When using Tween, number of shake cycles to play for duplicate entries. Negative values loop indefinitely."
        )]
        [WShowIf(
            nameof(duplicateRowAnimationMode),
            expectedValues: new object[] { DuplicateRowAnimationMode.Tween }
        )]
        private int duplicateRowTweenCycles = DefaultDuplicateTweenCycles;

        [SerializeField]
        [Tooltip(
            "Regular expressions evaluated against type names to exclude them from SerializableType pickers."
        )]
        private List<SerializableTypeIgnorePattern> serializableTypeIgnorePatterns = new();

        [SerializeField]
        [Tooltip("Named color palette applied to WButton priorities.")]
        private List<WButtonPriorityColor> wbuttonPriorityColors = new();

        [SerializeField]
        private bool serializableTypePatternsInitialized;

        [System.Serializable]
        private sealed class SerializableTypeIgnorePattern
        {
            [SerializeField]
            private string pattern = string.Empty;

            public SerializableTypeIgnorePattern() { }

            public SerializableTypeIgnorePattern(string pattern)
            {
                Pattern = pattern;
            }

            public string Pattern
            {
                get => pattern ?? string.Empty;
                set => pattern = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Retrieves the effective page size for StringInList drawers, clamped to safe bounds.
        /// </summary>
        public int StringInListPageSize
        {
            get => Mathf.Clamp(stringInListPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == stringInListPageSize)
                {
                    return;
                }

                stringInListPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Retrieves the configured page size for SerializableSet inspectors.
        /// </summary>
        public int SerializableSetPageSize
        {
            get => Mathf.Clamp(serializableSetPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == serializableSetPageSize)
                {
                    return;
                }

                serializableSetPageSize = clamped;
                SaveSettings();
            }
        }

        [System.Serializable]
        private sealed class WButtonPriorityColor
        {
            [SerializeField]
            private string priority = DefaultWButtonPriority;

            [SerializeField]
            private Color buttonColor = Color.white;

            [SerializeField]
            private Color textColor = Color.black;

            public WButtonPriorityColor() { }

            public WButtonPriorityColor(string priority, Color buttonColor, Color textColor)
            {
                Priority = priority;
                ButtonColor = buttonColor;
                TextColor = textColor;
            }

            public string Priority
            {
                get =>
                    string.IsNullOrWhiteSpace(priority) ? DefaultWButtonPriority : priority.Trim();
                set =>
                    priority = string.IsNullOrWhiteSpace(value)
                        ? DefaultWButtonPriority
                        : value.Trim();
            }

            public Color ButtonColor
            {
                get => buttonColor;
                set => buttonColor = value;
            }

            public Color TextColor
            {
                get => textColor;
                set => textColor = value;
            }
        }

        /// <summary>
        /// Retrieves the configured page size for WButton groups.
        /// </summary>
        public int WButtonPageSize
        {
            get => Mathf.Clamp(wbuttonPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == wbuttonPageSize)
                {
                    return;
                }

                wbuttonPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Retrieves the number of results retained per WButton method.
        /// </summary>
        public int WButtonHistorySize
        {
            get => Mathf.Clamp(wbuttonHistorySize, MinWButtonHistorySize, MaxWButtonHistorySize);
            set
            {
                int clamped = Mathf.Clamp(value, MinWButtonHistorySize, MaxWButtonHistorySize);
                if (clamped == wbuttonHistorySize)
                {
                    return;
                }

                wbuttonHistorySize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Current duplicate-row animation mode used by dictionary inspectors.
        /// </summary>
        public DuplicateRowAnimationMode DuplicateRowAnimation
        {
            get => duplicateRowAnimationMode;
            set
            {
                if (duplicateRowAnimationMode == value)
                {
                    return;
                }

                duplicateRowAnimationMode = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Number of tween cycles configured for duplicate rows. Negative values loop indefinitely.
        /// </summary>
        public int DuplicateRowTweenCycles
        {
            get => duplicateRowTweenCycles;
            set
            {
                if (duplicateRowTweenCycles == value)
                {
                    return;
                }

                duplicateRowTweenCycles = value;
                SaveSettings();
            }
        }

        internal IReadOnlyList<string> GetSerializableTypeIgnorePatterns()
        {
            if (serializableTypeIgnorePatterns == null || serializableTypeIgnorePatterns.Count == 0)
            {
                return System.Array.Empty<string>();
            }

            List<string> patterns = new(serializableTypeIgnorePatterns.Count);
            foreach (SerializableTypeIgnorePattern entry in serializableTypeIgnorePatterns)
            {
                if (entry == null)
                {
                    continue;
                }

                string pattern = entry.Pattern;
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                string trimmed = pattern.Trim();
                if (!patterns.Contains(trimmed))
                {
                    patterns.Add(trimmed);
                }
            }

            return patterns;
        }

        /// <summary>
        /// Returns the configured page size, falling back to defaults if unset.
        /// </summary>
        public static int GetStringInListPageLimit()
        {
            return instance.StringInListPageSize;
        }

        public static int GetSerializableSetPageSize()
        {
            return Mathf.Clamp(instance.SerializableSetPageSize, MinPageSize, MaxPageSize);
        }

        public static int GetWButtonPageSize()
        {
            return Mathf.Clamp(instance.WButtonPageSize, MinPageSize, MaxPageSize);
        }

        public static int GetWButtonHistorySize()
        {
            return Mathf.Clamp(
                instance.WButtonHistorySize,
                MinWButtonHistorySize,
                MaxWButtonHistorySize
            );
        }

        public static WButtonPaletteEntry ResolveWButtonPalette(string priority)
        {
            return instance.GetWButtonPaletteEntry(priority);
        }

        public static DuplicateRowAnimationMode GetDuplicateRowAnimationMode()
        {
            return instance.duplicateRowAnimationMode;
        }

        public static int GetDuplicateRowTweenCycleLimit()
        {
            return instance.duplicateRowTweenCycles;
        }

        /// <summary>
        /// Ensures persisted data stays within valid range.
        /// </summary>
        private void OnEnable()
        {
            stringInListPageSize = Mathf.Clamp(
                stringInListPageSize <= 0 ? DefaultStringInListPageSize : stringInListPageSize,
                MinPageSize,
                MaxPageSize
            );
            serializableSetPageSize = Mathf.Clamp(
                serializableSetPageSize <= 0
                    ? DefaultSerializableSetPageSize
                    : serializableSetPageSize,
                MinPageSize,
                MaxPageSize
            );
            wbuttonPageSize = Mathf.Clamp(
                wbuttonPageSize <= 0 ? DefaultWButtonPageSize : wbuttonPageSize,
                MinPageSize,
                MaxPageSize
            );
            wbuttonHistorySize = Mathf.Clamp(
                wbuttonHistorySize <= 0 ? DefaultWButtonHistorySize : wbuttonHistorySize,
                MinWButtonHistorySize,
                MaxWButtonHistorySize
            );
            if (EnsureWButtonPriorityDefaults())
            {
                SaveSettings();
            }
            if (EnsureSerializableTypePatternDefaults())
            {
                SaveSettings();
            }
            else
            {
                ApplyRuntimeConfiguration();
            }
        }

        /// <summary>
        /// Persists any modifications to disk.
        /// </summary>
        public void SaveSettings()
        {
            ApplyRuntimeConfiguration();
            Save(true);
        }

        private void ApplyRuntimeConfiguration()
        {
            IReadOnlyList<string> patterns = GetSerializableTypeIgnorePatterns();
            SerializableTypeCatalog.ConfigureTypeNameIgnorePatterns(patterns);
            SerializableTypeCatalog.WarmPatternStats(patterns);
        }

        private bool EnsureSerializableTypePatternDefaults()
        {
            serializableTypeIgnorePatterns ??= new List<SerializableTypeIgnorePattern>();

            if (serializableTypePatternsInitialized)
            {
                return false;
            }

            if (serializableTypeIgnorePatterns.Count == 0)
            {
                IReadOnlyList<string> defaults = SerializableTypeCatalog.GetDefaultIgnorePatterns();
                for (int index = 0; index < defaults.Count; index++)
                {
                    serializableTypeIgnorePatterns.Add(
                        new SerializableTypeIgnorePattern(defaults[index])
                    );
                }
            }

            bool changed = !serializableTypePatternsInitialized;
            serializableTypePatternsInitialized = true;
            return changed;
        }

        private bool EnsureWButtonPriorityDefaults()
        {
            wbuttonPriorityColors ??= new List<WButtonPriorityColor>();

            bool changed = false;
            RemoveNullPaletteEntries();
            changed |= EnsurePriorityTextColors();

            if (!ContainsPriority(DefaultWButtonPriority))
            {
                Color defaultButton = DefaultPriorityButtonColor;
                Color defaultText = WButtonColorUtility.GetReadableTextColor(defaultButton);
                wbuttonPriorityColors.Add(
                    new WButtonPriorityColor(DefaultWButtonPriority, defaultButton, defaultText)
                );
                changed = true;
            }

            return changed;
        }

        private bool EnsurePriorityTextColors()
        {
            if (wbuttonPriorityColors == null)
            {
                return false;
            }

            bool changed = false;
            foreach (WButtonPriorityColor entry in wbuttonPriorityColors)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.TextColor.maxColorComponent <= 0f)
                {
                    entry.TextColor = WButtonColorUtility.GetReadableTextColor(entry.ButtonColor);
                    changed = true;
                }
            }

            return changed;
        }

        private void RemoveNullPaletteEntries()
        {
            for (int index = wbuttonPriorityColors.Count - 1; index >= 0; index--)
            {
                if (wbuttonPriorityColors[index] == null)
                {
                    wbuttonPriorityColors.RemoveAt(index);
                }
            }
        }

        private bool ContainsPriority(string priority)
        {
            if (wbuttonPriorityColors == null || wbuttonPriorityColors.Count == 0)
            {
                return false;
            }

            foreach (WButtonPriorityColor entry in wbuttonPriorityColors)
            {
                if (entry == null)
                {
                    continue;
                }

                if (
                    string.Equals(
                        entry.Priority,
                        priority,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private WButtonPaletteEntry GetWButtonPaletteEntry(string priority)
        {
            string key = string.IsNullOrWhiteSpace(priority)
                ? DefaultWButtonPriority
                : priority.Trim();
            if (wbuttonPriorityColors != null)
            {
                foreach (WButtonPriorityColor entry in wbuttonPriorityColors)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (
                        string.Equals(
                            entry.Priority,
                            key,
                            System.StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return new WButtonPaletteEntry(entry.ButtonColor, entry.TextColor);
                    }
                }
            }

            if (
                !string.Equals(
                    key,
                    DefaultWButtonPriority,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetWButtonPaletteEntry(DefaultWButtonPriority);
            }

            Color fallbackButton = DefaultPriorityButtonColor;
            Color fallbackText = WButtonColorUtility.GetReadableTextColor(fallbackButton);
            return new WButtonPaletteEntry(fallbackButton, fallbackText);
        }

        private static void DrawSerializableTypeIgnorePatterns(
            SerializedProperty patternsProperty,
            SerializedProperty initializationFlagProperty
        )
        {
            if (patternsProperty == null)
            {
                return;
            }

            GUIContent label = new(
                "SerializableType Ignore Regexes",
                "Regex patterns evaluated against type names (simple and fully-qualified) to exclude them from the SerializableType picker."
            );

            patternsProperty.isExpanded = EditorGUILayout.Foldout(
                patternsProperty.isExpanded,
                label,
                true
            );
            if (!patternsProperty.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            for (int index = 0; index < patternsProperty.arraySize; index++)
            {
                SerializedProperty element = patternsProperty.GetArrayElementAtIndex(index);
                SerializedProperty patternProperty = element.FindPropertyRelative("pattern");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(patternProperty, GUIContent.none);

                    string patternValue = patternProperty.stringValue ?? string.Empty;
                    SerializableTypeCatalog.PatternStats stats =
                        SerializableTypeCatalog.GetPatternStats(patternValue);

                    string countLabel = stats.IsValid
                        ? $"{stats.MatchCount} match{(stats.MatchCount == 1 ? string.Empty : "es")}"
                        : "Invalid";
                    GUIContent countContent = stats.IsValid
                        ? new GUIContent(countLabel)
                        : new GUIContent(
                            "Invalid",
                            stats.ErrorMessage ?? "Pattern is not a valid regular expression."
                        );

                    GUILayoutOption width = GUILayout.Width(110f);
                    EditorGUILayout.LabelField(countContent, GUILayout.ExpandWidth(false), width);

                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        patternsProperty.DeleteArrayElementAtIndex(index);
                        if (initializationFlagProperty != null)
                        {
                            initializationFlagProperty.boolValue = true;
                        }
                        break;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Regex", GUILayout.Width(110f)))
                {
                    int newIndex = patternsProperty.arraySize;
                    patternsProperty.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty newElement = patternsProperty.GetArrayElementAtIndex(
                        newIndex
                    );
                    SerializedProperty patternProperty = newElement.FindPropertyRelative("pattern");
                    patternProperty.stringValue = string.Empty;
                    if (initializationFlagProperty != null)
                    {
                        initializationFlagProperty.boolValue = true;
                    }
                }

                if (GUILayout.Button("Reset To Defaults", GUILayout.Width(150f)))
                {
                    patternsProperty.ClearArray();
                    IReadOnlyList<string> defaults =
                        SerializableTypeCatalog.GetDefaultIgnorePatterns();
                    for (int index = 0; index < defaults.Count; index++)
                    {
                        patternsProperty.InsertArrayElementAtIndex(index);
                        SerializedProperty element = patternsProperty.GetArrayElementAtIndex(index);
                        element.FindPropertyRelative("pattern").stringValue = defaults[index];
                    }
                    if (initializationFlagProperty != null)
                    {
                        initializationFlagProperty.boolValue = true;
                    }
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUI.indentLevel--;
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(
                "Project/Wallstop Studios/Unity Helpers",
                SettingsScope.Project
            )
            {
                label = "Unity Helpers",
                guiHandler = _ =>
                {
                    UnityHelpersSettings settings = instance;
                    SerializedObject serializedSettings = new(settings);
                    SerializedProperty pageSizeProperty = serializedSettings.FindProperty(
                        "stringInListPageSize"
                    );
                    SerializedProperty setPageSizeProperty = serializedSettings.FindProperty(
                        "serializableSetPageSize"
                    );
                    SerializedProperty buttonPageSizeProperty = serializedSettings.FindProperty(
                        "wbuttonPageSize"
                    );
                    SerializedProperty buttonHistoryProperty = serializedSettings.FindProperty(
                        "wbuttonHistorySize"
                    );
                    SerializedProperty duplicateModeProperty = serializedSettings.FindProperty(
                        "duplicateRowAnimationMode"
                    );
                    SerializedProperty duplicateTweenCyclesProperty =
                        serializedSettings.FindProperty("duplicateRowTweenCycles");
                    SerializedProperty patternsProperty = serializedSettings.FindProperty(
                        "serializableTypeIgnorePatterns"
                    );
                    SerializedProperty patternsInitializedProperty =
                        serializedSettings.FindProperty("serializableTypePatternsInitialized");
                    SerializedProperty wbuttonPaletteProperty = serializedSettings.FindProperty(
                        "wbuttonPriorityColors"
                    );

                    serializedSettings.Update();

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.IntSlider(
                        pageSizeProperty,
                        MinPageSize,
                        MaxPageSize,
                        new GUIContent(
                            "StringInList Page Size",
                            "Number of options displayed per page in StringInList dropdowns."
                        )
                    );

                    EditorGUILayout.IntSlider(
                        setPageSizeProperty,
                        MinPageSize,
                        MaxPageSize,
                        new GUIContent(
                            "Serializable Set Page Size",
                            "Number of entries displayed per page in SerializableHashSet and SerializableSortedSet inspectors."
                        )
                    );

                    EditorGUILayout.IntSlider(
                        buttonPageSizeProperty,
                        MinPageSize,
                        MaxPageSize,
                        new GUIContent(
                            "WButton Page Size",
                            "Number of WButton actions displayed per page when grouped by draw order."
                        )
                    );

                    EditorGUILayout.IntSlider(
                        buttonHistoryProperty,
                        MinWButtonHistorySize,
                        MaxWButtonHistorySize,
                        new GUIContent(
                            "WButton History Size",
                            "Number of recent results remembered per WButton method for each inspected object."
                        )
                    );

                    EditorGUILayout.PropertyField(
                        duplicateModeProperty,
                        new GUIContent(
                            "Duplicate Row Animation",
                            "Controls how duplicate entries are presented in SerializableDictionary inspectors."
                        )
                    );

                    EditorGUILayout.PropertyField(
                        duplicateTweenCyclesProperty,
                        new GUIContent(
                            "Tween Cycle Limit",
                            "Number of shake cycles performed when highlighting duplicate entries. Negative values loop indefinitely."
                        )
                    );

                    DrawSerializableTypeIgnorePatterns(
                        patternsProperty,
                        patternsInitializedProperty
                    );
                    DrawWButtonPriorityColors(wbuttonPaletteProperty);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                        settings.SaveSettings();
                    }
                },
                keywords = new[]
                {
                    "StringInList",
                    "Pagination",
                    "SerializableSet",
                    "WButton",
                    "Buttons",
                    "UnityHelpers",
                    "Duplicate",
                    "SerializableType",
                    "Regex",
                },
            };
        }

        private static void DrawWButtonPriorityColors(SerializedProperty paletteProperty)
        {
            if (paletteProperty == null)
            {
                return;
            }

            GUIContent label = new(
                "WButton Priority Colors",
                "Configure named colors applied to WButton priorities. Buttons referencing a priority key will render using the associated color."
            );

            paletteProperty.isExpanded = EditorGUILayout.Foldout(
                paletteProperty.isExpanded,
                label,
                true
            );
            if (!paletteProperty.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            for (int index = 0; index < paletteProperty.arraySize; index++)
            {
                SerializedProperty element = paletteProperty.GetArrayElementAtIndex(index);
                SerializedProperty priorityProperty = element.FindPropertyRelative("priority");
                SerializedProperty buttonColorProperty = element.FindPropertyRelative(
                    "buttonColor"
                );
                SerializedProperty textColorProperty = element.FindPropertyRelative("textColor");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(
                        priorityProperty,
                        GUIContent.none,
                        GUILayout.Width(180f)
                    );

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(200f)))
                    {
                        Color previousButtonColor = buttonColorProperty.colorValue;
                        Color previousSuggestedText = WButtonColorUtility.GetReadableTextColor(
                            previousButtonColor
                        );

                        EditorGUILayout.PropertyField(
                            buttonColorProperty,
                            new GUIContent("Button")
                        );

                        if (buttonColorProperty.colorValue != previousButtonColor)
                        {
                            Color suggestedText = WButtonColorUtility.GetReadableTextColor(
                                buttonColorProperty.colorValue
                            );
                            if (
                                ColorsApproximatelyEqual(
                                    textColorProperty.colorValue,
                                    previousSuggestedText
                                )
                            )
                            {
                                textColorProperty.colorValue = suggestedText;
                            }
                        }

                        EditorGUILayout.PropertyField(textColorProperty, new GUIContent("Text"));
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(80f)))
                    {
                        paletteProperty.DeleteArrayElementAtIndex(index);
                        break;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Priority", GUILayout.Width(110f)))
                {
                    int newIndex = paletteProperty.arraySize;
                    paletteProperty.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty element = paletteProperty.GetArrayElementAtIndex(newIndex);
                    element.FindPropertyRelative("priority").stringValue = DefaultWButtonPriority;
                    Color suggestedButton = WButtonColorUtility.SuggestPaletteColor(newIndex);
                    element.FindPropertyRelative("buttonColor").colorValue = suggestedButton;
                    element.FindPropertyRelative("textColor").colorValue =
                        WButtonColorUtility.GetReadableTextColor(suggestedButton);
                }

                if (GUILayout.Button("Reset Palette", GUILayout.Width(120f)))
                {
                    paletteProperty.ClearArray();
                    paletteProperty.InsertArrayElementAtIndex(0);
                    SerializedProperty defaultElement = paletteProperty.GetArrayElementAtIndex(0);
                    defaultElement.FindPropertyRelative("priority").stringValue =
                        DefaultWButtonPriority;
                    defaultElement.FindPropertyRelative("buttonColor").colorValue =
                        DefaultPriorityButtonColor;
                    defaultElement.FindPropertyRelative("textColor").colorValue =
                        WButtonColorUtility.GetReadableTextColor(DefaultPriorityButtonColor);
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUI.indentLevel--;
        }

        private static bool ColorsApproximatelyEqual(Color left, Color right)
        {
            const float tolerance = 0.01f;
            return Mathf.Abs(left.r - right.r) <= tolerance
                && Mathf.Abs(left.g - right.g) <= tolerance
                && Mathf.Abs(left.b - right.b) <= tolerance
                && Mathf.Abs(left.a - right.a) <= tolerance;
        }
    }
#endif
}
