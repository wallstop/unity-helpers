// MIT License - Copyright (c) 2023 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Threading
{
#if !SINGLE_THREADED
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class SingleThreadedThreadPool : IDisposable
    {
        public ConcurrentQueue<Exception> Exceptions => _exceptions;
        public int Count => _work.Count + (_isWorking ? 1 : 0);

        private volatile bool _active = true;
        private volatile bool _isWorking;
        private volatile bool _disposed;

        private readonly Task _workerTask;
        private readonly ConcurrentQueue<WorkItem> _work;
        private readonly SemaphoreSlim _workAvailable;
        private readonly ConcurrentQueue<Exception> _exceptions;
        private readonly TimeSpan _noWorkWaitTime;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public SingleThreadedThreadPool(
            bool runInBackground = true,
            TimeSpan? noWorkWaitTime = null
        )
        {
            _work = new ConcurrentQueue<WorkItem>();
            _exceptions = new ConcurrentQueue<Exception>();
            _workAvailable = new SemaphoreSlim(0);
            _noWorkWaitTime = noWorkWaitTime ?? TimeSpan.FromSeconds(1);
            _cancellationTokenSource = new CancellationTokenSource();

            _workerTask = runInBackground
                ? Task.Run(DoWorkAsync)
                : Task.Factory.StartNew(DoWorkAsync, TaskCreationOptions.LongRunning).Unwrap();
        }

        public void Enqueue(Action work)
        {
            if (_disposed || !_active)
            {
                return;
            }

            _work.Enqueue(WorkItem.FromAction(work));
            Signal();
        }

        public void Enqueue(Func<Task> work)
        {
            if (_disposed || !_active)
            {
                return;
            }

            _work.Enqueue(WorkItem.FromTask(work));
            Signal();
        }

        public void Enqueue(Func<ValueTask> work)
        {
            if (_disposed || !_active)
            {
                return;
            }

            _work.Enqueue(WorkItem.FromValueTask(work));
            Signal();
        }

        private void Signal()
        {
            try
            {
                _workAvailable.Release();
            }
            catch
            {
                // Swallow
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _active = false;
            _disposed = true;

            _cancellationTokenSource.Cancel();
            Signal();
            try
            {
                await _workerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch
            {
                // Swallow other exceptions during disposal
            }

            _cancellationTokenSource?.Dispose();
            _workAvailable?.Dispose();

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private async Task DoWorkAsync()
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            while (_active && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_work.TryDequeue(out WorkItem workItem))
                    {
                        _isWorking = true;
                        try
                        {
                            await workItem.ExecuteAsync().ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            _exceptions.Enqueue(e);
                        }
                        finally
                        {
                            _isWorking = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            await _workAvailable
                                .WaitAsync(_noWorkWaitTime, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private enum WorkItemType
        {
            Unknown = 0,
            Action = 1,
            Task = 2,
            ValueTask = 3,
        }

        private readonly struct WorkItem
        {
            private readonly WorkItemType _type;
            private readonly Action _action;
            private readonly Func<Task> _taskFunc;
            private readonly Func<ValueTask> _valueTaskFunc;

            private WorkItem(
                WorkItemType type,
                Action action = null,
                Func<Task> taskFunc = null,
                Func<ValueTask> valueTaskFunc = null
            )
            {
                _type = type;
                _action = action;
                _taskFunc = taskFunc;
                _valueTaskFunc = valueTaskFunc;
            }

            public static WorkItem FromAction(Action action) =>
                new(WorkItemType.Action, action: action);

            public static WorkItem FromTask(Func<Task> taskFunc) =>
                new(WorkItemType.Task, taskFunc: taskFunc);

            public static WorkItem FromValueTask(Func<ValueTask> valueTaskFunc) =>
                new(WorkItemType.ValueTask, valueTaskFunc: valueTaskFunc);

            public ValueTask ExecuteAsync()
            {
                return _type switch
                {
                    WorkItemType.Action => ExecuteAction(),
                    WorkItemType.Task => ExecuteTask(),
                    WorkItemType.ValueTask => _valueTaskFunc(),
                    _ => default,
                };
            }

            private ValueTask ExecuteAction()
            {
                _action();
                return default;
            }

            private async ValueTask ExecuteTask()
            {
                await _taskFunc().ConfigureAwait(false);
            }
        }
    }
#endif
}
