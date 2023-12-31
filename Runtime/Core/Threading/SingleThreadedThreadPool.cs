namespace UnityHelpers.Core.Threading
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public sealed class SingleThreadedThreadPool : IDisposable
    {
        private int _active;
        private int _working;
        private Thread _worker;
        private readonly ConcurrentQueue<Action> _work;
        private AutoResetEvent _waitHandle;
        private bool _disposed;

        public SingleThreadedThreadPool()
        {
            _active = 1;
            _working = 1;
            _work = new ConcurrentQueue<Action>();
            _waitHandle = new AutoResetEvent(false);
            _worker = new Thread(DoWork);
            _worker.Start();
        }

        ~SingleThreadedThreadPool()
        {
            Dispose(false);
        }

        private void DoWork()
        {
            while (Interlocked.CompareExchange(ref _active, 0, 0) != 0)
            {
                _ = Interlocked.Exchange(ref _working, 0);
                if (_work.TryDequeue(out Action workItem))
                {
                    _ = Interlocked.Exchange(ref _working, 1);
                    try
                    {
                        workItem();
                    }
                    catch
                    {
                        // Ignore (TODO: Log to some output function? The downside here is that this is in another thread, which means standard Unity logging is a no-go)
                    }
                }
                else
                {
                    _ = _waitHandle.WaitOne(TimeSpan.FromSeconds(1));
                }
                _ = Interlocked.Exchange(ref _working, 0);
            }
        }

        public void Enqueue(Action work)
        {
            _work.Enqueue(work);
            _ = _waitHandle.Set();
        }

        public int Count => _work.Count + Interlocked.CompareExchange(ref _working, 0, 0);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            int active;
            do
            {
                active = Interlocked.CompareExchange(ref _active, 0, _active);
            }
            while (active != 0);

            if (disposing)
            {
                try
                {
                    _worker?.Join();
                    _waitHandle?.Dispose();
                }
                catch
                {
                    // Swallow
                }
                _waitHandle = null;
                _worker = null;
            }

            _disposed = true;
        }
    }
}
