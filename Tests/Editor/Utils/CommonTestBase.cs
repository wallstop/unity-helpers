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
    /// Editor assembly copy.
    /// </summary>
    public abstract class CommonTestBase
    {
        protected readonly List<UnityEngine.Object> _trackedObjects = new();
        protected readonly List<IDisposable> _trackedDisposables = new();

        protected T Track<T>(T obj)
            where T : UnityEngine.Object
        {
            if (obj != null)
            {
                _trackedObjects.Add(obj);
            }
            return obj;
        }

        protected T TrackDisposable<T>(T disposable)
            where T : IDisposable
        {
            if (disposable != null)
            {
                _trackedDisposables.Add(disposable);
            }
            return disposable;
        }

        [TearDown]
        public void TearDown()
        {
            if (_trackedDisposables.Count > 0)
            {
                for (int i = _trackedDisposables.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _trackedDisposables[i]?.Dispose();
                    }
                    catch { }
                }
                _trackedDisposables.Clear();
            }

            if (!Application.isPlaying && _trackedObjects.Count > 0)
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
        }

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
                    yield return null;
                }
                _trackedObjects.Clear();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
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
                    try
                    {
                        _trackedDisposables[i]?.Dispose();
                    }
                    catch { }
                }
                _trackedDisposables.Clear();
            }
        }
    }
}
