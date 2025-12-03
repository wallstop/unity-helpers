#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Editor.Utils
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    /// <summary>
    /// Tracks whether at least one Project window tab is both open and currently visible so we can
    /// suppress actions (like Ping buttons) that would have no effect otherwise.
    /// </summary>
    internal static class ProjectBrowserVisibilityUtility
    {
        private const string ProjectBrowserTypeName = "UnityEditor.ProjectBrowser, UnityEditor";
        private const string HostViewTypeName = "UnityEditor.HostView";
        private const double PollIntervalSeconds = 0.25d;

        private static readonly Type ProjectBrowserType = Type.GetType(ProjectBrowserTypeName);
        private static readonly FieldInfo EditorWindowParentField = typeof(EditorWindow).GetField(
            "m_Parent",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        private static readonly Type HostViewType = typeof(EditorWindow).Assembly.GetType(
            HostViewTypeName
        );
        private static readonly PropertyInfo HostViewActualViewProperty = HostViewType?.GetProperty(
            "actualView",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        private static bool? visibilityOverride;
        private static bool cachedVisibility;
        private static double nextPollTime;

        static ProjectBrowserVisibilityUtility()
        {
            cachedVisibility = EvaluateProjectBrowserVisibility();
            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;
            EditorApplication.update += HandleEditorApplicationUpdate;
        }

        internal static void SetProjectBrowserVisibilityForTesting(bool? visible)
        {
            visibilityOverride = visible;
            if (visible.HasValue)
            {
                UpdateCachedVisibility(visible.Value);
            }
            else
            {
                ForceVisibilityPoll();
            }
        }

        internal static bool IsProjectBrowserVisible()
        {
            if (visibilityOverride.HasValue)
            {
                return visibilityOverride.Value;
            }

            return cachedVisibility;
        }

        private static void HandleEditorApplicationUpdate()
        {
            if (visibilityOverride.HasValue)
            {
                UpdateCachedVisibility(visibilityOverride.Value);
                return;
            }

            double time = EditorApplication.timeSinceStartup;
            if (time < nextPollTime)
            {
                return;
            }

            nextPollTime = time + PollIntervalSeconds;
            bool visibility = EvaluateProjectBrowserVisibility();
            UpdateCachedVisibility(visibility);
        }

        private static void ForceVisibilityPoll()
        {
            nextPollTime = 0d;
        }

        private static void UpdateCachedVisibility(bool newValue)
        {
            if (cachedVisibility == newValue)
            {
                return;
            }

            cachedVisibility = newValue;
            InternalEditorUtility.RepaintAllViews();
        }

        private static bool EvaluateProjectBrowserVisibility()
        {
            if (ProjectBrowserType == null)
            {
                return true;
            }

            UnityEngine.Object[] projectBrowsers = Resources.FindObjectsOfTypeAll(
                ProjectBrowserType
            );
            if (projectBrowsers == null || projectBrowsers.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < projectBrowsers.Length; index++)
            {
                EditorWindow window = projectBrowsers[index] as EditorWindow;
                if (IsEditorWindowVisible(window))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEditorWindowVisible(EditorWindow window)
        {
            if (window == null || !window)
            {
                return false;
            }

            if (EditorWindowParentField == null || HostViewActualViewProperty == null)
            {
                return true;
            }

            object hostView = EditorWindowParentField.GetValue(window);
            if (hostView == null)
            {
                return false;
            }

            object currentView = HostViewActualViewProperty.GetValue(hostView);
            return ReferenceEquals(currentView, window);
        }
    }
}
#endif
