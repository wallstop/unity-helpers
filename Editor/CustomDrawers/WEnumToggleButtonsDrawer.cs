namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    [CustomPropertyDrawer(typeof(WEnumToggleButtonsAttribute))]
    public sealed class WEnumToggleButtonsDrawer : PropertyDrawer
    {
        private const float ToolbarSpacing = 4f;
        private const float MinButtonWidth = 68f;
        private const float PaginationButtonWidth = 22f;
        private const float PaginationLabelMinWidth = 80f;
        private const float SummarySpacing = 2f;

        private static readonly GUIContent FirstPageContent = EditorGUIUtility.TrTextContent(
            "<<",
            "First Page"
        );
        private static readonly GUIContent PreviousPageContent = EditorGUIUtility.TrTextContent(
            "<",
            "Previous Page"
        );
        private static readonly GUIContent NextPageContent = EditorGUIUtility.TrTextContent(
            ">",
            "Next Page"
        );
        private static readonly GUIContent LastPageContent = EditorGUIUtility.TrTextContent(
            ">>",
            "Last Page"
        );
        private static readonly GUIStyle SummaryStyle = new(EditorStyles.wordWrappedMiniLabel)
        {
            fontStyle = FontStyle.Italic,
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            WEnumToggleButtonsAttribute toggleAttribute = attribute as WEnumToggleButtonsAttribute;
            if (toggleAttribute == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(property, fieldInfo);
            if (toggleSet.IsEmpty)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float extraToolbarHeight = 0f;
            if (
                toggleSet.SupportsMultipleSelection
                && (toggleAttribute.ShowSelectAll || toggleAttribute.ShowSelectNone)
            )
            {
                extraToolbarHeight = EditorGUIUtility.singleLineHeight + ToolbarSpacing;
            }

            bool usePagination = WEnumToggleButtonsUtility.ShouldPaginate(
                toggleAttribute,
                toggleSet.Options.Count,
                out int pageSize
            );
            int startIndex = 0;
            int visibleCount = toggleSet.Options.Count;
            SelectionSummary summary = SelectionSummary.None;
            if (usePagination)
            {
                WEnumToggleButtonsPagination.PaginationState state =
                    WEnumToggleButtonsPagination.GetState(
                        property,
                        toggleSet.Options.Count,
                        pageSize
                    );
                startIndex = state.StartIndex;
                visibleCount = state.VisibleCount;
                extraToolbarHeight += EditorGUIUtility.singleLineHeight + ToolbarSpacing;
                summary = BuildSelectionSummary(
                    toggleSet,
                    property,
                    startIndex,
                    visibleCount,
                    true
                );
            }

            float estimatedWidth =
                EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 20f;
            LayoutMetrics metrics = WEnumToggleButtonsUtility.CalculateLayout(
                toggleAttribute.ButtonsPerRow,
                visibleCount,
                estimatedWidth,
                EditorGUIUtility.singleLineHeight,
                ToolbarSpacing,
                MinButtonWidth
            );

            if (summary.HasSummary)
            {
                float summaryHeight = SummaryStyle.CalcHeight(summary.Content, estimatedWidth);
                extraToolbarHeight += summaryHeight + SummarySpacing;
            }

            return extraToolbarHeight + metrics.TotalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            WEnumToggleButtonsAttribute toggleAttribute = attribute as WEnumToggleButtonsAttribute;
            if (toggleAttribute == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(property, fieldInfo);
            if (toggleSet.IsEmpty)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            bool usePagination = WEnumToggleButtonsUtility.ShouldPaginate(
                toggleAttribute,
                toggleSet.Options.Count,
                out int pageSize
            );
            int startIndex = 0;
            int visibleCount = toggleSet.Options.Count;
            WEnumToggleButtonsPagination.PaginationState paginationState = null;
            if (usePagination)
            {
                paginationState = WEnumToggleButtonsPagination.GetState(
                    property,
                    toggleSet.Options.Count,
                    pageSize
                );
                startIndex = paginationState.StartIndex;
                visibleCount = paginationState.VisibleCount;
            }
            SelectionSummary summary = BuildSelectionSummary(
                toggleSet,
                property,
                startIndex,
                visibleCount,
                usePagination
            );

            EditorGUI.BeginProperty(position, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            float currentY = contentRect.y;
            if (
                toggleSet.SupportsMultipleSelection
                && (toggleAttribute.ShowSelectAll || toggleAttribute.ShowSelectNone)
            )
            {
                Rect toolbarRect = new(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    EditorGUIUtility.singleLineHeight
                );
                DrawToolbar(toolbarRect, toggleSet, property, toggleAttribute);
                currentY += toolbarRect.height + ToolbarSpacing;
            }

            if (usePagination)
            {
                Rect paginationRect = new(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    EditorGUIUtility.singleLineHeight
                );
                DrawPagination(paginationRect, paginationState);
                currentY += paginationRect.height + ToolbarSpacing;
            }

            if (summary.HasSummary)
            {
                float summaryHeight = SummaryStyle.CalcHeight(summary.Content, contentRect.width);
                Rect summaryRect = new(contentRect.x, currentY, contentRect.width, summaryHeight);
                EditorGUI.LabelField(summaryRect, summary.Content, SummaryStyle);
                currentY += summaryHeight + SummarySpacing;
            }

            LayoutMetrics metrics = WEnumToggleButtonsUtility.CalculateLayout(
                toggleAttribute.ButtonsPerRow,
                visibleCount,
                contentRect.width,
                EditorGUIUtility.singleLineHeight,
                ToolbarSpacing,
                MinButtonWidth
            );

            Rect buttonsRect = new(contentRect.x, currentY, contentRect.width, metrics.TotalHeight);

            for (int index = 0; index < visibleCount; index += 1)
            {
                ToggleOption option = toggleSet.Options[startIndex + index];
                Rect buttonRect = metrics.GetItemRect(buttonsRect, index);
                DrawToggle(buttonRect, toggleSet, property, metrics, option, index, visibleCount);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawToolbar(
            Rect rect,
            ToggleSet toggleSet,
            SerializedProperty property,
            WEnumToggleButtonsAttribute toggleAttribute
        )
        {
            bool drawSelectAll = toggleAttribute.ShowSelectAll;
            bool drawSelectNone = toggleAttribute.ShowSelectNone;

            if (!drawSelectAll && !drawSelectNone)
            {
                return;
            }

            bool alignedPair = drawSelectAll && drawSelectNone;
            float halfWidth = rect.width * 0.5f;

            if (drawSelectAll)
            {
                Rect selectAllRect = alignedPair
                    ? new Rect(rect.x, rect.y, Mathf.Floor(halfWidth), rect.height)
                    : rect;

                bool allActive = WEnumToggleButtonsUtility.AreAllFlagsSelected(property, toggleSet);
                bool selectAllPressed = GUI.Toggle(
                    selectAllRect,
                    allActive,
                    new GUIContent("All"),
                    alignedPair ? EditorStyles.miniButtonLeft : EditorStyles.miniButton
                );

                if (selectAllPressed && !allActive)
                {
                    WEnumToggleButtonsUtility.ApplySelectAll(property, toggleSet);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            if (drawSelectNone)
            {
                Rect selectNoneRect;
                if (alignedPair)
                {
                    float width = rect.width - Mathf.Floor(rect.width * 0.5f);
                    selectNoneRect = new Rect(
                        rect.x + Mathf.Floor(rect.width * 0.5f),
                        rect.y,
                        width,
                        rect.height
                    );
                }
                else
                {
                    selectNoneRect = rect;
                }

                bool noneActive = WEnumToggleButtonsUtility.AreNoFlagsSelected(property);
                bool selectNonePressed = GUI.Toggle(
                    selectNoneRect,
                    noneActive,
                    new GUIContent("None"),
                    alignedPair ? EditorStyles.miniButtonRight : EditorStyles.miniButton
                );

                if (selectNonePressed && !noneActive)
                {
                    WEnumToggleButtonsUtility.ApplySelectNone(property);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private static void DrawPagination(
            Rect rect,
            WEnumToggleButtonsPagination.PaginationState state
        )
        {
            if (state.TotalPages <= 1)
            {
                return;
            }

            float spacing = ToolbarSpacing;
            float buttonWidth = Mathf.Min(PaginationButtonWidth, rect.width * 0.2f);
            float labelWidth = Mathf.Max(
                PaginationLabelMinWidth,
                rect.width - (buttonWidth * 4f) - spacing * 4f
            );

            Rect firstRect = new(rect.x, rect.y, buttonWidth, rect.height);
            Rect prevRect = new(firstRect.xMax + spacing, rect.y, buttonWidth, rect.height);
            Rect labelRect = new(prevRect.xMax + spacing, rect.y, labelWidth, rect.height);
            Rect nextRect = new(labelRect.xMax + spacing, rect.y, buttonWidth, rect.height);
            Rect lastRect = new(nextRect.xMax + spacing, rect.y, buttonWidth, rect.height);

            if (lastRect.xMax > rect.xMax)
            {
                float overflow = lastRect.xMax - rect.xMax;
                firstRect.x -= overflow * 0.5f;
                prevRect.x -= overflow * 0.5f;
                labelRect.x -= overflow * 0.5f;
                nextRect.x -= overflow * 0.5f;
                lastRect.x -= overflow * 0.5f;
            }

            EditorGUI.BeginDisabledGroup(state.PageIndex <= 0);
            if (GUI.Button(firstRect, FirstPageContent, EditorStyles.miniButtonLeft))
            {
                state.PageIndex = 0;
            }

            if (GUI.Button(prevRect, PreviousPageContent, EditorStyles.miniButtonMid))
            {
                state.PageIndex = Mathf.Max(0, state.PageIndex - 1);
            }
            EditorGUI.EndDisabledGroup();

            string pageLabel = $"Page {state.PageIndex + 1} / {state.TotalPages}";
            GUI.Label(labelRect, pageLabel, EditorStyles.miniLabel);

            EditorGUI.BeginDisabledGroup(state.PageIndex >= state.TotalPages - 1);
            if (GUI.Button(nextRect, NextPageContent, EditorStyles.miniButtonMid))
            {
                state.PageIndex = Mathf.Min(state.TotalPages - 1, state.PageIndex + 1);
            }

            if (GUI.Button(lastRect, LastPageContent, EditorStyles.miniButtonRight))
            {
                state.PageIndex = state.TotalPages - 1;
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawToggle(
            Rect rect,
            ToggleSet toggleSet,
            SerializedProperty property,
            LayoutMetrics metrics,
            ToggleOption option,
            int visibleIndex,
            int visibleCount
        )
        {
            bool isActive = WEnumToggleButtonsUtility.IsOptionActive(property, toggleSet, option);

            GUIStyle style = ResolveButtonStyle(visibleIndex, visibleCount, metrics.Columns);
            bool newState = GUI.Toggle(rect, isActive, option.Label, style);

            if (newState == isActive)
            {
                return;
            }

            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, option, newState);
            property.serializedObject.ApplyModifiedProperties();
        }

        private static SelectionSummary BuildSelectionSummary(
            ToggleSet toggleSet,
            SerializedProperty property,
            int startIndex,
            int visibleCount,
            bool usePagination
        )
        {
            if (!usePagination || toggleSet.IsEmpty || property == null)
            {
                return SelectionSummary.None;
            }

            int endIndex = startIndex + visibleCount;
            List<string> outOfView = null;
            IReadOnlyList<ToggleOption> options = toggleSet.Options;
            for (int index = 0; index < options.Count; index += 1)
            {
                ToggleOption option = options[index];
                if (!WEnumToggleButtonsUtility.IsOptionActive(property, toggleSet, option))
                {
                    continue;
                }

                if (index >= startIndex && index < endIndex)
                {
                    continue;
                }

                outOfView ??= new List<string>();
                outOfView.Add(option.Label);
            }

            if (outOfView == null || outOfView.Count == 0)
            {
                return SelectionSummary.None;
            }

            string joined = string.Join(", ", outOfView.ToArray());
            string text = $"Current (out of view): {joined}";
            return new SelectionSummary(true, new GUIContent(text));
        }

        private static GUIStyle ResolveButtonStyle(int index, int total, int columns)
        {
            if (columns <= 1)
            {
                return EditorStyles.miniButton;
            }

            int columnIndex = index % columns;
            bool isFirst = columnIndex == 0;
            bool isLast = columnIndex == columns - 1 || index == total - 1;

            if (isFirst && isLast)
            {
                return EditorStyles.miniButton;
            }

            if (isFirst)
            {
                return EditorStyles.miniButtonLeft;
            }

            if (isLast)
            {
                return EditorStyles.miniButtonRight;
            }

            return EditorStyles.miniButtonMid;
        }

        private readonly struct SelectionSummary
        {
            internal static SelectionSummary None { get; } = new(false, GUIContent.none);

            internal SelectionSummary(bool hasSummary, GUIContent content)
            {
                HasSummary = hasSummary;
                Content = content ?? GUIContent.none;
            }

            internal bool HasSummary { get; }

            internal GUIContent Content { get; }
        }
    }

    internal static class WEnumToggleButtonsUtility
    {
        internal static int ResolvePageSize(WEnumToggleButtonsAttribute attribute)
        {
            int overrideSize = attribute?.PageSize ?? 0;
            if (overrideSize > 0)
            {
                return Mathf.Clamp(
                    overrideSize,
                    UnityHelpersSettings.MinPageSize,
                    UnityHelpersSettings.MaxPageSize
                );
            }

            return Mathf.Clamp(
                UnityHelpersSettings.GetEnumToggleButtonsPageSize(),
                UnityHelpersSettings.MinPageSize,
                UnityHelpersSettings.MaxPageSize
            );
        }

        internal static bool ShouldPaginate(
            WEnumToggleButtonsAttribute attribute,
            int optionCount,
            out int pageSize
        )
        {
            pageSize = ResolvePageSize(attribute);
            if (attribute is { EnablePagination: false })
            {
                return false;
            }

            return optionCount > pageSize;
        }

        internal static ToggleSet CreateToggleSet(SerializedProperty property, FieldInfo fieldInfo)
        {
            if (property == null)
            {
                return ToggleSet.Empty;
            }

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                Type enumType = ResolveEnumType(fieldInfo);
                if (enumType == null)
                {
                    return ToggleSet.Empty;
                }

                bool isFlags = enumType.IsDefined(typeof(FlagsAttribute), true);
                ToggleOption[] enumOptions = BuildEnumOptions(enumType, isFlags);
                if (enumOptions.Length == 0)
                {
                    return ToggleSet.Empty;
                }

                ToggleSource source = isFlags ? ToggleSource.FlaggedEnum : ToggleSource.Enum;
                return new ToggleSet(enumOptions, isFlags, source, enumType);
            }

            ToggleOption[] dropdownOptions = BuildDropdownOptions(fieldInfo);
            if (dropdownOptions.Length == 0)
            {
                return ToggleSet.Empty;
            }

            Type valueType = fieldInfo != null ? fieldInfo.FieldType : null;
            return new ToggleSet(dropdownOptions, false, ToggleSource.Dropdown, valueType);
        }

        internal static LayoutMetrics CalculateLayout(
            int requestedPerRow,
            int optionCount,
            float availableWidth,
            float lineHeight,
            float spacing,
            float minWidth
        )
        {
            if (optionCount <= 0)
            {
                return new LayoutMetrics(
                    1,
                    1,
                    Mathf.Max(minWidth, availableWidth),
                    lineHeight,
                    spacing,
                    lineHeight
                );
            }

            int columns =
                requestedPerRow > 0
                    ? requestedPerRow
                    : DetermineAutoColumns(availableWidth, spacing, minWidth);
            columns = Mathf.Clamp(columns, 1, optionCount);

            int rows = Mathf.CeilToInt(optionCount / (float)columns);
            float workingWidth = availableWidth;
            if (workingWidth <= 0f)
            {
                workingWidth = columns * minWidth + (columns - 1) * spacing;
            }

            float buttonWidth = (workingWidth - (columns - 1) * spacing) / columns;
            if (buttonWidth < minWidth)
            {
                buttonWidth = minWidth;
            }

            float totalHeight = rows * lineHeight + Mathf.Max(0, rows - 1) * spacing;
            return new LayoutMetrics(columns, rows, buttonWidth, lineHeight, spacing, totalHeight);
        }

        internal static bool IsOptionActive(
            SerializedProperty property,
            ToggleSet toggleSet,
            ToggleOption option
        )
        {
            switch (toggleSet.Source)
            {
                case ToggleSource.FlaggedEnum:
                    ulong mask = ReadEnumValue(property);
                    if (option.IsZeroFlag)
                    {
                        return mask == 0UL;
                    }
                    return (mask & option.FlagValue) == option.FlagValue;
                case ToggleSource.Enum:
                    if (toggleSet.ValueType == null)
                    {
                        return false;
                    }
                    object enumValue = Enum.ToObject(toggleSet.ValueType, ReadEnumValue(property));
                    return Equals(enumValue, option.Value);
                case ToggleSource.Dropdown:
                    return DropdownValueEquals(property, option.Value);
                default:
                    return false;
            }
        }

        internal static void ApplyOption(
            SerializedProperty property,
            ToggleSet toggleSet,
            ToggleOption option,
            bool desiredState
        )
        {
            switch (toggleSet.Source)
            {
                case ToggleSource.FlaggedEnum:
                    ApplyFlagOption(property, option, desiredState);
                    break;
                case ToggleSource.Enum:
                    ApplyEnumOption(property, option);
                    break;
                case ToggleSource.Dropdown:
                    ApplyDropdownOption(property, option);
                    break;
            }
        }

        internal static bool AreAllFlagsSelected(SerializedProperty property, ToggleSet toggleSet)
        {
            if (!toggleSet.SupportsMultipleSelection || toggleSet.Options.Count == 0)
            {
                return false;
            }

            ulong targetMask = 0UL;
            for (int index = 0; index < toggleSet.Options.Count; index += 1)
            {
                ToggleOption option = toggleSet.Options[index];
                if (option.FlagValue == 0UL)
                {
                    continue;
                }
                targetMask |= option.FlagValue;
            }

            if (targetMask == 0UL)
            {
                return false;
            }

            ulong current = ReadEnumValue(property);
            return (current & targetMask) == targetMask;
        }

        internal static bool AreNoFlagsSelected(SerializedProperty property)
        {
            return ReadEnumValue(property) == 0UL;
        }

        internal static void ApplySelectAll(SerializedProperty property, ToggleSet toggleSet)
        {
            if (!toggleSet.SupportsMultipleSelection)
            {
                return;
            }

            ulong combined = 0UL;
            for (int index = 0; index < toggleSet.Options.Count; index += 1)
            {
                ToggleOption option = toggleSet.Options[index];
                if (option.FlagValue == 0UL)
                {
                    continue;
                }
                combined |= option.FlagValue;
            }

            SetEnumValue(property, combined);
        }

        internal static void ApplySelectNone(SerializedProperty property)
        {
            SetEnumValue(property, 0UL);
        }

        private static Type ResolveEnumType(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                return null;
            }

            Type enumType = fieldInfo.FieldType;
            if (enumType.IsArray)
            {
                enumType = enumType.GetElementType();
            }

            if (enumType != null)
            {
                Type nullableUnderlying = Nullable.GetUnderlyingType(enumType);
                if (nullableUnderlying != null)
                {
                    enumType = nullableUnderlying;
                }
            }

            if (enumType is not { IsEnum: true })
            {
                return null;
            }

            return enumType;
        }

        private static ToggleOption[] BuildEnumOptions(Type enumType, bool isFlags)
        {
            Array values = Enum.GetValues(enumType);
            List<ToggleOption> options = new(values.Length);

            for (int index = 0; index < values.Length; index += 1)
            {
                object value = values.GetValue(index);
                if (value == null)
                {
                    continue;
                }

                string name = Enum.GetName(enumType, value);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                ulong numericValue = ConvertToUInt64(value);
                if (isFlags && numericValue != 0UL && !IsPowerOfTwo(numericValue))
                {
                    continue;
                }

                string label = ObjectNames.NicifyVariableName(name);
                ToggleOption option = new(label, value, numericValue, numericValue == 0UL);
                options.Add(option);
            }

            return options.ToArray();
        }

        private static ToggleOption[] BuildDropdownOptions(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                return Array.Empty<ToggleOption>();
            }

            ValueDropdownAttribute valueDropdownAttribute = GetAttribute<ValueDropdownAttribute>(
                fieldInfo
            );
            if (valueDropdownAttribute != null)
            {
                object[] values = valueDropdownAttribute.Options ?? Array.Empty<object>();
                return BuildGenericOptions(values);
            }

            IntDropdownAttribute intDropdownAttribute = GetAttribute<IntDropdownAttribute>(
                fieldInfo
            );
            if (intDropdownAttribute != null)
            {
                int[] values = intDropdownAttribute.Options ?? Array.Empty<int>();
                ToggleOption[] options = new ToggleOption[values.Length];
                for (int index = 0; index < values.Length; index += 1)
                {
                    int value = values[index];
                    string label = FormatOption(value);
                    ToggleOption option = new(label, value, 0UL, value == 0);
                    options[index] = option;
                }
                return options;
            }

            StringInListAttribute stringInListAttribute = GetAttribute<StringInListAttribute>(
                fieldInfo
            );
            if (stringInListAttribute != null)
            {
                string[] values = stringInListAttribute.List ?? Array.Empty<string>();
                ToggleOption[] options = new ToggleOption[values.Length];
                for (int index = 0; index < values.Length; index += 1)
                {
                    string value = values[index] ?? string.Empty;
                    string label = FormatOption(value);
                    ToggleOption option = new(label, value, 0UL, false);
                    options[index] = option;
                }
                return options;
            }

            return Array.Empty<ToggleOption>();
        }

        private static ToggleOption[] BuildGenericOptions(object[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Array.Empty<ToggleOption>();
            }

            ToggleOption[] options = new ToggleOption[values.Length];
            for (int index = 0; index < values.Length; index += 1)
            {
                object value = values[index];
                string label = FormatOption(value);
                bool isZero = IsZeroEquivalent(value);
                ToggleOption option = new(label, value, 0UL, isZero);
                options[index] = option;
            }

            return options;
        }

        private static int DetermineAutoColumns(float availableWidth, float spacing, float minWidth)
        {
            if (availableWidth <= 0f)
            {
                return 1;
            }

            float effectiveWidth = minWidth + spacing;
            int columns = Mathf.FloorToInt((availableWidth + spacing) / effectiveWidth);
            if (columns < 1)
            {
                columns = 1;
            }

            return columns;
        }

        private static bool DropdownValueEquals(SerializedProperty property, object candidate)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return CompareLong(property.longValue, candidate);
                case SerializedPropertyType.Float:
                    return CompareDouble(property.doubleValue, candidate);
                case SerializedPropertyType.String:
                    return CompareString(property.stringValue, candidate);
                case SerializedPropertyType.ObjectReference:
                    return Equals(property.objectReferenceValue, candidate as UnityEngine.Object);
                case SerializedPropertyType.Enum:
                    return CompareLong(property.longValue, candidate);
                default:
                    return false;
            }
        }

        private static void ApplyFlagOption(
            SerializedProperty property,
            ToggleOption option,
            bool desiredState
        )
        {
            ulong current = ReadEnumValue(property);
            if (option.IsZeroFlag)
            {
                if (desiredState && current != 0UL)
                {
                    SetEnumValue(property, 0UL);
                }
                return;
            }

            ulong mask = option.FlagValue;
            if (desiredState)
            {
                ulong combined = current | mask;
                SetEnumValue(property, combined);
                return;
            }

            ulong cleared = current & ~mask;
            SetEnumValue(property, cleared);
        }

        private static void ApplyEnumOption(SerializedProperty property, ToggleOption option)
        {
            if (option.Value == null)
            {
                return;
            }

            if (option.Value is Enum enumValue)
            {
                long numeric = Convert.ToInt64(enumValue, CultureInfo.InvariantCulture);
                property.longValue = numeric;
                return;
            }

            if (TryConvertToInt64(option.Value, out long longValue))
            {
                property.longValue = longValue;
            }
        }

        private static void ApplyDropdownOption(SerializedProperty property, ToggleOption option)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (TryConvertToInt64(option.Value, out long longValue))
                    {
                        property.longValue = longValue;
                    }
                    break;
                case SerializedPropertyType.Float:
                    if (TryConvertToDouble(option.Value, out double doubleValue))
                    {
                        property.doubleValue = doubleValue;
                    }
                    break;
                case SerializedPropertyType.String:
                    property.stringValue =
                        option.Value == null
                            ? string.Empty
                            : Convert.ToString(option.Value, CultureInfo.InvariantCulture)
                                ?? string.Empty;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = option.Value as UnityEngine.Object;
                    break;
                case SerializedPropertyType.Enum:
                    if (TryConvertToInt64(option.Value, out long enumLong))
                    {
                        property.longValue = enumLong;
                    }
                    break;
            }
        }

        private static bool CompareLong(long current, object candidate)
        {
            if (!TryConvertToInt64(candidate, out long candidateValue))
            {
                return false;
            }

            return current == candidateValue;
        }

        private static bool CompareDouble(double current, object candidate)
        {
            if (!TryConvertToDouble(candidate, out double candidateValue))
            {
                return false;
            }

            return Math.Abs(current - candidateValue) <= 0.000001d;
        }

        private static bool CompareString(string current, object candidate)
        {
            string candidateString =
                candidate == null
                    ? string.Empty
                    : Convert.ToString(candidate, CultureInfo.InvariantCulture) ?? string.Empty;
            return string.Equals(
                current ?? string.Empty,
                candidateString,
                StringComparison.Ordinal
            );
        }

        private static bool TryConvertToInt64(object value, out long converted)
        {
            converted = 0L;
            if (value == null)
            {
                return false;
            }

            try
            {
                converted = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryConvertToDouble(object value, out double converted)
        {
            converted = 0d;
            if (value == null)
            {
                return false;
            }

            try
            {
                converted = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsZeroEquivalent(object value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is int intValue)
            {
                return intValue == 0;
            }

            if (value is long longValue)
            {
                return longValue == 0L;
            }

            if (value is short shortValue)
            {
                return shortValue == 0;
            }

            if (value is byte byteValue)
            {
                return byteValue == 0;
            }

            return false;
        }

        private static string FormatOption(object value)
        {
            if (value == null)
            {
                return "(null)";
            }

            if (value is string stringValue)
            {
                return stringValue;
            }

            if (value is Enum enumValue)
            {
                string name = enumValue.ToString();
                return ObjectNames.NicifyVariableName(name);
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            string fallback = value.ToString();
            return string.IsNullOrEmpty(fallback) ? "(empty)" : fallback;
        }

        private static ulong ReadEnumValue(SerializedProperty property)
        {
            return unchecked((ulong)property.longValue);
        }

        private static void SetEnumValue(SerializedProperty property, ulong value)
        {
            property.longValue = unchecked((long)value);
        }

        private static ulong ConvertToUInt64(object value)
        {
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        private static bool IsPowerOfTwo(ulong value)
        {
            return value != 0UL && (value & (value - 1UL)) == 0UL;
        }

        private static TAttribute GetAttribute<TAttribute>(FieldInfo fieldInfo)
            where TAttribute : Attribute
        {
            return ReflectionHelpers.GetAttributeSafe<TAttribute>(fieldInfo, true);
        }

        private readonly struct SelectionSummary
        {
            internal static SelectionSummary None { get; } =
                new SelectionSummary(false, GUIContent.none);

            internal SelectionSummary(bool hasSummary, GUIContent content)
            {
                HasSummary = hasSummary;
                Content = content ?? GUIContent.none;
            }

            internal bool HasSummary { get; }

            internal GUIContent Content { get; }
        }
    }

    internal enum ToggleSource
    {
        None,
        FlaggedEnum,
        Enum,
        Dropdown,
    }

    internal readonly struct ToggleOption
    {
        internal ToggleOption(string label, object value, ulong flagValue, bool isZeroFlag)
        {
            Label = string.IsNullOrEmpty(label) ? "(Unnamed)" : label;
            Value = value;
            FlagValue = flagValue;
            IsZeroFlag = isZeroFlag;
        }

        internal string Label { get; }

        internal object Value { get; }

        internal ulong FlagValue { get; }

        internal bool IsZeroFlag { get; }
    }

    internal readonly struct ToggleSet
    {
        private readonly ToggleOption[] _options;

        internal ToggleSet(
            ToggleOption[] options,
            bool supportsMultipleSelection,
            ToggleSource source,
            Type valueType
        )
        {
            _options = options ?? Array.Empty<ToggleOption>();
            SupportsMultipleSelection = supportsMultipleSelection;
            Source = source;
            ValueType = valueType;
        }

        internal static ToggleSet Empty { get; } =
            new(Array.Empty<ToggleOption>(), false, ToggleSource.None, null);

        internal IReadOnlyList<ToggleOption> Options => _options;

        internal bool SupportsMultipleSelection { get; }

        internal ToggleSource Source { get; }

        internal Type ValueType { get; }

        internal bool IsEmpty => _options == null || _options.Length == 0;
    }

    internal readonly struct LayoutMetrics
    {
        internal LayoutMetrics(
            int columns,
            int rows,
            float buttonWidth,
            float buttonHeight,
            float spacing,
            float totalHeight
        )
        {
            Columns = columns;
            Rows = rows;
            ButtonWidth = buttonWidth;
            ButtonHeight = buttonHeight;
            Spacing = spacing;
            TotalHeight = totalHeight;
        }

        internal int Columns { get; }

        internal int Rows { get; }

        internal float ButtonWidth { get; }

        internal float ButtonHeight { get; }

        internal float Spacing { get; }

        internal float TotalHeight { get; }

        internal Rect GetItemRect(Rect bounds, int index)
        {
            int row = index / Columns;
            int column = index % Columns;
            float x = bounds.x + column * (ButtonWidth + Spacing);
            float y = bounds.y + row * (ButtonHeight + Spacing);
            return new Rect(x, y, ButtonWidth, ButtonHeight);
        }
    }

    internal static class WEnumToggleButtonsPagination
    {
        internal sealed class PaginationState
        {
            private int _pageIndex;

            internal int PageSize { get; set; }

            internal int TotalItems { get; set; }

            internal int PageIndex
            {
                get => _pageIndex;
                set => _pageIndex = value;
            }

            internal int TotalPages
            {
                get
                {
                    if (PageSize <= 0)
                    {
                        return 1;
                    }

                    return Mathf.Max(1, Mathf.CeilToInt(TotalItems / (float)PageSize));
                }
            }

            internal int StartIndex
            {
                get
                {
                    if (TotalItems <= 0 || PageSize <= 0)
                    {
                        return 0;
                    }

                    int clampedIndex = Mathf.Clamp(PageIndex, 0, TotalPages - 1);
                    return clampedIndex * PageSize;
                }
            }

            internal int VisibleCount
            {
                get
                {
                    if (TotalItems <= 0 || PageSize <= 0)
                    {
                        return 0;
                    }

                    int clampedIndex = Mathf.Clamp(PageIndex, 0, TotalPages - 1);
                    int start = clampedIndex * PageSize;
                    return Mathf.Clamp(TotalItems - start, 0, PageSize);
                }
            }
        }

        private static readonly Dictionary<string, PaginationState> States = new(
            StringComparer.Ordinal
        );

        internal static PaginationState GetState(
            SerializedProperty property,
            int totalItems,
            int pageSize
        )
        {
            string key = BuildKey(property);
            PaginationState state = States.GetOrAdd(key);

            state.PageSize = Mathf.Max(1, pageSize);
            state.TotalItems = Mathf.Max(0, totalItems);

            int totalPages = state.TotalPages;
            if (state.PageIndex >= totalPages)
            {
                state.PageIndex = totalPages - 1;
            }

            if (state.PageIndex < 0)
            {
                state.PageIndex = 0;
            }

            return state;
        }

        internal static void Reset()
        {
            States.Clear();
        }

        private static string BuildKey(SerializedProperty property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            SerializedObject serializedObject = property.serializedObject;
            UnityEngine.Object target = serializedObject?.targetObject;
            if (target == null)
            {
                return property.propertyPath ?? string.Empty;
            }

            string instancePart = target
                .GetInstanceID()
                .ToString("X8", CultureInfo.InvariantCulture);
            string pathPart = property.propertyPath ?? string.Empty;
            return instancePart + ":" + pathPart;
        }
    }
}
