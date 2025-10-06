namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using DataStructure.Adapters;
    using Extension;
    using Random;
    using UnityEngine;
    using Utils;
    using Object = UnityEngine.Object;
#if !SINGLE_THREADED
    using System.Collections.Concurrent;
#endif
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditorInternal;
#endif
    public static partial class Helpers
    {
        private static readonly WaitForEndOfFrame WaitForEndOfFrame = new();
#if SINGLE_THREADED
        private static readonly Dictionary<
            Type,
            Func<object, object[], object>
        > AwakeMethodsByType = new();
#else
        private static readonly ConcurrentDictionary<
            Type,
            Func<object, object[], object>
        > AwakeMethodsByType = new();
#endif
        private static readonly Object LogObject = new();
        private static readonly Dictionary<string, Object> ObjectsByTag = new(
            StringComparer.Ordinal
        );

        internal static readonly Dictionary<string, string[]> CachedLabels = new(
            StringComparer.OrdinalIgnoreCase
        );

        private static string[] CachedLayerNames = Array.Empty<string>();
        private static bool LayerCacheInitialized;

#if UNITY_EDITOR
        private static readonly string[] DefaultPrefabSearchFolders =
        {
            "Assets/Prefabs",
            "Assets/Resources",
        };

        private static readonly string[] DefaultScriptableObjectSearchFolders =
        {
            "Assets/Prefabs",
            "Assets/Resources",
            "Assets/TileMaps",
        };
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CLearLayerNames()
        {
            CachedLayerNames = Array.Empty<string>();
            LayerCacheInitialized = false;
        }

        public static bool IsRunningInBatchMode => Application.isBatchMode;

        public static bool IsRunningInContinuousIntegration
        {
            get
            {
                if (
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
                )
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JENKINS_URL")))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITLAB_CI")))
                {
                    return true;
                }

                return false;
            }
        }

        internal static string[] AllSpriteLabels { get; private set; } = Array.Empty<string>();
        private static bool SpriteLabelCacheInitialized;

        public static string[] GetAllSpriteLabelNames()
        {
            if (IsRunningInContinuousIntegration || IsRunningInBatchMode)
            {
                return Array.Empty<string>();
            }

#if UNITY_EDITOR
            if (SpriteLabelCacheInitialized)
            {
                return AllSpriteLabels;
            }

            using PooledResource<List<string>> labelBuffer = Buffers<string>.List.Get();
            CollectSpriteLabels(labelBuffer.resource);
            return AllSpriteLabels;
#else
            return Array.Empty<string>();
#endif
        }

        public static void GetAllSpriteLabelNames(List<string> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();

            if (IsRunningInContinuousIntegration || IsRunningInBatchMode)
            {
                return;
            }

#if UNITY_EDITOR
            string[] cached = SpriteLabelCacheInitialized
                ? AllSpriteLabels
                : GetAllSpriteLabelNames();

            if (cached.Length == 0)
            {
                return;
            }

            destination.AddRange(cached);
#else
            _ = destination;
#endif
        }

        public static string[] GetAllLayerNames()
        {
#if UNITY_EDITOR
            try
            {
                // Prefer the editor API when available
                string[] editorLayers = InternalEditorUtility.layers;
                if (editorLayers is { Length: > 0 })
                {
                    LayerCacheInitialized = true;
                    CachedLayerNames = editorLayers;
                    return editorLayers;
                }
            }
            catch
            {
                // Fall through to runtime-safe fallback below
            }
#endif
            if (!Application.isEditor && Application.isPlaying && LayerCacheInitialized)
            {
                return CachedLayerNames;
            }

            using PooledResource<List<string>> layerBuffer = Buffers<string>.List.Get();
            List<string> layers = layerBuffer.resource;
            for (int i = 0; i < 32; ++i)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    layers.Add(name);
                }
            }

            LayerCacheInitialized = true;
            int layerCount = layers.Count;
            if (layerCount == 0)
            {
                CachedLayerNames = Array.Empty<string>();
                return CachedLayerNames;
            }

            if (CachedLayerNames == null || CachedLayerNames.Length != layerCount)
            {
                CachedLayerNames = new string[layerCount];
            }

            for (int i = 0; i < layerCount; ++i)
            {
                CachedLayerNames[i] = layers[i];
            }

            return CachedLayerNames;
        }

        public static void GetAllLayerNames(List<string> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();

            string[] layers = GetAllLayerNames();
            if (layers.Length == 0)
            {
                return;
            }

            destination.AddRange(layers);
        }

