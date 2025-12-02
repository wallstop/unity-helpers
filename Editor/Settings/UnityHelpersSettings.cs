namespace WallstopStudios.UnityHelpers.Editor.Settings
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Serialization;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;
    using WallstopStudios.UnityHelpers.Settings;
    using WallstopStudios.UnityHelpers.Utils;

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
        internal static event Action SettingsSaved;
        public const int MinPageSize = 5;
        public const int MaxPageSize = 500;
        public const int MaxSerializableDictionaryPageSize = 250;
        public const int DefaultStringInListPageSize = 25;
        public const int DefaultSerializableSetPageSize = 15;
        public const int DefaultSerializableDictionaryPageSize = 15;
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
        public const string DefaultWEnumToggleButtonsColorKey = "Default";
        public const string WEnumToggleButtonsLightThemeColorKey = "Default-Light";
        public const string WEnumToggleButtonsDarkThemeColorKey = "Default-Dark";
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

        [Obsolete("Use DefaultWButtonColorKey instead.")]
        public const string DefaultWButtonPriority = DefaultWButtonColorKey;
        public const int DefaultDuplicateTweenCycles = 3;
        public const float DefaultFoldoutSpeed = 2f;
        public const float MinFoldoutSpeed = 2f;
        public const float MaxFoldoutSpeed = 12f;
        private static readonly Color DefaultColorKeyButtonColor = new(0.243f, 0.525f, 0.988f, 1f);
        private static readonly Color DefaultLightThemeButtonColor = new(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color DefaultDarkThemeButtonColor = new(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color DefaultLightThemeEnumSelectedColor =
            DefaultColorKeyButtonColor;
        private static readonly Color DefaultLightThemeEnumSelectedTextColor = Color.white;
        private static readonly Color DefaultLightThemeEnumInactiveColor =
            DefaultLightThemeButtonColor;
        private static readonly Color DefaultLightThemeEnumInactiveTextColor = Color.black;
        private static readonly Color DefaultDarkThemeEnumSelectedColor =
            DefaultColorKeyButtonColor;
        private static readonly Color DefaultDarkThemeEnumSelectedTextColor = Color.white;
        private static readonly Color DefaultDarkThemeEnumInactiveColor =
            DefaultDarkThemeButtonColor;
        private static readonly Color DefaultDarkThemeEnumInactiveTextColor = Color.white;
        private static readonly Dictionary<int, bool> SettingsGroupFoldoutStates = new();
        private const float SettingsLabelWidth = 260f;
        private const float SettingsMinFieldWidth = 110f;
        private const string WaitInstructionBufferFoldoutKey = "Buffers";
        private static UnityHelpersBufferSettingsAsset waitInstructionBufferSettingsAsset;
        private static readonly GUIContent StringInListPageSizeContent =
            EditorGUIUtility.TrTextContent(
                "StringInList Page Size",
                "Number of options displayed per page in StringInList dropdowns."
            );
        private static readonly GUIContent SerializableSetPageSizeContent =
            EditorGUIUtility.TrTextContent(
                "Serializable Set Page Size",
                "Number of entries displayed per page in SerializableHashSet and SerializableSortedSet inspectors."
            );
        private static readonly GUIContent SerializableSetStartCollapsedContent =
            EditorGUIUtility.TrTextContent(
                "Start Serializable Sets Collapsed",
                "When enabled, SerializableHashSet and SerializableSortedSet inspectors start collapsed unless overridden per field via SerializableCollectionFoldoutAttribute."
            );
        private static readonly GUIContent SerializableDictionaryPageSizeContent =
            EditorGUIUtility.TrTextContent(
                "Serializable Dictionary Page Size",
                "Number of entries displayed per page in SerializableDictionary and SerializableSortedDictionary inspectors."
            );
        private static readonly GUIContent SerializableDictionaryStartCollapsedContent =
            EditorGUIUtility.TrTextContent(
                "Start Serializable Dictionaries Collapsed",
                "When enabled, SerializableDictionary and SerializableSortedDictionary inspectors start collapsed unless overridden with SerializableCollectionFoldoutAttribute."
            );
        private static readonly GUIContent EnumToggleButtonsPageSizeContent =
            EditorGUIUtility.TrTextContent(
                "WEnum Toggle Buttons Page Size",
                "Number of toggle buttons displayed per page when WEnumToggleButtons groups exceed the configured threshold."
            );
        private static readonly GUIContent WButtonPageSizeContent = EditorGUIUtility.TrTextContent(
            "WButton Page Size",
            "Number of WButton actions displayed per page when grouped by draw order."
        );
        private static readonly GUIContent WButtonHistorySizeContent =
            EditorGUIUtility.TrTextContent(
                "WButton Histroy Size",
                "Number of recent results remembered per WButton method for each inspected object."
            );
        private static readonly GUIContent WButtonPlacementContent = EditorGUIUtility.TrTextContent(
            "WButton Placement",
            "Controls where WButton actions render relative to the inspector content."
        );
        private static readonly GUIContent WButtonFoldoutBehaviorContent =
            EditorGUIUtility.TrTextContent(
                "WButton Foldout Behavior",
                "Determines whether WButton action groups are always visible, start expanded, or start collapsed when first drawn."
            );
        private static readonly GUIContent WButtonFoldoutTweenEnabledContent =
            EditorGUIUtility.TrTextContent(
                "WButton Foldout Tween Enabled",
                "Enable animated transitions when expanding or collapsing WButton action groups."
            );
        private static readonly GUIContent WButtonFoldoutSpeedContent =
            EditorGUIUtility.TrTextContent(
                "WButton Foldout Speed",
                "Animation speed used when expanding or collapsing WButton action groups."
            );
        private static readonly GUIContent WButtonCustomColorsContent =
            EditorGUIUtility.TrTextContent("WButton Custom Colors");
        private static readonly GUIContent WGroupCustomColorsContent =
            EditorGUIUtility.TrTextContent("WGroup Custom Colors");
        private static readonly GUIContent WEnumToggleButtonsCustomColorsContent =
            EditorGUIUtility.TrTextContent("WEnumToggleButtons Custom Colors");
        private static readonly GUIContent InlineEditorFoldoutBehaviorContent =
            EditorGUIUtility.TrTextContent(
                "WInLineEditor Foldout Behavior",
                "Default foldout state for inline object editors when a field does not specify a mode."
            );
        private static readonly GUIContent DictionaryFoldoutTweenEnabledContent =
            EditorGUIUtility.TrTextContent(
                "Tween Dictionary Foldouts",
                "Enable animated transitions when expanding or collapsing SerializableDictionary pending entries."
            );
        private static readonly GUIContent DictionaryFoldoutSpeedContent =
            EditorGUIUtility.TrTextContent(
                "Dictionary Foldout Speed",
                "Animation speed used when expanding or collapsing SerializableDictionary pending entries."
            );
        private static readonly GUIContent SortedDictionaryFoldoutTweenEnabledContent =
            EditorGUIUtility.TrTextContent(
                "Tween Sorted Dictionary Foldouts",
                "Enable animated transitions when expanding or collapsing SerializableSortedDictionary pending entries."
            );
        private static readonly GUIContent SortedDictionaryFoldoutSpeedContent =
            EditorGUIUtility.TrTextContent(
                "Sorted Dictionary Foldout Speed",
                "Animation speed used when expanding or collapsing SerializableSortedDictionary pending entries."
            );
        private static readonly GUIContent SetFoldoutTweenEnabledContent =
            EditorGUIUtility.TrTextContent(
                "Tween Serializable Set Foldouts",
                "Enable animated transitions when expanding or collapsing SerializableHashSet manual entry foldouts."
            );
        private static readonly GUIContent SetFoldoutSpeedContent = EditorGUIUtility.TrTextContent(
            "Serializable Set Foldout Speed",
            "Animation speed used when expanding or collapsing SerializableHashSet manual entry foldouts."
        );
        private static readonly GUIContent SortedSetFoldoutTweenEnabledContent =
            EditorGUIUtility.TrTextContent(
                "Tween Serializable Sorted Set Foldouts",
                "Enable animated transitions when expanding or collapsing SerializableSortedSet manual entry foldouts."
            );
        private static readonly GUIContent SortedSetFoldoutSpeedContent =
            EditorGUIUtility.TrTextContent(
                "Serializable Sorted Set Foldout Speed",
                "Animation speed used when expanding or collapsing SerializableSortedSet manual entry foldouts."
            );
        private const string WaitInstructionBufferDefaultsHelpText =
            "Configure the global defaults for Buffers.WaitInstruction pooling. These values are applied automatically on domain reload and when the player starts if Auto Apply is enabled.";
        private static readonly GUIContent WaitInstructionBufferApplyOnLoadContent =
            EditorGUIUtility.TrTextContent(
                "Auto Apply On Load",
                "When enabled, the configured defaults are applied automatically on domain reload, scene load, and in player builds."
            );
        private static readonly GUIContent WaitInstructionBufferQuantizationContent =
            EditorGUIUtility.TrTextContent(
                "Quantization Step (seconds)",
                "Durations are rounded to this step before being cached. Set to 0 to disable quantization."
            );
        private static readonly GUIContent WaitInstructionBufferMaxEntriesContent =
            EditorGUIUtility.TrTextContent(
                "Max Distinct Entries",
                "Maximum number of cached WaitForSeconds/Realtime durations (0 = unbounded)."
            );
        private static readonly GUIContent WaitInstructionBufferUseLruContent =
            EditorGUIUtility.TrTextContent(
                "Use LRU Eviction",
                "When enabled, the cache evicts the least recently used duration instead of refusing new entries once the limit is reached."
            );
        private static readonly GUIContent WaitInstructionBufferApplyNowButtonContent =
            EditorGUIUtility.TrTextContent("Apply Defaults Now");
        private static readonly GUIContent WaitInstructionBufferCaptureCurrentButtonContent =
            EditorGUIUtility.TrTextContent("Capture Current Values");
        private static readonly GUIContent DuplicateAnimationModeContent =
            EditorGUIUtility.TrTextContent(
                "Duplicate Row Animation",
                "Controls how duplicate entries are presented in SerializableDictionary inspectors."
            );
        private static readonly GUIContent DuplicateTweenCyclesContent =
            EditorGUIUtility.TrTextContent(
                "Tween Cycle Limit",
                "Number of shake cycles performed when highlighting duplicate entries. Negative values loop indefinitely."
            );
        private static readonly GUIContent SerializableSetDuplicateTweenEnabledContent =
            EditorGUIUtility.TrTextContent(
                "Tween Serializable Set Duplicates",
                "Enable lateral shake animations when highlighting duplicate or invalid entries in SerializableHashSet and SerializableSortedSet inspectors."
            );
        private static readonly GUIContent SerializableSetDuplicateTweenCyclesContent =
            EditorGUIUtility.TrTextContent(
                "Set Duplicate Tween Cycles",
                "Number of shake cycles performed for SerializableSet duplicate entries. Negative values loop indefinitely."
            );
        private static readonly GUIContent WGroupAutoIncludeModeContent =
            EditorGUIUtility.TrTextContent(
                "Auto Include Mode",
                "Default behavior for automatically extending WGroup declarations."
            );
        private static readonly GUIContent WGroupAutoIncludeCountContent =
            EditorGUIUtility.TrTextContent(
                "Finite Include Count",
                "Number of additional serialized members appended when auto include mode is Finite."
            );

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

        public enum InlineEditorFoldoutBehavior
        {
            AlwaysOpen = 0,
            StartExpanded = 1,
            StartCollapsed = 2,
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

        public readonly struct WEnumToggleButtonsPaletteEntry
        {
            public WEnumToggleButtonsPaletteEntry(
                Color selectedBackgroundColor,
                Color selectedTextColor,
                Color inactiveBackgroundColor,
                Color inactiveTextColor
            )
            {
                SelectedBackgroundColor = selectedBackgroundColor;
                SelectedTextColor = selectedTextColor;
                InactiveBackgroundColor = inactiveBackgroundColor;
                InactiveTextColor = inactiveTextColor;
            }

            public Color SelectedBackgroundColor { get; }

            public Color SelectedTextColor { get; }

            public Color InactiveBackgroundColor { get; }

            public Color InactiveTextColor { get; }
        }

        public enum DuplicateRowAnimationMode
        {
            [Obsolete("Disable duplicate duplicate-row animations only when required.", false)]
            None = 0,
            Static = 1,
            Tween = 2,
        }

        [SerializeField]
        [Tooltip(
            "Whether the configured wait instruction defaults should be applied automatically on domain reload and player start."
        )]
        [WGroup(
            WaitInstructionBufferFoldoutKey,
            displayName: "Buffers",
            collapsible: true,
            startCollapsed: true
        )]
        private bool waitInstructionBufferApplyOnLoad = true;

        [SerializeField]
        [Tooltip(
            "Rounds requested WaitForSeconds durations to this step size before caching (set to 0 to disable)."
        )]
        [Min(0f)]
        private float waitInstructionBufferQuantizationStepSeconds;

        [SerializeField]
        [Tooltip(
            "Maximum number of distinct WaitForSeconds/Realtime entries cached (0 = unlimited)."
        )]
        [Min(0)]
        private int waitInstructionBufferMaxDistinctEntries =
            Buffers.WaitInstructionDefaultMaxDistinctEntries;

        [SerializeField]
        [Tooltip(
            "Evict the least recently used duration when the cache hits the distinct entry limit."
        )]
        private bool waitInstructionBufferUseLruEviction;

        [SerializeField]
        [HideInInspector]
        private bool waitInstructionBufferDefaultsInitialized;

        [SerializeField]
        [Tooltip("Maximum number of entries shown per page for StringInList dropdowns.")]
        [Range(MinPageSize, MaxPageSize)]
        [WGroup(
            "Pagination",
            displayName: "Pagination Defaults",
            autoIncludeCount: 4,
            collapsible: true
        )]
        private int stringInListPageSize = DefaultStringInListPageSize;

        [SerializeField]
        [Tooltip(
            "Maximum number of entries shown per page when drawing SerializableHashSet/SerializableSortedSet inspectors."
        )]
        [Range(MinPageSize, MaxPageSize)]
        private int serializableSetPageSize = DefaultSerializableSetPageSize;

        [SerializeField]
        [Tooltip(
            "Whether SerializableHashSet and SerializableSortedSet inspectors start collapsed when first rendered."
        )]
        private bool serializableSetStartCollapsed = true;

        [SerializeField]
        [Tooltip(
            "Maximum number of entries shown per page when drawing SerializableDictionary/SerializableSortedDictionary inspectors."
        )]
        [Range(MinPageSize, MaxSerializableDictionaryPageSize)]
        private int serializableDictionaryPageSize = DefaultSerializableDictionaryPageSize;

        [SerializeField]
        [Tooltip(
            "Whether SerializableDictionary and SerializableSortedDictionary inspectors start collapsed when first rendered."
        )]
        private bool serializableDictionaryStartCollapsed = true;

        [SerializeField]
        [Tooltip(
            "Maximum number of toggle buttons shown per page when drawing WEnumToggleButtons groups."
        )]
        [Range(MinPageSize, MaxPageSize)]
        private int enumToggleButtonsPageSize = DefaultEnumToggleButtonsPageSize;

        [SerializeField]
        [Tooltip("Maximum number of WButton actions displayed per page in inspector trays.")]
        [Range(MinPageSize, MaxPageSize)]
        [WGroup(
            "WButton Actions",
            displayName: "WButton Actions",
            autoIncludeCount: 1,
            collapsible: true
        )]
        [WGroupEnd("Pagination")]
        private int wbuttonPageSize = DefaultWButtonPageSize;

        [SerializeField]
        [Tooltip("Number of recent invocation results retained per WButton method.")]
        [Range(MinWButtonHistorySize, MaxWButtonHistorySize)]
        private int wbuttonHistorySize = DefaultWButtonHistorySize;

        [SerializeField]
        [Tooltip("Controls where WButton actions are rendered relative to the inspector content.")]
        [WGroup(
            "WButton Layout",
            displayName: "WButton Layout",
            autoIncludeCount: 3,
            collapsible: true
        )]
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
        [WGroup(
            "Dictionary Foldouts",
            displayName: "Dictionary Foldouts",
            autoIncludeCount: 3,
            collapsible: true
        )]
        private bool serializableDictionaryFoldoutTweenEnabled = true;

        [SerializeField]
        [HideInInspector]
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
            "Enable animated transitions when expanding or collapsing SerializableHashSet manual entry foldouts."
        )]
        [WGroup("Serializable Sets", displayName: "Serializable Sets", collapsible: true)]
        private bool serializableSetFoldoutTweenEnabled = true;

        [SerializeField]
        [Tooltip("Animation speed used when toggling SerializableHashSet manual entry foldouts.")]
        [WShowIf(nameof(serializableSetFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float serializableSetFoldoutSpeed = DefaultFoldoutSpeed;

        [SerializeField]
        [Tooltip(
            "Enable lateral shake animations when highlighting duplicate or invalid entries in SerializableSet inspectors."
        )]
        private bool serializableSetDuplicateTweenEnabled = true;

        [SerializeField]
        [Tooltip(
            "When enabled, number of shake cycles to play for SerializableSet duplicate entries. Negative values loop indefinitely."
        )]
        [WShowIf(nameof(serializableSetDuplicateTweenEnabled))]
        private int serializableSetDuplicateTweenCycles = DefaultDuplicateTweenCycles;

        [SerializeField]
        [HideInInspector]
        private bool serializableSetTweensGroupEndSentinel;

        [SerializeField]
        [HideInInspector]
        private bool serializableSetDuplicateTweenSettingsInitialized;

        [SerializeField]
        [Tooltip(
            "Enable animated transitions when expanding or collapsing SerializableSortedSet manual entry foldouts."
        )]
        [WGroup("Sorted Set", displayName: "Sorted Sets", autoIncludeCount: 1, collapsible: true)]
        private bool serializableSortedSetFoldoutTweenEnabled = true;

        [SerializeField]
        [Tooltip("Animation speed used when toggling SerializableSortedSet manual entry foldouts.")]
        [WShowIf(nameof(serializableSortedSetFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        [WGroupEnd("Sorted Set Foldouts")]
        private float serializableSortedSetFoldoutSpeed = DefaultFoldoutSpeed;

        [SerializeField]
        [Tooltip(
            "Controls how duplicate entries are emphasized inside SerializableDictionary inspectors."
        )]
        [WGroup(
            "Duplicate Highlighting",
            displayName: "Duplicate Highlighting",
            autoIncludeCount: 1,
            collapsible: true
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
        [WGroup(
            "Serializable Types",
            displayName: "Serializable Types",
            autoIncludeCount: 0,
            collapsible: true
        )]
        private List<SerializableTypeIgnorePattern> serializableTypeIgnorePatterns;
        private string[] serializableTypeIgnorePatternCache = Array.Empty<string>();
        private int serializableTypeIgnorePatternCacheVersion = int.MinValue;

        [SerializeField]
        [Tooltip(
            "Controls how WGroup automatically includes additional serialized members after a group declaration."
        )]
        [WGroup(
            "WGroup Defaults",
            displayName: "WGroup Defaults",
            autoIncludeCount: 1,
            collapsible: true
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
        [Tooltip("Named color palette applied to WButton custom color keys.")]
        [WGroup(
            "Color Palettes",
            displayName: "Color Palettes",
            autoIncludeCount: 4,
            collapsible: true
        )]
        private WButtonCustomColorDictionary wbuttonCustomColors = new();

        [SerializeField]
        [Tooltip("Named color palette applied to WGroup custom color keys.")]
        private WGroupCustomColorDictionary wgroupCustomColors = new();

        [SerializeField]
        [Tooltip("Named color palette applied to WEnumToggleButtons color keys.")]
        [WGroupEnd("Color Palettes")]
        private WEnumToggleButtonsCustomColorDictionary wenumToggleButtonsCustomColors = new();

        [SerializeField]
        [FormerlySerializedAs("wbuttonPriorityColors")]
#pragma warning disable CS0618 // Type or member is obsolete
        [HideInInspector]
        private List<WButtonPriorityColor> legacyWButtonPriorityColors;
#pragma warning restore CS0618 // Type or member is obsolete

        [SerializeField]
        [HideInInspector]
        private bool serializableTypePatternsInitialized;

        [NonSerialized]
        private HashSet<string> wbuttonCustomColorSkipAutoSuggest;

        [NonSerialized]
        private HashSet<string> wgroupCustomColorSkipAutoSuggest;

        [SerializeField]
        [Tooltip(
            "Default foldout behavior used by WInLineEditor when a field does not override the mode."
        )]
        [WGroup(
            "InlineEditors",
            displayName: "Inline Editors",
            autoIncludeCount: 1,
            collapsible: true
        )]
        private InlineEditorFoldoutBehavior inlineEditorFoldoutBehavior =
            InlineEditorFoldoutBehavior.StartExpanded;

        internal HashSet<string> WButtonCustomColorSkipAutoSuggest
        {
            get => wbuttonCustomColorSkipAutoSuggest;
            set => wbuttonCustomColorSkipAutoSuggest = value;
        }

        internal HashSet<string> WGroupCustomColorSkipAutoSuggest
        {
            get => wgroupCustomColorSkipAutoSuggest;
            set => wgroupCustomColorSkipAutoSuggest = value;
        }

        [Serializable]
        internal sealed class SerializableTypeIgnorePattern
        {
            [SerializeField]
            internal string pattern = string.Empty;

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

        [Serializable]
        internal sealed class WButtonCustomColor
        {
            [SerializeField]
            internal Color buttonColor = Color.white;

            [SerializeField]
            internal Color textColor = Color.black;

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

        [Serializable]
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

        [CustomPropertyDrawer(typeof(WEnumToggleButtonsCustomColor))]
        private sealed class WEnumToggleButtonsCustomColorDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                return lineHeight * 2f + spacing;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty selectedBackground = property.FindPropertyRelative(
                    "selectedBackgroundColor"
                );
                SerializedProperty selectedText = property.FindPropertyRelative(
                    "selectedTextColor"
                );
                SerializedProperty inactiveBackground = property.FindPropertyRelative(
                    "inactiveBackgroundColor"
                );
                SerializedProperty inactiveText = property.FindPropertyRelative(
                    "inactiveTextColor"
                );

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float halfWidth = (position.width - spacing) * 0.5f;
                float lineHeight = EditorGUIUtility.singleLineHeight;

                Rect selectedBackgroundRect = new(position.x, position.y, halfWidth, lineHeight);
                Rect selectedTextRect = new(
                    position.x + halfWidth + spacing,
                    position.y,
                    halfWidth,
                    lineHeight
                );
                Rect inactiveBackgroundRect = new(
                    position.x,
                    position.y + lineHeight + spacing,
                    halfWidth,
                    lineHeight
                );
                Rect inactiveTextRect = new(
                    position.x + halfWidth + spacing,
                    position.y + lineHeight + spacing,
                    halfWidth,
                    lineHeight
                );

                EditorGUI.PropertyField(
                    selectedBackgroundRect,
                    selectedBackground,
                    EditorGUIUtility.TrTextContent("Selected Background")
                );
                EditorGUI.PropertyField(
                    selectedTextRect,
                    selectedText,
                    EditorGUIUtility.TrTextContent("Selected Text")
                );
                EditorGUI.PropertyField(
                    inactiveBackgroundRect,
                    inactiveBackground,
                    EditorGUIUtility.TrTextContent("Inactive Background")
                );
                EditorGUI.PropertyField(
                    inactiveTextRect,
                    inactiveText,
                    EditorGUIUtility.TrTextContent("Inactive Text")
                );
            }
        }
#endif

        [Serializable]
        internal sealed class WGroupCustomColor
        {
            [SerializeField]
            internal Color backgroundColor = DefaultColorKeyButtonColor;

            [SerializeField]
            internal Color textColor = Color.white;

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

        [Serializable]
        private sealed class WGroupCustomColorDictionary
            : SerializableDictionary<string, WGroupCustomColor> { }

        [Serializable]
        private sealed class WEnumToggleButtonsCustomColor
        {
            [SerializeField]
            private Color selectedBackgroundColor = DefaultColorKeyButtonColor;

            [SerializeField]
            private Color selectedTextColor = Color.white;

            [SerializeField]
            private Color inactiveBackgroundColor = DefaultLightThemeButtonColor;

            [SerializeField]
            private Color inactiveTextColor = Color.black;

            public Color SelectedBackgroundColor
            {
                get => selectedBackgroundColor;
                set => selectedBackgroundColor = value;
            }

            public Color SelectedTextColor
            {
                get => selectedTextColor;
                set => selectedTextColor = value;
            }

            public Color InactiveBackgroundColor
            {
                get => inactiveBackgroundColor;
                set => inactiveBackgroundColor = value;
            }

            public Color InactiveTextColor
            {
                get => inactiveTextColor;
                set => inactiveTextColor = value;
            }

            public void EnsureReadableText()
            {
                if (selectedTextColor.maxColorComponent <= 0f)
                {
                    selectedTextColor = WButtonColorUtility.GetReadableTextColor(
                        selectedBackgroundColor
                    );
                }

                if (inactiveTextColor.maxColorComponent <= 0f)
                {
                    inactiveTextColor = WButtonColorUtility.GetReadableTextColor(
                        inactiveBackgroundColor
                    );
                }
            }
        }

        [Serializable]
        private sealed class WEnumToggleButtonsCustomColorDictionary
            : SerializableDictionary<string, WEnumToggleButtonsCustomColor> { }

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
        /// Configures whether SerializableSet inspectors start collapsed by default.
        /// </summary>
        public bool SerializableSetStartCollapsed
        {
            get => serializableSetStartCollapsed;
            set
            {
                if (serializableSetStartCollapsed == value)
                {
                    return;
                }

                serializableSetStartCollapsed = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Retrieves the configured page size for SerializableDictionary inspectors.
        /// </summary>
        public int SerializableDictionaryPageSize
        {
            get =>
                Mathf.Clamp(
                    serializableDictionaryPageSize,
                    MinPageSize,
                    MaxSerializableDictionaryPageSize
                );
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxSerializableDictionaryPageSize);
                if (clamped == serializableDictionaryPageSize)
                {
                    return;
                }

                serializableDictionaryPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Configures whether SerializableDictionary inspectors start collapsed by default.
        /// </summary>
        public bool SerializableDictionaryStartCollapsed
        {
            get => serializableDictionaryStartCollapsed;
            set
            {
                if (serializableDictionaryStartCollapsed == value)
                {
                    return;
                }

                serializableDictionaryStartCollapsed = value;
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

        [Serializable]
        [Obsolete("Use WButtonCustomColorDictionary for serialization instead.")]
        private sealed class WButtonPriorityColor
        {
            [SerializeField]
            internal string priority = DefaultWButtonColorKey;

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
                serializableTypeIgnorePatternCache = Array.Empty<string>();
                serializableTypeIgnorePatternCacheVersion = 0;
                return serializableTypeIgnorePatternCache;
            }

            int version = ComputeSerializableTypePatternVersion();
            if (version == serializableTypeIgnorePatternCacheVersion)
            {
                return serializableTypeIgnorePatternCache;
            }

            using PooledResource<List<string>> patternsLease = Buffers<string>.List.Get(
                out List<string> patterns
            );
            using PooledResource<HashSet<string>> seenLease = Buffers<string>.HashSet.Get(
                out HashSet<string> seen
            );

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
                if (seen.Add(trimmed))
                {
                    patterns.Add(trimmed);
                }
            }

            serializableTypeIgnorePatternCache =
                patterns.Count == 0 ? Array.Empty<string>() : patterns.ToArray();
            serializableTypeIgnorePatternCacheVersion = version;
            return serializableTypeIgnorePatternCache;
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

        /// <summary>
        /// Determines whether SerializableSet inspectors should start collapsed by default.
        /// </summary>
        public static bool ShouldStartSerializableSetCollapsed()
        {
            UnityHelpersSettings settings = instance;
            return settings == null || settings.serializableSetStartCollapsed;
        }

        private void InvalidateSerializableTypePatternCache()
        {
            serializableTypeIgnorePatternCache = Array.Empty<string>();
            serializableTypeIgnorePatternCacheVersion = int.MinValue;
        }

        private int ComputeSerializableTypePatternVersion()
        {
            if (serializableTypeIgnorePatterns == null || serializableTypeIgnorePatterns.Count == 0)
            {
                return 0;
            }

            HashCode hash = new HashCode();
            hash.Add(serializableTypeIgnorePatterns.Count);
            for (int i = 0; i < serializableTypeIgnorePatterns.Count; i++)
            {
                SerializableTypeIgnorePattern entry = serializableTypeIgnorePatterns[i];
                string trimmed = entry?.Pattern?.Trim() ?? string.Empty;
                hash.Add(trimmed);
            }

            return hash.ToHashCode();
        }

        public static int GetSerializableDictionaryPageSize()
        {
            return Mathf.Clamp(
                instance.SerializableDictionaryPageSize,
                MinPageSize,
                MaxSerializableDictionaryPageSize
            );
        }

        /// <summary>
        /// Determines whether SerializableDictionary inspectors should start collapsed by default.
        /// </summary>
        public static bool ShouldStartSerializableDictionaryCollapsed()
        {
            UnityHelpersSettings settings = instance;
            return settings == null || settings.serializableDictionaryStartCollapsed;
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

        public static WEnumToggleButtonsPaletteEntry ResolveWEnumToggleButtonsPalette(
            string colorKey
        )
        {
            return instance.GetWEnumToggleButtonsPaletteEntry(colorKey);
        }

        internal static bool HasWEnumToggleButtonsPaletteColorKey(string colorKey)
        {
            return instance.ContainsWEnumToggleButtonsColorKey(colorKey);
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

        public static bool ShouldTweenSerializableSetFoldouts()
        {
            return instance.serializableSetFoldoutTweenEnabled;
        }

        public static float GetSerializableSetFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance.serializableSetFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
        }

        public static bool ShouldTweenSerializableSortedSetFoldouts()
        {
            return instance.serializableSortedSetFoldoutTweenEnabled;
        }

        public static float GetSerializableSortedSetFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance.serializableSortedSetFoldoutSpeed,
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

        public static bool ShouldTweenSerializableSetDuplicates()
        {
            return instance.serializableSetDuplicateTweenEnabled
                && instance.duplicateRowAnimationMode == DuplicateRowAnimationMode.Tween;
        }

        public static int GetSerializableSetDuplicateTweenCycleLimit()
        {
            if (!instance.serializableSetDuplicateTweenSettingsInitialized)
            {
                return instance.duplicateRowTweenCycles;
            }

            return instance.serializableSetDuplicateTweenCycles;
        }

        public static InlineEditorFoldoutBehavior GetInlineEditorFoldoutBehavior()
        {
            return instance.inlineEditorFoldoutBehavior;
        }

        internal static void RegisterPaletteManualEdit(string propertyPath, string key)
        {
            if (string.IsNullOrWhiteSpace(propertyPath) || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            instance?.RegisterPaletteManualEditInternal(propertyPath, key);
        }

        internal static class SerializedPropertyNames
        {
            internal const string SerializableTypeIgnorePatterns = nameof(
                serializableTypeIgnorePatterns
            );
            internal const string SerializableTypePatternsInitialized = nameof(
                serializableTypePatternsInitialized
            );
            internal const string SerializableTypePattern = nameof(
                SerializableTypeIgnorePattern.pattern
            );
            internal const string LegacyWButtonPriorityColors = nameof(legacyWButtonPriorityColors);
            internal const string WButtonCustomColors = nameof(wbuttonCustomColors);
            internal const string WGroupCustomColors = nameof(wgroupCustomColors);
            internal const string WEnumToggleButtonsCustomColors = nameof(
                wenumToggleButtonsCustomColors
            );
            internal const string InlineEditorFoldoutBehavior = nameof(inlineEditorFoldoutBehavior);
            internal const string WButtonFoldoutTweenEnabled = nameof(wbuttonFoldoutTweenEnabled);
            internal const string SerializableDictionaryFoldoutTweenEnabled = nameof(
                serializableDictionaryFoldoutTweenEnabled
            );
            internal const string SerializableSortedDictionaryFoldoutTweenEnabled = nameof(
                serializableSortedDictionaryFoldoutTweenEnabled
            );
            internal const string SerializableSetFoldoutTweenEnabled = nameof(
                serializableSetFoldoutTweenEnabled
            );
            internal const string SerializableSortedSetFoldoutTweenEnabled = nameof(
                serializableSortedSetFoldoutTweenEnabled
            );
            internal const string FoldoutTweenSettingsInitialized = nameof(
                foldoutTweenSettingsInitialized
            );
#pragma warning disable CS0618 // Type or member is obsolete
            internal const string WButtonPriority = nameof(WButtonPriorityColor.priority);
#pragma warning restore CS0618 // Type or member is obsolete
            internal const string WButtonCustomColorButton = nameof(WButtonCustomColor.buttonColor);
            internal const string WButtonCustomColorText = nameof(WButtonCustomColor.textColor);
            internal const string WGroupCustomColorBackground = nameof(
                WGroupCustomColor.backgroundColor
            );
            internal const string WGroupCustomColorText = nameof(WGroupCustomColor.textColor);
        }

        /// <summary>
        /// Ensures persisted data stays within valid range.
        /// </summary>
        internal void OnEnable()
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
            serializableDictionaryPageSize = Mathf.Clamp(
                serializableDictionaryPageSize <= 0
                    ? DefaultSerializableDictionaryPageSize
                    : serializableDictionaryPageSize,
                MinPageSize,
                MaxSerializableDictionaryPageSize
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
            if (!Enum.IsDefined(typeof(WButtonActionsPlacement), wbuttonActionsPlacement))
            {
                wbuttonActionsPlacement = WButtonActionsPlacement.Top;
            }

            if (!Enum.IsDefined(typeof(WButtonFoldoutBehavior), wbuttonFoldoutBehavior))
            {
                wbuttonFoldoutBehavior = WButtonFoldoutBehavior.StartExpanded;
            }
            if (!Enum.IsDefined(typeof(WGroupAutoIncludeMode), wgroupAutoIncludeMode))
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
            serializableSetFoldoutSpeed = Mathf.Clamp(
                serializableSetFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : serializableSetFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            serializableSortedSetFoldoutSpeed = Mathf.Clamp(
                serializableSortedSetFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : serializableSortedSetFoldoutSpeed,
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
            if (EnsureWEnumToggleButtonsCustomColorDefaults())
            {
                SaveSettings();
            }
            if (EnsureWGroupCustomColorDefaults())
            {
                SaveSettings();
            }

            bool shouldApplyRuntimeConfig = true;
            if (EnsureSerializableTypePatternDefaults())
            {
                SaveSettings();
                shouldApplyRuntimeConfig = false;
            }
            if (EnsureSerializableSetTweenDefaults())
            {
                SaveSettings();
                shouldApplyRuntimeConfig = false;
            }
            if (shouldApplyRuntimeConfig)
            {
                ApplyRuntimeConfiguration();
            }
        }

        /// <summary>
        /// Persists any modifications to disk.
        /// </summary>
        public void SaveSettings()
        {
            EnsureWButtonCustomColorDefaults();
            EnsureWEnumToggleButtonsCustomColorDefaults();
            EnsureWGroupCustomColorDefaults();
            ApplyRuntimeConfiguration();
            Save(true);
            SettingsSaved?.Invoke();
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
                InvalidateSerializableTypePatternCache();
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

            if (!serializableSetFoldoutTweenEnabled)
            {
                serializableSetFoldoutTweenEnabled = true;
            }

            if (!serializableSortedSetFoldoutTweenEnabled)
            {
                serializableSortedSetFoldoutTweenEnabled = true;
            }

            foldoutTweenSettingsInitialized = true;
            return true;
        }

        private bool EnsureSerializableSetTweenDefaults()
        {
            if (serializableSetDuplicateTweenSettingsInitialized)
            {
                return false;
            }

            serializableSetDuplicateTweenEnabled =
                duplicateRowAnimationMode == DuplicateRowAnimationMode.Tween;
            serializableSetDuplicateTweenCycles =
                duplicateRowTweenCycles != 0
                    ? duplicateRowTweenCycles
                    : DefaultDuplicateTweenCycles;
            serializableSetDuplicateTweenSettingsInitialized = true;
            return true;
        }

        internal bool EnsureWButtonCustomColorDefaults()
        {
            wbuttonCustomColors ??= new WButtonCustomColorDictionary();

            bool changed = false;
            changed |= MigrateLegacyWButtonPalette();

            if (
                wbuttonCustomColors.TryGetValue(
                    DefaultWButtonColorKey,
                    out WButtonCustomColor legacyDefault
                )
            )
            {
                wbuttonCustomColors.TryAdd(WButtonLegacyColorKey, legacyDefault);
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
            foreach (
                KeyValuePair<string, WButtonCustomColor> entry in wbuttonCustomColors.ToArray()
            )
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

                if (ShouldSkipWButtonAutoSuggest(entry.Key))
                {
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

        private bool ShouldSkipWButtonAutoSuggest(string key)
        {
            return ShouldSkipAutoSuggest(wbuttonCustomColorSkipAutoSuggest, key);
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

            return string.Equals(key, DefaultWButtonColorKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, WButtonLightThemeColorKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, WButtonDarkThemeColorKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, WButtonLegacyColorKey, StringComparison.OrdinalIgnoreCase);
        }

        private bool MigrateLegacyWButtonPalette()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (legacyWButtonPriorityColors == null || legacyWButtonPriorityColors.Count == 0)
            {
                return false;
            }

            bool changed = false;
            foreach (WButtonPriorityColor legacy in legacyWButtonPriorityColors)
            {
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

        internal bool EnsureWGroupCustomColorDefaults()
        {
            wgroupCustomColors ??= new WGroupCustomColorDictionary();

            bool changed = false;

            if (
                wgroupCustomColors.TryGetValue(
                    DefaultWGroupColorKey,
                    out WGroupCustomColor legacyDefault
                )
            )
            {
                wgroupCustomColors.TryAdd(WGroupLegacyColorKey, legacyDefault);
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

                if (ShouldSkipWGroupAutoSuggest(entry.Key))
                {
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

        private bool ShouldSkipWGroupAutoSuggest(string key)
        {
            return ShouldSkipAutoSuggest(wgroupCustomColorSkipAutoSuggest, key);
        }

        private bool EnsureWEnumToggleButtonsCustomColorDefaults()
        {
            if (wenumToggleButtonsCustomColors == null)
            {
                wenumToggleButtonsCustomColors = new WEnumToggleButtonsCustomColorDictionary();
            }

            bool changed = false;

            if (!wenumToggleButtonsCustomColors.ContainsKey(DefaultWEnumToggleButtonsColorKey))
            {
                bool proSkin = EditorGUIUtility.isProSkin;
                WEnumToggleButtonsCustomColor defaultColor = new()
                {
                    SelectedBackgroundColor = proSkin
                        ? DefaultDarkThemeEnumSelectedColor
                        : DefaultLightThemeEnumSelectedColor,
                    SelectedTextColor = proSkin
                        ? DefaultDarkThemeEnumSelectedTextColor
                        : DefaultLightThemeEnumSelectedTextColor,
                    InactiveBackgroundColor = proSkin
                        ? DefaultDarkThemeEnumInactiveColor
                        : DefaultLightThemeEnumInactiveColor,
                    InactiveTextColor = proSkin
                        ? DefaultDarkThemeEnumInactiveTextColor
                        : DefaultLightThemeEnumInactiveTextColor,
                };
                defaultColor.EnsureReadableText();
                wenumToggleButtonsCustomColors[DefaultWEnumToggleButtonsColorKey] = defaultColor;
                changed = true;
            }

            changed |= EnsureWEnumToggleButtonsThemeEntry(
                WEnumToggleButtonsLightThemeColorKey,
                DefaultLightThemeEnumSelectedColor,
                DefaultLightThemeEnumSelectedTextColor,
                DefaultLightThemeEnumInactiveColor,
                DefaultLightThemeEnumInactiveTextColor
            );
            changed |= EnsureWEnumToggleButtonsThemeEntry(
                WEnumToggleButtonsDarkThemeColorKey,
                DefaultDarkThemeEnumSelectedColor,
                DefaultDarkThemeEnumSelectedTextColor,
                DefaultDarkThemeEnumInactiveColor,
                DefaultDarkThemeEnumInactiveTextColor
            );

            foreach (
                KeyValuePair<
                    string,
                    WEnumToggleButtonsCustomColor
                > entry in wenumToggleButtonsCustomColors
            )
            {
                WEnumToggleButtonsCustomColor value = entry.Value;
                if (value == null)
                {
                    value = new WEnumToggleButtonsCustomColor();
                    wenumToggleButtonsCustomColors[entry.Key] = value;
                    changed = true;
                }

                Color previousSelectedText = value.SelectedTextColor;
                Color previousInactiveText = value.InactiveTextColor;
                value.EnsureReadableText();

                if (!ColorsApproximatelyEqual(value.SelectedTextColor, previousSelectedText))
                {
                    changed = true;
                }

                if (!ColorsApproximatelyEqual(value.InactiveTextColor, previousInactiveText))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private bool EnsureWEnumToggleButtonsThemeEntry(
            string key,
            Color selectedBackground,
            Color selectedTextDefault,
            Color inactiveBackground,
            Color inactiveTextDefault
        )
        {
            if (
                wenumToggleButtonsCustomColors.TryGetValue(
                    key,
                    out WEnumToggleButtonsCustomColor existing
                )
                && existing != null
            )
            {
                existing.EnsureReadableText();
                return false;
            }

            Color resolvedSelectedText =
                selectedTextDefault.maxColorComponent <= 0f
                    ? WButtonColorUtility.GetReadableTextColor(selectedBackground)
                    : selectedTextDefault;
            Color resolvedInactiveText =
                inactiveTextDefault.maxColorComponent <= 0f
                    ? WButtonColorUtility.GetReadableTextColor(inactiveBackground)
                    : inactiveTextDefault;

            WEnumToggleButtonsCustomColor themeColor = new()
            {
                SelectedBackgroundColor = selectedBackground,
                SelectedTextColor = resolvedSelectedText,
                InactiveBackgroundColor = inactiveBackground,
                InactiveTextColor = resolvedInactiveText,
            };
            themeColor.EnsureReadableText();
            wenumToggleButtonsCustomColors[key] = themeColor;
            return true;
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

        private static bool ShouldSkipAutoSuggest(HashSet<string> skipSet, string key)
        {
            if (skipSet == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return skipSet.Contains(key.Trim());
        }

        private static bool IsReservedWGroupColorKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            return string.Equals(key, DefaultWGroupColorKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, WGroupLightThemeColorKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, WGroupDarkThemeColorKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, WGroupLegacyColorKey, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReservedWEnumToggleButtonsColorKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            return string.Equals(
                    key,
                    DefaultWEnumToggleButtonsColorKey,
                    StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WEnumToggleButtonsLightThemeColorKey,
                    StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    key,
                    WEnumToggleButtonsDarkThemeColorKey,
                    StringComparison.OrdinalIgnoreCase
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
                if (string.Equals(existingKey, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsWEnumToggleButtonsColorKey(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return true;
            }

            if (IsReservedWEnumToggleButtonsColorKey(colorKey))
            {
                return true;
            }

            if (wenumToggleButtonsCustomColors == null || wenumToggleButtonsCustomColors.Count == 0)
            {
                return false;
            }

            string normalized = colorKey.Trim();

            foreach (string existingKey in wenumToggleButtonsCustomColors.Keys)
            {
                if (string.Equals(existingKey, normalized, StringComparison.OrdinalIgnoreCase))
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
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WButtonLightThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WButtonDarkThemeColorKey,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WButtonDarkThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WButtonLegacyColorKey,
                        StringComparison.OrdinalIgnoreCase
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
                    if (string.Equals(existingKey, normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return existingKey;
                    }
                }
            }

            return normalized;
        }

        private void RegisterPaletteManualEditInternal(string propertyPath, string key)
        {
            string trimmedKey = key?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedKey))
            {
                return;
            }

            if (
                string.Equals(
                    propertyPath,
                    SerializedPropertyNames.WButtonCustomColors,
                    StringComparison.Ordinal
                )
            )
            {
                wbuttonCustomColorSkipAutoSuggest ??= new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );
                wbuttonCustomColorSkipAutoSuggest.Add(trimmedKey);
                return;
            }

            if (
                string.Equals(
                    propertyPath,
                    SerializedPropertyNames.WGroupCustomColors,
                    StringComparison.Ordinal
                )
            )
            {
                wgroupCustomColorSkipAutoSuggest ??= new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );
                wgroupCustomColorSkipAutoSuggest.Add(trimmedKey);
            }
        }

        private string NormalizeWEnumToggleButtonsColorKey(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return DefaultWEnumToggleButtonsColorKey;
            }

            if (IsReservedWEnumToggleButtonsColorKey(colorKey))
            {
                if (
                    string.Equals(
                        colorKey,
                        WEnumToggleButtonsLightThemeColorKey,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WEnumToggleButtonsLightThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WEnumToggleButtonsDarkThemeColorKey,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WEnumToggleButtonsDarkThemeColorKey;
                }

                return DefaultWEnumToggleButtonsColorKey;
            }

            if (wenumToggleButtonsCustomColors != null)
            {
                foreach (string existingKey in wenumToggleButtonsCustomColors.Keys)
                {
                    if (string.Equals(existingKey, colorKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return existingKey;
                    }
                }
            }

            return colorKey.Trim();
        }

        private WButtonPaletteEntry GetWButtonPaletteEntry(string colorKey)
        {
            EnsureWButtonCustomColorDefaults();

            string normalized = NormalizeColorKey(colorKey);

            if (
                string.Equals(
                    normalized,
                    DefaultWButtonColorKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetThemeAwareDefaultWButtonPalette();
            }

            if (
                string.Equals(
                    normalized,
                    WButtonLightThemeColorKey,
                    StringComparison.OrdinalIgnoreCase
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
                    StringComparison.OrdinalIgnoreCase
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
                string.Equals(normalized, WButtonLegacyColorKey, StringComparison.OrdinalIgnoreCase)
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

            if (wbuttonCustomColors is not { Count: > 0 })
            {
                return GetThemeAwareDefaultWButtonPalette();
            }

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
                    string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase)
                    && entry.Value != null
                )
                {
                    entry.Value.EnsureReadableText();
                    return new WButtonPaletteEntry(entry.Value.ButtonColor, entry.Value.TextColor);
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

            wgroupCustomColors ??= new WGroupCustomColorDictionary();

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
                if (string.Equals(existingKey, normalized, StringComparison.OrdinalIgnoreCase))
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
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WGroupLightThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WGroupDarkThemeColorKey,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return WGroupDarkThemeColorKey;
                }

                if (
                    string.Equals(
                        colorKey,
                        WGroupLegacyColorKey,
                        StringComparison.OrdinalIgnoreCase
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
                            StringComparison.OrdinalIgnoreCase
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
                string.Equals(normalized, DefaultWGroupColorKey, StringComparison.OrdinalIgnoreCase)
            )
            {
                return GetThemeAwareDefaultGroupPalette();
            }

            if (
                string.Equals(
                    normalized,
                    WGroupLightThemeColorKey,
                    StringComparison.OrdinalIgnoreCase
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
                    StringComparison.OrdinalIgnoreCase
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
                        string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase)
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

        private WEnumToggleButtonsPaletteEntry GetWEnumToggleButtonsPaletteEntry(string colorKey)
        {
            EnsureWEnumToggleButtonsCustomColorDefaults();

            string normalized = NormalizeWEnumToggleButtonsColorKey(colorKey);

            if (
                string.Equals(
                    normalized,
                    DefaultWEnumToggleButtonsColorKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetThemeAwareDefaultWEnumToggleButtonsPalette();
            }

            if (
                string.Equals(
                    normalized,
                    WEnumToggleButtonsLightThemeColorKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetWEnumToggleButtonsThemePaletteEntry(
                    WEnumToggleButtonsLightThemeColorKey,
                    DefaultLightThemeEnumSelectedColor,
                    DefaultLightThemeEnumSelectedTextColor,
                    DefaultLightThemeEnumInactiveColor,
                    DefaultLightThemeEnumInactiveTextColor
                );
            }

            if (
                string.Equals(
                    normalized,
                    WEnumToggleButtonsDarkThemeColorKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return GetWEnumToggleButtonsThemePaletteEntry(
                    WEnumToggleButtonsDarkThemeColorKey,
                    DefaultDarkThemeEnumSelectedColor,
                    DefaultDarkThemeEnumSelectedTextColor,
                    DefaultDarkThemeEnumInactiveColor,
                    DefaultDarkThemeEnumInactiveTextColor
                );
            }

            if (
                wenumToggleButtonsCustomColors != null
                && wenumToggleButtonsCustomColors.TryGetValue(
                    normalized,
                    out WEnumToggleButtonsCustomColor directValue
                )
                && directValue != null
            )
            {
                directValue.EnsureReadableText();
                return new WEnumToggleButtonsPaletteEntry(
                    directValue.SelectedBackgroundColor,
                    directValue.SelectedTextColor,
                    directValue.InactiveBackgroundColor,
                    directValue.InactiveTextColor
                );
            }

            if (wenumToggleButtonsCustomColors != null)
            {
                foreach (
                    KeyValuePair<
                        string,
                        WEnumToggleButtonsCustomColor
                    > entry in wenumToggleButtonsCustomColors
                )
                {
                    if (
                        string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase)
                        && entry.Value != null
                    )
                    {
                        entry.Value.EnsureReadableText();
                        return new WEnumToggleButtonsPaletteEntry(
                            entry.Value.SelectedBackgroundColor,
                            entry.Value.SelectedTextColor,
                            entry.Value.InactiveBackgroundColor,
                            entry.Value.InactiveTextColor
                        );
                    }
                }
            }

            return GetThemeAwareDefaultWEnumToggleButtonsPalette();
        }

        private WEnumToggleButtonsPaletteEntry GetThemeAwareDefaultWEnumToggleButtonsPalette()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            return proSkin
                ? GetWEnumToggleButtonsThemePaletteEntry(
                    WEnumToggleButtonsDarkThemeColorKey,
                    DefaultDarkThemeEnumSelectedColor,
                    DefaultDarkThemeEnumSelectedTextColor,
                    DefaultDarkThemeEnumInactiveColor,
                    DefaultDarkThemeEnumInactiveTextColor
                )
                : GetWEnumToggleButtonsThemePaletteEntry(
                    WEnumToggleButtonsLightThemeColorKey,
                    DefaultLightThemeEnumSelectedColor,
                    DefaultLightThemeEnumSelectedTextColor,
                    DefaultLightThemeEnumInactiveColor,
                    DefaultLightThemeEnumInactiveTextColor
                );
        }

        private WEnumToggleButtonsPaletteEntry GetWEnumToggleButtonsThemePaletteEntry(
            string key,
            Color selectedBackground,
            Color selectedTextDefault,
            Color inactiveBackground,
            Color inactiveTextDefault
        )
        {
            EnsureWEnumToggleButtonsCustomColorDefaults();

            if (
                wenumToggleButtonsCustomColors != null
                && wenumToggleButtonsCustomColors.TryGetValue(
                    key,
                    out WEnumToggleButtonsCustomColor value
                )
                && value != null
            )
            {
                value.EnsureReadableText();
                return new WEnumToggleButtonsPaletteEntry(
                    value.SelectedBackgroundColor,
                    value.SelectedTextColor,
                    value.InactiveBackgroundColor,
                    value.InactiveTextColor
                );
            }

            Color resolvedSelectedText =
                selectedTextDefault.maxColorComponent <= 0f
                    ? WButtonColorUtility.GetReadableTextColor(selectedBackground)
                    : selectedTextDefault;
            Color resolvedInactiveText =
                inactiveTextDefault.maxColorComponent <= 0f
                    ? WButtonColorUtility.GetReadableTextColor(inactiveBackground)
                    : inactiveTextDefault;

            return new WEnumToggleButtonsPaletteEntry(
                selectedBackground,
                resolvedSelectedText,
                inactiveBackground,
                resolvedInactiveText
            );
        }

        private static bool DrawSerializableTypeIgnorePatterns(
            SerializedProperty patternsProperty,
            SerializedProperty initializationFlagProperty
        )
        {
            if (patternsProperty == null)
            {
                return false;
            }

            SerializedObject owner = patternsProperty.serializedObject;
            bool mutated = false;
            EditorGUI.BeginChangeCheck();
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
                return false;
            }

            EditorGUI.indentLevel++;

            for (int index = 0; index < patternsProperty.arraySize; index++)
            {
                SerializedProperty element = patternsProperty.GetArrayElementAtIndex(index);
                SerializedProperty patternProperty = element.FindPropertyRelative(
                    SerializedPropertyNames.SerializableTypePattern
                );

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
                    SerializedProperty patternProperty = AppendSerializableTypePatternElement(
                        patternsProperty
                    );
                    if (patternProperty != null)
                    {
                        patternProperty.stringValue = string.Empty;
                        mutated = true;
                    }

                    if (initializationFlagProperty != null)
                    {
                        initializationFlagProperty.boolValue = true;
                    }

                    ApplySerializableTypePatternChanges(owner);
                }

                if (GUILayout.Button("Reset To Defaults", GUILayout.Width(150f)))
                {
                    patternsProperty.ClearArray();
                    IReadOnlyList<string> defaults =
                        SerializableTypeCatalog.GetDefaultIgnorePatterns();
                    for (int index = 0; index < defaults.Count; index++)
                    {
                        SerializedProperty patternProperty = AppendSerializableTypePatternElement(
                            patternsProperty
                        );
                        if (patternProperty != null)
                        {
                            patternProperty.stringValue = defaults[index];
                        }
                    }

                    if (initializationFlagProperty != null)
                    {
                        initializationFlagProperty.boolValue = true;
                    }

                    mutated = true;
                    ApplySerializableTypePatternChanges(owner);
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUI.indentLevel--;
            return EditorGUI.EndChangeCheck() || mutated;
        }

        private static SerializedProperty AppendSerializableTypePatternElement(
            SerializedProperty patternsProperty
        )
        {
            if (patternsProperty == null)
            {
                return null;
            }

            int newIndex = Mathf.Max(0, patternsProperty.arraySize);
            patternsProperty.arraySize = newIndex + 1;
            SerializedProperty element = patternsProperty.GetArrayElementAtIndex(newIndex);
            if (element == null)
            {
                return null;
            }

            return element.FindPropertyRelative(SerializedPropertyNames.SerializableTypePattern);
        }

        private static void ApplySerializableTypePatternChanges(SerializedObject owner)
        {
            if (owner == null)
            {
                return;
            }

            owner.ApplyModifiedPropertiesWithoutUndo();
            owner.Update();
        }

        private static bool DrawIntSliderField(
            GUIContent content,
            int currentValue,
            int min,
            int max,
            Action<int> setter
        )
        {
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntSlider(content, currentValue, min, max);
            if (EditorGUI.EndChangeCheck())
            {
                setter(Mathf.Clamp(newValue, min, max));
                return true;
            }

            return false;
        }

        private static bool DrawIntField(GUIContent content, int currentValue, Action<int> setter)
        {
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntField(content, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                setter(newValue);
                return true;
            }

            return false;
        }

        private static bool DrawFloatField(
            GUIContent content,
            float currentValue,
            Action<float> setter
        )
        {
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.FloatField(content, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                setter(newValue);
                return true;
            }

            return false;
        }

        private static bool DrawFloatSliderField(
            GUIContent content,
            float currentValue,
            float min,
            float max,
            Action<float> setter,
            bool enabled
        )
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUILayout.Slider(content, currentValue, min, max);
                bool changed = EditorGUI.EndChangeCheck();
                if (changed && enabled)
                {
                    setter(Mathf.Clamp(newValue, min, max));
                    return true;
                }
            }

            return false;
        }

        private static bool DrawToggleField(
            GUIContent content,
            bool currentValue,
            Action<bool> setter
        )
        {
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle(content, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                setter(newValue);
                return true;
            }

            return false;
        }

        private static bool DrawEnumPopupField<TEnum>(
            GUIContent content,
            TEnum currentValue,
            Action<TEnum> setter
        )
            where TEnum : Enum
        {
            EditorGUI.BeginChangeCheck();
            TEnum newValue = (TEnum)EditorGUILayout.EnumPopup(content, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                setter(newValue);
                return true;
            }

            return false;
        }

        private static void DrawWaitInstructionBufferButtons(UnityHelpersSettings settings)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(WaitInstructionBufferApplyNowButtonContent))
                {
                    settings.ApplyWaitInstructionBufferDefaultsToAsset(applyToRuntime: true);
                }

                settings.EnsureWaitInstructionBufferDefaultsInitialized();
                bool isRuntimeInSync = settings.AreWaitInstructionDefaultsInSyncWithRuntime();
                using (new EditorGUI.DisabledScope(isRuntimeInSync))
                {
                    if (GUILayout.Button(WaitInstructionBufferCaptureCurrentButtonContent))
                    {
                        settings.CaptureWaitInstructionDefaultsFromRuntime();
                    }
                }
            }
        }

        private readonly struct LabelWidthScope : IDisposable
        {
            private readonly float _previousWidth;

            internal LabelWidthScope(float targetWidth)
            {
                _previousWidth = EditorGUIUtility.labelWidth;
                float currentViewWidth = EditorGUIUtility.currentViewWidth;
                float maxLabelWidth = Mathf.Max(0f, currentViewWidth - SettingsMinFieldWidth);
                float appliedWidth = Mathf.Clamp(targetWidth, 0f, maxLabelWidth);
                EditorGUIUtility.labelWidth = appliedWidth;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _previousWidth;
            }
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
                    settings.EnsureWaitInstructionBufferDefaultsInitialized();
                    SerializedObject serializedSettings = new(settings);
                    serializedSettings.UpdateIfRequiredOrScript();

                    bool dataChanged = false;
                    bool palettePropertyChanged = false;

                    using (
                        PooledResource<HashSet<string>> waitInstructionPropertiesLease =
                            SetBuffers<string>
                                .GetHashSetPool(StringComparer.Ordinal)
                                .Get(out HashSet<string> waitInstructionPropertiesDrawn)
                    )
                    using (new LabelWidthScope(SettingsLabelWidth))
                    {
                        SerializedProperty scriptProperty = serializedSettings.FindProperty(
                            "m_Script"
                        );
                        SerializedProperty patternsInitializedProperty =
                            serializedSettings.FindProperty(
                                nameof(serializableTypePatternsInitialized)
                            );

                        if (scriptProperty != null)
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.PropertyField(scriptProperty, true);
                            }
                            EditorGUILayout.Space();
                        }

                        bool TryDrawSettingsGroupProperty(
                            SerializedObject owner,
                            SerializedProperty property
                        )
                        {
                            if (property == null)
                            {
                                return false;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableTypeIgnorePatterns),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawSerializableTypeIgnorePatterns(
                                    property,
                                    patternsInitializedProperty
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(stringInListPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    StringInListPageSizeContent,
                                    settings.stringInListPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings.stringInListPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSetPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    SerializableSetPageSizeContent,
                                    settings.serializableSetPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings.serializableSetPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSetStartCollapsed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SerializableSetStartCollapsedContent,
                                    settings.serializableSetStartCollapsed,
                                    value => settings.serializableSetStartCollapsed = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableDictionaryPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    SerializableDictionaryPageSizeContent,
                                    settings.serializableDictionaryPageSize,
                                    MinPageSize,
                                    MaxSerializableDictionaryPageSize,
                                    value => settings.serializableDictionaryPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableDictionaryStartCollapsed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SerializableDictionaryStartCollapsedContent,
                                    settings.serializableDictionaryStartCollapsed,
                                    value => settings.serializableDictionaryStartCollapsed = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(enumToggleButtonsPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    EnumToggleButtonsPageSizeContent,
                                    settings.enumToggleButtonsPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings.enumToggleButtonsPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wbuttonPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    WButtonPageSizeContent,
                                    settings.wbuttonPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings.wbuttonPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wbuttonHistorySize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    WButtonHistorySizeContent,
                                    settings.wbuttonHistorySize,
                                    MinWButtonHistorySize,
                                    MaxWButtonHistorySize,
                                    value => settings.wbuttonHistorySize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wbuttonActionsPlacement),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    WButtonPlacementContent,
                                    settings.wbuttonActionsPlacement,
                                    value => settings.wbuttonActionsPlacement = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wbuttonFoldoutBehavior),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    WButtonFoldoutBehaviorContent,
                                    settings.wbuttonFoldoutBehavior,
                                    value => settings.wbuttonFoldoutBehavior = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(inlineEditorFoldoutBehavior),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    InlineEditorFoldoutBehaviorContent,
                                    settings.inlineEditorFoldoutBehavior,
                                    value => settings.inlineEditorFoldoutBehavior = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wbuttonFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    WButtonFoldoutTweenEnabledContent,
                                    settings.wbuttonFoldoutTweenEnabled,
                                    value => settings.wbuttonFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wbuttonFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings.wbuttonFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    WButtonFoldoutSpeedContent,
                                    settings.wbuttonFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings.wbuttonFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wbuttonCustomColors),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(
                                    property,
                                    WButtonCustomColorsContent,
                                    true
                                );
                                if (EditorGUI.EndChangeCheck())
                                {
                                    dataChanged = true;
                                    palettePropertyChanged = true;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wgroupCustomColors),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(
                                    property,
                                    WGroupCustomColorsContent,
                                    true
                                );
                                if (EditorGUI.EndChangeCheck())
                                {
                                    dataChanged = true;
                                    palettePropertyChanged = true;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wenumToggleButtonsCustomColors),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(
                                    property,
                                    WEnumToggleButtonsCustomColorsContent,
                                    true
                                );
                                if (EditorGUI.EndChangeCheck())
                                {
                                    dataChanged = true;
                                    palettePropertyChanged = true;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(waitInstructionBufferApplyOnLoad),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!waitInstructionPropertiesDrawn.Add(property.propertyPath))
                                {
                                    return true;
                                }
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    EditorGUILayout.HelpBox(
                                        WaitInstructionBufferDefaultsHelpText,
                                        MessageType.Info
                                    );
                                    bool changed = DrawToggleField(
                                        WaitInstructionBufferApplyOnLoadContent,
                                        settings.waitInstructionBufferApplyOnLoad,
                                        value => settings.waitInstructionBufferApplyOnLoad = value
                                    );
                                    dataChanged |= changed;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(waitInstructionBufferQuantizationStepSeconds),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!waitInstructionPropertiesDrawn.Add(property.propertyPath))
                                {
                                    return true;
                                }
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    bool changed = DrawFloatField(
                                        WaitInstructionBufferQuantizationContent,
                                        settings.waitInstructionBufferQuantizationStepSeconds,
                                        value =>
                                            settings.waitInstructionBufferQuantizationStepSeconds =
                                                Mathf.Max(0f, value)
                                    );
                                    dataChanged |= changed;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(waitInstructionBufferMaxDistinctEntries),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!waitInstructionPropertiesDrawn.Add(property.propertyPath))
                                {
                                    return true;
                                }
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    bool changed = DrawIntField(
                                        WaitInstructionBufferMaxEntriesContent,
                                        settings.waitInstructionBufferMaxDistinctEntries,
                                        value =>
                                            settings.waitInstructionBufferMaxDistinctEntries =
                                                Mathf.Max(0, value)
                                    );
                                    dataChanged |= changed;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(waitInstructionBufferUseLruEviction),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!waitInstructionPropertiesDrawn.Add(property.propertyPath))
                                {
                                    return true;
                                }
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    bool changed = DrawToggleField(
                                        WaitInstructionBufferUseLruContent,
                                        settings.waitInstructionBufferUseLruEviction,
                                        value =>
                                            settings.waitInstructionBufferUseLruEviction = value
                                    );
                                    dataChanged |= changed;
                                    DrawWaitInstructionBufferButtons(settings);
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableDictionaryFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    DictionaryFoldoutTweenEnabledContent,
                                    settings.serializableDictionaryFoldoutTweenEnabled,
                                    value =>
                                        settings.serializableDictionaryFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableDictionaryFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings.serializableDictionaryFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    DictionaryFoldoutSpeedContent,
                                    settings.serializableDictionaryFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings.serializableDictionaryFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSortedDictionaryFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SortedDictionaryFoldoutTweenEnabledContent,
                                    settings.serializableSortedDictionaryFoldoutTweenEnabled,
                                    value =>
                                        settings.serializableSortedDictionaryFoldoutTweenEnabled =
                                            value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSortedDictionaryFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings.serializableSortedDictionaryFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    SortedDictionaryFoldoutSpeedContent,
                                    settings.serializableSortedDictionaryFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value =>
                                        settings.serializableSortedDictionaryFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSetFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SetFoldoutTweenEnabledContent,
                                    settings.serializableSetFoldoutTweenEnabled,
                                    value => settings.serializableSetFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSetFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings.serializableSetFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    SetFoldoutSpeedContent,
                                    settings.serializableSetFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings.serializableSetFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSortedSetFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SortedSetFoldoutTweenEnabledContent,
                                    settings.serializableSortedSetFoldoutTweenEnabled,
                                    value =>
                                        settings.serializableSortedSetFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSortedSetFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings.serializableSortedSetFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    SortedSetFoldoutSpeedContent,
                                    settings.serializableSortedSetFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings.serializableSortedSetFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(duplicateRowAnimationMode),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    DuplicateAnimationModeContent,
                                    settings.duplicateRowAnimationMode,
                                    value => settings.duplicateRowAnimationMode = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(duplicateRowTweenCycles),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (
                                    settings.duplicateRowAnimationMode
                                    != DuplicateRowAnimationMode.Tween
                                )
                                {
                                    return true;
                                }

                                bool changed = DrawIntField(
                                    DuplicateTweenCyclesContent,
                                    settings.duplicateRowTweenCycles,
                                    value => settings.duplicateRowTweenCycles = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSetDuplicateTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SerializableSetDuplicateTweenEnabledContent,
                                    settings.serializableSetDuplicateTweenEnabled,
                                    value => settings.serializableSetDuplicateTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(serializableSetDuplicateTweenCycles),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings.serializableSetDuplicateTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawIntField(
                                    SerializableSetDuplicateTweenCyclesContent,
                                    settings.serializableSetDuplicateTweenCycles,
                                    value => settings.serializableSetDuplicateTweenCycles = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wgroupAutoIncludeMode),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    WGroupAutoIncludeModeContent,
                                    settings.wgroupAutoIncludeMode,
                                    value => settings.wgroupAutoIncludeMode = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(wgroupAutoIncludeRowCount),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (settings.wgroupAutoIncludeMode != WGroupAutoIncludeMode.Finite)
                                {
                                    return true;
                                }

                                bool changed = DrawIntSliderField(
                                    WGroupAutoIncludeCountContent,
                                    settings.wgroupAutoIncludeRowCount,
                                    MinWGroupAutoIncludeRowCount,
                                    MaxWGroupAutoIncludeRowCount,
                                    value => settings.wgroupAutoIncludeRowCount = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            return false;
                        }

                        EditorGUI.BeginChangeCheck();

                        string scriptPropertyPath = scriptProperty?.propertyPath;
                        WGroupLayout layout = WGroupLayoutBuilder.Build(
                            serializedSettings,
                            scriptPropertyPath
                        );
                        IReadOnlyList<WGroupDrawOperation> operations = layout.Operations;

                        for (int index = 0; index < operations.Count; index++)
                        {
                            WGroupDrawOperation operation = operations[index];
                            if (operation.Type == WGroupDrawOperationType.Group)
                            {
                                WGroupDefinition definition = operation.Group;
                                if (definition == null)
                                {
                                    continue;
                                }

                                WGroupGUI.DrawGroup(
                                    definition,
                                    serializedSettings,
                                    SettingsGroupFoldoutStates,
                                    TryDrawSettingsGroupProperty
                                );
                                continue;
                            }
                            SerializedProperty property = serializedSettings.FindProperty(
                                operation.PropertyPath
                            );
                            if (property == null)
                            {
                                continue;
                            }

                            string propertyPath = property.propertyPath;
                            if (
                                string.Equals(
                                    propertyPath,
                                    nameof(foldoutTweenSettingsInitialized),
                                    StringComparison.Ordinal
                                )
                                || string.Equals(
                                    propertyPath,
                                    nameof(serializableTypePatternsInitialized),
                                    StringComparison.Ordinal
                                )
                                || string.Equals(
                                    propertyPath,
                                    nameof(legacyWButtonPriorityColors),
                                    StringComparison.Ordinal
                                )
                                || string.Equals(
                                    propertyPath,
                                    nameof(serializableTypeIgnorePatterns),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                continue;
                            }

                            EditorGUILayout.PropertyField(property, true);
                        }
                    }

                    bool guiChanged = EditorGUI.EndChangeCheck();
                    bool applied = serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                    PaletteSerializationDiagnostics.ReportInspectorApplyResult(
                        serializedSettings,
                        palettePropertyChanged,
                        dataChanged,
                        guiChanged,
                        applied
                    );
                    if (!dataChanged && !guiChanged && !applied)
                    {
                        return;
                    }

                    settings.SaveSettings();
                    settings.ApplyWaitInstructionBufferDefaultsToAsset(
                        settings.waitInstructionBufferApplyOnLoad
                    );
                    serializedSettings.UpdateIfRequiredOrScript();
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

        private void EnsureWaitInstructionBufferDefaultsInitialized()
        {
            if (waitInstructionBufferDefaultsInitialized)
            {
                return;
            }

            UnityHelpersBufferSettingsAsset asset = EnsureWaitInstructionBufferSettingsAsset();
            if (asset == null)
            {
                return;
            }

            waitInstructionBufferApplyOnLoad = asset.ApplyOnLoad;
            waitInstructionBufferQuantizationStepSeconds = asset.QuantizationStepSeconds;
            waitInstructionBufferMaxDistinctEntries = asset.MaxDistinctEntries;
            waitInstructionBufferUseLruEviction = asset.UseLruEviction;
            waitInstructionBufferDefaultsInitialized = true;
        }

        private void CaptureWaitInstructionDefaultsFromRuntime()
        {
            waitInstructionBufferQuantizationStepSeconds = Mathf.Max(
                0f,
                Buffers.WaitInstructionQuantizationStepSeconds
            );
            waitInstructionBufferMaxDistinctEntries = Mathf.Max(
                0,
                Buffers.WaitInstructionMaxDistinctEntries
            );
            waitInstructionBufferUseLruEviction = Buffers.WaitInstructionUseLruEviction;
            waitInstructionBufferDefaultsInitialized = true;
            ApplyWaitInstructionBufferDefaultsToAsset(waitInstructionBufferApplyOnLoad);
        }

        private bool AreWaitInstructionDefaultsInSyncWithRuntime()
        {
            float runtimeQuantization = Mathf.Max(
                0f,
                Buffers.WaitInstructionQuantizationStepSeconds
            );
            float configuredQuantization = Mathf.Max(
                0f,
                waitInstructionBufferQuantizationStepSeconds
            );

            int runtimeMaxEntries = Mathf.Max(0, Buffers.WaitInstructionMaxDistinctEntries);
            int configuredMaxEntries = Mathf.Max(0, waitInstructionBufferMaxDistinctEntries);

            bool runtimeUseLru = Buffers.WaitInstructionUseLruEviction;

            return Mathf.Approximately(configuredQuantization, runtimeQuantization)
                && configuredMaxEntries == runtimeMaxEntries
                && waitInstructionBufferUseLruEviction == runtimeUseLru;
        }

        private void ApplyWaitInstructionBufferDefaultsToAsset(bool applyToRuntime)
        {
            UnityHelpersBufferSettingsAsset asset = EnsureWaitInstructionBufferSettingsAsset();
            if (asset == null)
            {
                return;
            }

            waitInstructionBufferQuantizationStepSeconds = Mathf.Max(
                0f,
                waitInstructionBufferQuantizationStepSeconds
            );
            waitInstructionBufferMaxDistinctEntries = Mathf.Max(
                0,
                waitInstructionBufferMaxDistinctEntries
            );

            SerializedObject assetSerialized = new(asset);
            SerializedProperty applyOnLoadProperty = assetSerialized.FindProperty(
                UnityHelpersBufferSettingsAsset.ApplyOnLoadPropertyName
            );
            SerializedProperty quantizationProperty = assetSerialized.FindProperty(
                UnityHelpersBufferSettingsAsset.QuantizationStepSecondsPropertyName
            );
            SerializedProperty maxEntriesProperty = assetSerialized.FindProperty(
                UnityHelpersBufferSettingsAsset.MaxDistinctEntriesPropertyName
            );
            SerializedProperty useLruProperty = assetSerialized.FindProperty(
                UnityHelpersBufferSettingsAsset.UseLruEvictionPropertyName
            );

            if (applyOnLoadProperty != null)
            {
                applyOnLoadProperty.boolValue = waitInstructionBufferApplyOnLoad;
            }

            if (quantizationProperty != null)
            {
                quantizationProperty.floatValue = waitInstructionBufferQuantizationStepSeconds;
            }

            if (maxEntriesProperty != null)
            {
                maxEntriesProperty.intValue = waitInstructionBufferMaxDistinctEntries;
            }

            if (useLruProperty != null)
            {
                useLruProperty.boolValue = waitInstructionBufferUseLruEviction;
            }

            bool assetChanged = assetSerialized.ApplyModifiedPropertiesWithoutUndo();
            if (assetChanged)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }

            if (applyToRuntime)
            {
                asset.ApplyToBuffers();
            }
        }

        private static bool ColorsApproximatelyEqual(Color left, Color right)
        {
            const float tolerance = 0.01f;
            return Mathf.Abs(left.r - right.r) <= tolerance
                && Mathf.Abs(left.g - right.g) <= tolerance
                && Mathf.Abs(left.b - right.b) <= tolerance
                && Mathf.Abs(left.a - right.a) <= tolerance;
        }

        private static UnityHelpersBufferSettingsAsset EnsureWaitInstructionBufferSettingsAsset()
        {
            if (waitInstructionBufferSettingsAsset != null)
            {
                return waitInstructionBufferSettingsAsset;
            }

            waitInstructionBufferSettingsAsset =
                AssetDatabase.LoadAssetAtPath<UnityHelpersBufferSettingsAsset>(
                    UnityHelpersBufferSettingsAsset.AssetPath
                );
            if (waitInstructionBufferSettingsAsset != null)
            {
                return waitInstructionBufferSettingsAsset;
            }

            string directory = Path.GetDirectoryName(UnityHelpersBufferSettingsAsset.AssetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            UnityHelpersBufferSettingsAsset created =
                ScriptableObject.CreateInstance<UnityHelpersBufferSettingsAsset>();
            created.SyncFromRuntime();
            AssetDatabase.CreateAsset(created, UnityHelpersBufferSettingsAsset.AssetPath);
            AssetDatabase.SaveAssets();
            waitInstructionBufferSettingsAsset = created;
            return waitInstructionBufferSettingsAsset;
        }
    }
#endif
}
