namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    /// Declares an inspector foldout grouping that automatically captures subsequent fields into an expandable section.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WFoldoutGroupAttribute"/> mirrors the ergonomics of Odin Inspector's <c>FoldoutGroup</c> while remaining dependency free.
    /// It is ideal for long forms where optional sections should stay collapsed by default but still support automatic member sweeping.
    /// </para>
    /// <para>
    /// Use <see cref="WFoldoutGroupEndAttribute"/> to terminate auto inclusion or selectively exclude specific members.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class CharacterSheet : ScriptableObject
    /// {
    ///     [WFoldoutGroup(\"Advanced\", autoIncludeCount: 3, startCollapsed: true)]
    ///     public float luck;
    ///     public float persuasion;
    ///     public AnimationCurve damageDropOff;
    ///
    ///     [WFoldoutGroupEnd]
    ///     public int hiddenSentinel; // closes the foldout group
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class WFoldoutGroupAttribute : Attribute
    {
        /// <summary>
        /// Sentinel value instructing the drawer to keep including members until a corresponding <see cref="WFoldoutGroupEndAttribute"/> is seen.
        /// </summary>
        public const int InfiniteAutoInclude = -1;

        /// <summary>
        /// Sentinel value that falls back to the global auto include count defined in <c>UnityHelpersSettings</c>.
        /// </summary>
        public const int UseGlobalAutoInclude = -2;

        /// <summary>
        /// Creates a collapsible inspector group anchored to the decorated field.
        /// </summary>
        /// <param name="groupName">Unique key that ties start and end attributes together.</param>
        /// <param name="displayName">Optional header text shown to the user.</param>
        /// <param name="autoIncludeCount">
        /// Number of serialized members after the annotated field to automatically include.
        /// Use <see cref="InfiniteAutoInclude"/> to keep including until a <see cref="WFoldoutGroupEndAttribute"/> is encountered.
        /// </param>
        /// <param name="startCollapsed">Set to <see langword="true"/> to render the foldout closed on first draw.</param>
        /// <param name="colorKey">Optional palette identifier for themed foldout headers.</param>
        /// <param name="hideHeader">Set to <see langword="true"/> to hide the header visuals while retaining the grouping behaviour.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="groupName"/> is null or whitespace.</exception>
        public WFoldoutGroupAttribute(
            string groupName,
            string displayName = null,
            int autoIncludeCount = UseGlobalAutoInclude,
            bool startCollapsed = false,
            string colorKey = null,
            bool hideHeader = false
        )
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException(
                    "Group name cannot be null or whitespace.",
                    nameof(groupName)
                );
            }

            GroupName = groupName.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? GroupName : displayName.Trim();
            AutoIncludeCount = NormalizeAutoIncludeCount(autoIncludeCount);
            StartCollapsed = startCollapsed;
            ColorKey = string.IsNullOrWhiteSpace(colorKey) ? null : colorKey.Trim();
            HideHeader = hideHeader;
        }

        /// <summary>
        /// Identifier shared between the start and end attributes.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Human-friendly title displayed in the inspector header (defaults to <see cref="GroupName"/>).
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Number of trailing serialized members swept into the foldout group.
        /// </summary>
        public int AutoIncludeCount { get; }

        /// <summary>
        /// Gets a value indicating whether the foldout is initialized in the collapsed state.
        /// </summary>
        public bool StartCollapsed { get; }

        /// <summary>
        /// Optional palette key used to look up color themes from <c>UnityHelpersSettings</c>.
        /// </summary>
        public string ColorKey { get; }

        /// <summary>
        /// Set to <see langword="true"/> to omit the header row while still grouping the members.
        /// </summary>
        public bool HideHeader { get; }

        private static int NormalizeAutoIncludeCount(int autoIncludeCount)
        {
            if (autoIncludeCount < InfiniteAutoInclude)
            {
                return UseGlobalAutoInclude;
            }

            if (autoIncludeCount == InfiniteAutoInclude)
            {
                return InfiniteAutoInclude;
            }

            return autoIncludeCount < 0 ? UseGlobalAutoInclude : autoIncludeCount;
        }
    }
}
