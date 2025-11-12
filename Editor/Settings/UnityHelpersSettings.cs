namespace WallstopStudios.UnityHelpers.Editor.Settings
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Serialization;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;

    /// <summary>
    /// Project-wide configuration surface for Unity Helpers editor tooling.
    /// </summary>
    /// <remarks>
    /// Currently exposes pagination defaults for <see cref="CustomDrawers.StringInListDrawer"/> and companion editor tooling (SerializableSet, WEnumToggleButtons, WButton trays, duplicate highlighting).
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
        public const int DefaultEnumToggleButtonsPageSize = 15;
        public const int DefaultWButtonPageSize = 6;
        public const int DefaultWButtonHistorySize = 5;
        public const int MinWButtonHistorySize = 1;
        public const int MaxWButtonHistorySize = 10;
        public const string DefaultWButtonColorKey = "Default";
        public const string WButtonLightThemeColorKey = "Default-Light";
        public const string WButtonDarkThemeColorKey = "Default-Dark";
        public const string WButtonLegacyColorKey = "WDefault";
        public const string DefaultWGroupColorKey = "Default";
        public const string WGroupLightThemeColorKey = "Default-Light";
        public const string WGroupDarkThemeColorKey = "Default-Dark";
        public const string WGroupLegacyColorKey = "WDefault";
        public const int DefaultWGroupAutoIncludeRowCount = 4;
        public const int MinWGroupAutoIncludeRowCount = 0;
        public const int MaxWGroupAutoIncludeRowCount = 32;
        private static readonly Color DefaultLightThemeGroupBackground = new(
            0.90f,
            0.90f,
            0.90f,
            1f
        );
        private static readonly Color DefaultDarkThemeGroupBackground = new(
            0.215f,
            0.215f,
            0.215f,
            1f
        );

        [System.Obsolete("Use DefaultWButtonColorKey instead.")]
        public const string DefaultWButtonPriority = DefaultWButtonColorKey;
        public const int DefaultDuplicateTweenCycles = 3;
        public const float DefaultFoldoutSpeed = 2f;
        public const float MinFoldoutSpeed = 2f;
        public const float MaxFoldoutSpeed = 12f;
        private static readonly Color DefaultColorKeyButtonColor = new(0.243f, 0.525f, 0.988f, 1f);
        private static readonly Color DefaultLightThemeButtonColor = new(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color DefaultDarkThemeButtonColor = new(0.35f, 0.35f, 0.35f, 1f);

        public enum WButtonActionsPlacement
        {
            Top = 0,
            Bottom = 1,
        }

        public enum WButtonFoldoutBehavior
        {
            AlwaysOpen = 0,
            StartExpanded = 1,
            StartCollapsed = 2,
        }

        public enum WGroupAutoIncludeMode
        {
            None = 0,
            Finite = 1,
            Infinite = 2,
        }

        public readonly struct WGroupAutoIncludeConfiguration
        {
            public WGroupAutoIncludeConfiguration(WGroupAutoIncludeMode mode, int rowCount)
            {
                Mode = mode;
                RowCount = rowCount < 0 ? 0 : rowCount;
            }

            public WGroupAutoIncludeMode Mode { get; }

            public int RowCount { get; }
        }

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

        public readonly struct WGroupPaletteEntry
        {
            public WGroupPaletteEntry(Color backgroundColor, Color textColor)
            {
                BackgroundColor = backgroundColor;
                TextColor = textColor;
            }

            public Color BackgroundColor { get; }

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
        [Tooltip(
            "Maximum number of toggle buttons shown per page when drawing WEnumToggleButtons groups."
        )]
        private int enumToggleButtonsPageSize = DefaultEnumToggleButtonsPageSize;

        [SerializeField]
        [Tooltip("Number of recent invocation results retained per WButton method.")]
        private int wbuttonHistorySize = DefaultWButtonHistorySize;

        [SerializeField]
        [Tooltip("Controls where WButton actions are rendered relative to the inspector content.")]
        private WButtonActionsPlacement wbuttonActionsPlacement = WButtonActionsPlacement.Top;

        [SerializeField]
        [Tooltip(
            "Determines whether WButton groups are always shown or foldouts start expanded/collapsed."
        )]
        private WButtonFoldoutBehavior wbuttonFoldoutBehavior =
            WButtonFoldoutBehavior.StartExpanded;

        [SerializeField]
        [Tooltip("Animate WButton action foldouts when toggled.")]
        private bool wbuttonFoldoutTweenEnabled = true;

        [SerializeField]
        [Tooltip("Animation speed used when toggling WButton action foldouts.")]
        [WShowIf(nameof(wbuttonFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float wbuttonFoldoutSpeed = DefaultFoldoutSpeed;

        [SerializeField]
        [Tooltip(
            "Animation speed used when toggling SerializableDictionary pending entry foldouts."
        )]
        private bool serializableDictionaryFoldoutTweenEnabled = true;

        [SerializeField]
        [Tooltip("Tracks whether foldout tween settings have been migrated to defaults.")]
        private bool foldoutTweenSettingsInitialized;

        [SerializeField]
        [Tooltip(
            "Animation speed used when toggling SerializableDictionary pending entry foldouts."
        )]
        [WShowIf(nameof(serializableDictionaryFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float serializableDictionaryFoldoutSpeed = DefaultFoldoutSpeed;

        [SerializeField]
        [Tooltip(
            "Animation speed used when toggling SerializableSortedDictionary pending entry foldouts."
        )]
        private bool serializableSortedDictionaryFoldoutTweenEnabled = true;

        [SerializeField]
        [Tooltip(
            "Animation speed used when toggling SerializableSortedDictionary pending entry foldouts."
        )]
        [WShowIf(nameof(serializableSortedDictionaryFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float serializableSortedDictionaryFoldoutSpeed = DefaultFoldoutSpeed;

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
        [Tooltip("Named color palette applied to WButton custom color keys.")]
        private WButtonCustomColorDictionary wbuttonCustomColors = new();

        [SerializeField]
        [Tooltip(
            "Controls how WGroup automatically includes additional serialized members after a group declaration."
        )]
        private WGroupAutoIncludeMode wgroupAutoIncludeMode = WGroupAutoIncludeMode.Infinite;

        [SerializeField]
        [Tooltip(
            "Number of additional serialized members captured when the WGroup auto include mode is set to Finite."
        )]
        [WShowIf(
            nameof(wgroupAutoIncludeMode),
            expectedValues: new object[] { WGroupAutoIncludeMode.Finite }
        )]
        [Range(MinWGroupAutoIncludeRowCount, MaxWGroupAutoIncludeRowCount)]
        private int wgroupAutoIncludeRowCount = DefaultWGroupAutoIncludeRowCount;

        [SerializeField]
        [Tooltip("Named color palette applied to WGroup custom color keys.")]
        private WGroupCustomColorDictionary wgroupCustomColors = new();

        [SerializeField]
        [FormerlySerializedAs("wbuttonPriorityColors")]
#pragma warning disable CS0618 // Type or member is obsolete
        private List<WButtonPriorityColor> legacyWButtonPriorityColors = new();
#pragma warning restore CS0618 // Type or member is obsolete

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

        [System.Serializable]
        private sealed class WButtonCustomColor
        {
            [SerializeField]
            private Color buttonColor = Color.white;

            [SerializeField]
            private Color textColor = Color.black;

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

            public void EnsureReadableText()
            {
                if (textColor.maxColorComponent <= 0f)
                {
                    textColor = WButtonColorUtility.GetReadableTextColor(buttonColor);
                }
            }
        }

        [System.Serializable]
        private sealed class WButtonCustomColorDictionary
            : SerializableDictionary<string, WButtonCustomColor> { }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(WButtonCustomColor))]
        private sealed class WButtonCustomColorDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty buttonColor = property.FindPropertyRelative("buttonColor");
                SerializedProperty textColor = property.FindPropertyRelative("textColor");

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float halfWidth = (position.width - spacing) * 0.5f;
                Rect buttonRect = new(position.x, position.y, halfWidth, position.height);
                Rect textRect = new(
                    position.x + halfWidth + spacing,
                    position.y,
                    halfWidth,
                    position.height
                );

                EditorGUI.PropertyField(
                    buttonRect,
                    buttonColor,
                    EditorGUIUtility.TrTextContent("Button")
                );
                EditorGUI.PropertyField(
                    textRect,
                    textColor,
                    EditorGUIUtility.TrTextContent("Text")
                );
            }
        }
#endif

        [System.Serializable]
        private sealed class WGroupCustomColor
        {
            [SerializeField]
            private Color backgroundColor = DefaultColorKeyButtonColor;

            [SerializeField]
            private Color textColor = Color.white;

            public Color BackgroundColor
            {
                get => backgroundColor;
                set => backgroundColor = value;
            }

            public Color TextColor
            {
                get => textColor;
                set => textColor = value;
            }

            public void EnsureReadableText()
            {
                if (textColor.maxColorComponent <= 0f)
                {
                    textColor = WButtonColorUtility.GetReadableTextColor(backgroundColor);
                }
            }
        }

        [System.Serializable]
        private sealed class WGroupCustomColorDictionary
            : SerializableDictionary<string, WGroupCustomColor> { }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(WGroupCustomColor))]
        private sealed class WGroupCustomColorDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty background = property.FindPropertyRelative("backgroundColor");
                SerializedProperty text = property.FindPropertyRelative("textColor");

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float halfWidth = (position.width - spacing) * 0.5f;
                Rect backgroundRect = new(position.x, position.y, halfWidth, position.height);
                Rect textRect = new(
                    position.x + halfWidth + spacing,
                    position.y,
                    halfWidth,
                    position.height
                );

                EditorGUI.PropertyField(
                    backgroundRect,
                    background,
                    EditorGUIUtility.TrTextContent("Background")
                );
                EditorGUI.PropertyField(textRect, text, EditorGUIUtility.TrTextContent("Text"));
            }
        }
#endif

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

        /// <summary>
        /// Gets the configured page size for WEnumToggleButtons groups.
        /// </summary>
        public int EnumToggleButtonsPageSize
        {
            get => Mathf.Clamp(enumToggleButtonsPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == enumToggleButtonsPageSize)
                {
                    return;
                }

                enumToggleButtonsPageSize = clamped;
                SaveSettings();
            }
        }

        [System.Serializable]
        [System.Obsolete("Use WButtonCustomColorDictionary for serialization instead.")]
        private sealed class WButtonPriorityColor
        {
            [SerializeField]
            private string priority = DefaultWButtonColorKey;

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
                    string.IsNullOrWhiteSpace(priority) ? DefaultWButtonColorKey : priority.Trim();
                set =>
                    priority = string.IsNullOrWhiteSpace(value)
                        ? DefaultWButtonColorKey
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

        public static int GetEnumToggleButtonsPageSize()
        {
            return Mathf.Clamp(instance.EnumToggleButtonsPageSize, MinPageSize, MaxPageSize);
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

        public static WGroupAutoIncludeConfiguration GetWGroupAutoIncludeConfiguration()
        {
            UnityHelpersSettings settings = instance;
            int clamped = Mathf.Clamp(
                settings.wgroupAutoIncludeRowCount,
                MinWGroupAutoIncludeRowCount,
                MaxWGroupAutoIncludeRowCount
            );
            return new WGroupAutoIncludeConfiguration(settings.wgroupAutoIncludeMode, clamped);
        }

#if UNITY_EDITOR
        internal static void SetWGroupAutoIncludeConfigurationForTests(
            WGroupAutoIncludeMode mode,
            int rowCount
        )
        {
            UnityHelpersSettings settings = instance;
            settings.wgroupAutoIncludeMode = mode;
            settings.wgroupAutoIncludeRowCount = Mathf.Clamp(
                rowCount,
                MinWGroupAutoIncludeRowCount,
                MaxWGroupAutoIncludeRowCount
            );
            settings.SaveSettings();
        }
#endif

        public static WButtonPaletteEntry ResolveWButtonPalette(string colorKey)
        {
            return instance.GetWButtonPaletteEntry(colorKey);
        }

        internal static bool HasWButtonPaletteColorKey(string colorKey)
        {
            return instance.ContainsColorKey(colorKey);
        }

        public static WGroupPaletteEntry ResolveWGroupPalette(string colorKey)
        {
            return instance.GetWGroupPaletteEntry(colorKey);
        }

        public static string EnsureWGroupColorKey(string colorKey)
        {
            return instance.EnsureWGroupColorKeyInternal(colorKey);
        }

        internal static bool HasWGroupPaletteColorKey(string colorKey)
        {
            return instance.ContainsWGroupColorKey(colorKey);
        }

        public static WButtonActionsPlacement GetWButtonActionsPlacement()
        {
            return instance.wbuttonActionsPlacement;
        }

        public static WButtonFoldoutBehavior GetWButtonFoldoutBehavior()
        {
            return instance.wbuttonFoldoutBehavior;
        }

        public static bool ShouldTweenWButtonFoldouts()
        {
            return instance.wbuttonFoldoutTweenEnabled;
        }

        public static float GetWButtonFoldoutSpeed()
        {
            return Mathf.Clamp(instance.wbuttonFoldoutSpeed, MinFoldoutSpeed, MaxFoldoutSpeed);
        }

        public static bool ShouldTweenSerializableDictionaryFoldouts()
        {
            return instance.serializableDictionaryFoldoutTweenEnabled;
        }

        public static float GetSerializableDictionaryFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance.serializableDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
        }

        public static bool ShouldTweenSerializableSortedDictionaryFoldouts()
        {
            return instance.serializableSortedDictionaryFoldoutTweenEnabled;
        }

        public static float GetSerializableSortedDictionaryFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance.serializableSortedDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
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
            enumToggleButtonsPageSize = Mathf.Clamp(
                enumToggleButtonsPageSize <= 0
                    ? DefaultEnumToggleButtonsPageSize
                    : enumToggleButtonsPageSize,
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
            if (!System.Enum.IsDefined(typeof(WButtonActionsPlacement), wbuttonActionsPlacement))
            {
                wbuttonActionsPlacement = WButtonActionsPlacement.Top;
            }

            if (!System.Enum.IsDefined(typeof(WButtonFoldoutBehavior), wbuttonFoldoutBehavior))
            {
                wbuttonFoldoutBehavior = WButtonFoldoutBehavior.StartExpanded;
            }
            if (!System.Enum.IsDefined(typeof(WGroupAutoIncludeMode), wgroupAutoIncludeMode))
            {
                wgroupAutoIncludeMode = WGroupAutoIncludeMode.Infinite;
            }
            if (wgroupAutoIncludeRowCount < MinWGroupAutoIncludeRowCount)
            {
                wgroupAutoIncludeRowCount = DefaultWGroupAutoIncludeRowCount;
            }
            wgroupAutoIncludeRowCount = Mathf.Clamp(
                wgroupAutoIncludeRowCount,
                MinWGroupAutoIncludeRowCount,
                MaxWGroupAutoIncludeRowCount
            );
            wbuttonFoldoutSpeed = Mathf.Clamp(
                wbuttonFoldoutSpeed <= 0f ? DefaultFoldoutSpeed : wbuttonFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            serializableDictionaryFoldoutSpeed = Mathf.Clamp(
                serializableDictionaryFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : serializableDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            serializableSortedDictionaryFoldoutSpeed = Mathf.Clamp(
                serializableSortedDictionaryFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : serializableSortedDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            if (EnsureFoldoutTweenDefaults())
            {
                SaveSettings();
            }
            if (EnsureWButtonCustomColorDefaults())
            {
                SaveSettings();
            }
            if (EnsureWGroupCustomColorDefaults())
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

        private static void DrawIntSlider(
            string label,
            string tooltip,
            int currentValue,
            int min,
            int max,
            Action<int> onValueChanged
        )
        {
            int newValue = EditorGUILayout.IntSlider(
                new GUIContent(label, tooltip),
                currentValue,
                min,
                max
            );

            if (newValue != currentValue)
            {
                onValueChanged?.Invoke(newValue);
            }
        }

        /// <summary>
        /// Persists any modifications to disk.
        /// </summary>
        public void SaveSettings()
        {
            EnsureWButtonCustomColorDefaults();
            EnsureWGroupCustomColorDefaults();
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

        private bool EnsureFoldoutTweenDefaults()
        {
            if (foldoutTweenSettingsInitialized)
            {
                return false;
            }

            if (!wbuttonFoldoutTweenEnabled)
            {
                wbuttonFoldoutTweenEnabled = true;
            }

            if (!serializableDictionaryFoldoutTweenEnabled)
            {
                serializableDictionaryFoldoutTweenEnabled = true;
            }

            if (!serializableSortedDictionaryFoldoutTweenEnabled)
            {
                serializableSortedDictionaryFoldoutTweenEnabled = true;
            }

            foldoutTweenSettingsInitialized = true;
            return true;
        }

        private bool EnsureWButtonCustomColorDefaults()
        {
            if (wbuttonCustomColors == null)
            {
                wbuttonCustomColors = new WButtonCustomColorDictionary();
            }

            bool changed = false;
            changed |= MigrateLegacyWButtonPalette();

            if (
                wbuttonCustomColors.TryGetValue(
                    DefaultWButtonColorKey,
                    out WButtonCustomColor legacyDefault
                )
            )
            {
                if (!wbuttonCustomColors.ContainsKey(WButtonLegacyColorKey))
                {
                    wbuttonCustomColors[WButtonLegacyColorKey] = legacyDefault;
                }
                wbuttonCustomColors.Remove(DefaultWButtonColorKey);
                changed = true;
            }

            if (!wbuttonCustomColors.ContainsKey(WButtonLegacyColorKey))
            {
                WButtonCustomColor legacyColor = new()
                {
                    ButtonColor = DefaultColorKeyButtonColor,
                    TextColor = WButtonColorUtility.GetReadableTextColor(
                        DefaultColorKeyButtonColor
                    ),
                };
                wbuttonCustomColors[WButtonLegacyColorKey] = legacyColor;
                changed = true;
            }

            changed |= EnsureWButtonThemeEntry(
                WButtonLightThemeColorKey,
                DefaultLightThemeButtonColor,
                Color.black
            );
            changed |= EnsureWButtonThemeEntry(
                WButtonDarkThemeColorKey,
                DefaultDarkThemeButtonColor,
                Color.white
            );

            int paletteIndex = 0;
            foreach (KeyValuePair<string, WButtonCustomColor> entry in wbuttonCustomColors)
            {
                WButtonCustomColor value = entry.Value;
                if (value == null)
                {
                    value = new WButtonCustomColor();
                    wbuttonCustomColors[entry.Key] = value;
                    value.ButtonColor = DefaultColorKeyButtonColor;
                    value.EnsureReadableText();
                    changed = true;
                }

                if (IsReservedWButtonColorKey(entry.Key))
                {
                    value.EnsureReadableText();
                    continue;
                }

                bool needsSuggestion =
                    value.ButtonColor.maxColorComponent <= 0f
                    || (
                        ColorsApproximatelyEqual(value.ButtonColor, Color.white)
                        && ColorsApproximatelyEqual(value.TextColor, Color.black)
                    );

                if (needsSuggestion)
                {
                    Color suggested = WButtonColorUtility.SuggestPaletteColor(paletteIndex);
                    value.ButtonColor = suggested;
                    value.TextColor = WButtonColorUtility.GetReadableTextColor(suggested);
                    changed = true;
                }
                else
                {
                    Color previousText = value.TextColor;
                    value.EnsureReadableText();
                    if (!ColorsApproximatelyEqual(value.TextColor, previousText))
                    {
                        changed = true;
                    }
                }

                paletteIndex++;
            }

            return changed;
        }

        private bool EnsureWButtonThemeEntry(string key, Color buttonColor, Color defaultTextColor)
        {
            if (
                wbuttonCustomColors.TryGetValue(key, out WButtonCustomColor existing)
                && existing != null
            )
            {
                existing.EnsureReadableText();
                return false;
            }

            Color textColor =
                defaultTextColor.maxColorComponent <= 0f
                    ? WButtonColorUtility.GetReadableTextColor(buttonColor)
                    : defaultTextColor;
            WButtonCustomColor themeColor = new()
            {
                ButtonColor = buttonColor,
                TextColor = textColor,
            };
            themeColor.EnsureReadableText();
            wbuttonCustomColors[key] = themeColor;
            return true;
        }

        private static bool IsReservedWButtonColorKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            return string.Equals(
                    key,
                    DefaultWButtonColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WButtonLightThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WButtonDarkThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WButtonLegacyColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                );
        }

        private bool MigrateLegacyWButtonPalette()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (legacyWButtonPriorityColors == null || legacyWButtonPriorityColors.Count == 0)
            {
                return false;
            }

            bool changed = false;
            for (int index = 0; index < legacyWButtonPriorityColors.Count; index++)
            {
                WButtonPriorityColor legacy = legacyWButtonPriorityColors[index];
#pragma warning restore CS0618 // Type or member is obsolete
                if (legacy == null)
                {
                    continue;
                }

                string normalizedKey = NormalizeColorKey(legacy.Priority);
                WButtonCustomColor color = new()
                {
                    ButtonColor = legacy.ButtonColor,
                    TextColor =
                        legacy.TextColor.maxColorComponent <= 0f
                            ? WButtonColorUtility.GetReadableTextColor(legacy.ButtonColor)
                            : legacy.TextColor,
                };
                wbuttonCustomColors[normalizedKey] = color;
                changed = true;
            }

            legacyWButtonPriorityColors.Clear();
            return changed;
        }

        private bool EnsureWGroupCustomColorDefaults()
        {
            if (wgroupCustomColors == null)
            {
                wgroupCustomColors = new WGroupCustomColorDictionary();
            }

            bool changed = false;

            if (
                wgroupCustomColors.TryGetValue(
                    DefaultWGroupColorKey,
                    out WGroupCustomColor legacyDefault
                )
            )
            {
                if (!wgroupCustomColors.ContainsKey(WGroupLegacyColorKey))
                {
                    wgroupCustomColors[WGroupLegacyColorKey] = legacyDefault;
                }
                wgroupCustomColors.Remove(DefaultWGroupColorKey);
                changed = true;
            }

            if (!wgroupCustomColors.ContainsKey(WGroupLegacyColorKey))
            {
                WGroupCustomColor legacyColor = new()
                {
                    BackgroundColor = DefaultColorKeyButtonColor,
                    TextColor = WButtonColorUtility.GetReadableTextColor(
                        DefaultColorKeyButtonColor
                    ),
                };
                wgroupCustomColors[WGroupLegacyColorKey] = legacyColor;
                changed = true;
            }

            changed |= EnsureThemeEntry(
                WGroupLightThemeColorKey,
                DefaultLightThemeGroupBackground,
                Color.black
            );
            changed |= EnsureThemeEntry(
                WGroupDarkThemeColorKey,
                DefaultDarkThemeGroupBackground,
                Color.white
            );

            int paletteIndex = 0;
            foreach (KeyValuePair<string, WGroupCustomColor> entry in wgroupCustomColors)
            {
                WGroupCustomColor value = entry.Value;
                if (value == null)
                {
                    value = new WGroupCustomColor();
                    wgroupCustomColors[entry.Key] = value;
                    value.BackgroundColor = DefaultColorKeyButtonColor;
                    value.EnsureReadableText();
                    changed = true;
                }

                if (IsReservedWGroupColorKey(entry.Key))
                {
                    value.EnsureReadableText();
                    continue;
                }

                bool needsSuggestion =
                    value.BackgroundColor.maxColorComponent <= 0f
                    || (
                        ColorsApproximatelyEqual(value.BackgroundColor, Color.white)
                        && ColorsApproximatelyEqual(value.TextColor, Color.black)
                    );

                if (needsSuggestion)
                {
                    Color suggested = WButtonColorUtility.SuggestPaletteColor(paletteIndex);
                    value.BackgroundColor = suggested;
                    value.TextColor = WButtonColorUtility.GetReadableTextColor(suggested);
                    changed = true;
                }
                else
                {
                    Color previousText = value.TextColor;
                    value.EnsureReadableText();
                    if (!ColorsApproximatelyEqual(value.TextColor, previousText))
                    {
                        changed = true;
                    }
                }

                paletteIndex++;
            }

            return changed;
        }

        private bool EnsureThemeEntry(string key, Color background, Color defaultText)
        {
            if (
                wgroupCustomColors.TryGetValue(key, out WGroupCustomColor existing)
                && existing != null
            )
            {
                existing.EnsureReadableText();
                return false;
            }

            WGroupCustomColor themeColor = new()
            {
                BackgroundColor = background,
                TextColor =
                    defaultText.maxColorComponent <= 0f
                        ? WButtonColorUtility.GetReadableTextColor(background)
                        : defaultText,
            };
            themeColor.EnsureReadableText();
            wgroupCustomColors[key] = themeColor;
            return true;
        }

        private static bool IsReservedWGroupColorKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            return string.Equals(
                    key,
                    DefaultWGroupColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WGroupLightThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WGroupDarkThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WGroupLegacyColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                );
        }

        private bool ContainsColorKey(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return true;
            }

            if (IsReservedWButtonColorKey(colorKey))
            {
                return true;
            }

            if (wbuttonCustomColors == null || wbuttonCustomColors.Count == 0)
            {
                return false;
            }

            string normalized = string.IsNullOrWhiteSpace(colorKey)
                ? DefaultWButtonColorKey
                : colorKey.Trim();

            foreach (string existingKey in wbuttonCustomColors.Keys)
            {
                if (
                    string.Equals(
                        existingKey,
                        normalized,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private string NormalizeColorKey(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return DefaultWButtonColorKey;
            }

            if (IsReservedWButtonColorKey(colorKey))
            {
                if (
                    string.Equals(
                        colorKey,
                        WButtonLightThemeColorKey,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WButtonLightThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WButtonDarkThemeColorKey,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WButtonDarkThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WButtonLegacyColorKey,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WButtonLegacyColorKey;
                }

                return DefaultWButtonColorKey;
            }

            string normalized = colorKey.Trim();

            if (wbuttonCustomColors != null)
            {
                foreach (string existingKey in wbuttonCustomColors.Keys)
                {
                    if (
                        string.Equals(
                            existingKey,
                            normalized,
                            System.StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return existingKey;
                    }
                }
            }

            return normalized;
        }

        private WButtonPaletteEntry GetWButtonPaletteEntry(string colorKey)
        {
            EnsureWButtonCustomColorDefaults();

            string normalized = NormalizeColorKey(colorKey);

            if (
                string.Equals(
                    normalized,
                    DefaultWButtonColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetThemeAwareDefaultWButtonPalette();
            }

            if (
                string.Equals(
                    normalized,
                    WButtonLightThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetWButtonThemePaletteEntry(
                    WButtonLightThemeColorKey,
                    DefaultLightThemeButtonColor,
                    Color.black
                );
            }

            if (
                string.Equals(
                    normalized,
                    WButtonDarkThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetWButtonThemePaletteEntry(
                    WButtonDarkThemeColorKey,
                    DefaultDarkThemeButtonColor,
                    Color.white
                );
            }

            if (
                string.Equals(
                    normalized,
                    WButtonLegacyColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (
                    wbuttonCustomColors != null
                    && wbuttonCustomColors.TryGetValue(
                        WButtonLegacyColorKey,
                        out WButtonCustomColor legacy
                    )
                    && legacy != null
                )
                {
                    legacy.EnsureReadableText();
                    return new WButtonPaletteEntry(legacy.ButtonColor, legacy.TextColor);
                }

                Color fallbackButton = DefaultColorKeyButtonColor;
                Color fallbackText = WButtonColorUtility.GetReadableTextColor(fallbackButton);
                return new WButtonPaletteEntry(fallbackButton, fallbackText);
            }

            if (wbuttonCustomColors != null && wbuttonCustomColors.Count > 0)
            {
                if (
                    wbuttonCustomColors.TryGetValue(normalized, out WButtonCustomColor directValue)
                    && directValue != null
                )
                {
                    directValue.EnsureReadableText();
                    return new WButtonPaletteEntry(directValue.ButtonColor, directValue.TextColor);
                }

                foreach (KeyValuePair<string, WButtonCustomColor> entry in wbuttonCustomColors)
                {
                    if (
                        string.Equals(
                            entry.Key,
                            normalized,
                            System.StringComparison.OrdinalIgnoreCase
                        )
                        && entry.Value != null
                    )
                    {
                        entry.Value.EnsureReadableText();
                        return new WButtonPaletteEntry(
                            entry.Value.ButtonColor,
                            entry.Value.TextColor
                        );
                    }
                }
            }

            return GetThemeAwareDefaultWButtonPalette();
        }

        private WButtonPaletteEntry GetThemeAwareDefaultWButtonPalette()
        {
            string themeKey = EditorGUIUtility.isProSkin
                ? WButtonDarkThemeColorKey
                : WButtonLightThemeColorKey;
            Color fallbackButton = EditorGUIUtility.isProSkin
                ? DefaultDarkThemeButtonColor
                : DefaultLightThemeButtonColor;
            Color fallbackText = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            return GetWButtonThemePaletteEntry(themeKey, fallbackButton, fallbackText);
        }

        private WButtonPaletteEntry GetWButtonThemePaletteEntry(
            string key,
            Color buttonColor,
            Color defaultTextColor
        )
        {
            EnsureWButtonCustomColorDefaults();

            if (
                wbuttonCustomColors != null
                && wbuttonCustomColors.TryGetValue(key, out WButtonCustomColor value)
                && value != null
            )
            {
                value.EnsureReadableText();
                return new WButtonPaletteEntry(value.ButtonColor, value.TextColor);
            }

            Color textColor =
                defaultTextColor.maxColorComponent <= 0f
                    ? WButtonColorUtility.GetReadableTextColor(buttonColor)
                    : defaultTextColor;
            return new WButtonPaletteEntry(buttonColor, textColor);
        }

        private string EnsureWGroupColorKeyInternal(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return DefaultWGroupColorKey;
            }

            if (IsReservedWGroupColorKey(colorKey))
            {
                return NormalizeWGroupColorKey(colorKey);
            }

            EnsureWGroupCustomColorDefaults();

            string normalized = NormalizeWGroupColorKey(colorKey);
            if (!ContainsWGroupColorKey(normalized))
            {
                AddWGroupColorKey(normalized);
            }

            return NormalizeWGroupColorKey(normalized);
        }

        private void AddWGroupColorKey(string normalizedKey)
        {
            if (string.IsNullOrEmpty(normalizedKey))
            {
                return;
            }

            if (wgroupCustomColors == null)
            {
                wgroupCustomColors = new WGroupCustomColorDictionary();
            }

            if (ContainsWGroupColorKey(normalizedKey))
            {
                return;
            }

            int paletteIndex = wgroupCustomColors.Count;
            Color suggested = WButtonColorUtility.SuggestPaletteColor(paletteIndex);
            WGroupCustomColor color = new()
            {
                BackgroundColor = suggested,
                TextColor = WButtonColorUtility.GetReadableTextColor(suggested),
            };
            wgroupCustomColors[normalizedKey] = color;
            SaveSettings();
        }

        private bool ContainsWGroupColorKey(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return true;
            }

            if (IsReservedWGroupColorKey(colorKey))
            {
                return true;
            }

            if (wgroupCustomColors == null || wgroupCustomColors.Count == 0)
            {
                return false;
            }

            string normalized = colorKey.Trim();

            foreach (string existingKey in wgroupCustomColors.Keys)
            {
                if (
                    string.Equals(
                        existingKey,
                        normalized,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private string NormalizeWGroupColorKey(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return DefaultWGroupColorKey;
            }

            if (IsReservedWGroupColorKey(colorKey))
            {
                if (
                    string.Equals(
                        colorKey,
                        WGroupLightThemeColorKey,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WGroupLightThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WGroupDarkThemeColorKey,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WGroupDarkThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WGroupLegacyColorKey,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WGroupLegacyColorKey;
                }

                return DefaultWGroupColorKey;
            }

            if (wgroupCustomColors != null)
            {
                foreach (string existingKey in wgroupCustomColors.Keys)
                {
                    if (
                        string.Equals(
                            existingKey,
                            colorKey.Trim(),
                            System.StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return existingKey;
                    }
                }
            }

            return colorKey.Trim();
        }

        private WGroupPaletteEntry GetThemeAwareDefaultGroupPalette()
        {
            string themeKey = EditorGUIUtility.isProSkin
                ? WGroupDarkThemeColorKey
                : WGroupLightThemeColorKey;
            Color fallbackBackground = EditorGUIUtility.isProSkin
                ? DefaultDarkThemeGroupBackground
                : DefaultLightThemeGroupBackground;
            Color fallbackText = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            return GetThemePaletteEntry(themeKey, fallbackBackground, fallbackText);
        }

        private WGroupPaletteEntry GetThemePaletteEntry(
            string key,
            Color fallbackBackground,
            Color fallbackText
        )
        {
            EnsureWGroupCustomColorDefaults();

            if (
                wgroupCustomColors != null
                && wgroupCustomColors.TryGetValue(key, out WGroupCustomColor value)
                && value != null
            )
            {
                value.EnsureReadableText();
                return new WGroupPaletteEntry(value.BackgroundColor, value.TextColor);
            }

            Color readableText =
                fallbackText.maxColorComponent <= 0f
                    ? WButtonColorUtility.GetReadableTextColor(fallbackBackground)
                    : fallbackText;
            return new WGroupPaletteEntry(fallbackBackground, readableText);
        }

        private WGroupPaletteEntry GetWGroupPaletteEntry(string colorKey)
        {
            EnsureWGroupCustomColorDefaults();

            string normalized = NormalizeWGroupColorKey(colorKey);

            if (
                string.Equals(
                    normalized,
                    DefaultWGroupColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetThemeAwareDefaultGroupPalette();
            }

            if (
                string.Equals(
                    normalized,
                    WGroupLightThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetThemePaletteEntry(
                    WGroupLightThemeColorKey,
                    DefaultLightThemeGroupBackground,
                    Color.black
                );
            }

            if (
                string.Equals(
                    normalized,
                    WGroupDarkThemeColorKey,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetThemePaletteEntry(
                    WGroupDarkThemeColorKey,
                    DefaultDarkThemeGroupBackground,
                    Color.white
                );
            }

            if (
                wgroupCustomColors != null
                && wgroupCustomColors.TryGetValue(normalized, out WGroupCustomColor directValue)
                && directValue != null
            )
            {
                directValue.EnsureReadableText();
                return new WGroupPaletteEntry(directValue.BackgroundColor, directValue.TextColor);
            }

            if (wgroupCustomColors != null)
            {
                foreach (KeyValuePair<string, WGroupCustomColor> entry in wgroupCustomColors)
                {
                    if (
                        string.Equals(
                            entry.Key,
                            normalized,
                            System.StringComparison.OrdinalIgnoreCase
                        )
                        && entry.Value != null
                    )
                    {
                        entry.Value.EnsureReadableText();
                        return new WGroupPaletteEntry(
                            entry.Value.BackgroundColor,
                            entry.Value.TextColor
                        );
                    }
                }
            }

            return GetThemeAwareDefaultGroupPalette();
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
                    serializedSettings.UpdateIfRequiredOrScript();

                    DrawIntSlider(
                        "StringInList Page Size",
                        "Number of options displayed per page in StringInList dropdowns.",
                        settings.StringInListPageSize,
                        MinPageSize,
                        MaxPageSize,
                        value =>
                        {
                            settings.StringInListPageSize = value;
                            serializedSettings.UpdateIfRequiredOrScript();
                        }
                    );

                    DrawIntSlider(
                        "Serializable Set Page Size",
                        "Number of entries displayed per page in SerializableHashSet and SerializableSortedSet inspectors.",
                        settings.SerializableSetPageSize,
                        MinPageSize,
                        MaxPageSize,
                        value =>
                        {
                            settings.SerializableSetPageSize = value;
                            serializedSettings.UpdateIfRequiredOrScript();
                        }
                    );

                    DrawIntSlider(
                        "WEnum Toggle Buttons Page Size",
                        "Number of toggle buttons displayed per page when WEnumToggleButtons groups exceed the configured threshold.",
                        settings.EnumToggleButtonsPageSize,
                        MinPageSize,
                        MaxPageSize,
                        value =>
                        {
                            settings.EnumToggleButtonsPageSize = value;
                            serializedSettings.UpdateIfRequiredOrScript();
                        }
                    );

                    DrawIntSlider(
                        "WButton Page Size",
                        "Number of WButton actions displayed per page when grouped by draw order.",
                        settings.WButtonPageSize,
                        MinPageSize,
                        MaxPageSize,
                        value =>
                        {
                            settings.WButtonPageSize = value;
                            serializedSettings.UpdateIfRequiredOrScript();
                        }
                    );

                    DrawIntSlider(
                        "WButton History Size",
                        "Number of recent results remembered per WButton method for each inspected object.",
                        settings.WButtonHistorySize,
                        MinWButtonHistorySize,
                        MaxWButtonHistorySize,
                        value =>
                        {
                            settings.WButtonHistorySize = value;
                            serializedSettings.UpdateIfRequiredOrScript();
                        }
                    );

                    SerializedProperty actionsPlacementProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.wbuttonActionsPlacement)
                    );
                    SerializedProperty foldoutBehaviorProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.wbuttonFoldoutBehavior)
                    );
                    SerializedProperty duplicateModeProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.duplicateRowAnimationMode)
                    );
                    SerializedProperty wgroupModeProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.wgroupAutoIncludeMode)
                    );
                    SerializedProperty wgroupCountProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.wgroupAutoIncludeRowCount)
                    );
                    SerializedProperty duplicateTweenCyclesProperty =
                        serializedSettings.FindProperty(
                            nameof(UnityHelpersSettings.duplicateRowTweenCycles)
                        );
                    SerializedProperty patternsProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.serializableTypeIgnorePatterns)
                    );
                    SerializedProperty patternsInitializedProperty =
                        serializedSettings.FindProperty(
                            nameof(UnityHelpersSettings.serializableTypePatternsInitialized)
                        );
                    SerializedProperty wbuttonPaletteProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.wbuttonCustomColors)
                    );
                    SerializedProperty wgroupPaletteProperty = serializedSettings.FindProperty(
                        nameof(UnityHelpersSettings.wgroupCustomColors)
                    );

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.LabelField("WGroup Defaults", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(
                        wgroupModeProperty,
                        new GUIContent(
                            "Auto Include Mode",
                            "Default behavior for automatically extending WGroup declarations."
                        )
                    );

                    EditorGUILayout.PropertyField(
                        wgroupCountProperty,
                        new GUIContent(
                            "Finite Include Count",
                            "Number of additional serialized members appended when auto include mode is Finite."
                        )
                    );

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(
                        actionsPlacementProperty,
                        new GUIContent(
                            "WButton Placement",
                            "Controls whether WButton actions render near the top of the inspector (after the Script field) or near the bottom."
                        )
                    );

                    EditorGUILayout.PropertyField(
                        foldoutBehaviorProperty,
                        new GUIContent(
                            "WButton Foldout Behavior",
                            "Determines whether WButton action groups are always visible, start expanded, or start collapsed when first drawn."
                        )
                    );
                    EditorGUILayout.PropertyField(
                        serializedSettings.FindProperty(
                            nameof(UnityHelpersSettings.wbuttonFoldoutTweenEnabled)
                        ),
                        new GUIContent(
                            "Tween WButton Foldouts",
                            "Enable animated transitions when expanding or collapsing WButton action groups."
                        )
                    );
                    EditorGUILayout.PropertyField(
                        serializedSettings.FindProperty(
                            nameof(UnityHelpersSettings.wbuttonFoldoutSpeed)
                        ),
                        new GUIContent(
                            "WButton Foldout Speed",
                            "Animation speed used when expanding or collapsing WButton action groups."
                        )
                    );
                    EditorGUILayout.PropertyField(
                        serializedSettings.FindProperty(
                            nameof(UnityHelpersSettings.serializableDictionaryFoldoutTweenEnabled)
                        ),
                        new GUIContent(
                            "Tween Dictionary Foldouts",
                            "Enable animated transitions when expanding or collapsing SerializableDictionary pending entries."
                        )
                    );
                    EditorGUILayout.PropertyField(
                        serializedSettings.FindProperty(
                            nameof(UnityHelpersSettings.serializableDictionaryFoldoutSpeed)
                        ),
                        new GUIContent(
                            "Dictionary Foldout Speed",
                            "Animation speed used when expanding or collapsing SerializableDictionary pending entries."
                        )
                    );
                    EditorGUILayout.PropertyField(
                        serializedSettings.FindProperty(
                            nameof(
                                UnityHelpersSettings.serializableSortedDictionaryFoldoutTweenEnabled
                            )
                        ),
                        new GUIContent(
                            "Tween Sorted Dictionary Foldouts",
                            "Enable animated transitions when expanding or collapsing SerializableSortedDictionary pending entries."
                        )
                    );
                    EditorGUILayout.PropertyField(
                        serializedSettings.FindProperty(
                            nameof(UnityHelpersSettings.serializableSortedDictionaryFoldoutSpeed)
                        ),
                        new GUIContent(
                            "Sorted Dictionary Foldout Speed",
                            "Animation speed used when expanding or collapsing SerializableSortedDictionary pending entries."
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
                    DrawWButtonCustomColors(wbuttonPaletteProperty);
                    DrawWGroupCustomColors(wgroupPaletteProperty);

                    bool guiChanged = EditorGUI.EndChangeCheck();
                    bool applied = serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                    if (guiChanged || applied)
                    {
                        settings.SaveSettings();
                        serializedSettings.UpdateIfRequiredOrScript();
                    }
                },
                keywords = new[]
                {
                    "StringInList",
                    "Pagination",
                    "SerializableSet",
                    "WButton",
                    "Buttons",
                    "WGroup",
                    "Groups",
                    "Placement",
                    "Foldout",
                    "UnityHelpers",
                    "Duplicate",
                    "SerializableType",
                    "Regex",
                    "Speed",
                    "Animation",
                },
            };
        }

        private static void DrawWButtonCustomColors(SerializedProperty paletteProperty)
        {
            if (paletteProperty == null)
            {
                return;
            }

            GUIContent label = new(
                "WButton Custom Colors",
                "Configure named colors applied to WButton custom color keys. Buttons referencing a color key will render using the associated color."
            );

            EditorGUILayout.PropertyField(paletteProperty, label, true);
        }

        private static void DrawWGroupCustomColors(SerializedProperty paletteProperty)
        {
            if (paletteProperty == null)
            {
                return;
            }

            GUIContent label = new(
                "WGroup Custom Colors",
                "Configure named colors applied to WGroup color keys. Groups referencing a color key render using the associated colors."
            );

            EditorGUILayout.PropertyField(paletteProperty, label, true);
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
