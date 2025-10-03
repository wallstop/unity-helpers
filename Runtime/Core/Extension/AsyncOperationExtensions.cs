namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using UnityEngine;

    public static class AsyncOperationExtensions
    {
        private static readonly ConcurrentDictionary<
            AsyncOperation,
            Action<AsyncOperation>
        > Handlers = new();
        private static readonly ConcurrentDictionary<AsyncOperation, Action> Continuations = new();

        public readonly struct AsyncOperationAwaiter : INotifyCompletion
        {
            private readonly AsyncOperation _operation;

            public AsyncOperationAwaiter(AsyncOperation operation)
            {
                _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            }

            public bool IsCompleted => _operation.isDone;

            public void OnCompleted(Action continuation)
            {
                Continuations[_operation] = continuation;

                Action<AsyncOperation> handler = CachedHandler;
                if (!Handlers.TryAdd(_operation, handler))
                {
                    return;
                }

                Handlers[_operation] = handler;
                _operation.completed += handler;
            }

            public void GetResult() { }
        }

        private static readonly Action<AsyncOperation> CachedHandler = OnOperationCompleted;

        private static void OnOperationCompleted(AsyncOperation operation)
        {
            Handlers.Remove(operation, out Action<AsyncOperation> _);
            if (!Continuations.Remove(operation, out Action completionCondition))
            {
                return;
            }

            completionCondition?.Invoke();
        }

        public static async Task AsTask(this AsyncOperation asyncOp)
        {
            if (asyncOp.isDone)
            {
                return;
            }

            await asyncOp;
        }

        public static async ValueTask AsValueTask(this AsyncOperation asyncOp)
        {
            if (asyncOp.isDone)
            {
                return;
            }

            await asyncOp;
        }

#if !UNITY_2023_1_OR_NEWER
        public static AsyncOperationAwaiter GetAwaiter(this AsyncOperation op)
        {
            return new AsyncOperationAwaiter(op);
        }
#endif

        public static async ValueTask WithContinuation(this ValueTask task, Action continuation)
        {
            await task;
            continuation?.Invoke();
        }

        public static async ValueTask<TResult> WithContinuation<TResult>(
            this ValueTask<TResult> task,
            Func<TResult, TResult> continuation
        )
        {
            TResult result = await task;
            return continuation != null ? continuation(result) : result;
        }

        public static async ValueTask WithContinuation<TResult>(
            this ValueTask<TResult> task,
            Action<TResult> continuation
        )
        {
            TResult result = await task;
            continuation?.Invoke(result);
        }

        // Task/ValueTask to IEnumerator conversions
        public static IEnumerator AsCoroutine(this Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        public static IEnumerator AsCoroutine<T>(this Task<T> task, Action<T> onResult = null)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            onResult?.Invoke(task.Result);
        }

        public static IEnumerator AsCoroutine(this ValueTask task)
        {
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                {
                    throw task.AsTask().Exception;
                }
                yield break;
            }

            Task innerTask = task.AsTask();
            while (!innerTask.IsCompleted)
            {
                yield return null;
            }

            if (innerTask.IsFaulted)
            {
                throw innerTask.Exception;
            }
        }

        public static IEnumerator AsCoroutine<T>(this ValueTask<T> task, Action<T> onResult = null)
        {
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                {
                    throw task.AsTask().Exception;
                }
                onResult?.Invoke(task.Result);
                yield break;
            }

            Task<T> innerTask = task.AsTask();
            while (!innerTask.IsCompleted)
            {
                yield return null;
            }

            if (innerTask.IsFaulted)
            {
                throw innerTask.Exception;
            }

            onResult?.Invoke(innerTask.Result);
        }

        // IEnumerator to Task/ValueTask conversions
        public static async Task AsTask(this IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                throw new ArgumentNullException(nameof(coroutine));
            }

            while (coroutine.MoveNext())
            {
                await Task.Yield();
            }
        }

        public static async ValueTask AsValueTask(this IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                throw new ArgumentNullException(nameof(coroutine));
            }

            while (coroutine.MoveNext())
            {
                await Task.Yield();
            }
        }
    }
}
