// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.EditorFramework
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using UnityEditor;
    using UnityEngine;

    internal sealed class TestIMGUIExecutor : EditorWindow
    {
        private Action _action;
        private bool _hasRun;

        internal static IEnumerator Run(Action action)
        {
            if (action == null)
            {
                yield break;
            }

            TestIMGUIExecutor window = CreateInstance<TestIMGUIExecutor>();
            window.hideFlags = HideFlags.HideAndDontSave;
            window.minSize = new Vector2(100f, 50f);
            window._action = action;
            window._hasRun = false;
            window.ShowUtility();
            window.Focus();

            while (!window._hasRun)
            {
                window.Repaint();
                yield return null;
            }

            window.Close();
        }

        private void OnGUI()
        {
            if (_action == null)
            {
                return;
            }

            EventType eventType = Event.current.type;
            if (eventType != EventType.Layout && eventType != EventType.Repaint)
            {
                return;
            }

            try
            {
                _action.Invoke();
            }
            catch
            {
                _action = null;
                _hasRun = true;
                throw;
            }

            if (eventType == EventType.Repaint)
            {
                _action = null;
                _hasRun = true;
            }
            else
            {
                Repaint();
            }
        }
    }
#endif
}
