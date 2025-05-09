namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using Extension;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public static partial class Helpers
    {
        public static T Find<T>(this Object component, string tag, bool log = true)
            where T : Object
        {
            if (ObjectsByTag.TryGetValue(tag, out Object value))
            {
                if (value != null && value is T typed)
                {
                    return typed;
                }

                _ = ObjectsByTag.Remove(tag);
            }

            GameObject gameObject = GameObject.FindGameObjectWithTag(tag);
            if (gameObject == null)
            {
                if (log)
                {
                    component.LogWarn($"Could not find {tag}.");
                }

                return default;
            }

            if (gameObject.TryGetComponent(out T instance))
            {
                ObjectsByTag[tag] = instance;
                return instance;
            }

            if (log)
            {
                component.LogWarn(
                    $"Failed to find {typeof(T).Name} on {tag} (name: {gameObject.name}), id [{gameObject.GetInstanceID()}]."
                );
            }

            return default;
        }

        public static T Find<T>(string tag, bool log = true)
            where T : MonoBehaviour
        {
            if (ObjectsByTag.TryGetValue(tag, out Object value))
            {
                if (value != null && value is T typed)
                {
                    return typed;
                }

                _ = ObjectsByTag.Remove(tag);
            }

            GameObject gameObject = GameObject.FindGameObjectWithTag(tag);
            if (gameObject == null)
            {
                if (log)
                {
                    LogObject.Log($"Could not find {tag}.");
                }

                return default;
            }

            if (gameObject.TryGetComponent(out T instance))
            {
                ObjectsByTag[tag] = instance;
                return instance;
            }

            if (log)
            {
                LogObject.Log($"Failed to find {typeof(T).Name} on {tag}");
            }

            return default;
        }

        public static void SetInstance<T>(string tag, T instance)
            where T : MonoBehaviour
        {
            ObjectsByTag[tag] = instance;
        }

        public static void ClearInstance<T>(string tag, T instance)
            where T : MonoBehaviour
        {
            if (ObjectsByTag.TryGetValue(tag, out Object existing) && existing == instance)
            {
                _ = ObjectsByTag.Remove(tag);
            }
        }

        public static bool HasComponent<T>(this Object unityObject)
            where T : Object
        {
            return unityObject switch
            {
                GameObject go => go.HasComponent<T>(),
                Component component => component.HasComponent<T>(),
                _ => false,
            };
        }

        public static bool HasComponent<T>(this Component component)
            where T : Object
        {
            return component.TryGetComponent<T>(out _);
        }

        public static bool HasComponent<T>(this GameObject gameObject)
            where T : Object
        {
            return gameObject.TryGetComponent<T>(out _);
        }

        public static bool HasComponent(this Object unityObject, Type type)
        {
            return unityObject switch
            {
                GameObject go => go.TryGetComponent(type, out _),
                Component component => component.TryGetComponent(type, out _),
                _ => false,
            };
        }

        public static void EnableRecursively<T>(
            this Component component,
            bool enabled,
            Func<T, bool> exclude = null
        )
            where T : Behaviour
        {
            if (component == null)
            {
                return;
            }

            foreach (T behaviour in component.GetComponents<T>())
            {
                if (behaviour != null && !(exclude?.Invoke(behaviour) ?? false))
                {
                    behaviour.enabled = enabled;
                }
            }

            Transform transform = component as Transform ?? component.transform;
            if (transform == null)
            {
                return;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                EnableRecursively<T>(child, enabled, exclude);
            }
        }

        public static void EnableRendererRecursively<T>(
            this Component component,
            bool enabled,
            Func<T, bool> exclude = null
        )
            where T : Renderer
        {
            if (component == null)
            {
                return;
            }

            T behavior = component as T ?? component.GetComponent<T>();
            if (behavior != null && !(exclude?.Invoke(behavior) ?? false))
            {
                behavior.enabled = enabled;
            }

            Transform transform = component as Transform ?? component.transform;
            if (transform == null)
            {
                return;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                EnableRendererRecursively<T>(child, enabled, exclude);
            }
        }

        public static void DestroyAllChildrenGameObjects(this GameObject gameObject)
        {
            if (Application.isEditor)
            {
                EditorDestroyAllChildrenGameObjects(gameObject);
            }
            else
            {
                PlayDestroyAllChildrenGameObjects(gameObject);
            }
        }

        public static void DestroyAllComponentsOfType<T>(this GameObject gameObject)
            where T : Component
        {
            foreach (T component in gameObject.GetComponents<T>())
            {
                SmartDestroy(component);
            }
        }

        public static void SmartDestroy(this Object obj, float? afterTime = null)
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
            }
            else
            {
                if (afterTime.HasValue)
                {
                    Object.Destroy(obj, afterTime.Value);
                }
                else
                {
                    Object.Destroy(obj);
                }
            }
        }

        public static void DestroyAllChildrenGameObjectsImmediatelyConditionally(
            this GameObject gameObject,
            Func<GameObject, bool> acceptancePredicate
        )
        {
            InternalDestroyAllChildrenGameObjects(
                gameObject,
                toDestroy =>
                {
                    if (!acceptancePredicate(toDestroy))
                    {
                        return;
                    }

                    Object.DestroyImmediate(toDestroy);
                }
            );
        }

        public static void DestroyAllChildGameObjectsConditionally(
            this GameObject gameObject,
            Func<GameObject, bool> acceptancePredicate
        )
        {
            InternalDestroyAllChildrenGameObjects(
                gameObject,
                toDestroy =>
                {
                    if (!acceptancePredicate(toDestroy))
                    {
                        return;
                    }

                    toDestroy.Destroy();
                }
            );
        }

        public static void DestroyAllChildrenGameObjectsImmediately(this GameObject gameObject) =>
            InternalDestroyAllChildrenGameObjects(gameObject, Object.DestroyImmediate);

        public static void PlayDestroyAllChildrenGameObjects(this GameObject gameObject) =>
            InternalDestroyAllChildrenGameObjects(gameObject, go => go.Destroy());

        public static void EditorDestroyAllChildrenGameObjects(this GameObject gameObject) =>
            InternalDestroyAllChildrenGameObjects(gameObject, go => go.Destroy());

        private static void InternalDestroyAllChildrenGameObjects(
            this GameObject gameObject,
            Action<GameObject> destroyFunction
        )
        {
            for (int i = gameObject.transform.childCount - 1; 0 <= i; --i)
            {
                destroyFunction(gameObject.transform.GetChild(i).gameObject);
            }
        }

        public static bool IsPrefab(this GameObject gameObject)
        {
            Scene scene = gameObject.scene;
#if UNITY_EDITOR
            if (scene.rootCount == 1 && string.Equals(scene.name, gameObject.name))
            {
                return true;
            }

            return PrefabUtility.GetPrefabAssetType(gameObject) switch
            {
                PrefabAssetType.NotAPrefab => false,
                PrefabAssetType.MissingAsset => scene.rootCount == 0,
                _ => true,
            };
#else
            return scene.rootCount == 0;
#endif
        }

        public static bool IsPrefab(this Component component)
        {
            return IsPrefab(component.gameObject);
        }

        public static T GetOrAddComponent<T>(this GameObject unityObject)
            where T : Component
        {
            if (!unityObject.TryGetComponent(out T instance))
            {
                instance = unityObject.AddComponent<T>();
            }

            return instance;
        }

        public static Component GetOrAddComponent(this GameObject unityObject, Type componentType)
        {
            if (!unityObject.TryGetComponent(componentType, out Component instance))
            {
                instance = unityObject.AddComponent(componentType);
            }

            return instance;
        }

        public static void ModifyAndSavePrefab(GameObject prefab, Action<GameObject> modifyAction)
        {
            if (prefab == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                string assetPath = AssetDatabase.GetAssetPath(prefab);
                GameObject content = PrefabUtility.LoadPrefabContents(assetPath);

                if (content == null)
                {
                    Debug.LogError($"Unable to load {prefab} as a prefab", prefab);
                    return;
                }

                modifyAction(content);
                _ = PrefabUtility.SaveAsPrefabAsset(content, assetPath);
                PrefabUtility.UnloadPrefabContents(content);
            }
            else
            {
                modifyAction(prefab);
                PrefabStage stage = PrefabStageUtility.GetPrefabStage(prefab);
                if (stage)
                {
                    _ = EditorSceneManager.MarkSceneDirty(stage.scene);
                }
            }
#endif
        }
    }
}
