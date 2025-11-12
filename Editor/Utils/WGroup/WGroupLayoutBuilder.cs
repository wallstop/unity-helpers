namespace WallstopStudios.UnityHelpers.Editor.Utils.WGroup
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Editor.Extensions;
    using WallstopStudios.UnityHelpers.Editor.Settings;

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
                    new Dictionary<string, WGroupDefinition>(StringComparer.OrdinalIgnoreCase),
                    new List<WFoldoutGroupDefinition>(0),
                    new Dictionary<string, WFoldoutGroupDefinition>(
                        StringComparer.OrdinalIgnoreCase
                    )
                );
            }

            UnityHelpersSettings.WGroupAutoIncludeConfiguration configuration =
                UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();
            AutoIncludeConfiguration globalConfiguration = ConvertConfiguration(configuration);

            Dictionary<string, GroupContext> contextsByName = new(StringComparer.OrdinalIgnoreCase);
            List<GroupContext> contextsInDeclarationOrder = new();
            List<GroupContext> activeAutoContexts = new();
            Dictionary<string, FoldoutGroupContext> foldoutContextsByName = new(
                StringComparer.OrdinalIgnoreCase
            );
            List<FoldoutGroupContext> foldoutContextsInDeclarationOrder = new();
            List<FoldoutGroupContext> activeFoldoutAutoContexts = new();

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

                HashSet<FoldoutGroupContext> explicitFoldoutContexts = null;
                if (descriptor.FoldoutAttributes.Count > 0)
                {
                    explicitFoldoutContexts = new HashSet<FoldoutGroupContext>();
                    foreach (WFoldoutGroupAttribute attribute in descriptor.FoldoutAttributes)
                    {
                        string normalizedName = NormalizeGroupName(attribute.GroupName);
                        if (
                            !foldoutContextsByName.TryGetValue(
                                normalizedName,
                                out FoldoutGroupContext context
                            )
                        )
                        {
                            context = new FoldoutGroupContext(
                                normalizedName,
                                foldoutContextsInDeclarationOrder.Count
                            );
                            foldoutContextsByName.Add(normalizedName, context);
                            foldoutContextsInDeclarationOrder.Add(context);
                        }

                        context.ApplyAttribute(attribute, descriptor.PropertyPath, index);

                        AutoIncludeConfiguration localConfiguration = ResolveAutoInclude(
                            attribute.AutoIncludeCount,
                            globalConfiguration
                        );
                        context.SetAutoInclude(localConfiguration);
                        UpdateActiveFoldoutContextList(activeFoldoutAutoContexts, context);

                        explicitFoldoutContexts.Add(context);
                    }
                }

                if (explicitFoldoutContexts == null || explicitFoldoutContexts.Count == 0)
                {
                    FoldoutGroupContext autoFoldoutContext = SelectFoldoutAutoIncludeTarget(
                        activeFoldoutAutoContexts,
                        explicitFoldoutContexts
                    );
                    if (autoFoldoutContext != null)
                    {
                        bool added = autoFoldoutContext.AddProperty(descriptor.PropertyPath, index);
                        if (added)
                        {
                            autoFoldoutContext.ConsumeAutoInclude();
                            UpdateActiveFoldoutContextList(
                                activeFoldoutAutoContexts,
                                autoFoldoutContext
                            );
                        }
                    }
                }
                else
                {
                    foreach (FoldoutGroupContext context in explicitFoldoutContexts)
                    {
                        context.AddProperty(descriptor.PropertyPath, index);
                    }
                }

                if (descriptor.FoldoutEndAttributes.Count > 0)
                {
                    ApplyFoldoutGroupEnds(
                        descriptor.FoldoutEndAttributes,
                        activeFoldoutAutoContexts,
                        foldoutContextsByName
                    );
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
            List<WFoldoutGroupDefinition> foldoutDefinitions = new(
                foldoutContextsInDeclarationOrder.Count
            );
            Dictionary<string, List<WFoldoutGroupDefinition>> foldoutGroupsByAnchor = new(
                StringComparer.Ordinal
            );
            Dictionary<string, WFoldoutGroupDefinition> foldoutGroupsByName = new(
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

            foreach (FoldoutGroupContext context in foldoutContextsInDeclarationOrder)
            {
                if (context.PropertyCount == 0)
                {
                    continue;
                }

                WFoldoutGroupDefinition definition = context.ToDefinition();
                foldoutDefinitions.Add(definition);
                foldoutGroupsByName[definition.Name] = definition;

                List<WFoldoutGroupDefinition> anchored = foldoutGroupsByAnchor.GetOrAdd(
                    definition.AnchorPropertyPath
                );
                anchored.Add(definition);
            }

            List<WGroupDrawOperation> operations = BuildDrawOperations(
                descriptors,
                groupsByAnchor,
                foldoutGroupsByAnchor
            );
            return new WGroupLayout(
                operations,
                definitions,
                groupsByName,
                foldoutDefinitions,
                foldoutGroupsByName
            );
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
                List<WFoldoutGroupAttribute> foldoutAttributes =
                    CollectAttributes<WFoldoutGroupAttribute>(fieldInfo);
                List<WFoldoutGroupEndAttribute> foldoutEndAttributes =
                    CollectAttributes<WFoldoutGroupEndAttribute>(fieldInfo);
                PropertyDescriptor descriptor = new(
                    iterator.propertyPath,
                    groupAttributes,
                    endAttributes,
                    foldoutAttributes,
                    foldoutEndAttributes
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

            object[] raw = fieldInfo.GetCustomAttributes(typeof(TAttribute), true);
            if (raw.Length == 0)
            {
                return new List<TAttribute>();
            }

            List<TAttribute> attributes = new(raw.Length);
            foreach (object rawAttribute in raw)
            {
                if (rawAttribute is TAttribute attribute)
                {
                    attributes.Add(attribute);
                }
            }
            return attributes;
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
            for (int index = 0; index < activeAutoContexts.Count; index++)
            {
                GroupContext candidate = activeAutoContexts[index];
                if (!candidate.HasAutoIncludeBudget)
                {
                    activeAutoContexts.RemoveAt(index);
                    index--;
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

        private static void UpdateActiveFoldoutContextList(
            List<FoldoutGroupContext> activeAutoContexts,
            FoldoutGroupContext context
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

            InsertActiveFoldoutContext(activeAutoContexts, context);
        }

        private static void InsertActiveFoldoutContext(
            List<FoldoutGroupContext> activeAutoContexts,
            FoldoutGroupContext context
        )
        {
            for (int index = 0; index < activeAutoContexts.Count; index++)
            {
                FoldoutGroupContext existing = activeAutoContexts[index];
                if (existing.DeclarationOrder > context.DeclarationOrder)
                {
                    activeAutoContexts.Insert(index, context);
                    return;
                }
            }

            activeAutoContexts.Add(context);
        }

        private static FoldoutGroupContext SelectFoldoutAutoIncludeTarget(
            List<FoldoutGroupContext> activeAutoContexts,
            HashSet<FoldoutGroupContext> explicitContexts
        )
        {
            for (int index = 0; index < activeAutoContexts.Count; index++)
            {
                FoldoutGroupContext candidate = activeAutoContexts[index];
                if (!candidate.HasAutoIncludeBudget)
                {
                    activeAutoContexts.RemoveAt(index);
                    index--;
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

        private static void ApplyFoldoutGroupEnds(
            List<WFoldoutGroupEndAttribute> endAttributes,
            List<FoldoutGroupContext> activeAutoContexts,
            Dictionary<string, FoldoutGroupContext> contextsByName
        )
        {
            for (int index = 0; index < endAttributes.Count; index++)
            {
                WFoldoutGroupEndAttribute attribute = endAttributes[index];
                IReadOnlyList<string> groupNames = attribute.GroupNames;
                if (groupNames.Count == 0)
                {
                    if (activeAutoContexts.Count > 0)
                    {
                        FoldoutGroupContext last = activeAutoContexts[^1];
                        last.SetAutoInclude(new AutoIncludeConfiguration(false, 0));
                        UpdateActiveFoldoutContextList(activeAutoContexts, last);
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
                    if (
                        !contextsByName.TryGetValue(normalizedName, out FoldoutGroupContext context)
                    )
                    {
                        continue;
                    }

                    context.SetAutoInclude(new AutoIncludeConfiguration(false, 0));
                    UpdateActiveFoldoutContextList(activeAutoContexts, context);
                }
            }
        }

        private static string NormalizeGroupName(string groupName)
        {
            return string.IsNullOrWhiteSpace(groupName) ? string.Empty : groupName.Trim();
        }

        private static List<WGroupDrawOperation> BuildDrawOperations(
            List<PropertyDescriptor> descriptors,
            Dictionary<string, List<WGroupDefinition>> groupsByAnchor,
            Dictionary<string, List<WFoldoutGroupDefinition>> foldoutGroupsByAnchor
        )
        {
            List<WGroupDrawOperation> operations = new(descriptors.Count);
            HashSet<string> consumed = new(StringComparer.Ordinal);

            for (int index = 0; index < descriptors.Count; index++)
            {
                PropertyDescriptor descriptor = descriptors[index];
                string propertyPath = descriptor.PropertyPath;
                bool anchoredHandled = false;

                if (
                    foldoutGroupsByAnchor.TryGetValue(
                        propertyPath,
                        out List<WFoldoutGroupDefinition> anchoredFoldouts
                    )
                )
                {
                    anchoredHandled = true;
                    anchoredFoldouts.Sort(
                        (left, right) => left.DeclarationOrder.CompareTo(right.DeclarationOrder)
                    );
                    foreach (WFoldoutGroupDefinition definition in anchoredFoldouts)
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

                if (consumed.Contains(propertyPath))
                {
                    continue;
                }

                operations.Add(new WGroupDrawOperation(propertyPath));
                consumed.Add(propertyPath);
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
                if (_lookup.Contains(propertyPath))
                {
                    return false;
                }

                PropertyEntry entry = new(propertyPath, propertyIndex);
                _entries.Add(entry);
                _lookup.Add(propertyPath);

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

        private sealed class FoldoutGroupContext
        {
            private readonly List<PropertyEntry> _entries = new();
            private readonly HashSet<string> _lookup = new(StringComparer.Ordinal);

            internal FoldoutGroupContext(string name, int declarationOrder)
            {
                Name = name;
                DeclarationOrder = declarationOrder;
                AnchorIndex = int.MaxValue;
            }

            internal string Name { get; }

            internal int DeclarationOrder { get; }

            internal string DisplayName { get; private set; }

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
                WFoldoutGroupAttribute attribute,
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
                    string normalized = UnityHelpersSettings.EnsureWFoldoutGroupColorKey(
                        attribute.ColorKey
                    );
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        ColorKey = normalized;
                    }
                }

                StartCollapsed = attribute.StartCollapsed;
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
                if (_lookup.Contains(propertyPath))
                {
                    return false;
                }

                PropertyEntry entry = new(propertyPath, propertyIndex);
                _entries.Add(entry);
                _lookup.Add(propertyPath);

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

            internal WFoldoutGroupDefinition ToDefinition()
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

                return new WFoldoutGroupDefinition(
                    Name,
                    displayName,
                    ColorKey,
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
                List<WGroupEndAttribute> endAttributes,
                List<WFoldoutGroupAttribute> foldoutAttributes,
                List<WFoldoutGroupEndAttribute> foldoutEndAttributes
            )
            {
                PropertyPath = propertyPath;
                GroupAttributes = groupAttributes ?? new List<WGroupAttribute>();
                EndAttributes = endAttributes ?? new List<WGroupEndAttribute>();
                FoldoutAttributes = foldoutAttributes ?? new List<WFoldoutGroupAttribute>();
                FoldoutEndAttributes =
                    foldoutEndAttributes ?? new List<WFoldoutGroupEndAttribute>();
            }

            internal string PropertyPath { get; }

            internal List<WGroupAttribute> GroupAttributes { get; }

            internal List<WGroupEndAttribute> EndAttributes { get; }

            internal List<WFoldoutGroupAttribute> FoldoutAttributes { get; }

            internal List<WFoldoutGroupEndAttribute> FoldoutEndAttributes { get; }
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
        FoldoutGroup = 2,
    }

    internal readonly struct WGroupDrawOperation
    {
        internal WGroupDrawOperation(string propertyPath)
        {
            Type = WGroupDrawOperationType.Property;
            PropertyPath = propertyPath;
            Group = null;
            FoldoutGroup = null;
        }

        internal WGroupDrawOperation(WGroupDefinition group)
        {
            Type = WGroupDrawOperationType.Group;
            PropertyPath = null;
            Group = group;
            FoldoutGroup = null;
        }

        internal WGroupDrawOperation(WFoldoutGroupDefinition foldoutGroup)
        {
            Type = WGroupDrawOperationType.FoldoutGroup;
            PropertyPath = null;
            Group = null;
            FoldoutGroup = foldoutGroup;
        }

        internal WGroupDrawOperationType Type { get; }

        internal string PropertyPath { get; }

        internal WGroupDefinition Group { get; }

        internal WFoldoutGroupDefinition FoldoutGroup { get; }
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

    internal sealed class WFoldoutGroupDefinition
    {
        internal WFoldoutGroupDefinition(
            string name,
            string displayName,
            string colorKey,
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
            IReadOnlyDictionary<string, WGroupDefinition> groupsByName,
            IReadOnlyList<WFoldoutGroupDefinition> foldoutGroups,
            IReadOnlyDictionary<string, WFoldoutGroupDefinition> foldoutGroupsByName
        )
        {
            Operations = operations;
            Groups = groups;
            GroupsByName = groupsByName;
            FoldoutGroups = foldoutGroups;
            FoldoutGroupsByName = foldoutGroupsByName;
        }

        internal IReadOnlyList<WGroupDrawOperation> Operations { get; }

        internal IReadOnlyList<WGroupDefinition> Groups { get; }

        internal IReadOnlyDictionary<string, WGroupDefinition> GroupsByName { get; }

        internal IReadOnlyList<WFoldoutGroupDefinition> FoldoutGroups { get; }

        internal IReadOnlyDictionary<string, WFoldoutGroupDefinition> FoldoutGroupsByName { get; }

        internal bool TryGetGroup(string groupName, out WGroupDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                definition = null;
                return false;
            }

            return GroupsByName.TryGetValue(groupName.Trim(), out definition);
        }

        internal bool TryGetFoldoutGroup(string groupName, out WFoldoutGroupDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                definition = null;
                return false;
            }

            return FoldoutGroupsByName.TryGetValue(groupName.Trim(), out definition);
        }
    }
#endif
}
