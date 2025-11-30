namespace WallstopStudios.UnityHelpers.Editor.Utils.WGroup
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Extensions;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Utils;

    internal static class WGroupLayoutBuilder
    {
        internal static WGroupLayout Build(
            SerializedObject serializedObject,
            string scriptPropertyPath
        )
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException(nameof(serializedObject));
            }

            List<PropertyDescriptor> descriptors = CollectPropertyDescriptors(
                serializedObject,
                scriptPropertyPath
            );
            if (descriptors.Count == 0)
            {
                return new WGroupLayout(
                    new List<WGroupDrawOperation>(0),
                    new List<WGroupDefinition>(0),
                    new Dictionary<string, WGroupDefinition>(StringComparer.OrdinalIgnoreCase)
                );
            }

            UnityHelpersSettings.WGroupAutoIncludeConfiguration configuration =
                UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();
            AutoIncludeConfiguration globalConfiguration = ConvertConfiguration(configuration);
            Dictionary<string, GroupContext> contextsByName = new(StringComparer.OrdinalIgnoreCase);
            List<GroupContext> contextsInDeclarationOrder = new();
            List<GroupContext> activeAutoContexts = new();

            for (int index = 0; index < descriptors.Count; index++)
            {
                PropertyDescriptor descriptor = descriptors[index];
                HashSet<GroupContext> explicitContexts = null;

                if (descriptor.GroupAttributes.Count > 0)
                {
                    explicitContexts = new HashSet<GroupContext>();
                    foreach (WGroupAttribute attribute in descriptor.GroupAttributes)
                    {
                        string normalizedName = NormalizeGroupName(attribute.GroupName);
                        if (!contextsByName.TryGetValue(normalizedName, out GroupContext context))
                        {
                            context = new GroupContext(
                                normalizedName,
                                contextsInDeclarationOrder.Count
                            );
                            contextsByName.Add(normalizedName, context);
                            contextsInDeclarationOrder.Add(context);
                        }

                        context.ApplyAttribute(attribute, descriptor.PropertyPath, index);
                        AutoIncludeConfiguration localConfiguration = ResolveAutoInclude(
                            attribute.AutoIncludeCount,
                            globalConfiguration
                        );
                        context.SetAutoInclude(localConfiguration);
                        UpdateActiveContextList(activeAutoContexts, context);

                        explicitContexts.Add(context);
                    }
                }

                if (explicitContexts == null || explicitContexts.Count == 0)
                {
                    GroupContext autoContext = SelectAutoIncludeTarget(
                        activeAutoContexts,
                        explicitContexts
                    );
                    if (autoContext != null)
                    {
                        bool added = autoContext.AddProperty(descriptor.PropertyPath, index);
                        if (added)
                        {
                            autoContext.ConsumeAutoInclude();
                            UpdateActiveContextList(activeAutoContexts, autoContext);
                        }
                    }
                }
                else
                {
                    foreach (GroupContext context in explicitContexts)
                    {
                        context.AddProperty(descriptor.PropertyPath, index);
                    }
                }

                if (descriptor.EndAttributes.Count > 0)
                {
                    ApplyGroupEnds(descriptor.EndAttributes, activeAutoContexts, contextsByName);
                }
            }

            List<WGroupDefinition> definitions = new(contextsInDeclarationOrder.Count);
            Dictionary<string, List<WGroupDefinition>> groupsByAnchor = new(StringComparer.Ordinal);
            Dictionary<string, WGroupDefinition> groupsByName = new(
                StringComparer.OrdinalIgnoreCase
            );

            foreach (GroupContext context in contextsInDeclarationOrder)
            {
                if (context.PropertyCount == 0)
                {
                    continue;
                }

                WGroupDefinition definition = context.ToDefinition();
                definitions.Add(definition);
                groupsByName[definition.Name] = definition;

                List<WGroupDefinition> anchored = groupsByAnchor.GetOrAdd(
                    definition.AnchorPropertyPath
                );
                anchored.Add(definition);
            }

            List<WGroupDrawOperation> operations = BuildDrawOperations(descriptors, groupsByAnchor);
            return new WGroupLayout(operations, definitions, groupsByName);
        }

        private static List<PropertyDescriptor> CollectPropertyDescriptors(
            SerializedObject serializedObject,
            string scriptPropertyPath
        )
        {
            List<PropertyDescriptor> descriptors = new();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (
                    !string.IsNullOrEmpty(scriptPropertyPath)
                    && string.Equals(
                        iterator.propertyPath,
                        scriptPropertyPath,
                        StringComparison.Ordinal
                    )
                )
                {
                    continue;
                }

                iterator.GetEnclosingObject(out FieldInfo fieldInfo);

                List<WGroupAttribute> groupAttributes = CollectAttributes<WGroupAttribute>(
                    fieldInfo
                );
                List<WGroupEndAttribute> endAttributes = CollectAttributes<WGroupEndAttribute>(
                    fieldInfo
                );
                PropertyDescriptor descriptor = new(
                    iterator.propertyPath,
                    groupAttributes,
                    endAttributes
                );
                descriptors.Add(descriptor);
            }

            return descriptors;
        }

        private static List<TAttribute> CollectAttributes<TAttribute>(FieldInfo fieldInfo)
            where TAttribute : Attribute
        {
            if (fieldInfo == null)
            {
                return new List<TAttribute>();
            }

            TAttribute[] attributes = fieldInfo.GetAllAttributesSafe<TAttribute>(inherit: true);
            if (attributes == null || attributes.Length == 0)
            {
                return new List<TAttribute>();
            }

            return new List<TAttribute>(attributes);
        }

        private static AutoIncludeConfiguration ConvertConfiguration(
            UnityHelpersSettings.WGroupAutoIncludeConfiguration configuration
        )
        {
            switch (configuration.Mode)
            {
                case UnityHelpersSettings.WGroupAutoIncludeMode.Infinite:
                    return new AutoIncludeConfiguration(true, 0);
                case UnityHelpersSettings.WGroupAutoIncludeMode.Finite:
                    return new AutoIncludeConfiguration(false, configuration.RowCount);
                default:
                    return new AutoIncludeConfiguration(false, 0);
            }
        }

        private static AutoIncludeConfiguration ResolveAutoInclude(
            int requestedValue,
            AutoIncludeConfiguration globalConfiguration
        )
        {
            if (requestedValue == WGroupAttribute.UseGlobalAutoInclude)
            {
                return globalConfiguration;
            }

            if (requestedValue == WGroupAttribute.InfiniteAutoInclude)
            {
                return new AutoIncludeConfiguration(true, 0);
            }

            if (requestedValue <= 0)
            {
                return new AutoIncludeConfiguration(false, 0);
            }

            return new AutoIncludeConfiguration(false, requestedValue);
        }

        private static void UpdateActiveContextList(
            List<GroupContext> activeAutoContexts,
            GroupContext context
        )
        {
            for (int index = 0; index < activeAutoContexts.Count; index++)
            {
                if (ReferenceEquals(activeAutoContexts[index], context))
                {
                    activeAutoContexts.RemoveAt(index);
                    break;
                }
            }

            if (!context.HasAutoIncludeBudget)
            {
                return;
            }

            InsertActiveContext(activeAutoContexts, context);
        }

        private static void InsertActiveContext(
            List<GroupContext> activeAutoContexts,
            GroupContext context
        )
        {
            for (int index = 0; index < activeAutoContexts.Count; index++)
            {
                GroupContext existing = activeAutoContexts[index];
                if (existing.DeclarationOrder > context.DeclarationOrder)
                {
                    activeAutoContexts.Insert(index, context);
                    return;
                }
            }

            activeAutoContexts.Add(context);
        }

        private static GroupContext SelectAutoIncludeTarget(
            List<GroupContext> activeAutoContexts,
            HashSet<GroupContext> explicitContexts
        )
        {
            for (int index = activeAutoContexts.Count - 1; index >= 0; index--)
            {
                GroupContext candidate = activeAutoContexts[index];
                if (!candidate.HasAutoIncludeBudget)
                {
                    activeAutoContexts.RemoveAt(index);
                    continue;
                }

                if (explicitContexts != null && explicitContexts.Contains(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static void ApplyGroupEnds(
            List<WGroupEndAttribute> endAttributes,
            List<GroupContext> activeAutoContexts,
            Dictionary<string, GroupContext> contextsByName
        )
        {
            for (int index = 0; index < endAttributes.Count; index++)
            {
                WGroupEndAttribute attribute = endAttributes[index];
                IReadOnlyList<string> groupNames = attribute.GroupNames;
                if (groupNames.Count == 0)
                {
                    if (activeAutoContexts.Count > 0)
                    {
                        GroupContext last = activeAutoContexts[^1];
                        last.SetAutoInclude(new AutoIncludeConfiguration(false, 0));
                        UpdateActiveContextList(activeAutoContexts, last);
                    }
                    continue;
                }

                for (int nameIndex = 0; nameIndex < groupNames.Count; nameIndex++)
                {
                    string groupName = groupNames[nameIndex];
                    if (string.IsNullOrEmpty(groupName))
                    {
                        continue;
                    }

                    string normalizedName = NormalizeGroupName(groupName);
                    if (!contextsByName.TryGetValue(normalizedName, out GroupContext context))
                    {
                        continue;
                    }

                    context.SetAutoInclude(new AutoIncludeConfiguration(false, 0));
                    UpdateActiveContextList(activeAutoContexts, context);
                }
            }
        }

        private static string NormalizeGroupName(string groupName)
        {
            return string.IsNullOrWhiteSpace(groupName) ? string.Empty : groupName.Trim();
        }

        private static List<WGroupDrawOperation> BuildDrawOperations(
            List<PropertyDescriptor> descriptors,
            Dictionary<string, List<WGroupDefinition>> groupsByAnchor
        )
        {
            List<WGroupDrawOperation> operations = new(descriptors.Count);
            WallstopGenericPool<HashSet<string>> consumedPool = SetBuffers<string>.GetHashSetPool(
                StringComparer.Ordinal
            );
            using PooledResource<HashSet<string>> consumedLease = consumedPool.Get(
                out HashSet<string> consumed
            );
            {
                for (int index = 0; index < descriptors.Count; index++)
                {
                    PropertyDescriptor descriptor = descriptors[index];
                    string propertyPath = descriptor.PropertyPath;
                    bool anchoredHandled = false;

                    if (
                        groupsByAnchor.TryGetValue(
                            propertyPath,
                            out List<WGroupDefinition> anchoredGroups
                        )
                    )
                    {
                        anchoredHandled = true;
                        anchoredGroups.Sort(
                            (left, right) => left.DeclarationOrder.CompareTo(right.DeclarationOrder)
                        );
                        foreach (WGroupDefinition definition in anchoredGroups)
                        {
                            operations.Add(new WGroupDrawOperation(definition));
                            for (
                                int memberIndex = 0;
                                memberIndex < definition.PropertyPaths.Count;
                                memberIndex++
                            )
                            {
                                consumed.Add(definition.PropertyPaths[memberIndex]);
                            }
                        }
                    }

                    if (anchoredHandled)
                    {
                        continue;
                    }

                    if (!consumed.Add(propertyPath))
                    {
                        continue;
                    }

                    operations.Add(new WGroupDrawOperation(propertyPath));
                }
            }

            return operations;
        }

        private sealed class GroupContext
        {
            private readonly List<PropertyEntry> _entries = new();
            private readonly HashSet<string> _lookup = new(StringComparer.Ordinal);

            internal GroupContext(string name, int declarationOrder)
            {
                Name = name;
                DeclarationOrder = declarationOrder;
                AnchorIndex = int.MaxValue;
            }

            internal string Name { get; }

            internal int DeclarationOrder { get; }

            internal string DisplayName { get; private set; }

            internal bool Collapsible { get; private set; }

            internal bool StartCollapsed { get; private set; }

            internal bool HideHeader { get; private set; }

            internal string ColorKey { get; private set; }

            internal int AnchorIndex { get; private set; }

            internal string AnchorPropertyPath { get; private set; }

            internal bool AutoIncludeInfinite { get; private set; }

            internal int RemainingAutoInclude { get; private set; }

            internal int PropertyCount
            {
                get { return _entries.Count; }
            }

            internal void ApplyAttribute(
                WGroupAttribute attribute,
                string propertyPath,
                int propertyIndex
            )
            {
                if (!string.IsNullOrWhiteSpace(attribute.DisplayName))
                {
                    DisplayName = attribute.DisplayName;
                }

                if (!string.IsNullOrWhiteSpace(attribute.ColorKey))
                {
                    string normalized = UnityHelpersSettings.EnsureWGroupColorKey(
                        attribute.ColorKey
                    );
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        ColorKey = normalized;
                    }
                }

                if (Collapsible != attribute.Collapsible)
                {
                    Collapsible = attribute.Collapsible;
                }

                if (attribute.Collapsible)
                {
                    StartCollapsed = attribute.StartCollapsed;
                }

                HideHeader = attribute.HideHeader;

                AddProperty(propertyPath, propertyIndex);
            }

            internal void SetAutoInclude(AutoIncludeConfiguration configuration)
            {
                AutoIncludeInfinite = configuration.IsInfinite;
                RemainingAutoInclude = configuration.IsInfinite ? 0 : configuration.Count;
            }

            internal bool AddProperty(string propertyPath, int propertyIndex)
            {
                if (!_lookup.Add(propertyPath))
                {
                    return false;
                }

                PropertyEntry entry = new(propertyPath, propertyIndex);
                _entries.Add(entry);

                if (propertyIndex < AnchorIndex)
                {
                    AnchorIndex = propertyIndex;
                    AnchorPropertyPath = propertyPath;
                }

                return true;
            }

            internal void ConsumeAutoInclude()
            {
                if (AutoIncludeInfinite)
                {
                    return;
                }

                if (RemainingAutoInclude <= 0)
                {
                    return;
                }

                RemainingAutoInclude--;
            }

            internal bool HasAutoIncludeBudget
            {
                get
                {
                    if (AutoIncludeInfinite)
                    {
                        return true;
                    }

                    return RemainingAutoInclude > 0;
                }
            }

            internal WGroupDefinition ToDefinition()
            {
                _entries.Sort((left, right) => left.PropertyIndex.CompareTo(right.PropertyIndex));
                List<string> orderedPaths = new(_entries.Count);
                for (int index = 0; index < _entries.Count; index++)
                {
                    orderedPaths.Add(_entries[index].PropertyPath);
                }

                string displayName = DisplayName ?? Name;
                string anchorPath = AnchorPropertyPath ?? orderedPaths[0];
                int anchorIndex = AnchorIndex == int.MaxValue ? 0 : AnchorIndex;

                return new WGroupDefinition(
                    Name,
                    displayName,
                    ColorKey,
                    Collapsible,
                    StartCollapsed,
                    HideHeader,
                    orderedPaths,
                    anchorPath,
                    anchorIndex,
                    DeclarationOrder
                );
            }

            private readonly struct PropertyEntry
            {
                internal PropertyEntry(string propertyPath, int propertyIndex)
                {
                    PropertyPath = propertyPath;
                    PropertyIndex = propertyIndex;
                }

                internal string PropertyPath { get; }

                internal int PropertyIndex { get; }
            }
        }

        private sealed class PropertyDescriptor
        {
            internal PropertyDescriptor(
                string propertyPath,
                List<WGroupAttribute> groupAttributes,
                List<WGroupEndAttribute> endAttributes
            )
            {
                PropertyPath = propertyPath;
                GroupAttributes = groupAttributes ?? new List<WGroupAttribute>();
                EndAttributes = endAttributes ?? new List<WGroupEndAttribute>();
            }

            internal string PropertyPath { get; }

            internal List<WGroupAttribute> GroupAttributes { get; }

            internal List<WGroupEndAttribute> EndAttributes { get; }
        }

        private readonly struct AutoIncludeConfiguration
        {
            internal AutoIncludeConfiguration(bool isInfinite, int count)
            {
                IsInfinite = isInfinite;
                Count = count < 0 ? 0 : count;
            }

            internal bool IsInfinite { get; }

            internal int Count { get; }
        }
    }

    internal enum WGroupDrawOperationType
    {
        Property = 0,
        Group = 1,
    }

    internal readonly struct WGroupDrawOperation
    {
        internal WGroupDrawOperation(string propertyPath)
        {
            Type = WGroupDrawOperationType.Property;
            PropertyPath = propertyPath;
            Group = null;
        }

        internal WGroupDrawOperation(WGroupDefinition group)
        {
            Type = WGroupDrawOperationType.Group;
            PropertyPath = null;
            Group = group;
        }

        internal WGroupDrawOperationType Type { get; }

        internal string PropertyPath { get; }

        internal WGroupDefinition Group { get; }
    }

    internal sealed class WGroupDefinition
    {
        internal WGroupDefinition(
            string name,
            string displayName,
            string colorKey,
            bool collapsible,
            bool startCollapsed,
            bool hideHeader,
            IReadOnlyList<string> propertyPaths,
            string anchorPropertyPath,
            int anchorIndex,
            int declarationOrder
        )
        {
            Name = name;
            DisplayName = displayName;
            ColorKey = colorKey;
            Collapsible = collapsible;
            StartCollapsed = startCollapsed;
            HideHeader = hideHeader;
            PropertyPaths = propertyPaths;
            AnchorPropertyPath = anchorPropertyPath;
            AnchorIndex = anchorIndex;
            DeclarationOrder = declarationOrder;
        }

        internal string Name { get; }

        internal string DisplayName { get; }

        internal string ColorKey { get; }

        internal bool Collapsible { get; }

        internal bool StartCollapsed { get; }

        internal bool HideHeader { get; }

        internal IReadOnlyList<string> PropertyPaths { get; }

        internal string AnchorPropertyPath { get; }

        internal int AnchorIndex { get; }

        internal int DeclarationOrder { get; }
    }

    internal sealed class WGroupLayout
    {
        internal WGroupLayout(
            IReadOnlyList<WGroupDrawOperation> operations,
            IReadOnlyList<WGroupDefinition> groups,
            IReadOnlyDictionary<string, WGroupDefinition> groupsByName
        )
        {
            Operations = operations;
            Groups = groups;
            GroupsByName = groupsByName;
        }

        internal IReadOnlyList<WGroupDrawOperation> Operations { get; }

        internal IReadOnlyList<WGroupDefinition> Groups { get; }

        internal IReadOnlyDictionary<string, WGroupDefinition> GroupsByName { get; }

        internal bool TryGetGroup(string groupName, out WGroupDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                definition = null;
                return false;
            }

            return GroupsByName.TryGetValue(groupName.Trim(), out definition);
        }
    }
#endif
}