#if UNITY_EDITOR
        private static void CollectSpriteLabels(List<string> destination)
        {
            destination.Clear();

            using PooledResource<HashSet<string>> labelSetResource = Buffers<string>.HashSet.Get();
            HashSet<string> labelSet = labelSetResource.resource;

            string[] guids = AssetDatabase.FindAssets("t:Sprite");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null)
                {
                    continue;
                }

                string[] labels = AssetDatabase.GetLabels(asset);
                if (labels.Length == 0)
                {
                    continue;
                }

                CachedLabels[path] = labels;
                labelSet.UnionWith(labels);
            }

            if (labelSet.Count == 0)
            {
                SetSpriteLabelCache(Array.Empty<string>(), alreadySorted: true);
                return;
            }

            destination.AddRange(labelSet);
            destination.Sort(StringComparer.Ordinal);
            SetSpriteLabelCache(destination, alreadySorted: true);
        }
#endif

        internal static void SetSpriteLabelCache(
            IReadOnlyCollection<string> labels,
            bool alreadySorted = false
        )
        {
            if (labels == null || labels.Count == 0)
            {
                AllSpriteLabels = Array.Empty<string>();
                SpriteLabelCacheInitialized = true;
                return;
            }

            string[] cache =
                AllSpriteLabels.Length == labels.Count ? AllSpriteLabels : new string[labels.Count];

            if (labels is IReadOnlyList<string> list)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    cache[i] = list[i];
                }
            }
            else
            {
                int index = 0;
                foreach (string label in labels)
                {
                    cache[index++] = label;
                }
            }

            if (!alreadySorted)
            {
                Array.Sort(cache, StringComparer.Ordinal);
            }

            AllSpriteLabels = cache;
            SpriteLabelCacheInitialized = true;
        }

        internal static void ResetSpriteLabelCache()
        {
            SpriteLabelCacheInitialized = false;
            AllSpriteLabels = Array.Empty<string>();
        }

        // https://gamedevelopment.tutsplus.com/tutorials/unity-solution-for-hitting-moving-targets--cms-29633
        public static Vector2 PredictCurrentTarget(
            this GameObject currentTarget,
            Vector2 launchLocation,
            float projectileSpeed,
            bool predictiveFiring,
            Vector2 targetVelocity
        )
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

            float a =
                targetVelocity.x * targetVelocity.x
                + targetVelocity.y * targetVelocity.y
                - projectileSpeed * projectileSpeed;

            float b =
                2
                * (
                    targetVelocity.x * (target.x - launchLocation.x)
                    + targetVelocity.y * (target.y - launchLocation.y)
                );

            float c =
                (target.x - launchLocation.x) * (target.x - launchLocation.x)
                + (target.y - launchLocation.y) * (target.y - launchLocation.y);

            float disc = b * b - 4 * a * c;
            if (disc < 0)
            {
                return target;
            }

            float t1 = (-1 * b + Mathf.Sqrt(disc)) / (2 * a);
            float t2 = (-1 * b - Mathf.Sqrt(disc)) / (2 * a);
            float t = Mathf.Max(t1, t2); // let us take the larger time value

            float aimX = target.x + targetVelocity.x * t;
            float aimY = target.y + targetVelocity.y * t;

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
                GameObject go => go != null ? go.GetComponent<T>() : default,
                Component c => c != null ? c.GetComponent<T>() : default,
                _ => default,
            };
        }

        public static T[] GetComponents<T>(this Object target)
        {
            return target switch
            {
                GameObject go => go != null ? go.GetComponents<T>() : Array.Empty<T>(),
                Component c => c != null ? c.GetComponents<T>() : Array.Empty<T>(),
                _ => default,
            };
        }

        public static List<T> GetComponents<T>(this Object target, List<T> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();

            switch (target)
            {
                case GameObject go when go != null:
                    go.GetComponents(buffer);
                    break;
                case Component component when component != null:
                    component.GetComponents(buffer);
                    break;
            }

            return buffer;
        }

        public static GameObject GetGameObject(this object target)
        {
            return target switch
            {
                GameObject go => go,
                Component c => c != null ? c.gameObject : null,
                _ => null,
            };
        }

        public static bool TryGetComponent<T>(this Object target, out T component)
        {
            component = default;
            return target switch
            {
                GameObject go => go != null && go.TryGetComponent(out component),
                Component c => c != null && c.TryGetComponent(out component),
                _ => false,
            };
        }

        public static GameObject FindChildGameObjectWithTag(this GameObject gameObject, string tag)
        {
            using PooledResource<List<Transform>> bufferResource = Buffers<Transform>.List.Get();
            foreach (
                Transform t in gameObject.transform.IterateOverAllChildrenRecursively(
                    bufferResource.resource,
                    includeSelf: true
                )
            )
            {
                GameObject go = t.gameObject;

                if (go.CompareTag(tag))
                {
                    return go;
                }
            }

            return null;
        }

        public static Coroutine StartFunctionAsCoroutine(
            this MonoBehaviour monoBehaviour,
            Action action,
            float updateRate,
            bool useJitter = false,
            bool waitBefore = false
        )
        {
            return monoBehaviour.StartCoroutine(
                FunctionAsCoroutine(action, updateRate, useJitter, waitBefore)
            );
        }

        private static IEnumerator FunctionAsCoroutine(
            Action action,
            float updateRate,
            bool useJitter,
            bool waitBefore
        )
        {
            bool usedJitter = false;
            while (true)
            {
                float startTime;
                if (waitBefore)
                {
                    if (useJitter && !usedJitter)
                    {
                        float delay = PRNG.Instance.NextFloat(updateRate);
                        startTime = Time.time;
                        while (!HasEnoughTimePassed(startTime, delay))
                        {
                            yield return null;
                        }

                        usedJitter = true;
                    }

                    startTime = Time.time;
                    while (!HasEnoughTimePassed(startTime, updateRate))
                    {
                        yield return null;
                    }
                }

                action();

                if (!waitBefore)
                {
                    if (useJitter && !usedJitter)
                    {
                        float delay = PRNG.Instance.NextFloat(updateRate);
                        startTime = Time.time;
                        while (!HasEnoughTimePassed(startTime, delay))
                        {
                            yield return null;
                        }

                        usedJitter = true;
                    }

                    startTime = Time.time;
                    while (!HasEnoughTimePassed(startTime, updateRate))
                    {
                        yield return null;
                    }
                }
            }
        }

        public static Coroutine ExecuteFunctionAfterDelay(
            this MonoBehaviour monoBehaviour,
            Action action,
            float delay
        )
        {
            return monoBehaviour.StartCoroutine(FunctionDelayAsCoroutine(action, delay));
        }

        public static Coroutine ExecuteFunctionNextFrame(
            this MonoBehaviour monoBehaviour,
            Action action
        )
        {
            return monoBehaviour.ExecuteFunctionAfterDelay(action, 0f);
        }

        public static Coroutine ExecuteFunctionAfterFrame(
            this MonoBehaviour monoBehaviour,
            Action action
        )
        {
            return monoBehaviour.StartCoroutine(FunctionAfterFrame(action));
        }

        public static IEnumerator ExecuteOverTime(
            Action action,
            int totalCount,
            float duration,
            bool delay = true
        )
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
                if (
                    totalExecuted < totalCount
                    && (totalExecuted + (delay ? 1f : 0f)) / totalCount <= percent
                )
                {
                    action();
                    ++totalExecuted;
                }

                yield return null;
            }

            for (; totalExecuted < totalCount; )
            {
                action();
                ++totalExecuted;
                yield return null;
            }
        }

        private static IEnumerator FunctionDelayAsCoroutine(Action action, float delay)
        {
            float startTime = Time.time;
            while (!HasEnoughTimePassed(startTime, delay))
            {
                yield return null;
            }

            action();
        }

        private static IEnumerator FunctionAfterFrame(Action action)
        {
            yield return Buffers.WaitForEndOfFrame;
            action();
        }

        public static bool HasEnoughTimePassed(float timestamp, float desiredDuration)
        {
            return timestamp + desiredDuration < Time.time;
        }

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
            int xStart = bounds.xMin - padding;
            int xEnd = bounds.xMax + padding;
            int yStart = bounds.yMin - padding;
            int yEnd = bounds.yMax + padding;

            for (int x = xStart; x <= xEnd; ++x)
            {
                for (int y = yStart; y <= yEnd; ++y)
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
            return new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z);
        }

        public static Vector3Int AsVector3Int(this Vector3 vector)
        {
            return new Vector3Int(
                (int)Math.Round(vector.x),
                (int)Math.Round(vector.y),
                (int)Math.Round(vector.z)
            );
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

        public static Rect AsRect(this BoundsInt bounds)
        {
            return new Rect(bounds.x, bounds.y, bounds.size.x, bounds.size.y);
        }

        public static Vector2 GetRandomPointInCircle(
            Vector2 center,
            float radius,
            IRandom random = null
        )
        {
            random ??= PRNG.Instance;
            double r = radius * Math.Sqrt(random.NextDouble());
            double theta = random.NextDouble() * 2 * Math.PI;
            return new Vector2(
                center.x + (float)(r * Math.Cos(theta)),
                center.y + (float)(r * Math.Sin(theta))
            );
        }

        public static Vector3 GetRandomPointInSphere(
            Vector3 center,
            float radius,
            IRandom random = null
        )
        {
            random ??= PRNG.Instance;
            double u = random.NextDouble();
            double v = random.NextDouble();
            double theta = 2 * Math.PI * u;
            double phi = Math.Acos(2 * v - 1);
            double r = radius * Math.Pow(random.NextDouble(), 1.0 / 3.0);
            double sinPhi = Math.Sin(phi);
            return new Vector3(
                center.x + (float)(r * sinPhi * Math.Cos(theta)),
                center.y + (float)(r * sinPhi * Math.Sin(theta)),
                center.z + (float)(r * Math.Cos(phi))
            );
        }

        public static GameObject GetPlayerObjectInChildHierarchy(
            this GameObject gameObject,
            string playerTag = "Player"
        )
        {
            return gameObject.GetTagObjectInChildHierarchy(playerTag);
        }

        public static GameObject GetTagObjectInChildHierarchy(
            this GameObject gameObject,
            string tag
        )
        {
            using PooledResource<List<Transform>> bufferResource = Buffers<Transform>.List.Get();
            foreach (
                Transform t in gameObject.transform.IterateOverAllChildrenRecursively(
                    bufferResource.resource,
                    includeSelf: true
                )
            )
            {
                GameObject go = t.gameObject;
                if (go.CompareTag(tag))
                {
                    return go;
                }
            }

            return null;
        }

        //https://answers.unity.com/questions/722748/refreshing-the-polygon-collider-2d-upon-sprite-cha.html
        public static void UpdateShapeToSprite(this Component component)
        {
            if (
                !component.TryGetComponent(out SpriteRenderer spriteRenderer)
                || !component.TryGetComponent(out PolygonCollider2D collider)
            )
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

            using PooledResource<List<Vector2>> pathResource = Buffers<Vector2>.List.Get();
            List<Vector2> path = pathResource.resource;
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

        public static GameObject TryGetClosestParentWithComponentIncludingSelf<T>(
            this GameObject current
        )
            where T : Component
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
        private static string[] PrepareSearchFolders(
            IEnumerable<string> assetPaths,
            string[] defaultFolders,
            out PooledResource<List<string>> listResource,
            out PooledResource<string[]> arrayResource
        )
        {
            listResource = default;
            arrayResource = default;

            if (assetPaths == null)
            {
                return defaultFolders;
            }

            if (assetPaths is string[] array)
            {
                return array;
            }

            if (assetPaths is ICollection<string> collection)
            {
                arrayResource = WallstopFastArrayPool<string>.Get(
                    collection.Count,
                    out string[] buffer
                );
                int index = 0;
                foreach (string path in collection)
                {
                    buffer[index++] = path;
                }

                return buffer;
            }

            listResource = Buffers<string>.List.Get();
            List<string> list = listResource.resource;
            foreach (string path in assetPaths)
            {
                list.Add(path);
            }

            arrayResource = WallstopFastArrayPool<string>.Get(list.Count, out string[] temp);
            list.CopyTo(temp);
            return temp;
        }

        public static IEnumerable<GameObject> EnumeratePrefabs(
            IEnumerable<string> assetPaths = null
        )
        {
            string[] searchFolders = PrepareSearchFolders(
                assetPaths,
                DefaultPrefabSearchFolders,
                out PooledResource<List<string>> pathListResource,
                out PooledResource<string[]> pathArrayResource
            );

            try
            {
                foreach (string assetGuid in AssetDatabase.FindAssets("t:prefab", searchFolders))
                {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null)
                    {
                        yield return go;
                    }
                }
            }
            finally
            {
                pathArrayResource.Dispose();
                pathListResource.Dispose();
            }
        }

        public static IEnumerable<T> EnumerateScriptableObjects<T>(
            IEnumerable<string> assetPaths = null
        )
            where T : ScriptableObject
        {
            string[] searchFolders = PrepareSearchFolders(
                assetPaths,
                DefaultScriptableObjectSearchFolders,
                out PooledResource<List<string>> pathListResource,
                out PooledResource<string[]> pathArrayResource
            );

            try
            {
                foreach (
                    string assetGuid in AssetDatabase.FindAssets(
                        "t:" + typeof(T).Name,
                        searchFolders
                    )
                )
                {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                    T so = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (so != null)
                    {
                        yield return so;
                    }
                }
            }
            finally
            {
                pathArrayResource.Dispose();
                pathListResource.Dispose();
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

            if (string.Equals(lhs.name, rhs.name, StringComparison.Ordinal))
            {
                return true;
            }

            const string clone = "(Clone)";
            string lhsName = lhs.name;
            while (lhsName.EndsWith(clone, StringComparison.Ordinal))
            {
                lhsName = lhsName.Substring(0, lhsName.Length - clone.Length);
                lhsName = lhsName.Trim();
            }

            string rhsName = rhs.name;
            while (rhsName.EndsWith(clone, StringComparison.Ordinal))
            {
                rhsName = rhsName.Substring(0, rhsName.Length - clone.Length);
                rhsName = rhsName.Trim();
            }

            return string.Equals(lhsName, rhsName, StringComparison.Ordinal);
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
            using PooledResource<List<MonoBehaviour>> componentResource =
                Buffers<MonoBehaviour>.List.Get();
            List<MonoBehaviour> components = componentResource.resource;
            gameObject.GetComponentsInChildren(false, components);
            foreach (MonoBehaviour script in components)
            {
                Func<object, object[], object> awakeInfo = AwakeMethodsByType.GetOrAdd(
                    script.GetType(),
                    type =>
                    {
                        MethodInfo[] methods = type.GetMethods(
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                        foreach (MethodInfo method in methods)
                        {
                            if (
                                string.Equals(method.Name, "Awake", StringComparison.Ordinal)
                                && method.GetParameters().Length == 0
                            )
                            {
                                return ReflectionHelpers.GetMethodInvoker(method);
                            }
                        }

                        return null;
                    }
                );
                _ = awakeInfo?.Invoke(script, null);
            }
        }

        public static Vector2 GetAngleWithSpeed(
            Vector2 targetDirection,
            Vector2 currentDirection,
            float rotationSpeed
        )
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
