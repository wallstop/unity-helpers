namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extension;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;
    using Utils;
    using Object = UnityEngine.Object;

    public static class SceneHelper
    {
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

        public static async ValueTask<DeferredDisposalResult<T>> GetObjectOfTypeInScene<T>(
            string scenePath
        )
            where T : Object
        {
            DeferredDisposalResult<T[]> result = await GetAllObjectsOfTypeInScene<T>(scenePath);
            T value = result.result.Length == 0 ? default : result.result[0];
            return new DeferredDisposalResult<T>(value, result.DisposeAsync);
        }

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
                    TaskCompletionSource<bool> disposalComplete = new();
                    UnityMainThreadDispatcher.Instance.RunOnMainThread(
                        () =>
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

        public sealed class SceneLoadScope
        {
            private
#if UNITY_EDITOR
            readonly
#endif
            Scene? _openedScene;
            private readonly UnityAction<Scene, LoadSceneMode> _onSceneLoaded;
            private readonly bool _eventAdded;

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
