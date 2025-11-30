namespace WallstopStudios.UnityHelpers.Editor.Utils.WButton
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.AnimatedValues;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Utils;

    internal enum WButtonPlacement
    {
        Top = 0,
        Bottom = 1,
    }

    internal static class WButtonGUI
    {
        private static readonly Dictionary<int, int> GroupCounts = new();
        private static readonly Dictionary<int, string> GroupNames = new();
        private static readonly Dictionary<int, AnimBool> FoldoutAnimations = new();
        private static readonly GUIContent ClearHistoryContent = new("Clear History");
        private static readonly GUIContent RecentResultsHeaderContent = new("Recent Results");
        private const float ClearHistoryButtonPadding = 12f;
        private const float ClearHistoryMinWidth = 96f;
        private const float ClearHistorySpacing = 6f;

        internal static bool DrawButtons(
            Editor editor,
            WButtonPlacement placement,
            IDictionary<int, WButtonPaginationState> paginationStates,
            IDictionary<int, bool> foldoutStates,
            UnityHelpersSettings.WButtonFoldoutBehavior foldoutBehavior,
            List<WButtonMethodContext> triggeredContexts = null
        )
        {
            if (editor == null)
            {
                throw new ArgumentNullException(nameof(editor));
            }

            UnityEngine.Object[] targets = editor.targets;
            if (targets == null || targets.Length == 0 || targets[0] == null)
            {
                return false;
            }

            Type inspectedType = targets[0].GetType();
            IReadOnlyList<WButtonMethodMetadata> metadataList = WButtonMetadataCache.GetMetadata(
                inspectedType
            );
            if (metadataList.Count == 0)
            {
                return false;
            }

            using PooledResource<List<WButtonMethodContext>> contextsLease =
                Buffers<WButtonMethodContext>.GetList(
                    metadataList.Count,
                    out List<WButtonMethodContext> contexts
                );
            BuildContexts(metadataList, targets, contexts);
            if (contexts.Count == 0)
            {
                return false;
            }

            SortedDictionary<int, List<WButtonMethodContext>> groups = new();
            Dictionary<int, PooledResource<List<WButtonMethodContext>>> groupLeases = new();
            GroupByDrawOrder(contexts, groups, groupLeases);

            try
            {
                bool anyDrawn = false;
                GroupCounts.Clear();
                GroupNames.Clear();
                foreach (KeyValuePair<int, List<WButtonMethodContext>> entry in groups)
                {
                    List<WButtonMethodContext> groupContexts = entry.Value;
                    GroupCounts[entry.Key] = groupContexts?.Count ?? 0;
                    string resolvedGroupName = ResolveGroupName(groupContexts);
                    if (!string.IsNullOrWhiteSpace(resolvedGroupName))
                    {
                        GroupNames[entry.Key] = resolvedGroupName;
                    }
                }

                foreach (KeyValuePair<int, List<WButtonMethodContext>> entry in groups)
                {
                    int drawOrder = entry.Key;
                    bool drawOnTop = drawOrder >= -1;
                    if (
                        (placement == WButtonPlacement.Top && drawOnTop)
                        || (placement == WButtonPlacement.Bottom && !drawOnTop)
                    )
                    {
                        DrawGroup(
                            drawOrder,
                            entry.Value,
                            paginationStates,
                            foldoutStates,
                            foldoutBehavior,
                            triggeredContexts
                        );
                        anyDrawn = true;
                    }
                }

                return anyDrawn;
            }
            finally
            {
                foreach (
                    KeyValuePair<
                        int,
                        PooledResource<List<WButtonMethodContext>>
                    > entry in groupLeases
                )
                {
                    entry.Value.Dispose();
                }
            }
        }

        internal static Dictionary<int, int> GetGroupCountsForTesting()
        {
            return GroupCounts;
        }

        internal static Dictionary<int, string> GetGroupNamesForTesting()
        {
            return GroupNames;
        }

        private static void BuildContexts(
            IReadOnlyList<WButtonMethodMetadata> metadataList,
            UnityEngine.Object[] targets,
            List<WButtonMethodContext> contexts
        )
        {
            contexts.Clear();
            for (int index = 0; index < metadataList.Count; index++)
            {
                WButtonMethodMetadata metadata = metadataList[index];
                WButtonMethodState[] states = new WButtonMethodState[targets.Length];
                UnityEngine.Object[] contextTargets = new UnityEngine.Object[targets.Length];
                bool allValid = true;
                for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                {
                    UnityEngine.Object target = targets[targetIndex];
                    if (target == null)
                    {
                        allValid = false;
                        break;
                    }

                    WButtonTargetState targetState = WButtonStateRepository.GetOrCreate(target);
                    states[targetIndex] = targetState.GetOrCreateMethodState(metadata);
                    contextTargets[targetIndex] = target;
                }

                if (!allValid)
                {
                    continue;
                }

                contexts.Add(new WButtonMethodContext(metadata, states, contextTargets));
            }
        }

        private static void GroupByDrawOrder(
            List<WButtonMethodContext> contexts,
            SortedDictionary<int, List<WButtonMethodContext>> groups,
            Dictionary<int, PooledResource<List<WButtonMethodContext>>> leases
        )
        {
            groups.Clear();
            leases.Clear();

            foreach (WButtonMethodContext context in contexts)
            {
                int drawOrder = context.Metadata.DrawOrder;
                if (!groups.TryGetValue(drawOrder, out List<WButtonMethodContext> group))
                {
                    PooledResource<List<WButtonMethodContext>> lease =
                        Buffers<WButtonMethodContext>.GetList(4, out group);
                    groups[drawOrder] = group;
                    leases[drawOrder] = lease;
                }

                group.Add(context);
            }
        }

        private static void DrawGroup(
            int drawOrder,
            List<WButtonMethodContext> contexts,
            IDictionary<int, WButtonPaginationState> paginationStates,
            IDictionary<int, bool> foldoutStates,
            UnityHelpersSettings.WButtonFoldoutBehavior foldoutBehavior,
            List<WButtonMethodContext> triggeredContexts
        )
        {
            if (contexts == null || contexts.Count == 0)
            {
                return;
            }

            GUIContent header = BuildGroupHeader(drawOrder);
            bool alwaysOpen =
                foldoutBehavior == UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen;
            bool expanded =
                alwaysOpen || GetFoldoutState(foldoutStates, drawOrder, foldoutBehavior);
            bool tweenEnabled = UnityHelpersSettings.ShouldTweenWButtonFoldouts();
            AnimBool foldoutAnim =
                alwaysOpen || !tweenEnabled ? null : GetFoldoutAnim(drawOrder, expanded);
            if (!tweenEnabled)
            {
                if (FoldoutAnimations.TryGetValue(drawOrder, out AnimBool cached) && cached != null)
                {
                    cached.valueChanged.RemoveListener(RequestRepaint);
                }
                FoldoutAnimations.Remove(drawOrder);
            }

            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = WButtonStyles.GetFoldoutBackgroundColor(expanded || alwaysOpen);

            GUILayout.BeginVertical(
                alwaysOpen
                    ? WButtonStyles.GroupStyle
                    : WButtonStyles.GetFoldoutContainerStyle(expanded)
            );

            GUI.backgroundColor = previousBackground;

            if (alwaysOpen)
            {
                GUILayout.Label(header, WButtonStyles.HeaderStyle);
                EditorGUILayout.Space(WButtonStyles.FoldoutContentSpacing);
                DrawGroupContent(drawOrder, contexts, paginationStates, triggeredContexts);
            }
            else
            {
                Rect headerRect = GUILayoutUtility.GetRect(
                    header,
                    WButtonStyles.FoldoutHeaderStyle,
                    GUILayout.ExpandWidth(true)
                );
                headerRect.xMin += WButtonStyles.FoldoutIconOffset;

                EditorGUI.indentLevel++;
                bool newExpanded = EditorGUI.Foldout(
                    headerRect,
                    expanded,
                    header,
                    true,
                    WButtonStyles.FoldoutHeaderStyle
                );
                EditorGUI.indentLevel--;

                if (foldoutStates != null)
                {
                    foldoutStates[drawOrder] = newExpanded;
                }

                if (foldoutAnim != null)
                {
                    foldoutAnim.target = newExpanded;
                }

                EditorGUILayout.Space(WButtonStyles.FoldoutContentSpacing);

                float fade = foldoutAnim?.faded ?? (newExpanded ? 1f : 0f);
                if (foldoutAnim == null)
                {
                    if (newExpanded)
                    {
                        DrawGroupContent(drawOrder, contexts, paginationStates, triggeredContexts);
                    }
                }
                else
                {
                    bool visible = EditorGUILayout.BeginFadeGroup(fade);
                    if (visible)
                    {
                        DrawGroupContent(drawOrder, contexts, paginationStates, triggeredContexts);
                    }
                    EditorGUILayout.EndFadeGroup();
                }
            }

            GUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private static void DrawGroupContent(
            int drawOrder,
            List<WButtonMethodContext> contexts,
            IDictionary<int, WButtonPaginationState> paginationStates,
            List<WButtonMethodContext> triggeredContexts
        )
        {
            int pageSize = UnityHelpersSettings.GetWButtonPageSize();
            WButtonPaginationState state = GetPaginationState(
                paginationStates,
                drawOrder,
                contexts.Count
            );

            DrawPaginationControls(state, contexts.Count, pageSize);

            int startIndex = state._pageIndex * pageSize;
            int endIndex = Mathf.Min(startIndex + pageSize, contexts.Count);

            for (int index = startIndex; index < endIndex; index++)
            {
                WButtonMethodContext context = contexts[index];
                DrawMethod(context, triggeredContexts);
                if (index < endIndex - 1)
                {
                    EditorGUILayout.Space(6f);
                }
            }

            if (endIndex > startIndex)
            {
                EditorGUILayout.Space(2f);
            }
        }

        private static void DrawPaginationControls(
            WButtonPaginationState state,
            int totalItems,
            int pageSize
        )
        {
            if (totalItems <= pageSize)
            {
                state._pageIndex = 0;
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalItems / pageSize));
            if (state._pageIndex >= totalPages)
            {
                state._pageIndex = totalPages - 1;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"Page {state._pageIndex + 1} / {totalPages}",
                    GUILayout.Width(90f)
                );

                using (new EditorGUI.DisabledScope(state._pageIndex == 0))
                {
                    if (GUILayout.Button("Prev", GUILayout.Width(50f)))
                    {
                        state._pageIndex--;
                        if (state._pageIndex < 0)
                        {
                            state._pageIndex = 0;
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(state._pageIndex >= totalPages - 1))
                {
                    if (GUILayout.Button("Next", GUILayout.Width(50f)))
                    {
                        state._pageIndex++;
                        if (state._pageIndex >= totalPages)
                        {
                            state._pageIndex = totalPages - 1;
                        }
                    }
                }
            }
            EditorGUILayout.Space(4f);
        }

        private static bool GetFoldoutState(
            IDictionary<int, bool> foldoutStates,
            int drawOrder,
            UnityHelpersSettings.WButtonFoldoutBehavior behavior
        )
        {
            bool defaultExpanded =
                behavior != UnityHelpersSettings.WButtonFoldoutBehavior.StartCollapsed;
            if (foldoutStates == null)
            {
                return defaultExpanded;
            }

            if (foldoutStates.TryGetValue(drawOrder, out bool current))
            {
                return current;
            }

            foldoutStates[drawOrder] = defaultExpanded;
            return defaultExpanded;
        }

        private static AnimBool GetFoldoutAnim(int drawOrder, bool expanded)
        {
            float speed = UnityHelpersSettings.GetWButtonFoldoutSpeed();
            if (!FoldoutAnimations.TryGetValue(drawOrder, out AnimBool anim) || anim == null)
            {
                anim = new AnimBool(expanded) { speed = speed };
                anim.valueChanged.AddListener(RequestRepaint);
                FoldoutAnimations[drawOrder] = anim;
            }

            anim.speed = speed;
            anim.target = expanded;
            return anim;
        }

        private static void RequestRepaint()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        internal static GUIContent BuildGroupHeader(int drawOrder)
        {
            GUIContent baseLabel =
                drawOrder >= -1 ? WButtonStyles.TopGroupLabel : WButtonStyles.BottomGroupLabel;

            if (
                GroupNames.TryGetValue(drawOrder, out string customName)
                && !string.IsNullOrWhiteSpace(customName)
            )
            {
                return new GUIContent(customName, baseLabel.tooltip);
            }

            if (GroupCounts.Count <= 1)
            {
                return baseLabel;
            }

            if (!GroupCounts.TryGetValue(drawOrder, out int count) || count <= 0)
            {
                return baseLabel;
            }

            string textWithOrder = $"{baseLabel.text} ({drawOrder})";
            return new GUIContent(textWithOrder, baseLabel.tooltip);
        }

        private static string ResolveGroupName(List<WButtonMethodContext> contexts)
        {
            if (contexts == null)
            {
                return null;
            }

            for (int index = 0; index < contexts.Count; index++)
            {
                WButtonMethodContext context = contexts[index];
                string groupName = context?.Metadata?.GroupName;
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    return groupName;
                }
            }

            return null;
        }

        private static void DrawMethod(
            WButtonMethodContext context,
            List<WButtonMethodContext> triggeredContexts
        )
        {
            WButtonMethodMetadata metadata = context.Metadata;
            GUILayout.BeginVertical(EditorStyles.helpBox);

            WButtonMethodState[] states = context.States;
            if (states.Length > 0 && states[0].Parameters.Length > 0)
            {
                EditorGUI.indentLevel++;
                WButtonParameterDrawer.DrawParameters(states);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(3f);
            }

            DrawInvocationStatus(context);

            UnityHelpersSettings.WButtonPaletteEntry palette =
                UnityHelpersSettings.ResolveWButtonPalette(metadata.ColorKey);
            GUIStyle buttonStyle = WButtonStyles.GetColoredButtonStyle(
                palette.ButtonColor,
                palette.TextColor
            );
            Rect buttonRect = GUILayoutUtility.GetRect(
                new GUIContent(metadata.DisplayName),
                buttonStyle,
                GUILayout.Height(WButtonStyles.ButtonHeight),
                GUILayout.ExpandWidth(true)
            );

            if (GUI.Button(buttonRect, metadata.DisplayName, buttonStyle))
            {
                context.MarkTriggered();
                if (triggeredContexts != null)
                {
                    triggeredContexts.Add(context);
                }
            }

            GUILayout.EndVertical();
        }

        private static void DrawInvocationStatus(WButtonMethodContext context)
        {
            WButtonMethodState[] states = context.States;
            if (states == null || states.Length == 0)
            {
                return;
            }

            int runningCount = 0;
            bool cancellable = false;
            foreach (WButtonMethodState state in states)
            {
                WButtonInvocationHandle handle = state.ActiveInvocation;
                if (handle == null)
                {
                    continue;
                }

                if (
                    handle.Status == WButtonInvocationStatus.Running
                    || handle.Status == WButtonInvocationStatus.CancelRequested
                )
                {
                    runningCount++;
                    cancellable |= handle.SupportsCancellation;
                }
            }

            if (runningCount > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string label = runningCount == 1 ? "Running..." : $"Running ({runningCount})";
                    EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (cancellable)
                    {
                        if (GUILayout.Button("Cancel", GUILayout.Width(70f)))
                        {
                            WButtonInvocationController.CancelActiveInvocations(context);
                        }
                    }
                }
                EditorGUILayout.Space(2f);
            }

            DrawHistory(states[0]);
        }

        private static void DrawHistory(WButtonMethodState state)
        {
            if (state == null || !state.HasHistory)
            {
                return;
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);
            Rect headerRect = EditorGUILayout.GetControlRect(
                false,
                EditorGUIUtility.singleLineHeight,
                GUILayout.ExpandWidth(true)
            );
            Vector2 labelSize = EditorStyles.miniBoldLabel.CalcSize(RecentResultsHeaderContent);
            float buttonWidth = Mathf.Max(
                ClearHistoryMinWidth,
                EditorStyles.miniButton.CalcSize(ClearHistoryContent).x + ClearHistoryButtonPadding
            );
            float availableWidth = headerRect.width;
            bool canShowButton =
                availableWidth >= (labelSize.x + ClearHistorySpacing + buttonWidth);
            float labelWidth = canShowButton
                ? Mathf.Min(labelSize.x, availableWidth - (buttonWidth + ClearHistorySpacing))
                : availableWidth;

            Rect labelRect = new(headerRect.x, headerRect.y, labelWidth, headerRect.height);
            GUI.Label(labelRect, RecentResultsHeaderContent, EditorStyles.miniBoldLabel);

            if (canShowButton)
            {
                Rect buttonRect = new(
                    headerRect.xMax - buttonWidth,
                    headerRect.y,
                    buttonWidth,
                    headerRect.height
                );
                if (GUI.Button(buttonRect, ClearHistoryContent, EditorStyles.miniButton))
                {
                    state.ClearHistory();
                    GUI.FocusControl(null);
                }
            }

            if (!state.HasHistory)
            {
                GUILayout.EndVertical();
                EditorGUILayout.Space(3f);
                return;
            }

            for (int index = state.History.Count - 1; index >= 0; index--)
            {
                WButtonResultEntry entry = state.History[index];
                DrawHistoryEntry(entry);
            }

            GUILayout.EndVertical();
            EditorGUILayout.Space(3f);
        }

        private static void DrawHistoryEntry(WButtonResultEntry entry)
        {
            string prefix = entry.Kind switch
            {
                WButtonResultKind.Success => "[OK]",
                WButtonResultKind.Error => "[ERR]",
                WButtonResultKind.Cancelled => "[CANCEL]",
                _ => "[INFO]",
            };

            string timestamp = entry.Timestamp.ToLocalTime().ToString("HH:mm:ss");
            string summary = $"{prefix} {timestamp} {entry.Summary}";
            GUIStyle labelStyle =
                entry.Kind == WButtonResultKind.Error
                    ? EditorStyles.miniBoldLabel
                    : EditorStyles.miniLabel;

            EditorGUILayout.LabelField(summary, labelStyle);

            if (entry.ObjectReference != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Result",
                        entry.ObjectReference,
                        entry.ObjectReference.GetType(),
                        true
                    );
                }
            }

            if (entry.Kind == WButtonResultKind.Error && entry.Exception != null)
            {
                EditorGUILayout.LabelField(
                    entry.Exception.GetType().Name,
                    EditorStyles.wordWrappedMiniLabel
                );
            }
        }

        private static WButtonPaginationState GetPaginationState(
            IDictionary<int, WButtonPaginationState> paginationStates,
            int drawOrder,
            int itemCount
        )
        {
            if (paginationStates == null)
            {
                return new WButtonPaginationState();
            }

            WButtonPaginationState state = paginationStates.GetOrAdd(drawOrder);

            int pageSize = UnityHelpersSettings.GetWButtonPageSize();
            int pageCount = Mathf.Max(1, Mathf.CeilToInt((float)itemCount / pageSize));
            if (state._pageIndex >= pageCount)
            {
                state._pageIndex = pageCount - 1;
            }

            if (state._pageIndex < 0)
            {
                state._pageIndex = 0;
            }

            return state;
        }
    }

    internal sealed class WButtonPaginationState
    {
        internal int _pageIndex;
    }

    internal sealed class WButtonMethodContext
    {
        internal WButtonMethodContext(
            WButtonMethodMetadata metadata,
            WButtonMethodState[] states,
            UnityEngine.Object[] targets
        )
        {
            Metadata = metadata;
            States = states;
            Targets = targets;
        }

        internal WButtonMethodMetadata Metadata { get; }

        internal WButtonMethodState[] States { get; }

        internal UnityEngine.Object[] Targets { get; }

        internal bool InvocationRequested { get; private set; }

        internal void MarkTriggered()
        {
            InvocationRequested = true;
        }

        internal void ResetTrigger()
        {
            InvocationRequested = false;
        }
    }
#endif
}
