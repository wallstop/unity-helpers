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
        internal static event Action OnSettingsSaved;
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
        public const float DefaultDetectAssetChangeLoopWindowSeconds = 15f;
        public const float MinDetectAssetChangeLoopWindowSeconds = 1f;
        public const float MaxDetectAssetChangeLoopWindowSeconds = 120f;
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
        private static readonly Color DefaultCancelButtonColor = new(0.85f, 0.2f, 0.2f, 1f);
        private static readonly Color DefaultCancelButtonTextColor = Color.white;
        private static readonly Color DefaultClearHistoryButtonColor = new(0.75f, 0.45f, 0.45f, 1f);
        private static readonly Color DefaultClearHistoryButtonTextColor = Color.white;
        private static readonly Dictionary<int, bool> SettingsGroupFoldoutStates = new();
        private const float SettingsLabelWidth = 260f;
        private const float SettingsMinFieldWidth = 110f;
        private const float CustomColorDrawerMinColorFieldWidth = 42f;
        private const float CustomColorDrawerLabelWidthRatio = 0.38f;
        private const float CustomColorDrawerMinLabelWidth = 28f;
        private const float CustomColorDrawerMaxLabelWidth = 90f;
        private const string WaitInstructionBufferFoldoutKey = "Buffers";
        private static UnityHelpersBufferSettingsAsset _waitInstructionBufferSettingsAsset;
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
        private static readonly GUIContent WButtonCancelButtonColorContent =
            EditorGUIUtility.TrTextContent(
                "Cancel Button Color",
                "Background color for the Cancel button that appears during async WButton execution."
            );
        private static readonly GUIContent WButtonCancelButtonTextColorContent =
            EditorGUIUtility.TrTextContent(
                "Cancel Button Text Color",
                "Text color for the Cancel button."
            );
        private static readonly GUIContent WButtonClearHistoryButtonColorContent =
            EditorGUIUtility.TrTextContent(
                "Clear History Button Color",
                "Background color for the Clear History button in WButton result history."
            );
        private static readonly GUIContent WButtonClearHistoryButtonTextColorContent =
            EditorGUIUtility.TrTextContent(
                "Clear History Button Text Color",
                "Text color for the Clear History button."
            );
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
        private static readonly GUIContent DetectAssetChangeLoopWindowContent =
            EditorGUIUtility.TrTextContent(
                "Detect Asset Change Loop Window (seconds)",
                "Time window used to detect repeated DetectAssetChanged callbacks before loop suppression disables them."
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
        private static readonly GUIContent WGroupStartCollapsedContent =
            EditorGUIUtility.TrTextContent(
                "Start WGroups Collapsed",
                "Default foldout state used when collapsible WGroups do not specify startCollapsed explicitly."
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

        [FormerlySerializedAs("waitInstructionBufferApplyOnLoad")]
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
        private bool _waitInstructionBufferApplyOnLoad = true;

        [FormerlySerializedAs("waitInstructionBufferQuantizationStepSeconds")]
        [SerializeField]
        [Tooltip(
            "Rounds requested WaitForSeconds durations to this step size before caching (set to 0 to disable)."
        )]
        [Min(0f)]
        private float _waitInstructionBufferQuantizationStepSeconds;

        [FormerlySerializedAs("waitInstructionBufferMaxDistinctEntries")]
        [SerializeField]
        [Tooltip(
            "Maximum number of distinct WaitForSeconds/Realtime entries cached (0 = unlimited)."
        )]
        [Min(0)]
        private int _waitInstructionBufferMaxDistinctEntries =
            Buffers.WaitInstructionDefaultMaxDistinctEntries;

        [FormerlySerializedAs("waitInstructionBufferUseLruEviction")]
        [SerializeField]
        [Tooltip(
            "Evict the least recently used duration when the cache hits the distinct entry limit."
        )]
        private bool _waitInstructionBufferUseLruEviction;

        [FormerlySerializedAs("waitInstructionBufferDefaultsInitialized")]
        [SerializeField]
        [HideInInspector]
        private bool _waitInstructionBufferDefaultsInitialized;

        [FormerlySerializedAs("stringInListPageSize")]
        [SerializeField]
        [Tooltip("Maximum number of entries shown per page for StringInList dropdowns.")]
        [Range(MinPageSize, MaxPageSize)]
        [WGroup(
            "Pagination",
            displayName: "Pagination Defaults",
            autoIncludeCount: 4,
            collapsible: true
        )]
        private int _stringInListPageSize = DefaultStringInListPageSize;

        [FormerlySerializedAs("serializableSetPageSize")]
        [SerializeField]
        [Tooltip(
            "Maximum number of entries shown per page when drawing SerializableHashSet/SerializableSortedSet inspectors."
        )]
        [Range(MinPageSize, MaxPageSize)]
        private int _serializableSetPageSize = DefaultSerializableSetPageSize;

        [FormerlySerializedAs("serializableSetStartCollapsed")]
        [SerializeField]
        [Tooltip(
            "Whether SerializableHashSet and SerializableSortedSet inspectors start collapsed when first rendered."
        )]
        private bool _serializableSetStartCollapsed = true;

        [FormerlySerializedAs("serializableDictionaryPageSize")]
        [SerializeField]
        [Tooltip(
            "Maximum number of entries shown per page when drawing SerializableDictionary/SerializableSortedDictionary inspectors."
        )]
        [Range(MinPageSize, MaxSerializableDictionaryPageSize)]
        private int _serializableDictionaryPageSize = DefaultSerializableDictionaryPageSize;

        [FormerlySerializedAs("serializableDictionaryStartCollapsed")]
        [SerializeField]
        [Tooltip(
            "Whether SerializableDictionary and SerializableSortedDictionary inspectors start collapsed when first rendered."
        )]
        private bool _serializableDictionaryStartCollapsed = true;

        [FormerlySerializedAs("enumToggleButtonsPageSize")]
        [SerializeField]
        [Tooltip(
            "Maximum number of toggle buttons shown per page when drawing WEnumToggleButtons groups."
        )]
        [Range(MinPageSize, MaxPageSize)]
        private int _enumToggleButtonsPageSize = DefaultEnumToggleButtonsPageSize;

        [FormerlySerializedAs("wbuttonPageSize")]
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
        private int _wbuttonPageSize = DefaultWButtonPageSize;

        [FormerlySerializedAs("wbuttonHistorySize")]
        [SerializeField]
        [Tooltip("Number of recent invocation results retained per WButton method.")]
        [Range(MinWButtonHistorySize, MaxWButtonHistorySize)]
        private int _wbuttonHistorySize = DefaultWButtonHistorySize;

        [FormerlySerializedAs("wbuttonActionsPlacement")]
        [SerializeField]
        [Tooltip("Controls where WButton actions are rendered relative to the inspector content.")]
        [WGroup(
            "WButton Layout",
            displayName: "WButton Layout",
            autoIncludeCount: 3,
            collapsible: true
        )]
        private WButtonActionsPlacement _wbuttonActionsPlacement = WButtonActionsPlacement.Top;

        [FormerlySerializedAs("wbuttonFoldoutBehavior")]
        [SerializeField]
        [Tooltip(
            "Determines whether WButton groups are always shown or foldouts start expanded/collapsed."
        )]
        private WButtonFoldoutBehavior _wbuttonFoldoutBehavior =
            WButtonFoldoutBehavior.StartExpanded;

        [FormerlySerializedAs("wbuttonFoldoutTweenEnabled")]
        [SerializeField]
        [Tooltip("Animate WButton action foldouts when toggled.")]
        private bool _wbuttonFoldoutTweenEnabled = true;

        [FormerlySerializedAs("wbuttonFoldoutSpeed")]
        [SerializeField]
        [Tooltip("Animation speed used when toggling WButton action foldouts.")]
        [WShowIf(nameof(_wbuttonFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float _wbuttonFoldoutSpeed = DefaultFoldoutSpeed;

        [FormerlySerializedAs("wbuttonCancelButtonColor")]
        [SerializeField]
        [Tooltip(
            "Background color for the Cancel button that appears during async WButton execution."
        )]
        [WGroupEnd("WButton Layout")]
        [WGroup(
            "WButton Colors",
            displayName: "WButton Colors",
            autoIncludeCount: 3,
            collapsible: true
        )]
        private Color _wbuttonCancelButtonColor = DefaultCancelButtonColor;

        [FormerlySerializedAs("wbuttonCancelButtonTextColor")]
        [SerializeField]
        [Tooltip("Text color for the Cancel button.")]
        private Color _wbuttonCancelButtonTextColor = DefaultCancelButtonTextColor;

        [FormerlySerializedAs("wbuttonClearHistoryButtonColor")]
        [SerializeField]
        [Tooltip("Background color for the Clear History button in WButton result history.")]
        private Color _wbuttonClearHistoryButtonColor = DefaultClearHistoryButtonColor;

        [FormerlySerializedAs("wbuttonClearHistoryButtonTextColor")]
        [SerializeField]
        [Tooltip("Text color for the Clear History button.")]
        private Color _wbuttonClearHistoryButtonTextColor = DefaultClearHistoryButtonTextColor;

        [FormerlySerializedAs("serializableDictionaryFoldoutTweenEnabled")]
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
        private bool _serializableDictionaryFoldoutTweenEnabled = true;

        [FormerlySerializedAs("foldoutTweenSettingsInitialized")]
        [SerializeField]
        [HideInInspector]
        private bool _foldoutTweenSettingsInitialized;

        [FormerlySerializedAs("serializableDictionaryFoldoutSpeed")]
        [SerializeField]
        [Tooltip(
            "Animation speed used when toggling SerializableDictionary pending entry foldouts."
        )]
        [WShowIf(nameof(_serializableDictionaryFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float _serializableDictionaryFoldoutSpeed = DefaultFoldoutSpeed;

        [FormerlySerializedAs("serializableSortedDictionaryFoldoutTweenEnabled")]
        [SerializeField]
        [Tooltip(
            "Animation speed used when toggling SerializableSortedDictionary pending entry foldouts."
        )]
        private bool _serializableSortedDictionaryFoldoutTweenEnabled = true;

        [FormerlySerializedAs("serializableSortedDictionaryFoldoutSpeed")]
        [SerializeField]
        [Tooltip(
            "Animation speed used when toggling SerializableSortedDictionary pending entry foldouts."
        )]
        [WShowIf(nameof(_serializableSortedDictionaryFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float _serializableSortedDictionaryFoldoutSpeed = DefaultFoldoutSpeed;

        [FormerlySerializedAs("serializableSetFoldoutTweenEnabled")]
        [SerializeField]
        [Tooltip(
            "Enable animated transitions when expanding or collapsing SerializableHashSet manual entry foldouts."
        )]
        [WGroup("Serializable Sets", displayName: "Serializable Sets", collapsible: true)]
        private bool _serializableSetFoldoutTweenEnabled = true;

        [FormerlySerializedAs("serializableSetFoldoutSpeed")]
        [SerializeField]
        [Tooltip("Animation speed used when toggling SerializableHashSet manual entry foldouts.")]
        [WShowIf(nameof(_serializableSetFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        private float _serializableSetFoldoutSpeed = DefaultFoldoutSpeed;

        [FormerlySerializedAs("serializableSetDuplicateTweenEnabled")]
        [SerializeField]
        [Tooltip(
            "Enable lateral shake animations when highlighting duplicate or invalid entries in SerializableSet inspectors."
        )]
        private bool _serializableSetDuplicateTweenEnabled = true;

        [FormerlySerializedAs("serializableSetDuplicateTweenCycles")]
        [SerializeField]
        [Tooltip(
            "When enabled, number of shake cycles to play for SerializableSet duplicate entries. Negative values loop indefinitely."
        )]
        [WShowIf(nameof(_serializableSetDuplicateTweenEnabled))]
        private int _serializableSetDuplicateTweenCycles = DefaultDuplicateTweenCycles;

        [FormerlySerializedAs("serializableSetTweensGroupEndSentinel")]
        [SerializeField]
        [HideInInspector]
#pragma warning disable CS0169 // Field is never used
        private bool _serializableSetTweensGroupEndSentinel;
#pragma warning restore CS0169 // Field is never used

        [FormerlySerializedAs("serializableSetDuplicateTweenSettingsInitialized")]
        [SerializeField]
        [HideInInspector]
        private bool _serializableSetDuplicateTweenSettingsInitialized;

        [FormerlySerializedAs("serializableSortedSetFoldoutTweenEnabled")]
        [SerializeField]
        [Tooltip(
            "Enable animated transitions when expanding or collapsing SerializableSortedSet manual entry foldouts."
        )]
        [WGroup("Sorted Set", displayName: "Sorted Sets", autoIncludeCount: 1, collapsible: true)]
        private bool _serializableSortedSetFoldoutTweenEnabled = true;

        [FormerlySerializedAs("serializableSortedSetFoldoutSpeed")]
        [SerializeField]
        [Tooltip("Animation speed used when toggling SerializableSortedSet manual entry foldouts.")]
        [WShowIf(nameof(_serializableSortedSetFoldoutTweenEnabled))]
        [Range(MinFoldoutSpeed, MaxFoldoutSpeed)]
        [WGroupEnd("Sorted Set Foldouts")]
        private float _serializableSortedSetFoldoutSpeed = DefaultFoldoutSpeed;

        [FormerlySerializedAs("duplicateRowAnimationMode")]
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
        private DuplicateRowAnimationMode _duplicateRowAnimationMode =
            DuplicateRowAnimationMode.Tween;

        [FormerlySerializedAs("duplicateRowTweenCycles")]
        [SerializeField]
        [Tooltip(
            "When using Tween, number of shake cycles to play for duplicate entries. Negative values loop indefinitely."
        )]
        [WShowIf(
            nameof(_duplicateRowAnimationMode),
            expectedValues: new object[] { DuplicateRowAnimationMode.Tween }
        )]
        private int _duplicateRowTweenCycles = DefaultDuplicateTweenCycles;

        [FormerlySerializedAs("detectAssetChangeLoopWindowSeconds")]
        [SerializeField]
        [Tooltip(
            "Time window used to detect repeated DetectAssetChanged callbacks before loop suppression engages."
        )]
        [Min(MinDetectAssetChangeLoopWindowSeconds)]
        [WGroup(
            "Detect Asset Changes",
            displayName: "Detect Asset Changes",
            autoIncludeCount: 1,
            collapsible: true
        )]
        private float _detectAssetChangeLoopWindowSeconds =
            DefaultDetectAssetChangeLoopWindowSeconds;

        [FormerlySerializedAs("serializableTypeIgnorePatterns")]
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
        private List<SerializableTypeIgnorePattern> _serializableTypeIgnorePatterns;
        private string[] _serializableTypeIgnorePatternCache = Array.Empty<string>();
        private int _serializableTypeIgnorePatternCacheVersion = int.MinValue;

        [FormerlySerializedAs("wgroupAutoIncludeMode")]
        [SerializeField]
        [Tooltip(
            "Controls how WGroup automatically includes additional serialized members after a group declaration."
        )]
        [WGroup(
            "WGroup Defaults",
            displayName: "WGroup Defaults",
            autoIncludeCount: 2,
            collapsible: true
        )]
        private WGroupAutoIncludeMode _wgroupAutoIncludeMode = WGroupAutoIncludeMode.Infinite;

        [FormerlySerializedAs("wgroupAutoIncludeRowCount")]
        [SerializeField]
        [Tooltip(
            "Number of additional serialized members captured when the WGroup auto include mode is set to Finite."
        )]
        [WShowIf(
            nameof(_wgroupAutoIncludeMode),
            expectedValues: new object[] { WGroupAutoIncludeMode.Finite }
        )]
        [Range(MinWGroupAutoIncludeRowCount, MaxWGroupAutoIncludeRowCount)]
        private int _wgroupAutoIncludeRowCount = DefaultWGroupAutoIncludeRowCount;

        [FormerlySerializedAs("wgroupFoldoutsStartCollapsed")]
        [SerializeField]
        [Tooltip(
            "When enabled, collapsible WGroup headers start closed unless the attribute overrides startCollapsed."
        )]
        private bool _wgroupFoldoutsStartCollapsed = true;

        [FormerlySerializedAs("wbuttonCustomColors")]
        [SerializeField]
        [Tooltip("Named color palette applied to WButton custom color keys.")]
        [WGroup(
            "Color Palettes",
            displayName: "Color Palettes",
            autoIncludeCount: 4,
            collapsible: true
        )]
        private WButtonCustomColorDictionary _wbuttonCustomColors = new();

        [FormerlySerializedAs("wgroupCustomColors")]
        [SerializeField]
        [Tooltip("Named color palette applied to WGroup custom color keys.")]
        private WGroupCustomColorDictionary _wgroupCustomColors = new();

        [FormerlySerializedAs("wenumToggleButtonsCustomColors")]
        [SerializeField]
        [Tooltip("Named color palette applied to WEnumToggleButtons color keys.")]
        [WGroupEnd("Color Palettes")]
        private WEnumToggleButtonsCustomColorDictionary _wenumToggleButtonsCustomColors = new();

        [FormerlySerializedAs("legacyWButtonPriorityColors")]
        [SerializeField]
        [FormerlySerializedAs("wbuttonPriorityColors")]
#pragma warning disable CS0618 // Type or member is obsolete
        [HideInInspector]
        private List<WButtonPriorityColor> _legacyWButtonPriorityColors;
#pragma warning restore CS0618 // Type or member is obsolete

        [FormerlySerializedAs("serializableTypePatternsInitialized")]
        [SerializeField]
        [HideInInspector]
        private bool _serializableTypePatternsInitialized;

        [NonSerialized]
        private HashSet<string> _wbuttonCustomColorSkipAutoSuggest;

        [NonSerialized]
        private HashSet<string> _wgroupCustomColorSkipAutoSuggest;

        [FormerlySerializedAs("inlineEditorFoldoutBehavior")]
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
        private InlineEditorFoldoutBehavior _inlineEditorFoldoutBehavior =
            InlineEditorFoldoutBehavior.StartCollapsed;

        internal HashSet<string> WButtonCustomColorSkipAutoSuggest
        {
            get => _wbuttonCustomColorSkipAutoSuggest;
            set => _wbuttonCustomColorSkipAutoSuggest = value;
        }

        internal HashSet<string> WGroupCustomColorSkipAutoSuggest
        {
            get => _wgroupCustomColorSkipAutoSuggest;
            set => _wgroupCustomColorSkipAutoSuggest = value;
        }

        [Serializable]
        internal sealed class SerializableTypeIgnorePattern
        {
            [FormerlySerializedAs("pattern")]
            [SerializeField]
            internal string _pattern = string.Empty;

            public SerializableTypeIgnorePattern() { }

            public SerializableTypeIgnorePattern(string pattern)
            {
                Pattern = pattern;
            }

            public string Pattern
            {
                get => _pattern ?? string.Empty;
                set => _pattern = value ?? string.Empty;
            }
        }

        [Serializable]
        internal sealed class WButtonCustomColor
        {
            [FormerlySerializedAs("buttonColor")]
            [SerializeField]
            internal Color _buttonColor = Color.white;

            [FormerlySerializedAs("textColor")]
            [SerializeField]
            internal Color _textColor = Color.black;

            public Color ButtonColor
            {
                get => _buttonColor;
                set => _buttonColor = value;
            }

            public Color TextColor
            {
                get => _textColor;
                set => _textColor = value;
            }

            public void EnsureReadableText()
            {
                if (_textColor.maxColorComponent <= 0f)
                {
                    _textColor = WButtonColorUtility.GetReadableTextColor(_buttonColor);
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
            private static readonly GUIContent ButtonLabelContent = EditorGUIUtility.TrTextContent(
                "Button"
            );
            private static readonly GUIContent TextLabelContent = EditorGUIUtility.TrTextContent(
                "Text"
            );

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty buttonColor = property.FindPropertyRelative(
                    SerializedPropertyNames.WButtonCustomColorButton
                );
                SerializedProperty textColor = property.FindPropertyRelative(
                    SerializedPropertyNames.WButtonCustomColorText
                );

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float availableWidth = Mathf.Max(0f, position.width - spacing);
                float halfWidth = availableWidth * 0.5f;

                float labelWidth = Mathf.Clamp(
                    halfWidth * CustomColorDrawerLabelWidthRatio,
                    CustomColorDrawerMinLabelWidth,
                    CustomColorDrawerMaxLabelWidth
                );

                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth;

                try
                {
                    Rect buttonRect = new(position.x, position.y, halfWidth, position.height);
                    Rect textRect = new(
                        position.x + halfWidth + spacing,
                        position.y,
                        halfWidth,
                        position.height
                    );

                    float minFieldWidth = CustomColorDrawerMinColorFieldWidth + labelWidth;
                    bool useLabels = halfWidth >= minFieldWidth;

                    EditorGUI.PropertyField(
                        buttonRect,
                        buttonColor,
                        useLabels ? ButtonLabelContent : GUIContent.none
                    );
                    EditorGUI.PropertyField(
                        textRect,
                        textColor,
                        useLabels ? TextLabelContent : GUIContent.none
                    );
                }
                finally
                {
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            }
        }

        [CustomPropertyDrawer(typeof(WEnumToggleButtonsCustomColor))]
        private sealed class WEnumToggleButtonsCustomColorDrawer : PropertyDrawer
        {
            private static readonly GUIContent SelectedBackgroundLabelContent =
                EditorGUIUtility.TrTextContent("Selected BG");
            private static readonly GUIContent SelectedTextLabelContent =
                EditorGUIUtility.TrTextContent("Selected Text");
            private static readonly GUIContent InactiveBackgroundLabelContent =
                EditorGUIUtility.TrTextContent("Inactive BG");
            private static readonly GUIContent InactiveTextLabelContent =
                EditorGUIUtility.TrTextContent("Inactive Text");

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                return lineHeight * 2f + spacing;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty selectedBackground = property.FindPropertyRelative(
                    SerializedPropertyNames.WEnumToggleButtonsSelectedBackground
                );
                SerializedProperty selectedText = property.FindPropertyRelative(
                    SerializedPropertyNames.WEnumToggleButtonsSelectedText
                );
                SerializedProperty inactiveBackground = property.FindPropertyRelative(
                    SerializedPropertyNames.WEnumToggleButtonsInactiveBackground
                );
                SerializedProperty inactiveText = property.FindPropertyRelative(
                    SerializedPropertyNames.WEnumToggleButtonsInactiveText
                );

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float availableWidth = Mathf.Max(0f, position.width - spacing);
                float halfWidth = availableWidth * 0.5f;
                float lineHeight = EditorGUIUtility.singleLineHeight;

                float labelWidth = Mathf.Clamp(
                    halfWidth * CustomColorDrawerLabelWidthRatio,
                    CustomColorDrawerMinLabelWidth,
                    CustomColorDrawerMaxLabelWidth
                );

                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth;

                try
                {
                    Rect selectedBackgroundRect = new(
                        position.x,
                        position.y,
                        halfWidth,
                        lineHeight
                    );
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

                    float minFieldWidth = CustomColorDrawerMinColorFieldWidth + labelWidth;
                    bool useLabels = halfWidth >= minFieldWidth;

                    EditorGUI.PropertyField(
                        selectedBackgroundRect,
                        selectedBackground,
                        useLabels ? SelectedBackgroundLabelContent : GUIContent.none
                    );
                    EditorGUI.PropertyField(
                        selectedTextRect,
                        selectedText,
                        useLabels ? SelectedTextLabelContent : GUIContent.none
                    );
                    EditorGUI.PropertyField(
                        inactiveBackgroundRect,
                        inactiveBackground,
                        useLabels ? InactiveBackgroundLabelContent : GUIContent.none
                    );
                    EditorGUI.PropertyField(
                        inactiveTextRect,
                        inactiveText,
                        useLabels ? InactiveTextLabelContent : GUIContent.none
                    );
                }
                finally
                {
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            }
        }
#endif

        [Serializable]
        internal sealed class WGroupCustomColor
        {
            [FormerlySerializedAs("backgroundColor")]
            [SerializeField]
            internal Color _backgroundColor = DefaultColorKeyButtonColor;

            [FormerlySerializedAs("textColor")]
            [SerializeField]
            internal Color _textColor = Color.white;

            public Color BackgroundColor
            {
                get => _backgroundColor;
                set => _backgroundColor = value;
            }

            public Color TextColor
            {
                get => _textColor;
                set => _textColor = value;
            }

            public void EnsureReadableText()
            {
                if (_textColor.maxColorComponent <= 0f)
                {
                    _textColor = WButtonColorUtility.GetReadableTextColor(_backgroundColor);
                }
            }
        }

        [Serializable]
        private sealed class WGroupCustomColorDictionary
            : SerializableDictionary<string, WGroupCustomColor> { }

        [Serializable]
        private sealed class WEnumToggleButtonsCustomColor
        {
            [FormerlySerializedAs("selectedBackgroundColor")]
            [SerializeField]
            internal Color _selectedBackgroundColor = DefaultColorKeyButtonColor;

            [FormerlySerializedAs("selectedTextColor")]
            [SerializeField]
            internal Color _selectedTextColor = Color.white;

            [FormerlySerializedAs("inactiveBackgroundColor")]
            [SerializeField]
            internal Color _inactiveBackgroundColor = DefaultLightThemeButtonColor;

            [FormerlySerializedAs("inactiveTextColor")]
            [SerializeField]
            internal Color _inactiveTextColor = Color.black;

            public Color SelectedBackgroundColor
            {
                get => _selectedBackgroundColor;
                set => _selectedBackgroundColor = value;
            }

            public Color SelectedTextColor
            {
                get => _selectedTextColor;
                set => _selectedTextColor = value;
            }

            public Color InactiveBackgroundColor
            {
                get => _inactiveBackgroundColor;
                set => _inactiveBackgroundColor = value;
            }

            public Color InactiveTextColor
            {
                get => _inactiveTextColor;
                set => _inactiveTextColor = value;
            }

            public void EnsureReadableText()
            {
                if (_selectedTextColor.maxColorComponent <= 0f)
                {
                    _selectedTextColor = WButtonColorUtility.GetReadableTextColor(
                        _selectedBackgroundColor
                    );
                }

                if (_inactiveTextColor.maxColorComponent <= 0f)
                {
                    _inactiveTextColor = WButtonColorUtility.GetReadableTextColor(
                        _inactiveBackgroundColor
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
            private static readonly GUIContent BackgroundLabelContent =
                EditorGUIUtility.TrTextContent("Background");
            private static readonly GUIContent TextLabelContent = EditorGUIUtility.TrTextContent(
                "Text"
            );

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty background = property.FindPropertyRelative(
                    SerializedPropertyNames.WGroupCustomColorBackground
                );
                SerializedProperty text = property.FindPropertyRelative(
                    SerializedPropertyNames.WGroupCustomColorText
                );

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float availableWidth = Mathf.Max(0f, position.width - spacing);
                float halfWidth = availableWidth * 0.5f;

                float labelWidth = Mathf.Clamp(
                    halfWidth * CustomColorDrawerLabelWidthRatio,
                    CustomColorDrawerMinLabelWidth,
                    CustomColorDrawerMaxLabelWidth
                );

                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth;

                try
                {
                    Rect backgroundRect = new(position.x, position.y, halfWidth, position.height);
                    Rect textRect = new(
                        position.x + halfWidth + spacing,
                        position.y,
                        halfWidth,
                        position.height
                    );

                    float minFieldWidth = CustomColorDrawerMinColorFieldWidth + labelWidth;
                    bool useLabels = halfWidth >= minFieldWidth;

                    EditorGUI.PropertyField(
                        backgroundRect,
                        background,
                        useLabels ? BackgroundLabelContent : GUIContent.none
                    );
                    EditorGUI.PropertyField(
                        textRect,
                        text,
                        useLabels ? TextLabelContent : GUIContent.none
                    );
                }
                finally
                {
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            }
        }
#endif

        /// <summary>
        /// Retrieves the effective page size for StringInList drawers, clamped to safe bounds.
        /// </summary>
        public int StringInListPageSize
        {
            get => Mathf.Clamp(_stringInListPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == _stringInListPageSize)
                {
                    return;
                }

                _stringInListPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Retrieves the configured page size for SerializableSet inspectors.
        /// </summary>
        public int SerializableSetPageSize
        {
            get => Mathf.Clamp(_serializableSetPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == _serializableSetPageSize)
                {
                    return;
                }

                _serializableSetPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Configures whether SerializableSet inspectors start collapsed by default.
        /// </summary>
        public bool SerializableSetStartCollapsed
        {
            get => _serializableSetStartCollapsed;
            set
            {
                if (_serializableSetStartCollapsed == value)
                {
                    return;
                }

                _serializableSetStartCollapsed = value;
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
                    _serializableDictionaryPageSize,
                    MinPageSize,
                    MaxSerializableDictionaryPageSize
                );
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxSerializableDictionaryPageSize);
                if (clamped == _serializableDictionaryPageSize)
                {
                    return;
                }

                _serializableDictionaryPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Configures whether SerializableDictionary inspectors start collapsed by default.
        /// </summary>
        public bool SerializableDictionaryStartCollapsed
        {
            get => _serializableDictionaryStartCollapsed;
            set
            {
                if (_serializableDictionaryStartCollapsed == value)
                {
                    return;
                }

                _serializableDictionaryStartCollapsed = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Configures whether collapsible WGroup headers start closed when their attribute does not specify a preference.
        /// </summary>
        public bool WGroupFoldoutsStartCollapsed
        {
            get => _wgroupFoldoutsStartCollapsed;
            set
            {
                if (_wgroupFoldoutsStartCollapsed == value)
                {
                    return;
                }

                _wgroupFoldoutsStartCollapsed = value;
                WGroupLayoutBuilder.ClearCache();
                SaveSettings();
            }
        }

        /// <summary>
        /// Gets the configured page size for WEnumToggleButtons groups.
        /// </summary>
        public int EnumToggleButtonsPageSize
        {
            get => Mathf.Clamp(_enumToggleButtonsPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == _enumToggleButtonsPageSize)
                {
                    return;
                }

                _enumToggleButtonsPageSize = clamped;
                SaveSettings();
            }
        }

        [Serializable]
        [Obsolete("Use WButtonCustomColorDictionary for serialization instead.")]
        private sealed class WButtonPriorityColor
        {
            [FormerlySerializedAs("priority")]
            [SerializeField]
            internal string _priority = DefaultWButtonColorKey;

            [FormerlySerializedAs("buttonColor")]
            [SerializeField]
            private Color _buttonColor = Color.white;

            [FormerlySerializedAs("textColor")]
            [SerializeField]
            private Color _textColor = Color.black;

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
                    string.IsNullOrWhiteSpace(_priority)
                        ? DefaultWButtonColorKey
                        : _priority.Trim();
                set =>
                    _priority = string.IsNullOrWhiteSpace(value)
                        ? DefaultWButtonColorKey
                        : value.Trim();
            }

            public Color ButtonColor
            {
                get => _buttonColor;
                set => _buttonColor = value;
            }

            public Color TextColor
            {
                get => _textColor;
                set => _textColor = value;
            }
        }

        /// <summary>
        /// Retrieves the configured page size for WButton groups.
        /// </summary>
        public int WButtonPageSize
        {
            get => Mathf.Clamp(_wbuttonPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == _wbuttonPageSize)
                {
                    return;
                }

                _wbuttonPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Retrieves the number of results retained per WButton method.
        /// </summary>
        public int WButtonHistorySize
        {
            get => Mathf.Clamp(_wbuttonHistorySize, MinWButtonHistorySize, MaxWButtonHistorySize);
            set
            {
                int clamped = Mathf.Clamp(value, MinWButtonHistorySize, MaxWButtonHistorySize);
                if (clamped == _wbuttonHistorySize)
                {
                    return;
                }

                _wbuttonHistorySize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Current duplicate-row animation mode used by dictionary inspectors.
        /// </summary>
        public DuplicateRowAnimationMode DuplicateRowAnimation
        {
            get => _duplicateRowAnimationMode;
            set
            {
                if (_duplicateRowAnimationMode == value)
                {
                    return;
                }

                _duplicateRowAnimationMode = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Number of tween cycles configured for duplicate rows. Negative values loop indefinitely.
        /// </summary>
        public int DuplicateRowTweenCycles
        {
            get => _duplicateRowTweenCycles;
            set
            {
                if (_duplicateRowTweenCycles == value)
                {
                    return;
                }

                _duplicateRowTweenCycles = value;
                SaveSettings();
            }
        }

        /// <summary>
        /// Duration used to detect repeated DetectAssetChanged callbacks before loop suppression activates.
        /// </summary>
        public float DetectAssetChangeLoopWindowSeconds
        {
            get =>
                Mathf.Clamp(
                    _detectAssetChangeLoopWindowSeconds <= 0f
                        ? DefaultDetectAssetChangeLoopWindowSeconds
                        : _detectAssetChangeLoopWindowSeconds,
                    MinDetectAssetChangeLoopWindowSeconds,
                    MaxDetectAssetChangeLoopWindowSeconds
                );
            set
            {
                float clamped = Mathf.Clamp(
                    value <= 0f ? DefaultDetectAssetChangeLoopWindowSeconds : value,
                    MinDetectAssetChangeLoopWindowSeconds,
                    MaxDetectAssetChangeLoopWindowSeconds
                );
                if (Mathf.Approximately(clamped, _detectAssetChangeLoopWindowSeconds))
                {
                    return;
                }

                _detectAssetChangeLoopWindowSeconds = clamped;
                SaveSettings();
            }
        }

        internal IReadOnlyList<string> GetSerializableTypeIgnorePatterns()
        {
            if (
                _serializableTypeIgnorePatterns == null
                || _serializableTypeIgnorePatterns.Count == 0
            )
            {
                _serializableTypeIgnorePatternCache = Array.Empty<string>();
                _serializableTypeIgnorePatternCacheVersion = 0;
                return _serializableTypeIgnorePatternCache;
            }

            int version = ComputeSerializableTypePatternVersion();
            if (version == _serializableTypeIgnorePatternCacheVersion)
            {
                return _serializableTypeIgnorePatternCache;
            }

            using PooledResource<List<string>> patternsLease = Buffers<string>.List.Get(
                out List<string> patterns
            );
            using PooledResource<HashSet<string>> seenLease = Buffers<string>.HashSet.Get(
                out HashSet<string> seen
            );

            foreach (SerializableTypeIgnorePattern entry in _serializableTypeIgnorePatterns)
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

            _serializableTypeIgnorePatternCache =
                patterns.Count == 0 ? Array.Empty<string>() : patterns.ToArray();
            _serializableTypeIgnorePatternCacheVersion = version;
            return _serializableTypeIgnorePatternCache;
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
            return settings == null || settings._serializableSetStartCollapsed;
        }

        private void InvalidateSerializableTypePatternCache()
        {
            _serializableTypeIgnorePatternCache = Array.Empty<string>();
            _serializableTypeIgnorePatternCacheVersion = int.MinValue;
        }

        private int ComputeSerializableTypePatternVersion()
        {
            if (
                _serializableTypeIgnorePatterns == null
                || _serializableTypeIgnorePatterns.Count == 0
            )
            {
                return 0;
            }

            HashCode hash = new HashCode();
            hash.Add(_serializableTypeIgnorePatterns.Count);
            for (int i = 0; i < _serializableTypeIgnorePatterns.Count; i++)
            {
                SerializableTypeIgnorePattern entry = _serializableTypeIgnorePatterns[i];
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
            return settings == null || settings._serializableDictionaryStartCollapsed;
        }

        /// <summary>
        /// Determines whether collapsible WGroup headers should default to a collapsed state.
        /// </summary>
        public static bool ShouldStartWGroupCollapsed()
        {
            UnityHelpersSettings settings = instance;
            return settings == null || settings._wgroupFoldoutsStartCollapsed;
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

        public static float GetDetectAssetChangeLoopWindowSeconds()
        {
            UnityHelpersSettings settings = instance;
            if (settings == null)
            {
                return DefaultDetectAssetChangeLoopWindowSeconds;
            }

            return Mathf.Clamp(
                settings.DetectAssetChangeLoopWindowSeconds,
                MinDetectAssetChangeLoopWindowSeconds,
                MaxDetectAssetChangeLoopWindowSeconds
            );
        }

        public static WGroupAutoIncludeConfiguration GetWGroupAutoIncludeConfiguration()
        {
            UnityHelpersSettings settings = instance;
            int clamped = Mathf.Clamp(
                settings._wgroupAutoIncludeRowCount,
                MinWGroupAutoIncludeRowCount,
                MaxWGroupAutoIncludeRowCount
            );
            return new WGroupAutoIncludeConfiguration(settings._wgroupAutoIncludeMode, clamped);
        }

#if UNITY_EDITOR
        internal static void SetWGroupAutoIncludeConfigurationForTests(
            WGroupAutoIncludeMode mode,
            int rowCount
        )
        {
            UnityHelpersSettings settings = instance;
            settings._wgroupAutoIncludeMode = mode;
            settings._wgroupAutoIncludeRowCount = Mathf.Clamp(
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
            return instance._wbuttonActionsPlacement;
        }

        public static WButtonFoldoutBehavior GetWButtonFoldoutBehavior()
        {
            return instance._wbuttonFoldoutBehavior;
        }

        public static bool ShouldTweenWButtonFoldouts()
        {
            return instance._wbuttonFoldoutTweenEnabled;
        }

        public static float GetWButtonFoldoutSpeed()
        {
            return Mathf.Clamp(instance._wbuttonFoldoutSpeed, MinFoldoutSpeed, MaxFoldoutSpeed);
        }

        public static WButtonPaletteEntry GetWButtonCancelButtonColors()
        {
            UnityHelpersSettings settings = instance;
            return new WButtonPaletteEntry(
                settings._wbuttonCancelButtonColor,
                settings._wbuttonCancelButtonTextColor
            );
        }

        public static WButtonPaletteEntry GetWButtonClearHistoryButtonColors()
        {
            UnityHelpersSettings settings = instance;
            return new WButtonPaletteEntry(
                settings._wbuttonClearHistoryButtonColor,
                settings._wbuttonClearHistoryButtonTextColor
            );
        }

        public static bool ShouldTweenSerializableDictionaryFoldouts()
        {
            return instance._serializableDictionaryFoldoutTweenEnabled;
        }

        public static float GetSerializableDictionaryFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance._serializableDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
        }

        public static bool ShouldTweenSerializableSortedDictionaryFoldouts()
        {
            return instance._serializableSortedDictionaryFoldoutTweenEnabled;
        }

        public static float GetSerializableSortedDictionaryFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance._serializableSortedDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
        }

        public static bool ShouldTweenSerializableSetFoldouts()
        {
            return instance._serializableSetFoldoutTweenEnabled;
        }

        public static float GetSerializableSetFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance._serializableSetFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
        }

        public static bool ShouldTweenSerializableSortedSetFoldouts()
        {
            return instance._serializableSortedSetFoldoutTweenEnabled;
        }

        public static float GetSerializableSortedSetFoldoutSpeed()
        {
            return Mathf.Clamp(
                instance._serializableSortedSetFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
        }

        public static DuplicateRowAnimationMode GetDuplicateRowAnimationMode()
        {
            return instance._duplicateRowAnimationMode;
        }

        public static int GetDuplicateRowTweenCycleLimit()
        {
            return instance._duplicateRowTweenCycles;
        }

        public static bool ShouldTweenSerializableSetDuplicates()
        {
            return instance._serializableSetDuplicateTweenEnabled
                && instance._duplicateRowAnimationMode == DuplicateRowAnimationMode.Tween;
        }

        public static int GetSerializableSetDuplicateTweenCycleLimit()
        {
            if (!instance._serializableSetDuplicateTweenSettingsInitialized)
            {
                return instance._duplicateRowTweenCycles;
            }

            return instance._serializableSetDuplicateTweenCycles;
        }

        public static InlineEditorFoldoutBehavior GetInlineEditorFoldoutBehavior()
        {
            return instance._inlineEditorFoldoutBehavior;
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
                _serializableTypeIgnorePatterns
            );
            internal const string SerializableTypePatternsInitialized = nameof(
                _serializableTypePatternsInitialized
            );
            internal const string SerializableTypePattern = nameof(
                SerializableTypeIgnorePattern._pattern
            );
            internal const string LegacyWButtonPriorityColors = nameof(
                _legacyWButtonPriorityColors
            );
            internal const string WButtonCustomColors = nameof(_wbuttonCustomColors);
            internal const string WGroupCustomColors = nameof(_wgroupCustomColors);
            internal const string WGroupFoldoutsStartCollapsed = nameof(
                _wgroupFoldoutsStartCollapsed
            );
            internal const string WEnumToggleButtonsCustomColors = nameof(
                _wenumToggleButtonsCustomColors
            );
            internal const string InlineEditorFoldoutBehavior = nameof(
                _inlineEditorFoldoutBehavior
            );
            internal const string WButtonFoldoutTweenEnabled = nameof(_wbuttonFoldoutTweenEnabled);
            internal const string SerializableDictionaryFoldoutTweenEnabled = nameof(
                _serializableDictionaryFoldoutTweenEnabled
            );
            internal const string SerializableSortedDictionaryFoldoutTweenEnabled = nameof(
                _serializableSortedDictionaryFoldoutTweenEnabled
            );
            internal const string SerializableSetFoldoutTweenEnabled = nameof(
                _serializableSetFoldoutTweenEnabled
            );
            internal const string SerializableSortedSetFoldoutTweenEnabled = nameof(
                _serializableSortedSetFoldoutTweenEnabled
            );
            internal const string FoldoutTweenSettingsInitialized = nameof(
                _foldoutTweenSettingsInitialized
            );
            internal const string DetectAssetChangeLoopWindowSeconds = nameof(
                _detectAssetChangeLoopWindowSeconds
            );
#pragma warning disable CS0618 // Type or member is obsolete
            internal const string WButtonPriority = nameof(WButtonPriorityColor._priority);
#pragma warning restore CS0618 // Type or member is obsolete
            internal const string WButtonCustomColorButton = nameof(
                WButtonCustomColor._buttonColor
            );
            internal const string WButtonCustomColorText = nameof(WButtonCustomColor._textColor);
            internal const string WGroupCustomColorBackground = nameof(
                WGroupCustomColor._backgroundColor
            );
            internal const string WGroupCustomColorText = nameof(WGroupCustomColor._textColor);
            internal const string WEnumToggleButtonsSelectedBackground = nameof(
                WEnumToggleButtonsCustomColor._selectedBackgroundColor
            );
            internal const string WEnumToggleButtonsSelectedText = nameof(
                WEnumToggleButtonsCustomColor._selectedTextColor
            );
            internal const string WEnumToggleButtonsInactiveBackground = nameof(
                WEnumToggleButtonsCustomColor._inactiveBackgroundColor
            );
            internal const string WEnumToggleButtonsInactiveText = nameof(
                WEnumToggleButtonsCustomColor._inactiveTextColor
            );
        }

        /// <summary>
        /// Constants for custom color drawer layout calculations, exposed for testing.
        /// </summary>
        internal static class CustomColorDrawerLayout
        {
            /// <summary>
            /// Minimum width for a color field (the picker itself, excluding label).
            /// </summary>
            internal const float MinColorFieldWidth = CustomColorDrawerMinColorFieldWidth;

            /// <summary>
            /// Ratio of column width to label width (e.g., 0.38 means label is 38% of the column).
            /// </summary>
            internal const float LabelWidthRatio = CustomColorDrawerLabelWidthRatio;

            /// <summary>
            /// Minimum label width in pixels.
            /// </summary>
            internal const float MinLabelWidth = CustomColorDrawerMinLabelWidth;

            /// <summary>
            /// Maximum label width in pixels.
            /// </summary>
            internal const float MaxLabelWidth = CustomColorDrawerMaxLabelWidth;

            /// <summary>
            /// Calculates the label width for a given column width using the same logic as the drawers.
            /// </summary>
            internal static float CalculateLabelWidth(float columnWidth)
            {
                return Mathf.Clamp(columnWidth * LabelWidthRatio, MinLabelWidth, MaxLabelWidth);
            }

            /// <summary>
            /// Determines whether labels should be displayed for a given column width.
            /// </summary>
            internal static bool ShouldShowLabels(float columnWidth)
            {
                float labelWidth = CalculateLabelWidth(columnWidth);
                float minFieldWidth = MinColorFieldWidth + labelWidth;
                return columnWidth >= minFieldWidth;
            }
        }

        /// <summary>
        /// Ensures persisted data stays within valid range.
        /// </summary>
        internal void OnEnable()
        {
            _stringInListPageSize = Mathf.Clamp(
                _stringInListPageSize <= 0 ? DefaultStringInListPageSize : _stringInListPageSize,
                MinPageSize,
                MaxPageSize
            );
            _serializableSetPageSize = Mathf.Clamp(
                _serializableSetPageSize <= 0
                    ? DefaultSerializableSetPageSize
                    : _serializableSetPageSize,
                MinPageSize,
                MaxPageSize
            );
            _serializableDictionaryPageSize = Mathf.Clamp(
                _serializableDictionaryPageSize <= 0
                    ? DefaultSerializableDictionaryPageSize
                    : _serializableDictionaryPageSize,
                MinPageSize,
                MaxSerializableDictionaryPageSize
            );
            _enumToggleButtonsPageSize = Mathf.Clamp(
                _enumToggleButtonsPageSize <= 0
                    ? DefaultEnumToggleButtonsPageSize
                    : _enumToggleButtonsPageSize,
                MinPageSize,
                MaxPageSize
            );
            _wbuttonPageSize = Mathf.Clamp(
                _wbuttonPageSize <= 0 ? DefaultWButtonPageSize : _wbuttonPageSize,
                MinPageSize,
                MaxPageSize
            );
            _wbuttonHistorySize = Mathf.Clamp(
                _wbuttonHistorySize <= 0 ? DefaultWButtonHistorySize : _wbuttonHistorySize,
                MinWButtonHistorySize,
                MaxWButtonHistorySize
            );
            if (!Enum.IsDefined(typeof(WButtonActionsPlacement), _wbuttonActionsPlacement))
            {
                _wbuttonActionsPlacement = WButtonActionsPlacement.Top;
            }

            if (!Enum.IsDefined(typeof(WButtonFoldoutBehavior), _wbuttonFoldoutBehavior))
            {
                _wbuttonFoldoutBehavior = WButtonFoldoutBehavior.StartExpanded;
            }
            if (!Enum.IsDefined(typeof(WGroupAutoIncludeMode), _wgroupAutoIncludeMode))
            {
                _wgroupAutoIncludeMode = WGroupAutoIncludeMode.Infinite;
            }
            if (_wgroupAutoIncludeRowCount < MinWGroupAutoIncludeRowCount)
            {
                _wgroupAutoIncludeRowCount = DefaultWGroupAutoIncludeRowCount;
            }
            _wgroupAutoIncludeRowCount = Mathf.Clamp(
                _wgroupAutoIncludeRowCount,
                MinWGroupAutoIncludeRowCount,
                MaxWGroupAutoIncludeRowCount
            );
            _wbuttonFoldoutSpeed = Mathf.Clamp(
                _wbuttonFoldoutSpeed <= 0f ? DefaultFoldoutSpeed : _wbuttonFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            _serializableDictionaryFoldoutSpeed = Mathf.Clamp(
                _serializableDictionaryFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : _serializableDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            _serializableSortedDictionaryFoldoutSpeed = Mathf.Clamp(
                _serializableSortedDictionaryFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : _serializableSortedDictionaryFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            _serializableSetFoldoutSpeed = Mathf.Clamp(
                _serializableSetFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : _serializableSetFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            _serializableSortedSetFoldoutSpeed = Mathf.Clamp(
                _serializableSortedSetFoldoutSpeed <= 0f
                    ? DefaultFoldoutSpeed
                    : _serializableSortedSetFoldoutSpeed,
                MinFoldoutSpeed,
                MaxFoldoutSpeed
            );
            _detectAssetChangeLoopWindowSeconds = Mathf.Clamp(
                _detectAssetChangeLoopWindowSeconds <= 0f
                    ? DefaultDetectAssetChangeLoopWindowSeconds
                    : _detectAssetChangeLoopWindowSeconds,
                MinDetectAssetChangeLoopWindowSeconds,
                MaxDetectAssetChangeLoopWindowSeconds
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
            OnSettingsSaved?.Invoke();
        }

        private void ApplyRuntimeConfiguration()
        {
            IReadOnlyList<string> patterns = GetSerializableTypeIgnorePatterns();
            SerializableTypeCatalog.ConfigureTypeNameIgnorePatterns(patterns);
            SerializableTypeCatalog.WarmPatternStats(patterns);
        }

        private bool EnsureSerializableTypePatternDefaults()
        {
            _serializableTypeIgnorePatterns ??= new List<SerializableTypeIgnorePattern>();

            if (_serializableTypePatternsInitialized)
            {
                return false;
            }

            if (_serializableTypeIgnorePatterns.Count == 0)
            {
                IReadOnlyList<string> defaults = SerializableTypeCatalog.GetDefaultIgnorePatterns();
                for (int index = 0; index < defaults.Count; index++)
                {
                    _serializableTypeIgnorePatterns.Add(
                        new SerializableTypeIgnorePattern(defaults[index])
                    );
                }
                InvalidateSerializableTypePatternCache();
            }

            bool changed = !_serializableTypePatternsInitialized;
            _serializableTypePatternsInitialized = true;
            return changed;
        }

        private bool EnsureFoldoutTweenDefaults()
        {
            if (_foldoutTweenSettingsInitialized)
            {
                return false;
            }

            if (!_wbuttonFoldoutTweenEnabled)
            {
                _wbuttonFoldoutTweenEnabled = true;
            }

            if (!_serializableDictionaryFoldoutTweenEnabled)
            {
                _serializableDictionaryFoldoutTweenEnabled = true;
            }

            if (!_serializableSortedDictionaryFoldoutTweenEnabled)
            {
                _serializableSortedDictionaryFoldoutTweenEnabled = true;
            }

            if (!_serializableSetFoldoutTweenEnabled)
            {
                _serializableSetFoldoutTweenEnabled = true;
            }

            if (!_serializableSortedSetFoldoutTweenEnabled)
            {
                _serializableSortedSetFoldoutTweenEnabled = true;
            }

            _foldoutTweenSettingsInitialized = true;
            return true;
        }

        private bool EnsureSerializableSetTweenDefaults()
        {
            if (_serializableSetDuplicateTweenSettingsInitialized)
            {
                return false;
            }

            _serializableSetDuplicateTweenEnabled =
                _duplicateRowAnimationMode == DuplicateRowAnimationMode.Tween;
            _serializableSetDuplicateTweenCycles =
                _duplicateRowTweenCycles != 0
                    ? _duplicateRowTweenCycles
                    : DefaultDuplicateTweenCycles;
            _serializableSetDuplicateTweenSettingsInitialized = true;
            return true;
        }

        internal bool EnsureWButtonCustomColorDefaults()
        {
            _wbuttonCustomColors ??= new WButtonCustomColorDictionary();

            bool changed = false;
            changed |= MigrateLegacyWButtonPalette();

            if (
                _wbuttonCustomColors.TryGetValue(
                    DefaultWButtonColorKey,
                    out WButtonCustomColor legacyDefault
                )
            )
            {
                _wbuttonCustomColors.TryAdd(WButtonLegacyColorKey, legacyDefault);
                _wbuttonCustomColors.Remove(DefaultWButtonColorKey);
                changed = true;
            }

            if (!_wbuttonCustomColors.ContainsKey(WButtonLegacyColorKey))
            {
                WButtonCustomColor legacyColor = new()
                {
                    ButtonColor = DefaultColorKeyButtonColor,
                    TextColor = WButtonColorUtility.GetReadableTextColor(
                        DefaultColorKeyButtonColor
                    ),
                };
                _wbuttonCustomColors[WButtonLegacyColorKey] = legacyColor;
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
                KeyValuePair<string, WButtonCustomColor> entry in _wbuttonCustomColors.ToArray()
            )
            {
                WButtonCustomColor value = entry.Value;
                if (value == null)
                {
                    value = new WButtonCustomColor();
                    _wbuttonCustomColors[entry.Key] = value;
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
            return ShouldSkipAutoSuggest(_wbuttonCustomColorSkipAutoSuggest, key);
        }

        private bool EnsureWButtonThemeEntry(string key, Color buttonColor, Color defaultTextColor)
        {
            if (
                _wbuttonCustomColors.TryGetValue(key, out WButtonCustomColor existing)
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
            _wbuttonCustomColors[key] = themeColor;
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
            if (_legacyWButtonPriorityColors == null || _legacyWButtonPriorityColors.Count == 0)
            {
                return false;
            }

            bool changed = false;
            foreach (WButtonPriorityColor legacy in _legacyWButtonPriorityColors)
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
                _wbuttonCustomColors[normalizedKey] = color;
                changed = true;
            }

            _legacyWButtonPriorityColors.Clear();
            return changed;
        }

        internal bool EnsureWGroupCustomColorDefaults()
        {
            _wgroupCustomColors ??= new WGroupCustomColorDictionary();

            bool changed = false;

            if (
                _wgroupCustomColors.TryGetValue(
                    DefaultWGroupColorKey,
                    out WGroupCustomColor legacyDefault
                )
            )
            {
                _wgroupCustomColors.TryAdd(WGroupLegacyColorKey, legacyDefault);
                _wgroupCustomColors.Remove(DefaultWGroupColorKey);
                changed = true;
            }

            if (!_wgroupCustomColors.ContainsKey(WGroupLegacyColorKey))
            {
                WGroupCustomColor legacyColor = new()
                {
                    BackgroundColor = DefaultColorKeyButtonColor,
                    TextColor = WButtonColorUtility.GetReadableTextColor(
                        DefaultColorKeyButtonColor
                    ),
                };
                _wgroupCustomColors[WGroupLegacyColorKey] = legacyColor;
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
            foreach (KeyValuePair<string, WGroupCustomColor> entry in _wgroupCustomColors)
            {
                WGroupCustomColor value = entry.Value;
                if (value == null)
                {
                    value = new WGroupCustomColor();
                    _wgroupCustomColors[entry.Key] = value;
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
            return ShouldSkipAutoSuggest(_wgroupCustomColorSkipAutoSuggest, key);
        }

        private bool EnsureWEnumToggleButtonsCustomColorDefaults()
        {
            if (_wenumToggleButtonsCustomColors == null)
            {
                _wenumToggleButtonsCustomColors = new WEnumToggleButtonsCustomColorDictionary();
            }

            bool changed = false;

            if (!_wenumToggleButtonsCustomColors.ContainsKey(DefaultWEnumToggleButtonsColorKey))
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
                _wenumToggleButtonsCustomColors[DefaultWEnumToggleButtonsColorKey] = defaultColor;
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
                > entry in _wenumToggleButtonsCustomColors
            )
            {
                WEnumToggleButtonsCustomColor value = entry.Value;
                if (value == null)
                {
                    value = new WEnumToggleButtonsCustomColor();
                    _wenumToggleButtonsCustomColors[entry.Key] = value;
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
                _wenumToggleButtonsCustomColors.TryGetValue(
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
            _wenumToggleButtonsCustomColors[key] = themeColor;
            return true;
        }

        private bool EnsureThemeEntry(string key, Color background, Color defaultText)
        {
            if (
                _wgroupCustomColors.TryGetValue(key, out WGroupCustomColor existing)
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
            _wgroupCustomColors[key] = themeColor;
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

            if (_wbuttonCustomColors == null || _wbuttonCustomColors.Count == 0)
            {
                return false;
            }

            string normalized = string.IsNullOrWhiteSpace(colorKey)
                ? DefaultWButtonColorKey
                : colorKey.Trim();

            foreach (string existingKey in _wbuttonCustomColors.Keys)
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

            if (
                _wenumToggleButtonsCustomColors == null
                || _wenumToggleButtonsCustomColors.Count == 0
            )
            {
                return false;
            }

            string normalized = colorKey.Trim();

            foreach (string existingKey in _wenumToggleButtonsCustomColors.Keys)
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

            if (_wbuttonCustomColors != null)
            {
                foreach (string existingKey in _wbuttonCustomColors.Keys)
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
                _wbuttonCustomColorSkipAutoSuggest ??= new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );
                _wbuttonCustomColorSkipAutoSuggest.Add(trimmedKey);
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
                _wgroupCustomColorSkipAutoSuggest ??= new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );
                _wgroupCustomColorSkipAutoSuggest.Add(trimmedKey);
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

            if (_wenumToggleButtonsCustomColors != null)
            {
                foreach (string existingKey in _wenumToggleButtonsCustomColors.Keys)
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
                    _wbuttonCustomColors != null
                    && _wbuttonCustomColors.TryGetValue(
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

            if (_wbuttonCustomColors is not { Count: > 0 })
            {
                return GetThemeAwareDefaultWButtonPalette();
            }

            if (
                _wbuttonCustomColors.TryGetValue(normalized, out WButtonCustomColor directValue)
                && directValue != null
            )
            {
                directValue.EnsureReadableText();
                return new WButtonPaletteEntry(directValue.ButtonColor, directValue.TextColor);
            }

            foreach (KeyValuePair<string, WButtonCustomColor> entry in _wbuttonCustomColors)
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
                _wbuttonCustomColors != null
                && _wbuttonCustomColors.TryGetValue(key, out WButtonCustomColor value)
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

            _wgroupCustomColors ??= new WGroupCustomColorDictionary();

            if (ContainsWGroupColorKey(normalizedKey))
            {
                return;
            }

            int paletteIndex = _wgroupCustomColors.Count;
            Color suggested = WButtonColorUtility.SuggestPaletteColor(paletteIndex);
            WGroupCustomColor color = new()
            {
                BackgroundColor = suggested,
                TextColor = WButtonColorUtility.GetReadableTextColor(suggested),
            };
            _wgroupCustomColors[normalizedKey] = color;
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

            if (_wgroupCustomColors == null || _wgroupCustomColors.Count == 0)
            {
                return false;
            }

            string normalized = colorKey.Trim();

            foreach (string existingKey in _wgroupCustomColors.Keys)
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

            if (_wgroupCustomColors != null)
            {
                foreach (string existingKey in _wgroupCustomColors.Keys)
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
                _wgroupCustomColors != null
                && _wgroupCustomColors.TryGetValue(key, out WGroupCustomColor value)
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
                _wgroupCustomColors != null
                && _wgroupCustomColors.TryGetValue(normalized, out WGroupCustomColor directValue)
                && directValue != null
            )
            {
                directValue.EnsureReadableText();
                return new WGroupPaletteEntry(directValue.BackgroundColor, directValue.TextColor);
            }

            if (_wgroupCustomColors != null)
            {
                foreach (KeyValuePair<string, WGroupCustomColor> entry in _wgroupCustomColors)
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
                _wenumToggleButtonsCustomColors != null
                && _wenumToggleButtonsCustomColors.TryGetValue(
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

            if (_wenumToggleButtonsCustomColors != null)
            {
                foreach (
                    KeyValuePair<
                        string,
                        WEnumToggleButtonsCustomColor
                    > entry in _wenumToggleButtonsCustomColors
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
                _wenumToggleButtonsCustomColors != null
                && _wenumToggleButtonsCustomColors.TryGetValue(
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

        private static bool DrawColorField(
            GUIContent content,
            Color currentValue,
            Action<Color> setter
        )
        {
            EditorGUI.BeginChangeCheck();
            Color newValue = EditorGUILayout.ColorField(content, currentValue);
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
                                nameof(_serializableTypePatternsInitialized)
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
                                    nameof(_serializableTypeIgnorePatterns),
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
                                    nameof(_stringInListPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    StringInListPageSizeContent,
                                    settings._stringInListPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings._stringInListPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSetPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    SerializableSetPageSizeContent,
                                    settings._serializableSetPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings._serializableSetPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSetStartCollapsed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SerializableSetStartCollapsedContent,
                                    settings._serializableSetStartCollapsed,
                                    value => settings._serializableSetStartCollapsed = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableDictionaryPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    SerializableDictionaryPageSizeContent,
                                    settings._serializableDictionaryPageSize,
                                    MinPageSize,
                                    MaxSerializableDictionaryPageSize,
                                    value => settings._serializableDictionaryPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableDictionaryStartCollapsed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SerializableDictionaryStartCollapsedContent,
                                    settings._serializableDictionaryStartCollapsed,
                                    value => settings._serializableDictionaryStartCollapsed = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_enumToggleButtonsPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    EnumToggleButtonsPageSizeContent,
                                    settings._enumToggleButtonsPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings._enumToggleButtonsPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonPageSize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    WButtonPageSizeContent,
                                    settings._wbuttonPageSize,
                                    MinPageSize,
                                    MaxPageSize,
                                    value => settings._wbuttonPageSize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonHistorySize),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawIntSliderField(
                                    WButtonHistorySizeContent,
                                    settings._wbuttonHistorySize,
                                    MinWButtonHistorySize,
                                    MaxWButtonHistorySize,
                                    value => settings._wbuttonHistorySize = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonActionsPlacement),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    WButtonPlacementContent,
                                    settings._wbuttonActionsPlacement,
                                    value => settings._wbuttonActionsPlacement = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonFoldoutBehavior),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    WButtonFoldoutBehaviorContent,
                                    settings._wbuttonFoldoutBehavior,
                                    value => settings._wbuttonFoldoutBehavior = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_inlineEditorFoldoutBehavior),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    InlineEditorFoldoutBehaviorContent,
                                    settings._inlineEditorFoldoutBehavior,
                                    value => settings._inlineEditorFoldoutBehavior = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    WButtonFoldoutTweenEnabledContent,
                                    settings._wbuttonFoldoutTweenEnabled,
                                    value => settings._wbuttonFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings._wbuttonFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    WButtonFoldoutSpeedContent,
                                    settings._wbuttonFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings._wbuttonFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonCancelButtonColor),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawColorField(
                                    WButtonCancelButtonColorContent,
                                    settings._wbuttonCancelButtonColor,
                                    value => settings._wbuttonCancelButtonColor = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonCancelButtonTextColor),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawColorField(
                                    WButtonCancelButtonTextColorContent,
                                    settings._wbuttonCancelButtonTextColor,
                                    value => settings._wbuttonCancelButtonTextColor = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonClearHistoryButtonColor),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawColorField(
                                    WButtonClearHistoryButtonColorContent,
                                    settings._wbuttonClearHistoryButtonColor,
                                    value => settings._wbuttonClearHistoryButtonColor = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonClearHistoryButtonTextColor),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawColorField(
                                    WButtonClearHistoryButtonTextColorContent,
                                    settings._wbuttonClearHistoryButtonTextColor,
                                    value => settings._wbuttonClearHistoryButtonTextColor = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wbuttonCustomColors),
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
                                    nameof(_wgroupCustomColors),
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
                                    nameof(_wenumToggleButtonsCustomColors),
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
                                    nameof(_waitInstructionBufferApplyOnLoad),
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
                                        settings._waitInstructionBufferApplyOnLoad,
                                        value => settings._waitInstructionBufferApplyOnLoad = value
                                    );
                                    dataChanged |= changed;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_waitInstructionBufferQuantizationStepSeconds),
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
                                        settings._waitInstructionBufferQuantizationStepSeconds,
                                        value =>
                                            settings._waitInstructionBufferQuantizationStepSeconds =
                                                Mathf.Max(0f, value)
                                    );
                                    dataChanged |= changed;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_waitInstructionBufferMaxDistinctEntries),
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
                                        settings._waitInstructionBufferMaxDistinctEntries,
                                        value =>
                                            settings._waitInstructionBufferMaxDistinctEntries =
                                                Mathf.Max(0, value)
                                    );
                                    dataChanged |= changed;
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_waitInstructionBufferUseLruEviction),
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
                                        settings._waitInstructionBufferUseLruEviction,
                                        value =>
                                            settings._waitInstructionBufferUseLruEviction = value
                                    );
                                    dataChanged |= changed;
                                    DrawWaitInstructionBufferButtons(settings);
                                }
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableDictionaryFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    DictionaryFoldoutTweenEnabledContent,
                                    settings._serializableDictionaryFoldoutTweenEnabled,
                                    value =>
                                        settings._serializableDictionaryFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableDictionaryFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings._serializableDictionaryFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    DictionaryFoldoutSpeedContent,
                                    settings._serializableDictionaryFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings._serializableDictionaryFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSortedDictionaryFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SortedDictionaryFoldoutTweenEnabledContent,
                                    settings._serializableSortedDictionaryFoldoutTweenEnabled,
                                    value =>
                                        settings._serializableSortedDictionaryFoldoutTweenEnabled =
                                            value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSortedDictionaryFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings._serializableSortedDictionaryFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    SortedDictionaryFoldoutSpeedContent,
                                    settings._serializableSortedDictionaryFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value =>
                                        settings._serializableSortedDictionaryFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSetFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SetFoldoutTweenEnabledContent,
                                    settings._serializableSetFoldoutTweenEnabled,
                                    value => settings._serializableSetFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSetFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings._serializableSetFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    SetFoldoutSpeedContent,
                                    settings._serializableSetFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings._serializableSetFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSortedSetFoldoutTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SortedSetFoldoutTweenEnabledContent,
                                    settings._serializableSortedSetFoldoutTweenEnabled,
                                    value =>
                                        settings._serializableSortedSetFoldoutTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSortedSetFoldoutSpeed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings._serializableSortedSetFoldoutTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawFloatSliderField(
                                    SortedSetFoldoutSpeedContent,
                                    settings._serializableSortedSetFoldoutSpeed,
                                    MinFoldoutSpeed,
                                    MaxFoldoutSpeed,
                                    value => settings._serializableSortedSetFoldoutSpeed = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_duplicateRowAnimationMode),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    DuplicateAnimationModeContent,
                                    settings._duplicateRowAnimationMode,
                                    value => settings._duplicateRowAnimationMode = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_duplicateRowTweenCycles),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (
                                    settings._duplicateRowAnimationMode
                                    != DuplicateRowAnimationMode.Tween
                                )
                                {
                                    return true;
                                }

                                bool changed = DrawIntField(
                                    DuplicateTweenCyclesContent,
                                    settings._duplicateRowTweenCycles,
                                    value => settings._duplicateRowTweenCycles = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_detectAssetChangeLoopWindowSeconds),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawFloatSliderField(
                                    DetectAssetChangeLoopWindowContent,
                                    settings.DetectAssetChangeLoopWindowSeconds,
                                    MinDetectAssetChangeLoopWindowSeconds,
                                    MaxDetectAssetChangeLoopWindowSeconds,
                                    value => settings._detectAssetChangeLoopWindowSeconds = value,
                                    true
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSetDuplicateTweenEnabled),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    SerializableSetDuplicateTweenEnabledContent,
                                    settings._serializableSetDuplicateTweenEnabled,
                                    value => settings._serializableSetDuplicateTweenEnabled = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_serializableSetDuplicateTweenCycles),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (!settings._serializableSetDuplicateTweenEnabled)
                                {
                                    return true;
                                }

                                bool changed = DrawIntField(
                                    SerializableSetDuplicateTweenCyclesContent,
                                    settings._serializableSetDuplicateTweenCycles,
                                    value => settings._serializableSetDuplicateTweenCycles = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wgroupAutoIncludeMode),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawEnumPopupField(
                                    WGroupAutoIncludeModeContent,
                                    settings._wgroupAutoIncludeMode,
                                    value => settings._wgroupAutoIncludeMode = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wgroupAutoIncludeRowCount),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                if (settings._wgroupAutoIncludeMode != WGroupAutoIncludeMode.Finite)
                                {
                                    return true;
                                }

                                bool changed = DrawIntSliderField(
                                    WGroupAutoIncludeCountContent,
                                    settings._wgroupAutoIncludeRowCount,
                                    MinWGroupAutoIncludeRowCount,
                                    MaxWGroupAutoIncludeRowCount,
                                    value => settings._wgroupAutoIncludeRowCount = value
                                );
                                dataChanged |= changed;
                                return true;
                            }

                            if (
                                string.Equals(
                                    property.propertyPath,
                                    nameof(_wgroupFoldoutsStartCollapsed),
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                bool changed = DrawToggleField(
                                    WGroupStartCollapsedContent,
                                    settings._wgroupFoldoutsStartCollapsed,
                                    value => settings._wgroupFoldoutsStartCollapsed = value
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
                                    nameof(_foldoutTweenSettingsInitialized),
                                    StringComparison.Ordinal
                                )
                                || string.Equals(
                                    propertyPath,
                                    nameof(_serializableTypePatternsInitialized),
                                    StringComparison.Ordinal
                                )
                                || string.Equals(
                                    propertyPath,
                                    nameof(_legacyWButtonPriorityColors),
                                    StringComparison.Ordinal
                                )
                                || string.Equals(
                                    propertyPath,
                                    nameof(_serializableTypeIgnorePatterns),
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
                        settings._waitInstructionBufferApplyOnLoad
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
            if (_waitInstructionBufferDefaultsInitialized)
            {
                return;
            }

            UnityHelpersBufferSettingsAsset asset = EnsureWaitInstructionBufferSettingsAsset();
            if (asset == null)
            {
                return;
            }

            _waitInstructionBufferApplyOnLoad = asset.ApplyOnLoad;
            _waitInstructionBufferQuantizationStepSeconds = asset.QuantizationStepSeconds;
            _waitInstructionBufferMaxDistinctEntries = asset.MaxDistinctEntries;
            _waitInstructionBufferUseLruEviction = asset.UseLruEviction;
            _waitInstructionBufferDefaultsInitialized = true;
        }

        private void CaptureWaitInstructionDefaultsFromRuntime()
        {
            _waitInstructionBufferQuantizationStepSeconds = Mathf.Max(
                0f,
                Buffers.WaitInstructionQuantizationStepSeconds
            );
            _waitInstructionBufferMaxDistinctEntries = Mathf.Max(
                0,
                Buffers.WaitInstructionMaxDistinctEntries
            );
            _waitInstructionBufferUseLruEviction = Buffers.WaitInstructionUseLruEviction;
            _waitInstructionBufferDefaultsInitialized = true;
            ApplyWaitInstructionBufferDefaultsToAsset(_waitInstructionBufferApplyOnLoad);
        }

        private bool AreWaitInstructionDefaultsInSyncWithRuntime()
        {
            float runtimeQuantization = Mathf.Max(
                0f,
                Buffers.WaitInstructionQuantizationStepSeconds
            );
            float configuredQuantization = Mathf.Max(
                0f,
                _waitInstructionBufferQuantizationStepSeconds
            );

            int runtimeMaxEntries = Mathf.Max(0, Buffers.WaitInstructionMaxDistinctEntries);
            int configuredMaxEntries = Mathf.Max(0, _waitInstructionBufferMaxDistinctEntries);

            bool runtimeUseLru = Buffers.WaitInstructionUseLruEviction;

            return Mathf.Approximately(configuredQuantization, runtimeQuantization)
                && configuredMaxEntries == runtimeMaxEntries
                && _waitInstructionBufferUseLruEviction == runtimeUseLru;
        }

        private void ApplyWaitInstructionBufferDefaultsToAsset(bool applyToRuntime)
        {
            UnityHelpersBufferSettingsAsset asset = EnsureWaitInstructionBufferSettingsAsset();
            if (asset == null)
            {
                return;
            }

            _waitInstructionBufferQuantizationStepSeconds = Mathf.Max(
                0f,
                _waitInstructionBufferQuantizationStepSeconds
            );
            _waitInstructionBufferMaxDistinctEntries = Mathf.Max(
                0,
                _waitInstructionBufferMaxDistinctEntries
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
                applyOnLoadProperty.boolValue = _waitInstructionBufferApplyOnLoad;
            }

            if (quantizationProperty != null)
            {
                quantizationProperty.floatValue = _waitInstructionBufferQuantizationStepSeconds;
            }

            if (maxEntriesProperty != null)
            {
                maxEntriesProperty.intValue = _waitInstructionBufferMaxDistinctEntries;
            }

            if (useLruProperty != null)
            {
                useLruProperty.boolValue = _waitInstructionBufferUseLruEviction;
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
            if (_waitInstructionBufferSettingsAsset != null)
            {
                return _waitInstructionBufferSettingsAsset;
            }

            _waitInstructionBufferSettingsAsset =
                AssetDatabase.LoadAssetAtPath<UnityHelpersBufferSettingsAsset>(
                    UnityHelpersBufferSettingsAsset.AssetPath
                );
            if (_waitInstructionBufferSettingsAsset != null)
            {
                return _waitInstructionBufferSettingsAsset;
            }

            string directory = Path.GetDirectoryName(UnityHelpersBufferSettingsAsset.AssetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            UnityHelpersBufferSettingsAsset created =
                CreateInstance<UnityHelpersBufferSettingsAsset>();
            created.SyncFromRuntime();
            AssetDatabase.CreateAsset(created, UnityHelpersBufferSettingsAsset.AssetPath);
            AssetDatabase.SaveAssets();
            _waitInstructionBufferSettingsAsset = created;
            return _waitInstructionBufferSettingsAsset;
        }
    }
#endif
}
