namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    internal static class WInLineEditorDiagnostics
    {
        internal const int MaxSamplesPerSession = 48;
        private const string RecordingPrefKey =
            "WallstopStudios.UnityHelpers.WInLineEditor.Diagnostics.RecordingEnabled";
        private const string ConsolePrefKey =
            "WallstopStudios.UnityHelpers.WInLineEditor.Diagnostics.ConsoleLoggingEnabled";
        private const string MenuRoot = "Tools/Unity Helpers/WInLine Editor/Diagnostics";

        private static readonly Dictionary<
            string,
            Queue<InlineInspectorLayoutSample>
        > LayoutSamples = new(StringComparer.Ordinal);
        private static readonly Dictionary<
            string,
            Queue<InlineInspectorReservationSample>
        > ReservationSamples = new(StringComparer.Ordinal);

        static WInLineEditorDiagnostics()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearAllDiagnostics;
            EditorApplication.quitting += ClearAllDiagnostics;
        }

        internal static bool RecordingEnabled
        {
            get => EditorPrefs.GetBool(RecordingPrefKey, false);
            set => EditorPrefs.SetBool(RecordingPrefKey, value);
        }

        internal static bool ConsoleLoggingEnabled
        {
            get => EditorPrefs.GetBool(ConsolePrefKey, false);
            set => EditorPrefs.SetBool(ConsolePrefKey, value);
        }

        [MenuItem(MenuRoot + "/Enable Recording")]
        private static void ToggleRecording()
        {
            RecordingEnabled = !RecordingEnabled;
            Debug.Log(
                $"[WInLineEditor][Diagnostics] Recording {(RecordingEnabled ? "enabled" : "disabled")}."
            );
        }

        [MenuItem(MenuRoot + "/Enable Recording", true)]
        private static bool ToggleRecordingValidate()
        {
            Menu.SetChecked(MenuRoot + "/Enable Recording", RecordingEnabled);
            return true;
        }

        [MenuItem(MenuRoot + "/Log Samples To Console")]
        private static void ToggleConsoleLogging()
        {
            ConsoleLoggingEnabled = !ConsoleLoggingEnabled;
            Debug.Log(
                $"[WInLineEditor][Diagnostics] Console logging {(ConsoleLoggingEnabled ? "enabled" : "disabled")}."
            );
        }

        [MenuItem(MenuRoot + "/Log Samples To Console", true)]
        private static bool ToggleConsoleLoggingValidate()
        {
            Menu.SetChecked(MenuRoot + "/Log Samples To Console", ConsoleLoggingEnabled);
            return true;
        }

        internal static void RecordLayoutSample(
            string sessionKey,
            InlineInspectorLayoutSample sample
        )
        {
            if (!RecordingEnabled || string.IsNullOrEmpty(sessionKey))
            {
                return;
            }

            Queue<InlineInspectorLayoutSample> queue = GetOrCreate(LayoutSamples, sessionKey);
            queue.Enqueue(sample);
            TrimQueue(queue);

            if (ConsoleLoggingEnabled && sample.EventType == EventType.Repaint)
            {
                Debug.LogFormat(
                    "[WInLineEditor][Layout] session={0} event={1} inline={2:F1} inspector={3:F1} pref={4:F1} resolvedView={5:F1} visibleRect={6:F1} reserved={7} requiresScroll={8} hasScroll={9} offset={10:F1} indent={11} leftPad={12:F1} rightPad={13:F1} fits={14}",
                    sessionKey,
                    sample.EventType,
                    sample.InlineRectWidth,
                    sample.InspectorRectWidth,
                    sample.PreferredContentWidth,
                    sample.ResolvedViewWidth,
                    sample.VisibleRectWidth,
                    sample.HasHorizontalReservation,
                    sample.RequiresHorizontalScroll,
                    sample.DisplayHorizontalScroll,
                    sample.HorizontalScrollOffset,
                    sample.IndentLevel,
                    sample.LeftGroupPadding,
                    sample.RightGroupPadding,
                    sample.ConsecutiveFitRepaints
                );
            }
        }

        internal static void RecordReservationSample(
            string sessionKey,
            InlineInspectorReservationSample sample
        )
        {
            if (!RecordingEnabled || string.IsNullOrEmpty(sessionKey))
            {
                return;
            }

            Queue<InlineInspectorReservationSample> queue = GetOrCreate(
                ReservationSamples,
                sessionKey
            );
            queue.Enqueue(sample);
            TrimQueue(queue);

            if (ConsoleLoggingEnabled)
            {
                Debug.LogFormat(
                    "[WInLineEditor][Reservation] session={0} available={1:F1} content={2:F1} min={3:F1} needs={4} awaitingFit={5} prev={6} next={7}",
                    sessionKey,
                    sample.AvailableWidth,
                    sample.EstimatedContentWidth,
                    sample.MinInspectorWidth,
                    sample.NeedsReservation,
                    sample.AwaitingStableFrames,
                    sample.PreviousReservation,
                    sample.ResultReservation
                );
            }
        }

        internal static bool TryGetLayoutSamples(
            string sessionKey,
            out InlineInspectorLayoutSample[] samples
        )
        {
            if (
                !string.IsNullOrEmpty(sessionKey)
                && LayoutSamples.TryGetValue(
                    sessionKey,
                    out Queue<InlineInspectorLayoutSample> queue
                )
            )
            {
                samples = queue.ToArray();
                return samples.Length > 0;
            }

            samples = Array.Empty<InlineInspectorLayoutSample>();
            return false;
        }

        internal static bool TryGetReservationSamples(
            string sessionKey,
            out InlineInspectorReservationSample[] samples
        )
        {
            if (
                !string.IsNullOrEmpty(sessionKey)
                && ReservationSamples.TryGetValue(
                    sessionKey,
                    out Queue<InlineInspectorReservationSample> queue
                )
            )
            {
                samples = queue.ToArray();
                return samples.Length > 0;
            }

            samples = Array.Empty<InlineInspectorReservationSample>();
            return false;
        }

        internal static void ClearAllDiagnostics()
        {
            LayoutSamples.Clear();
            ReservationSamples.Clear();
        }

        internal static void ClearSession(string sessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                return;
            }

            LayoutSamples.Remove(sessionKey);
            ReservationSamples.Remove(sessionKey);
        }

        private static Queue<T> GetOrCreate<T>(Dictionary<string, Queue<T>> map, string key)
        {
            if (!map.TryGetValue(key, out Queue<T> queue))
            {
                queue = new Queue<T>(MaxSamplesPerSession);
                map[key] = queue;
            }

            return queue;
        }

        private static void TrimQueue<T>(Queue<T> queue)
        {
            while (queue.Count > MaxSamplesPerSession)
            {
                queue.Dequeue();
            }
        }

        internal readonly struct InlineInspectorLayoutSample
        {
            public InlineInspectorLayoutSample(
                EventType eventType,
                float inlineRectWidth,
                float inspectorRectWidth,
                float preferredContentWidth,
                float inspectorContentWidth,
                float effectiveViewportWidth,
                float resolvedViewWidth,
                float visibleRectWidth,
                bool hasHorizontalReservation,
                bool requiresHorizontalScroll,
                bool displayHorizontalScroll,
                float horizontalScrollOffset,
                bool requiresVerticalScroll,
                bool displayVerticalScroll,
                float verticalScrollOffset,
                float indentLevel,
                float leftPadding,
                float rightPadding,
                float preferredInlineWidth,
                bool widthWasClipped,
                int consecutiveFitRepaints
            )
            {
                EventType = eventType;
                InlineRectWidth = inlineRectWidth;
                InspectorRectWidth = inspectorRectWidth;
                PreferredContentWidth = preferredContentWidth;
                InspectorContentWidth = inspectorContentWidth;
                EffectiveViewportWidth = effectiveViewportWidth;
                ResolvedViewWidth = resolvedViewWidth;
                VisibleRectWidth = visibleRectWidth;
                HasHorizontalReservation = hasHorizontalReservation;
                RequiresHorizontalScroll = requiresHorizontalScroll;
                DisplayHorizontalScroll = displayHorizontalScroll;
                HorizontalScrollOffset = horizontalScrollOffset;
                RequiresVerticalScroll = requiresVerticalScroll;
                DisplayVerticalScroll = displayVerticalScroll;
                VerticalScrollOffset = verticalScrollOffset;
                IndentLevel = indentLevel;
                LeftGroupPadding = leftPadding;
                RightGroupPadding = rightPadding;
                PreferredInlineWidth = preferredInlineWidth;
                WidthWasClipped = widthWasClipped;
                ConsecutiveFitRepaints = consecutiveFitRepaints;
            }

            public EventType EventType { get; }
            public float InlineRectWidth { get; }
            public float InspectorRectWidth { get; }
            public float PreferredContentWidth { get; }
            public float InspectorContentWidth { get; }
            public float EffectiveViewportWidth { get; }
            public float ResolvedViewWidth { get; }
            public float VisibleRectWidth { get; }
            public bool HasHorizontalReservation { get; }
            public bool RequiresHorizontalScroll { get; }
            public bool DisplayHorizontalScroll { get; }
            public float HorizontalScrollOffset { get; }
            public bool RequiresVerticalScroll { get; }
            public bool DisplayVerticalScroll { get; }
            public float VerticalScrollOffset { get; }
            public float IndentLevel { get; }
            public float LeftGroupPadding { get; }
            public float RightGroupPadding { get; }
            public float PreferredInlineWidth { get; }
            public bool WidthWasClipped { get; }
            public int ConsecutiveFitRepaints { get; }
        }

        internal readonly struct InlineInspectorReservationSample
        {
            public InlineInspectorReservationSample(
                float availableWidth,
                float estimatedContentWidth,
                float minInspectorWidth,
                bool needsReservation,
                bool awaitingStableFrames,
                bool previousReservation,
                bool resultReservation,
                bool hadActiveScroll,
                int consecutiveFitRepaints
            )
            {
                AvailableWidth = availableWidth;
                EstimatedContentWidth = estimatedContentWidth;
                MinInspectorWidth = minInspectorWidth;
                NeedsReservation = needsReservation;
                AwaitingStableFrames = awaitingStableFrames;
                PreviousReservation = previousReservation;
                ResultReservation = resultReservation;
                HadActiveScroll = hadActiveScroll;
                ConsecutiveFitRepaints = consecutiveFitRepaints;
            }

            public float AvailableWidth { get; }
            public float EstimatedContentWidth { get; }
            public float MinInspectorWidth { get; }
            public bool NeedsReservation { get; }
            public bool AwaitingStableFrames { get; }
            public bool PreviousReservation { get; }
            public bool ResultReservation { get; }
            public bool HadActiveScroll { get; }
            public int ConsecutiveFitRepaints { get; }
        }
    }
#endif
}
