namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    /// Declares an inspector grouping similar to Odin's BoxGroup while supporting automatic member inclusion.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class WGroupAttribute : Attribute
    {
        public const int InfiniteAutoInclude = -1;
        public const int UseGlobalAutoInclude = -2;

        public WGroupAttribute(
            string groupName,
            string displayName = null,
            int autoIncludeCount = UseGlobalAutoInclude,
            bool collapsible = false,
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
            Collapsible = collapsible;
            StartCollapsed = collapsible && startCollapsed;
            ColorKey = string.IsNullOrWhiteSpace(colorKey) ? null : colorKey.Trim();
            HideHeader = hideHeader;
        }

        public string GroupName { get; }

        public string DisplayName { get; }

        public int AutoIncludeCount { get; }

        public bool Collapsible { get; }

        public bool StartCollapsed { get; }

        public string ColorKey { get; }

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
