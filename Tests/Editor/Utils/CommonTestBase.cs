namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Common test base that tracks spawned Unity objects and disposables
    /// and cleans them up safely across NUnit and Unity test teardowns.
    /// Editor assembly copy.
    /// </summary>
    public abstract class CommonTestBase
    {
        [SetUp]
        public virtual void BaseSetUp()
        {
#if REFLEX_PRESENT
            Type supportType = Type.GetType(
                "WallstopStudios.UnityHelpers.Tests.TestUtils.ReflexTestSupport, WallstopStudios.UnityHelpers.Tests.Runtime",
                throwOnError: false
            );
            MethodInfo ensureMethod = supportType?.GetMethod(
                "EnsureReflexSettings",
                BindingFlags.Public | BindingFlags.Static
            );
            ensureMethod?.Invoke(null, null);
#endif
        }

        protected readonly List<Object> _trackedObjects = new();
        protected readonly List<IDisposable> _trackedDisposables = new();
        protected readonly List<Scene> _trackedScenes = new();

        protected T CreateScriptableObject<T>()
            where T : ScriptableObject
        {
            return Track(ScriptableObject.CreateInstance<T>());
        }

        protected GameObject NewGameObject(string name = "GameObject")
        {
            return Track(new GameObject(name));
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
            {
                scene = SceneManager.CreateScene(name);
            }
#else
            scene = SceneManager.CreateScene(name);
#endif

            if (setActive)
            {
                SceneManager.SetActiveScene(scene);
            }

            _trackedScenes.Add(scene);
            return scene;
        }

        protected GameObject Track(GameObject obj)
        {
            if (obj != null)
            {
                _trackedObjects.Add(obj);
            }
            return obj;
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
        public virtual void TearDown()
        {
            // Close any tracked scenes first to ensure objects are not resurrected
#if UNITY_EDITOR
            if (_trackedScenes.Count > 0)
            {
                for (int i = _trackedScenes.Count - 1; i >= 0; i--)
                {
                    Scene scene = _trackedScenes[i];
                    if (scene.IsValid())
                    {
                        try
                        {
                            if (SceneManager.GetActiveScene() == scene)
                            {
                                int count = SceneManager.sceneCount;
                                for (int j = 0; j < count; j++)
                                {
                                    Scene candidate = SceneManager.GetSceneAt(j);
                                    if (
                                        candidate.IsValid()
                                        && candidate.isLoaded
                                        && candidate != scene
                                    )
                                    {
                                        SceneManager.SetActiveScene(candidate);
                                        break;
                                    }
                                }
                                if (SceneManager.sceneCount == 1)
                                {
                                    continue;
                                }
                            }
                            EditorSceneManager.CloseScene(scene, true);
                        }
                        catch { }
                    }
                }
                _trackedScenes.Clear();
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
                    catch { }
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
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
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
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            // Close tracked scenes as a final safety net
#if UNITY_EDITOR
            if (_trackedScenes.Count > 0)
            {
                for (int i = _trackedScenes.Count - 1; i >= 0; i--)
                {
                    Scene scene = _trackedScenes[i];
                    if (scene.IsValid())
                    {
                        try
                        {
                            if (SceneManager.GetActiveScene() == scene)
                            {
                                int count = SceneManager.sceneCount;
                                for (int j = 0; j < count; j++)
                                {
                                    Scene candidate = SceneManager.GetSceneAt(j);
                                    if (
                                        candidate.IsValid()
                                        && candidate.isLoaded
                                        && candidate != scene
                                    )
                                    {
                                        SceneManager.SetActiveScene(candidate);
                                        break;
                                    }
                                }
                                if (SceneManager.sceneCount == 1)
                                {
                                    continue;
                                }
                            }
                            EditorSceneManager.CloseScene(scene, true);
                        }
                        catch { }
                    }
                }
                _trackedScenes.Clear();
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
                    catch { }
                }
                _trackedDisposables.Clear();
            }
        }
    }
}
