namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    /// Declares an inspector foldout grouping with automatic member inclusion support.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class WFoldoutGroupAttribute : Attribute
    {
        public const int InfiniteAutoInclude = -1;
        public const int UseGlobalAutoInclude = -2;

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

        public string GroupName { get; }

        public string DisplayName { get; }

        public int AutoIncludeCount { get; }

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
