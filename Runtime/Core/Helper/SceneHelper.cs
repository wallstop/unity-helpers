namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extension;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;
    using Utils;
    using Object = UnityEngine.Object;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
#endif

    /// <summary>
    /// Utilities for scene discovery, loading, and object retrieval in editor and runtime.
    /// </summary>
    public static class SceneHelper
    {
        /// <summary>
        /// Returns true if a scene with the given name or path is currently loaded.
        /// </summary>
        public static bool IsSceneLoaded(string sceneNameOrPath)
        {
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (
                    string.Equals(scene.name, sceneNameOrPath, StringComparison.Ordinal)
                    || string.Equals(scene.path, sceneNameOrPath, StringComparison.Ordinal)
                )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds all scene asset paths under the specified search folders (Editor only).
        /// </summary>
        public static string[] GetAllScenePaths(string[] searchFolders = null)
        {
#if UNITY_EDITOR
            searchFolders ??= Array.Empty<string>();
            return AssetDatabase
                .FindAssets("t:Scene", searchFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
#else
            return Array.Empty<string>();
#endif
        }

        /// <summary>
        /// Returns all enabled scenes included in Build Settings (Editor only).
        /// </summary>
        public static string[] GetScenesInBuild()
        {
#if UNITY_EDITOR
            return EditorBuildSettings
                .scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
#else
            return Array.Empty<string>();
#endif
        }

        /// <summary>
        /// Loads a scene additively if needed and returns the first object of type <typeparamref name="T"/> along with a disposal callback to unload the scene.
        /// </summary>
        public static async ValueTask<DeferredDisposalResult<T>> GetObjectOfTypeInScene<T>(
            string scenePath
        )
            where T : Object
        {
            DeferredDisposalResult<T[]> result = await GetAllObjectsOfTypeInScene<T>(scenePath);
            T value = result.result.Length == 0 ? default : result.result[0];
            return new DeferredDisposalResult<T>(value, result.DisposeAsync);
        }

        /// <summary>
        /// Loads a scene additively if needed and returns all objects of type <typeparamref name="T"/> along with a disposal callback to unload the scene.
        /// </summary>
        public static async ValueTask<DeferredDisposalResult<T[]>> GetAllObjectsOfTypeInScene<T>(
            string scenePath
        )
            where T : Object
        {
            // Ensure singleton is created
            _ = UnityMainThreadDispatcher.Instance;
            TaskCompletionSource<T[]> taskCompletionSource = new();

            SceneLoadScope sceneScope = new(scenePath, OnSceneLoaded);
            T[] result = await taskCompletionSource.Task;

            return new DeferredDisposalResult<T[]>(
                result,
                async () =>
                {
                    if (!UnityMainThreadDispatcher.HasInstance)
                    {
                        await sceneScope.DisposeAsync();
                        return;
                    }

                    TaskCompletionSource<bool> disposalComplete = new();
                    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
                        _ = sceneScope
                            .DisposeAsync()
                            .WithContinuation(() => disposalComplete.SetResult(true))
                    );

                    await disposalComplete.Task;
                }
            );

            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (!string.Equals(scene.path, scenePath, StringComparison.Ordinal))
                {
                    return;
                }

                T[] foundObjects = Object
                    .FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(obj =>
                    {
                        GameObject go = obj.GetGameObject();
                        if (go == null)
                        {
                            return false;
                        }

                        return go.scene == scene;
                    })
                    .ToArray();
                taskCompletionSource.SetResult(foundObjects);
            }
        }

        /// <summary>
        /// A helper scope that ensures a target scene is loaded and provides an async disposal to unload it.
        /// </summary>
        public sealed class SceneLoadScope
        {
            private
#if UNITY_EDITOR
            readonly
#endif
            Scene? _openedScene;
            private readonly UnityAction<Scene, LoadSceneMode> _onSceneLoaded;
            private readonly bool _eventAdded;

            /// <summary>
            /// Creates the scope and ensures the target scene is loaded. If the active scene already matches, no loading occurs.
            /// </summary>
            public SceneLoadScope(string scenePath, UnityAction<Scene, LoadSceneMode> onSceneLoaded)
            {
                _onSceneLoaded = onSceneLoaded;
                _eventAdded = false;
                Scene activeScene = SceneManager.GetActiveScene();
                if (
                    !activeScene.IsValid()
                    || !activeScene.isLoaded
                    || !string.Equals(activeScene.path, scenePath, StringComparison.Ordinal)
                )
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        SceneManager.sceneLoaded += onSceneLoaded;
                        _eventAdded = true;
                        _openedScene = EditorSceneManager.LoadSceneInPlayMode(
                            scenePath,
                            new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.None)
                        );
                    }
                    else
                    {
                        _openedScene = EditorSceneManager.OpenScene(
                            scenePath,
                            OpenSceneMode.Additive
                        );
                        onSceneLoaded?.Invoke(_openedScene.Value, LoadSceneMode.Additive);
                    }
#else
                    SceneManager.sceneLoaded += onSceneLoaded;
                    _eventAdded = true;
                    SceneManager.sceneLoaded += LocalSceneLoaded;
                    SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
                    _openedScene = SceneManager.GetSceneByPath(scenePath);

                    void LocalSceneLoaded(Scene scene, LoadSceneMode mode)
                    {
                        if (!string.Equals(scene.path, scenePath, StringComparison.Ordinal))
                        {
                            return;
                        }

                        _openedScene = scene;
                        SceneManager.sceneLoaded -= LocalSceneLoaded;
                    }
#endif
                }
                else
                {
                    onSceneLoaded?.Invoke(activeScene, LoadSceneMode.Single);
                    _openedScene = null;
                }
            }

            /// <summary>
            /// Unloads the scene if it was opened by this scope; otherwise no-ops.
            /// </summary>
            public async ValueTask DisposeAsync()
            {
                if (_eventAdded)
                {
                    SceneManager.sceneLoaded -= _onSceneLoaded;
                }

                if (_openedScene == null)
                {
                    return;
                }

                Scene openedScene = _openedScene.Value;
                if (!openedScene.IsValid())
                {
                    return;
                }

                if (!openedScene.isLoaded)
                {
                    return;
                }

#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    await SceneManager.UnloadSceneAsync(openedScene, UnloadSceneOptions.None);
                }
                else
                {
                    EditorSceneManager.CloseScene(openedScene, true);
                }
#else
                await SceneManager.UnloadSceneAsync(openedScene, UnloadSceneOptions.None);
#endif
            }
        }
    }
}
