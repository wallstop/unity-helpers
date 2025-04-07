namespace UnityHelpers.Utils
{
    using System;
    using System.Threading.Tasks;

    public readonly struct DeferredDisposalResult<T>
    {
        public readonly T result;

        private readonly Func<ValueTask> _disposeAsync;

        public DeferredDisposalResult(T result, Func<ValueTask> disposeAsync)
        {
            this.result = result;
            _disposeAsync = disposeAsync ?? throw new ArgumentNullException(nameof(disposeAsync));
        }

        public async ValueTask DisposeAsync()
        {
            await _disposeAsync();
        }
    }
}
