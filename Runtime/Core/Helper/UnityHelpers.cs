namespace UnityHelpers.Core.Helper
{
    using Extension;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using DataStructure.Adapters;
    using Random;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
#endif
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Utils;
    using Object = UnityEngine.Object;

    public static partial class UnityHelpers
    {
        private static readonly WaitForEndOfFrame WaitForEndOfFrame = new();
        private static readonly Dictionary<Type, MethodInfo> AwakeMethodsByType = new();
        private static readonly Object LogObject = new();
        private static readonly Dictionary<string, Object> ObjectsByTag = new();

        public static T Find<T>(this Object component, string tag, bool log = true) where T : Object
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
                    component.LogWarn("Could not find {0}.", tag);
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
                component.LogWarn("Failed to find {0} on {1} (name: {2}), id [{3}].", typeof(T).Name, tag, gameObject.name, gameObject.GetInstanceID());
            }

            return default;
        }

        public static T Find<T>(string tag, bool log = true) where T : MonoBehaviour
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

        public static void SetInstance<T>(string tag, T instance) where T : MonoBehaviour
        {
            ObjectsByTag[tag] = instance;
        }

        public static void ClearInstance<T>(string tag, T instance) where T : MonoBehaviour
        {
            if (ObjectsByTag.TryGetValue(tag, out Object existing) && existing == instance)
            {
                _ = ObjectsByTag.Remove(tag);
            }
        }

        public static bool HasComponent<T>(this Object unityObject)
        {
            return (unityObject) switch
            {
                GameObject go => go.HasComponent<T>(),
                Component component => component.HasComponent<T>(),
                _ => false
            };
        }

        public static bool HasComponent<T>(this Component component)
        {
            return component.TryGetComponent<T>(out _);
        }

        public static bool HasComponent<T>(this GameObject gameObject)
        {
            return gameObject.TryGetComponent<T>(out _);
        }

        public static bool HasComponent(this GameObject gameObject, Type type)
        {
            return gameObject.TryGetComponent(type, out _);
        }

        public static void LogNotAssigned(this Object component, string name)
        {
            component.LogWarn("{0} not found.", name);
        }

        public static IEnumerable<GameObject> IterateOverChildGameObjects(this GameObject gameObject)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                yield return gameObject.transform.GetChild(i).gameObject;
            }
        }

        public static IEnumerable<GameObject> IterateOverChildGameObjectsRecursively(this GameObject gameObject)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject child = gameObject.transform.GetChild(i).gameObject;
                yield return child;
                foreach (GameObject go in child.IterateOverChildGameObjectsRecursively())
                {
                    yield return go;
                }
            }
        }

        public static IEnumerable<GameObject> IterateOverChildGameObjectsRecursivelyIncludingSelf(this GameObject gameObject)
        {
            yield return gameObject;

            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                GameObject child = gameObject.transform.GetChild(i).gameObject;
                foreach (GameObject c in child.IterateOverChildGameObjectsRecursivelyIncludingSelf())
                {
                    yield return c;
                }
            }
        }

        public static IEnumerable<GameObject> IterateOverParentGameObjects(this GameObject gameObject)
        {
            Transform currentTransform = gameObject.transform.parent;
            while (currentTransform != null)
            {
                yield return currentTransform.gameObject;
                currentTransform = currentTransform.parent;
            }
        }

        public static IEnumerable<GameObject> IterateOverParentGameObjectsRecursivelyIncludingSelf(this GameObject gameObject)
        {
            yield return gameObject;

            foreach (GameObject parent in IterateOverParentGameObjects(gameObject))
            {
                yield return parent;
            }
        }

        public static void EnableRecursively<T>(this Component component, bool enabled, Func<T, bool> exclude = null)
            where T : Behaviour
        {
            if (component == null)
            {
                return;
            }

            foreach (T behaviour in component.GetComponents<T>())
            {
                if (behaviour != null && (exclude?.Invoke(behaviour) ?? true))
                {
                    behaviour.enabled = enabled;
                }
            }

            Transform transform = (component as Transform) ?? component.transform;
            if (transform == null)
            {
                return;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                EnableRecursively<T>(child, enabled);
            }
        }

        public static void EnableRendererRecursively<T>(this Component component, bool enabled,
            Func<T, bool> exclude = null) where T : Renderer
        {
            if (component == null)
            {
                return;
            }

            T behavior = component as T ?? component.GetComponent<T>();
            if (behavior != null && (exclude?.Invoke(behavior) ?? true))
            {
                behavior.enabled = enabled;
            }

            Transform transform = (component as Transform) ?? component.transform;
            if (transform == null)
            {
                return;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                EnableRendererRecursively<T>(child, enabled);
            }
        }

        public static IEnumerable<T> IterateOverAllChildComponentsRecursively<T>(this Component component)
        {
            if (component == null)
            {
                yield break;
            }

            foreach (T c in component.gameObject.GetComponents<T>())
            {
                yield return c;
            }

            for (int i = 0; i < component.transform.childCount; ++i)
            {
                Transform child = component.transform.GetChild(i);

                foreach (T c in child.IterateOverAllChildComponentsRecursively<T>())
                {
                    yield return c;
                }
            }
        }

        public static IEnumerable<Transform> IterateOverAllChildren(this Component component)
        {
            if (component == null)
            {
                yield break;
            }

            for (int i = 0; i < component.transform.childCount; ++i)
            {
                yield return component.transform.GetChild(i);
            }
        }

        public static IEnumerable<Transform> IterateOverAllParents(this Component component)
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            while (transform.parent != null)
            {
                yield return transform.parent;
                transform = transform.parent;
            }
        }

        public static IEnumerable<Transform> IterateOverAllParentsIncludingSelf(this Component component)
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            while (transform != null)
            {
                yield return transform;
                transform = transform.parent;
            }
        }

        public static IEnumerable<Transform> IterateOverAllChildrenRecursively(this Component component)
        {
            if (component == null)
            {
                yield break;
            }

            for (int i = 0; i < component.transform.childCount; ++i)
            {
                Transform childTransform = component.transform.GetChild(i);
                yield return childTransform;
                foreach (Transform childChildTransform in childTransform.IterateOverAllChildrenRecursively())
                {
                    yield return childChildTransform;
                }
            }
        }

        public static bool IsLeft(Vector2 a, Vector2 b, Vector2 point)
        {
            // http://alienryderflex.com/point_left_of_ray/

            //check which side of line AB the point P is on
            if ((b.x - a.x) * (point.y - a.y) - (point.x - a.x) * (b.y - a.y) > 0)
            {
                return false;
            }

            return true;
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

        public static void DestroyAllComponentsOfType<T>(this GameObject gameObject) where T : Component
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

        public static void DestroyAllChildrenGameObjectsImmediatelyConditionally(this GameObject gameObject,
            Func<GameObject, bool> acceptancePredicate)
        {
            InternalDestroyAllChildrenGameObjects(gameObject, toDestroy =>
            {
                if (!acceptancePredicate(toDestroy))
                {
                    return;
                }

                Object.DestroyImmediate(toDestroy);
            });
        }

        public static void DestroyAllChildGameObjectsConditionally(this GameObject gameObject,
            Func<GameObject, bool> acceptancePredicate)
        {
            InternalDestroyAllChildrenGameObjects(gameObject, toDestroy =>
            {
                if (!acceptancePredicate(toDestroy))
                {
                    return;
                }
                toDestroy.Destroy();
            });
        }

        public static void DestroyAllChildrenGameObjectsImmediately(this GameObject gameObject) =>
            InternalDestroyAllChildrenGameObjects(gameObject, Object.DestroyImmediate);

        public static void PlayDestroyAllChildrenGameObjects(this GameObject gameObject) =>
            InternalDestroyAllChildrenGameObjects(gameObject, go => go.Destroy());

        public static void EditorDestroyAllChildrenGameObjects(this GameObject gameObject) =>
            InternalDestroyAllChildrenGameObjects(gameObject, go => go.Destroy());

        private static void InternalDestroyAllChildrenGameObjects(this GameObject gameObject,
            Action<GameObject> destroyFunction)
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
#endif
            return scene.rootCount == 0;
        }

        public static bool IsPrefab(this Component component)
        {
            return IsPrefab(component.gameObject);
        }

        public static Vector2 RadianToVector2(float radian)
        {
            return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        }

        public static Vector2 DegreeToVector2(float degree)
        {
            return RadianToVector2(degree * Mathf.Deg2Rad);
        }

        public static T GetOrAddComponent<T>(this GameObject unityObject) where T : Component
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
                    Debug.LogError($"Unable to load {prefab} as a prefab");
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

        // https://gamedevelopment.tutsplus.com/tutorials/unity-solution-for-hitting-moving-targets--cms-29633
        public static Vector2 PredictCurrentTarget(this GameObject currentTarget, Vector2 launchLocation, float projectileSpeed, bool predictiveFiring, Vector2 targetVelocity)
        {
            Vector2 target = currentTarget.transform.position;

            if (!predictiveFiring)
            {
                return target;
            }

            if (projectileSpeed <= 0)
            {
                return target;
            }

            float a = (targetVelocity.x * targetVelocity.x) + (targetVelocity.y * targetVelocity.y) - (projectileSpeed * projectileSpeed);
            
            float b = 2 * (targetVelocity.x * (target.x - launchLocation.x) +
                           targetVelocity.y * (target.y - launchLocation.y));

            float c =
                ((target.x - launchLocation.x) *
                 (target.x - launchLocation.x)) +
                ((target.y - launchLocation.y) *
                 (target.y - launchLocation.y));

            float disc = b * b - (4 * a * c);
            if (disc < 0)
            {
                return target;
            }

            float t1 = (-1 * b + Mathf.Sqrt(disc)) / (2 * a);
            float t2 = (-1 * b - Mathf.Sqrt(disc)) / (2 * a);
            float t = Mathf.Max(t1, t2); // let us take the larger time value

            float aimX = target.x + (targetVelocity.x * t);
            float aimY = target.y + (targetVelocity.y * t);

            if (float.IsNaN(aimX) || float.IsNaN(aimY))
            {
                return target;
            }

            if (float.IsInfinity(aimX) || float.IsInfinity(aimY))
            {
                return target;
            }

            return new Vector2(aimX, aimY);
        }

        public static T GetComponent<T>(this Object target)
        {
            return target switch
            {
                GameObject go => go.GetComponent<T>(),
                Component c => c.GetComponent<T>(),
                _ => default,
            };
        }

        public static T[] GetComponents<T>(this Object target)
        {
            return target switch
            {
                GameObject go => go.GetComponents<T>(),
                Component c => c.GetComponents<T>(),
                _ => default,
            };
        }

        public static GameObject GetGameObject(this object target)
        {
            return target switch
            {
                GameObject go => go,
                Component c => c.gameObject,
                _ => default
            };
        }

        public static bool TryGetComponent<T>(this Object target, out T component)
        {
            component = default;
            return target switch
            {
                GameObject go => go.TryGetComponent(out component),
                Component c => c.TryGetComponent(out component),
                _ => false,
            };
        }

        public static GameObject FindChildGameObjectWithTag(this GameObject gameObject, string tag)
        {
            return gameObject.IterateOverChildGameObjectsRecursivelyIncludingSelf().FirstOrDefault(child => child.CompareTag(tag));
        }

        public static bool HasLineOfSight(Vector2 initialLocation, Vector2 direction, Transform transform, float totalDistance, float delta)
        {
            int hits = Physics2D.RaycastNonAlloc(initialLocation, direction, Buffers.RaycastHits);
            for (int i = 0; i < hits; ++i)
            {
                RaycastHit2D hit = Buffers.RaycastHits[i];
                if (hit.transform != transform)
                {
                    continue;
                }

                float distanceToEdge = totalDistance - hit.distance;
                if (delta <= distanceToEdge)
                {
                    return false;
                }
            }

            return true;
        }

        public static Coroutine StartFunctionAsCoroutine(this MonoBehaviour monoBehaviour, Action action, float updateRate, bool useJitter = false, bool waitBefore = false)
        {
            return monoBehaviour.StartCoroutine(FunctionAsCoroutine(action, updateRate, useJitter, waitBefore));
        }

        private static IEnumerator FunctionAsCoroutine(Action action, float updateRate, bool useJitter, bool waitBefore)
        {
            bool usedJitter = false;
            WaitForSeconds wait = Buffers.WaitForSeconds.GetOrAdd(updateRate, time => new WaitForSeconds(time));

            while (true)
            {
                if (waitBefore)
                {
                    // Copy-pasta the code, no way to unify in a performant way without generating garbage
                    yield return wait;
                    if (useJitter && !usedJitter)
                    {
                        yield return new WaitForSeconds(PcgRandom.Instance.NextFloat(updateRate));
                        usedJitter = true;
                    }
                }

                action();
                if (!waitBefore)
                {
                    yield return wait;
                    if (useJitter && !usedJitter)
                    {
                        yield return new WaitForSeconds(PcgRandom.Instance.NextFloat(updateRate));
                        usedJitter = true;
                    }
                }
            }
        }

        public static Coroutine ExecuteFunctionAfterDelay(this MonoBehaviour monoBehaviour, Action action, float delay)
        {
            return monoBehaviour.StartCoroutine(FunctionDelayAsCoroutine(action, delay));
        }

        public static Coroutine ExecuteFunctionNextFrame(this MonoBehaviour monoBehaviour, Action action)
        {
            return monoBehaviour.ExecuteFunctionAfterDelay(action, 0f);
        }

        public static Coroutine ExecuteFunctionAfterFrame(this MonoBehaviour monoBehaviour, Action action)
        {
            return monoBehaviour.StartCoroutine(FunctionAfterFrame(action));
        }

        public static IEnumerator ExecuteOverTime(Action action, int totalCount, float duration, bool delay = true)
        {
            if (action == null)
            {
                yield break;
            }

            if (totalCount <= 0)
            {
                yield break;
            }

            int totalExecuted = 0;
            float startTime = Time.time;
            while (!HasEnoughTimePassed(startTime, duration))
            {
                float percent = (Time.time - startTime) / duration;
                // optional delay execution from happening on 0, 1, 2, ... n-1 to 1, 2, ... n
                if (totalExecuted < totalCount && ((totalExecuted + (delay ? 1f : 0f)) / totalCount) <= percent)
                {
                    action();
                    ++totalExecuted;
                }

                yield return null;
            }

            for (; totalExecuted < totalCount;)
            {
                action();
                ++totalExecuted;
                yield return null;
            }
        }

        private static IEnumerator FunctionDelayAsCoroutine(Action action, float delay)
        {
            yield return Buffers.WaitForSeconds.GetOrAdd(delay, time => new WaitForSeconds(time));
            action();
        }

        private static IEnumerator FunctionAfterFrame(Action action)
        {
            yield return WaitForEndOfFrame;
            action();
        }

        public static bool HasEnoughTimePassed(float timestamp, float desiredDuration) => timestamp + desiredDuration < Time.time;

        public static Vector2 Opposite(this Vector2 vector)
        {
            return vector * -1;
        }

        public static Vector3 Opposite(this Vector3 vector)
        {
            return vector * -1;
        }

        public static IEnumerable<Vector3Int> IterateArea(this BoundsInt bounds)
        {
            foreach (Vector3Int position in bounds.allPositionsWithin)
            {
                yield return position;
            }
        }

        public static IEnumerable<Vector3Int> IterateBounds(this BoundsInt bounds, int padding = 1)
        {
            for (int x = bounds.xMin - padding; x <= bounds.xMax + padding; ++x)
            {
                for (int y = bounds.yMin; y <= bounds.yMax + padding; ++y)
                {
                    yield return new Vector3Int(x, y, 0);
                }
            }
        }

        public static Vector3Int AsVector3Int(this (int x, int y, int z) vector)
        {
            return new Vector3Int(vector.x, vector.y, vector.z);
        }

        public static Vector3Int AsVector3Int(this (uint x, uint y, uint z) vector)
        {
            return new Vector3Int((int) vector.x, (int) vector.y, (int) vector.z);
        }

        public static Vector3Int AsVector3Int(this Vector3 vector)
        {
            return new Vector3Int((int) Math.Round(vector.x), (int) Math.Round(vector.y), (int) Math.Round(vector.z));
        }

        public static Vector3 AsVector3(this (uint x, uint y, uint z) vector)
        {
            return new Vector3(vector.x, vector.y, vector.z);
        }

        public static Vector3 AsVector3(this Vector3Int vector)
        {
            return new Vector3(vector.x, vector.y, vector.z);
        }

        public static Vector2 AsVector2(this Vector3Int vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        public static Vector2Int AsVector2Int(this Vector3Int vector)
        {
            return new Vector2Int(vector.x, vector.y);
        }

        public static Vector3Int AsVector3Int(this Vector2Int vector)
        {
            return new Vector3Int(vector.x, vector.y);
        }

        public static T CopyTo<T>(this T original, GameObject destination) where T : Component
        {
            Type type = original.GetType();
            T copied = destination.GetComponent(type) as T;
            if (copied == null)
            {
                copied = destination.AddComponent(type) as T;
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                try
                {
                    field.SetValue(copied, field.GetValue(original));
                }
                catch
                {
                    original.LogWarn("Failed to copy public field {0}.", field.Name);
                }
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(field => Attribute.IsDefined(field, typeof(SerializeField))))
            {
                try
                {
                    field.SetValue(copied, field.GetValue(original));
                }
                catch
                {
                    original.LogWarn("Failed to copy non-public field {0}.", field.Name);
                }
            }

            foreach (PropertyInfo property in type.GetProperties())
            {
                if (!property.CanWrite || property.Name == nameof(Object.name))
                {
                    continue;
                }

                try
                {
                    property.SetValue(copied, property.GetValue(original));
                }
                catch
                {
                    original.LogWarn("Failed to copy property {0}.", property.Name);
                }
            }

            return copied;
        }

        public static Rect AsRect(this BoundsInt bounds)
        {
            return new Rect(bounds.x, bounds.y, bounds.size.x, bounds.size.y);
        }

        public static Vector3 GetRandomPointInCircle(Vector2 center, float radius, IRandom random = null)
        {
            random ??= PcgRandom.Instance;
            float r = radius * Mathf.Sqrt(random.NextFloat());
            float theta = random.NextFloat() * 2 * Mathf.PI;
            return new Vector3(center.x + r * Mathf.Cos(theta), center.y + r * Mathf.Sin(theta), 0.0f);
        }

        public static GameObject GetPlayerObjectInChildHierarchy(this GameObject gameObject)
        {
            return gameObject.GetTagObjectInChildHierarchy("Player");
        }

        public static GameObject GetTagObjectInChildHierarchy(this GameObject gameObject, string tag)
        {
            return gameObject.IterateOverChildGameObjectsRecursivelyIncludingSelf().FirstOrDefault(go => go.CompareTag(tag));
        }

        //https://answers.unity.com/questions/722748/refreshing-the-polygon-collider-2d-upon-sprite-cha.html
        public static void UpdateShapeToSprite(this Component component)
        {
            if (!component.TryGetComponent(out SpriteRenderer spriteRenderer) || component.TryGetComponent(out PolygonCollider2D collider))
            {
                return;
            }

            UpdateShapeToSprite(spriteRenderer.sprite, collider);
        }

        public static void UpdateShapeToSprite(Sprite sprite, PolygonCollider2D collider)
        {
            if (sprite == null || collider == null)
            {
                return;
            }

            int pathCount = collider.pathCount = sprite.GetPhysicsShapeCount();

            List<Vector2> path = new();
            for (int i = 0; i < pathCount; ++i)
            {
                path.Clear();
                _ = sprite.GetPhysicsShape(i, path);
                collider.SetPath(i, path);
            }
        }

        public static Vector3Int Cross(this Vector3Int vector, Vector3Int other)
        {
            int x = vector.y * other.z - other.y * vector.z;
            int y = (vector.x * other.z - other.x * vector.z) * -1;
            int z = vector.x * other.y - other.x * vector.y;

            return new Vector3Int(x, y, z);
        }

        public static GameObject TryGetClosestParentWithComponentIncludingSelf<T>(this GameObject current) where T : Component
        {
            while (current != null)
            {
                if (current.HasComponent<T>())
                {
                    return current;
                }

                Transform parent = current.transform.parent;
                current = parent != null ? parent.gameObject : null;
            }

            return null;
        }

#if UNITY_EDITOR
        public static IEnumerable<GameObject> EnumeratePrefabs(string[] assetPaths = null)
        {
            assetPaths ??= new[] {"Assets/Prefabs", "Assets/Resources"};

            foreach (string assetGuid in AssetDatabase.FindAssets("t:prefab", assetPaths))
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    yield return go;
                }
            }
        }

        public static IEnumerable<T> EnumerateScriptableObjects<T>(string[] assetPaths = null) where T : ScriptableObject
        {
            assetPaths ??= new[] { "Assets/Prefabs", "Assets/Resources", "Assets/TileMaps" };

            foreach (string assetGuid in AssetDatabase.FindAssets("t:" + typeof(T).Name, assetPaths))
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                T so = AssetDatabase.LoadAssetAtPath<T>(path);
                if (so != null)
                {
                    yield return so;
                }
            }
        }
