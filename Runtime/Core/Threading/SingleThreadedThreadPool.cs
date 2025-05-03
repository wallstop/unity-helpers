namespace WallstopStudios.UnityHelpers.Core.Threading
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public sealed class SingleThreadedThreadPool : IDisposable
    {
        public ConcurrentQueue<Exception> Exceptions => _exceptions;
        public int Count => _work.Count + Interlocked.CompareExchange(ref _working, 0, 0);

        private int _active;
        private int _working;
        private Thread _worker;
        private readonly ConcurrentQueue<Action> _work;
        private AutoResetEvent _waitHandle;
        private bool _disposed;
        private readonly ConcurrentQueue<Exception> _exceptions;

        public SingleThreadedThreadPool(bool runInBackground = false)
        {
            _active = 1;
            _working = 1;
            _work = new ConcurrentQueue<Action>();
            _exceptions = new ConcurrentQueue<Exception>();
            _waitHandle = new AutoResetEvent(false);
            _worker = new Thread(DoWork) { IsBackground = runInBackground };
            _worker.Start();
        }

        ~SingleThreadedThreadPool()
        {
            Dispose(false);
        }

        public void Enqueue(Action work)
        {
            _work.Enqueue(work);
            _ = _waitHandle.Set();
        }

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

            Interlocked.Exchange(ref _active, 0);

            if (disposing)
            {
                try
                {
                    _worker?.Join(TimeSpan.FromSeconds(30));
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

        private void DoWork()
        {
            while (Interlocked.CompareExchange(ref _active, 0, 0) != 0)
            {
                try
                {
                    if (_work.TryDequeue(out Action workItem))
                    {
                        _ = Interlocked.Exchange(ref _working, 1);
                        try
                        {
                            workItem();
                        }
                        catch (Exception e)
                        {
                            _exceptions.Enqueue(e);
                        }
                    }
                    else
                    {
                        try
                        {
                            _ = _waitHandle?.WaitOne(TimeSpan.FromSeconds(1));
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                    }
                }
                finally
                {
                    _ = Interlocked.Exchange(ref _working, 0);
                }
            }
        }
    }
}
