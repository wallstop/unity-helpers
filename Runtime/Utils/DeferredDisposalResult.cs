namespace UnityHelpers.Utils
{
    using System;
    using System.Threading.Tasks;

    public sealed class DeferredDisposalResult<T>
    {
        public T Result { get; }
        private readonly Func<Task> _disposeAsync;

        public DeferredDisposalResult(T result, Func<Task> disposeAsync)
        {
            Result = result;
            _disposeAsync = disposeAsync;
        }

        public Task DisposeAsync() => _disposeAsync();
    }
}