#endif

        public static bool NameEquals(Object lhs, Object rhs)
        {
            if (lhs == rhs)
            {
                return true;
            }

            if (lhs == null || rhs == null)
            {
                return false;
            }

            if (string.Equals(lhs.name, rhs.name))
            {
                return true;
            }

            if (lhs.name == null || rhs.name == null)
            {
                return false;
            }

            const string clone = "(Clone)";
            string lhsName = lhs.name;
            while (lhsName.EndsWith(clone))
            {
                lhsName = lhsName.Substring(lhsName.Length - clone.Length - 1);
                lhsName = lhsName.Trim();
            }

            string rhsName = rhs.name;
            while (rhsName.EndsWith(clone))
            {
                rhsName = rhsName.Substring(rhsName.Length - clone.Length - 1);
                rhsName = rhsName.Trim();
            }

            return string.Equals(lhsName, rhsName);
        }

        public static Color ChangeColorBrightness(this Color color, float correctionFactor)
        {
            correctionFactor = Math.Clamp(correctionFactor, -1f, 1f);

            float red = color.r;
            float green = color.g;
            float blue = color.b;
            if (correctionFactor < 0)
            {
                correctionFactor += 1;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (1f - red) * correctionFactor + red;
                green = (1f - green) * correctionFactor + green;
                blue = (1f - blue) * correctionFactor + blue;
            }

            return new Color(red, green, blue, color.a);
        }

        public static void AwakeObject(this GameObject gameObject)
        {
            foreach (MonoBehaviour script in gameObject.GetComponentsInChildren<MonoBehaviour>())
            {
                MethodInfo awakeInfo = AwakeMethodsByType.GetOrAdd(script.GetType(), type => type.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                if (awakeInfo != null)
                {
                    _ = awakeInfo.Invoke(script, null);
                }
            }
        }

        public static Vector2 GetAngleWithSpeed(Vector2 targetDirection, Vector2 currentDirection, float rotationSpeed)
        {
            if (targetDirection == Vector2.zero)
            {
                return currentDirection;
            }

            float turnRatePerFrame = rotationSpeed * Time.deltaTime;
            float angleDiscrepancy = Vector2.SignedAngle(currentDirection, targetDirection);
            float turnRateThisFrame;
            if (Math.Sign(angleDiscrepancy) < 0)
            {
                turnRateThisFrame = -1 * Math.Min(turnRatePerFrame, -angleDiscrepancy);
            }
            else
            {
                turnRateThisFrame = Math.Min(turnRatePerFrame, angleDiscrepancy);
            }

            float currentAngle = Vector2.SignedAngle(Vector2.right, currentDirection);
            currentAngle += turnRateThisFrame;
            return (Quaternion.AngleAxis(currentAngle, Vector3.forward) * Vector3.right).normalized;
        }

        public static void Extend2D(ref BoundsInt bounds, FastVector3Int position)
        {
            if (position.x < bounds.xMin)
            {
                bounds.xMin = position.x;
            }

            if (bounds.xMax < position.x)
            {
                bounds.xMax = position.x;
            }

            if (position.y < bounds.yMin)
            {
                bounds.yMin = position.y;
            }

            if (bounds.yMax < position.y)
            {
                bounds.yMax = position.y;
            }
        }
    }
}
