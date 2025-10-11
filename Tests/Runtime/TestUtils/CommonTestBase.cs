namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Common test base that tracks spawned Unity objects and disposables
    /// and cleans them up safely across NUnit and Unity test teardowns.
    /// </summary>
    public abstract class CommonTestBase
    {
        // Per-test tracked UnityEngine.Objects
        protected readonly List<Object> _trackedObjects = new();

        // Per-test tracked IDisposable instances
        protected readonly List<IDisposable> _trackedDisposables = new();

        // Convenience overload for GameObject to support target-typed 'new(...)' at call sites
        protected GameObject Track(GameObject obj)
        {
            if (obj != null)
            {
                _trackedObjects.Add(obj);
            }
            return obj;
        }

        // Per-test tracked async disposal producers (executed during UnityTearDown)
        protected readonly List<Func<ValueTask>> _trackedAsyncDisposals = new();

        /// <summary>
        /// Track a UnityEngine.Object for automatic cleanup.
        /// </summary>
        protected T Track<T>(T obj)
            where T : Object
        {
            if (obj != null)
            {
                _trackedObjects.Add(obj);
            }
            return obj;
        }

        /// <summary>
        /// Track an IDisposable for automatic disposal during teardown.
        /// </summary>
        protected T TrackDisposable<T>(T disposable)
            where T : IDisposable
        {
            if (disposable != null)
            {
                _trackedDisposables.Add(disposable);
            }
            return disposable;
        }

        /// <summary>
        /// Track an async disposal producer to be executed in UnityTearDown.
        /// </summary>
        protected Func<ValueTask> TrackAsyncDisposal(Func<ValueTask> producer)
        {
            if (producer != null)
            {
                _trackedAsyncDisposals.Add(producer);
            }
            return producer;
        }

        /// <summary>
        /// NUnit teardown for non-Unity tests and EditMode cleanup.
        /// Uses DestroyImmediate when not playing and disposes disposables.
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            // Dispose tracked disposables (reverse order best-effort)
            if (_trackedDisposables.Count > 0)
            {
                for (int i = _trackedDisposables.Count - 1; i >= 0; i--)
                {
                    IDisposable d = _trackedDisposables[i];
                    try
                    {
                        d?.Dispose();
                    }
                    catch
                    {
                        // Swallow exceptions during teardown to avoid masking test results
                    }
                }
                _trackedDisposables.Clear();
            }

            // In EditMode (or non-Unity tests), prefer DestroyImmediate
            if (!Application.isPlaying && _trackedObjects.Count > 0)
            {
                // Snapshot to avoid list mutation issues during destruction
                Object[] snapshot = _trackedObjects.ToArray();
                foreach (Object obj in snapshot)
                {
                    if (obj != null)
                    {
                        Object.DestroyImmediate(obj);
                    }
                }
                _trackedObjects.Clear();
            }
        }

        /// <summary>
        /// Unity coroutine teardown for PlayMode/UnityTest cases.
        /// Uses Destroy with yields to allow Unity to finalize destruction.
        /// </summary>
        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            // Run tracked async disposals first to release scene resources
            if (_trackedAsyncDisposals.Count > 0)
            {
                foreach (Func<ValueTask> producer in _trackedAsyncDisposals.ToArray())
                {
                    if (producer == null)
                    {
                        continue;
                    }
                    ValueTask vt = producer();
                    while (!vt.IsCompleted)
                    {
                        yield return null;
                    }
                }
                _trackedAsyncDisposals.Clear();
            }

            if (_trackedObjects.Count > 0)
            {
                Object[] snapshot = _trackedObjects.ToArray();
                foreach (Object obj in snapshot)
                {
                    if (obj == null)
                    {
                        continue;
                    }
                    Object.Destroy(obj);
                    // allow at least one frame for destruction to process
                    yield return null;
                }
                _trackedObjects.Clear();
            }
        }

        /// <summary>
        /// One-time teardown safety net in case any objects remain.
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Best-effort final cleanup using DestroyImmediate (editor-safe)
            if (_trackedObjects.Count > 0)
            {
                Object[] snapshot = _trackedObjects.ToArray();
                foreach (Object obj in snapshot)
                {
                    if (obj != null)
                    {
                        Object.DestroyImmediate(obj);
                    }
                }
                _trackedObjects.Clear();
            }

            if (_trackedDisposables.Count > 0)
            {
                for (int i = _trackedDisposables.Count - 1; i >= 0; i--)
                {
                    IDisposable d = _trackedDisposables[i];
                    try
                    {
                        d?.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
                _trackedDisposables.Clear();
            }

            if (_trackedAsyncDisposals.Count > 0)
            {
                // Fire and forget in editor context
                foreach (Func<ValueTask> producer in _trackedAsyncDisposals.ToArray())
                {
                    try
                    {
                        ValueTask? _ = producer?.Invoke();
                    }
                    catch
                    {
                        // ignore
                    }
                }
                _trackedAsyncDisposals.Clear();
            }
        }
    }
}
