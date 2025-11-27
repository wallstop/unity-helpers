namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using NUnit.Framework;
#if UNITY_EDITOR
    using UnityEditor.SceneManagement;
#endif
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Shared test base that tracks spawned Unity objects, disposable resources,
    /// and temporary scenes across both EditMode and PlayMode tests.
    /// </summary>
    public abstract class CommonTestBase
    {
        private UnityMainThreadDispatcher.AutoCreationScope _dispatcherScope;

        protected readonly List<Object> _trackedObjects = new();
        protected readonly List<IDisposable> _trackedDisposables = new();
        protected readonly List<Scene> _trackedScenes = new();
        protected readonly List<Func<ValueTask>> _trackedAsyncDisposals = new();

        [SetUp]
        public virtual void BaseSetUp()
        {
#if REFLEX_PRESENT
            EnsureReflexSettings();
#endif
            InitializeDispatcherScope();
        }

        protected GameObject NewGameObject(string name = "GameObject")
        {
            return Track(new GameObject(name));
        }

        protected T CreateScriptableObject<T>()
            where T : ScriptableObject
        {
            return Track(ScriptableObject.CreateInstance<T>());
        }

        protected T Track<T>(T obj)
            where T : Object
        {
            if (obj != null)
            {
                _trackedObjects.Add(obj);
            }
            return obj;
        }

        protected GameObject Track(GameObject obj)
        {
            return Track<GameObject>(obj);
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

        protected Func<ValueTask> TrackAsyncDisposal(Func<ValueTask> producer)
        {
            if (producer != null)
            {
                _trackedAsyncDisposals.Add(producer);
            }
            return producer;
        }

        protected Scene CreateTempScene(string name, bool setActive = true)
        {
            Scene scene;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            else
#endif
            {
                scene = SceneManager.CreateScene(name);
            }

            if (setActive)
            {
                SceneManager.SetActiveScene(scene);
            }

            _trackedScenes.Add(scene);

            if (Application.isPlaying)
            {
                TrackAsyncDisposal(() => UnloadSceneAsync(scene));
            }

            return scene;
        }

        [TearDown]
        public virtual void TearDown()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _trackedScenes.Count > 0)
            {
                CloseTrackedScenesInEditor();
            }
#endif

            if (_trackedDisposables.Count > 0)
            {
                for (int i = _trackedDisposables.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _trackedDisposables[i]?.Dispose();
                    }
                    catch
                    {
                        // best-effort teardown
                    }
                }
                _trackedDisposables.Clear();
            }

            if (!Application.isPlaying && _trackedObjects.Count > 0)
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

            DisposeDispatcherScope();
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            if (_trackedAsyncDisposals.Count > 0)
            {
                foreach (Func<ValueTask> producer in _trackedAsyncDisposals.ToArray())
                {
                    if (producer == null)
                    {
                        continue;
                    }

                    ValueTask task = producer();
                    while (!task.IsCompleted)
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
                    yield return null;
                }
                _trackedObjects.Clear();
            }

            DisposeDispatcherScope();
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
#if UNITY_EDITOR
            if (_trackedScenes.Count > 0)
            {
                CloseTrackedScenesInEditor();
            }
#endif

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
                    try
                    {
                        _trackedDisposables[i]?.Dispose();
                    }
                    catch
                    {
                        // ignore final teardown errors
                    }
                }
                _trackedDisposables.Clear();
            }

            if (_trackedAsyncDisposals.Count > 0)
            {
                foreach (Func<ValueTask> producer in _trackedAsyncDisposals.ToArray())
                {
                    try
                    {
                        producer?.Invoke();
                    }
                    catch
                    {
                        // ignore
                    }
                }
                _trackedAsyncDisposals.Clear();
            }

            DisposeDispatcherScope();
            UnityMainThreadDispatcher.SetAutoCreationEnabled(true);
        }

#if REFLEX_PRESENT
        private static void EnsureReflexSettings()
        {
            Type supportType = Type.GetType(
                "WallstopStudios.UnityHelpers.Tests.TestUtils.ReflexTestSupport, WallstopStudios.UnityHelpers.Tests.Runtime",
                throwOnError: false
            );
            MethodInfo ensureMethod = supportType?.GetMethod(
                "EnsureReflexSettings",
                BindingFlags.Public | BindingFlags.Static
            );
            ensureMethod?.Invoke(null, null);
        }
#endif

        private void InitializeDispatcherScope()
        {
            DisposeDispatcherScope();
            _dispatcherScope = UnityMainThreadDispatcher.CreateTestScope(destroyImmediate: true);
        }

        private void DisposeDispatcherScope()
        {
            if (_dispatcherScope == null)
            {
                return;
            }

            _dispatcherScope.Dispose();
            _dispatcherScope = null;
        }

        private static async ValueTask UnloadSceneAsync(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload == null)
            {
                return;
            }

            while (!unload.isDone)
            {
                await Task.Yield();
            }
        }

#if UNITY_EDITOR
        private void CloseTrackedScenesInEditor()
        {
            for (int i = _trackedScenes.Count - 1; i >= 0; i--)
            {
                Scene scene = _trackedScenes[i];
                if (!scene.IsValid())
                {
                    continue;
                }

                try
                {
                    if (SceneManager.GetActiveScene() == scene)
                    {
                        TryPromoteAnotherScene(scene);
                    }

                    EditorSceneManager.CloseScene(scene, true);
                }
                catch
                {
                    // ignore
                }
            }

            _trackedScenes.Clear();
        }

        private static void TryPromoteAnotherScene(Scene current)
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (candidate.IsValid() && candidate.isLoaded && candidate != current)
                {
                    SceneManager.SetActiveScene(candidate);
                    return;
                }
            }
        }
#endif
    }
}
