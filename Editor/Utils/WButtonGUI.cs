namespace WallstopStudios.UnityHelpers.Editor.WButton
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    internal enum WButtonPlacement
    {
        Top,
        Bottom,
    }

    internal static class WButtonGUI
    {
        internal static bool DrawButtons(
            Editor editor,
            WButtonPlacement placement,
            IDictionary<int, WButtonPaginationState> paginationStates,
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

            List<WButtonMethodContext> contexts = BuildContexts(metadataList, targets);
            if (contexts.Count == 0)
            {
                return false;
            }

            SortedDictionary<int, List<WButtonMethodContext>> groups = GroupByDrawOrder(contexts);

            bool anyDrawn = false;
            foreach (KeyValuePair<int, List<WButtonMethodContext>> entry in groups)
            {
                int drawOrder = entry.Key;
                bool drawOnTop = drawOrder >= -1;
                if (
                    (placement == WButtonPlacement.Top && drawOnTop)
                    || (placement == WButtonPlacement.Bottom && !drawOnTop)
                )
                {
                    DrawGroup(drawOrder, entry.Value, paginationStates, triggeredContexts);
                    anyDrawn = true;
                }
            }

            return anyDrawn;
        }

        private static List<WButtonMethodContext> BuildContexts(
            IReadOnlyList<WButtonMethodMetadata> metadataList,
            UnityEngine.Object[] targets
        )
        {
            List<WButtonMethodContext> contexts = new List<WButtonMethodContext>();
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

            return contexts;
        }

        private static SortedDictionary<int, List<WButtonMethodContext>> GroupByDrawOrder(
            List<WButtonMethodContext> contexts
        )
        {
            SortedDictionary<int, List<WButtonMethodContext>> groups =
                new SortedDictionary<int, List<WButtonMethodContext>>();

            for (int index = 0; index < contexts.Count; index++)
            {
                WButtonMethodContext context = contexts[index];
                int drawOrder = context.Metadata.DrawOrder;
                if (!groups.TryGetValue(drawOrder, out List<WButtonMethodContext> group))
                {
                    group = new List<WButtonMethodContext>();
                    groups.Add(drawOrder, group);
                }
                group.Add(context);
            }

            return groups;
        }

        private static void DrawGroup(
            int drawOrder,
            List<WButtonMethodContext> contexts,
            IDictionary<int, WButtonPaginationState> paginationStates,
            List<WButtonMethodContext> triggeredContexts
        )
        {
            if (contexts == null || contexts.Count == 0)
            {
                return;
            }

            GUIContent header =
                drawOrder >= -1 ? WButtonStyles.TopGroupLabel : WButtonStyles.BottomGroupLabel;
            GUILayout.BeginVertical(WButtonStyles.GroupStyle);
            GUILayout.Label(header, WButtonStyles.HeaderStyle);

            int pageSize = UnityHelpersSettings.GetWButtonPageSize();
            WButtonPaginationState state = GetPaginationState(
                paginationStates,
                drawOrder,
                contexts.Count
            );

            DrawPaginationControls(state, contexts.Count, pageSize);

            int startIndex = state.PageIndex * pageSize;
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

            GUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private static void DrawPaginationControls(
            WButtonPaginationState state,
            int totalItems,
            int pageSize
        )
        {
            if (totalItems <= pageSize)
            {
                state.PageIndex = 0;
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalItems / pageSize));
            if (state.PageIndex >= totalPages)
            {
                state.PageIndex = totalPages - 1;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"Page {state.PageIndex + 1} / {totalPages}",
                    GUILayout.Width(90f)
                );

                using (new EditorGUI.DisabledScope(state.PageIndex == 0))
                {
                    if (GUILayout.Button("Prev", GUILayout.Width(50f)))
                    {
                        state.PageIndex--;
                        if (state.PageIndex < 0)
                        {
                            state.PageIndex = 0;
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(state.PageIndex >= totalPages - 1))
                {
                    if (GUILayout.Button("Next", GUILayout.Width(50f)))
                    {
                        state.PageIndex++;
                        if (state.PageIndex >= totalPages)
                        {
                            state.PageIndex = totalPages - 1;
                        }
                    }
                }
            }
            EditorGUILayout.Space(4f);
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
                UnityHelpersSettings.ResolveWButtonPalette(metadata.Priority);
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
            for (int index = 0; index < states.Length; index++)
            {
                WButtonInvocationHandle handle = states[index].ActiveInvocation;
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
            if (state == null || state.History.Count == 0)
            {
                return;
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Recent Results", EditorStyles.miniBoldLabel);

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

            if (!paginationStates.TryGetValue(drawOrder, out WButtonPaginationState state))
            {
                state = new WButtonPaginationState();
                paginationStates[drawOrder] = state;
            }

            int pageSize = UnityHelpersSettings.GetWButtonPageSize();
            int pageCount = Mathf.Max(1, Mathf.CeilToInt((float)itemCount / pageSize));
            if (state.PageIndex >= pageCount)
            {
                state.PageIndex = pageCount - 1;
            }

            if (state.PageIndex < 0)
            {
                state.PageIndex = 0;
            }

            return state;
        }
    }

    internal sealed class WButtonPaginationState
    {
        internal int PageIndex;
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
