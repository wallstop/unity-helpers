namespace UnityHelpers.Utils
{
    using System;
    using System.Threading.Tasks;

    public sealed class DeferredDisposalResult<T>
    {
        public readonly T result;

        private readonly Func<Task> _disposeAsync;

        public DeferredDisposalResult(T result, Func<Task> disposeAsync)
        {
            this.result = result;
            _disposeAsync = disposeAsync ?? throw new ArgumentNullException(nameof(disposeAsync));
        }

        public async Task DisposeAsync()
        {
            await _disposeAsync();
        }
    }
}
