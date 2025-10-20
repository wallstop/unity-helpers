namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using UnityEngine;
    using Utils;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Thread-safe dispatcher that enqueues work to run on Unity's main thread.
    /// </summary>
    /// <remarks>
    /// Works in both edit mode and play mode. Use for marshalling callbacks from tasks/threads to main thread.
    /// </remarks>
    [ExecuteAlways]
    public sealed class UnityMainThreadDispatcher : RuntimeSingleton<UnityMainThreadDispatcher>
    {
        private readonly ConcurrentQueue<Action> _actions = new();

        protected override bool Preserve => false;

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

        /// <summary>
        /// Enqueues an action to be executed on the main thread during the next Update.
        /// </summary>
        /// <example>
        /// <code>
        /// Task.Run(async () =>
        /// {
        ///     var data = await FetchAsync();
        ///     UnityMainThreadDispatcher.Instance.RunOnMainThread(() => Apply(data));
        /// });
        /// </code>
        /// </example>
        public void RunOnMainThread(Action action)
        {
            _actions.Enqueue(action);
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!_attachedEditorUpdate && !Application.isPlaying)
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

        /// <summary>
        /// Posts an action to run on the main thread and returns a Task that completes after execution.
        /// </summary>
        public System.Threading.Tasks.Task RunAsync(Action action)
        {
            TaskCompletionSource<bool> tcs =
                new System.Threading.Tasks.TaskCompletionSource<bool>();
            RunOnMainThread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Posts a function to run on the main thread and returns its result via Task.
        /// </summary>
        public System.Threading.Tasks.Task<T> Post<T>(Func<T> func)
        {
            TaskCompletionSource<T> tcs = new System.Threading.Tasks.TaskCompletionSource<T>();
            RunOnMainThread(() =>
            {
                try
                {
                    T result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
