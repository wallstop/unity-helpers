namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using Utils;
    using WallstopStudios.UnityHelpers.Core.Extension;
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
        private const int DefaultQueueLimit = 4096;
        private int _pendingActionCount;
        private int _lastOverflowFrame = -1;

        [SerializeField]
        [Tooltip(
            "Maximum number of queued actions before new submissions are dropped. Set to 0 for unlimited."
        )]
        private int maxPendingActions = DefaultQueueLimit;

        protected override bool Preserve => false;

        /// <summary>
        /// Gets the number of actions currently waiting to be executed on the main thread.
        /// </summary>
        public int PendingActionCount => Volatile.Read(ref _pendingActionCount);

        /// <summary>
        /// Gets or sets the maximum number of queued actions allowed before new submissions are dropped.
        /// A value of 0 disables the limit.
        /// </summary>
        public int PendingActionLimit
        {
            get => maxPendingActions;
            set => maxPendingActions = Mathf.Max(0, value);
        }

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void EnsureDispatcherBootstrap()
        {
            EnsureDispatcherExists("runtime bootstrap");
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EnsureDispatcherBootstrapInEditor()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureDispatcherExists("editor bootstrap");
        }
#endif

        private static void EnsureDispatcherExists(string reason)
        {
            if (HasInstance)
            {
                return;
            }

            UnityMainThreadGuard.EnsureMainThread(reason);
            UnityMainThreadDispatcher dispatcher = Instance;
            if (!Application.isPlaying && dispatcher != null)
            {
                dispatcher.hideFlags = HideFlags.HideAndDontSave;
            }
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
            _ = Enqueue(action, logOverflow: true);
        }

        /// <summary>
        /// Attempts to enqueue an action to execute on the main thread without logging overflow warnings.
        /// Returns <c>true</c> when successfully queued; otherwise <c>false</c>.
        /// </summary>
        public bool TryRunOnMainThread(Action action)
        {
            return Enqueue(action, logOverflow: false);
        }

        private bool Enqueue(Action action, bool logOverflow)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!TryEnqueueInternal(action))
            {
                if (logOverflow)
                {
                    LogOverflow();
                }
                return false;
            }

            return true;
        }

        private bool TryEnqueueInternal(Action action)
        {
            int newCount = Interlocked.Increment(ref _pendingActionCount);
            if (maxPendingActions > 0 && newCount > maxPendingActions)
            {
                Interlocked.Decrement(ref _pendingActionCount);
                return false;
            }

            _actions.Enqueue(action);
            return true;
        }

        private void LogOverflow()
        {
            string message = BuildOverflowMessage();

            if (!Application.isPlaying)
            {
                FormattableString formatted = FormattableStringFactory.Create("{0}", message);
                this.LogWarn(formatted);
                return;
            }

            int currentFrame = Time.frameCount;
            if (currentFrame == _lastOverflowFrame)
            {
                return;
            }

            _lastOverflowFrame = currentFrame;
            FormattableString throttled = FormattableStringFactory.Create("{0}", message);
            this.LogWarn(throttled);
        }

        private string BuildOverflowMessage()
        {
            int limit = maxPendingActions;
            if (limit <= 0)
            {
                limit = 0;
            }

            int pending = PendingActionCount;
            return $"UnityMainThreadDispatcher queue overflow (limit {limit}). Dropping action. Pending count: {pending}.";
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
                    this.LogError(
                        $"UnityMainThreadDispatcher action threw {e.GetType().Name}: {e.Message}",
                        e
                    );
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingActionCount);
                }
            }
        }

        /// <summary>
        /// Posts an action to run on the main thread and returns a Task that completes after execution.
        /// </summary>
        public Task RunAsync(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            TaskCompletionSource<bool> taskCompletionSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            bool queued = Enqueue(
                () =>
                {
                    try
                    {
                        action();
                        taskCompletionSource.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                },
                logOverflow: true
            );

            if (!queued)
            {
                taskCompletionSource.TrySetException(
                    new InvalidOperationException(BuildOverflowMessage())
                );
            }

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Posts an asynchronous delegate that receives a <see cref="CancellationToken"/> and completes when the returned Task does.
        /// </summary>
        public Task RunAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default
        )
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            TaskCompletionSource<bool> taskCompletionSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            CancellationTokenRegistration registration = default;

            if (cancellationToken.CanBeCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetCanceled(cancellationToken);
                    return taskCompletionSource.Task;
                }

                registration = cancellationToken.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled(cancellationToken);
                });
            }

            bool queued = Enqueue(
                () =>
                {
                    if (taskCompletionSource.Task.IsCompleted)
                    {
                        registration.Dispose();
                        return;
                    }

                    Task runTask;
                    try
                    {
                        runTask = action(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        registration.Dispose();
                        taskCompletionSource.TrySetException(ex);
                        return;
                    }

                    if (runTask == null)
                    {
                        registration.Dispose();
                        taskCompletionSource.TrySetException(
                            new InvalidOperationException(
                                "UnityMainThreadDispatcher.RunAsync expected the delegate to return a Task."
                            )
                        );
                        return;
                    }

                    if (runTask.IsCompleted)
                    {
                        registration.Dispose();
                        CompleteFromTask(runTask, taskCompletionSource, cancellationToken);
                        return;
                    }

                    runTask.ContinueWith(
                        completedTask =>
                        {
                            registration.Dispose();
                            CompleteFromTask(
                                completedTask,
                                taskCompletionSource,
                                cancellationToken
                            );
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default
                    );
                },
                logOverflow: true
            );

            if (!queued)
            {
                registration.Dispose();
                taskCompletionSource.TrySetException(
                    new InvalidOperationException(BuildOverflowMessage())
                );
            }

            return taskCompletionSource.Task;
        }

        private static void CompleteFromTask(
            Task task,
            TaskCompletionSource<bool> completion,
            CancellationToken cancellationToken
        )
        {
            if (task == null || completion == null)
            {
                return;
            }

            if (task.IsCanceled)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    completion.TrySetCanceled(cancellationToken);
                }
                else
                {
                    completion.TrySetCanceled();
                }

                return;
            }

            if (task.IsFaulted)
            {
                AggregateException aggregateException = task.Exception;
                if (aggregateException != null)
                {
                    AggregateException flattened = aggregateException.Flatten();
                    completion.TrySetException(flattened.InnerExceptions);
                }
                else
                {
                    completion.TrySetException(
                        new InvalidOperationException("Dispatcher task faulted.")
                    );
                }

                return;
            }

            completion.TrySetResult(true);
        }

        /// <summary>
        /// Posts a function to run on the main thread and returns its result via Task.
        /// </summary>
        public Task<T> Post<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            TaskCompletionSource<T> taskCompletionSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            bool queued = Enqueue(
                () =>
                {
                    try
                    {
                        T result = func();
                        taskCompletionSource.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                },
                logOverflow: true
            );

            if (!queued)
            {
                taskCompletionSource.TrySetException(
                    new InvalidOperationException(BuildOverflowMessage())
                );
            }

            return taskCompletionSource.Task;
        }
    }
}
