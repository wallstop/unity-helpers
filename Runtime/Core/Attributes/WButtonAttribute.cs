namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    /// Marks a method for exposure as an inspector button with optional naming and ordering.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class WButtonAttribute : Attribute
    {
        public const int UseGlobalHistory = -1;

        public WButtonAttribute(
            string displayName = null,
            int drawOrder = 0,
            int historyCapacity = UseGlobalHistory,
            string priority = null,
            string groupName = null
        )
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
            DrawOrder = drawOrder;
            HistoryCapacity = historyCapacity < 0 ? UseGlobalHistory : historyCapacity;
            ColorKey = string.IsNullOrWhiteSpace(priority) ? null : priority.Trim();
            GroupName = string.IsNullOrWhiteSpace(groupName) ? null : groupName.Trim();
        }

        /// <summary>
        /// Explicit label override for the button. Falls back to the method name when null.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Controls grouping and sorting; values of -1 or higher render above the default inspector.
        /// </summary>
        public int DrawOrder { get; }

        /// <summary>
        /// Optional override for the number of results retained. Negative values defer to the global setting.
        /// </summary>
        public int HistoryCapacity { get; }

        /// <summary>
        /// Optional custom color key used to resolve palette-based styling.
        /// </summary>
        public string ColorKey { get; }

        /// <summary>
        /// Optional override for the inspector group header associated with this draw order.
        /// The first non-empty name encountered for a draw order wins.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Legacy alias for <see cref="ColorKey"/> to maintain backwards compatibility.
        /// </summary>
        [Obsolete("Use ColorKey instead.")]
        public string Priority => ColorKey;
    }
}
