namespace UnityHelpers.Core.Extension
{
    using System;
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

        public static AsyncOperationAwaiter GetAwaiter(this AsyncOperation op)
        {
            return new AsyncOperationAwaiter(op);
        }

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
            return continuation(result);
        }

        public static async ValueTask WithContinuation<TResult>(
            this ValueTask<TResult> task,
            Action<TResult> continuation
        )
        {
            TResult result = await task;
            continuation(result);
        }
    }
}
