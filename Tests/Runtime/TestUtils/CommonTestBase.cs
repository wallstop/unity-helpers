namespace WallstopStudios.UnityHelpers.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    /// <summary>
    /// Common test base that tracks spawned Unity objects and disposables
    /// and cleans them up safely across NUnit and Unity test teardowns.
    /// </summary>
    public abstract class CommonTestBase
    {
        // Per-test tracked UnityEngine.Objects
        protected readonly List<UnityEngine.Object> _trackedObjects = new();

        // Per-test tracked IDisposable instances
        protected readonly List<IDisposable> _trackedDisposables = new();

        /// <summary>
        /// Track a UnityEngine.Object for automatic cleanup.
        /// </summary>
        protected T Track<T>(T obj)
            where T : UnityEngine.Object
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
        /// NUnit teardown for non-Unity tests and EditMode cleanup.
        /// Uses DestroyImmediate when not playing and disposes disposables.
        /// </summary>
        [TearDown]
        public void TearDown()
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
                var snapshot = _trackedObjects.ToArray();
                foreach (UnityEngine.Object obj in snapshot)
                {
                    if (obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
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
            if (_trackedObjects.Count > 0)
            {
                var snapshot = _trackedObjects.ToArray();
                foreach (UnityEngine.Object obj in snapshot)
                {
                    if (obj == null)
                    {
                        continue;
                    }
                    UnityEngine.Object.Destroy(obj);
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
                var snapshot = _trackedObjects.ToArray();
                foreach (UnityEngine.Object obj in snapshot)
                {
                    if (obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
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
        }
    }
}
