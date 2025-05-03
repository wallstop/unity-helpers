namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Concurrent;
    using UnityEditor;
    using UnityEngine;
    using Utils;

    [ExecuteAlways]
    public sealed class UnityMainThreadDispatcher : RuntimeSingleton<UnityMainThreadDispatcher>
    {
        private readonly ConcurrentQueue<Action> _actions = new();

#if UNITY_EDITOR
        private readonly EditorApplication.CallbackFunction _update;
        private bool _attachedEditorUpdate;
#endif

        public UnityMainThreadDispatcher()
        {
#if UNITY_EDITOR
            _update = Update;
#endif
        }

        public void RunOnMainThread(Action action)
        {
            _actions.Enqueue(action);
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!_attachedEditorUpdate)
            {
                EditorApplication.update += _update;
                _attachedEditorUpdate = true;
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (_attachedEditorUpdate)
            {
                EditorApplication.update -= _update;
                _attachedEditorUpdate = false;
            }
#endif
        }

        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            if (_attachedEditorUpdate)
            {
                EditorApplication.update -= _update;
                _attachedEditorUpdate = false;
            }
#endif
            base.OnDestroy();
        }

        private void Update()
        {
            while (_actions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }
    }
}
