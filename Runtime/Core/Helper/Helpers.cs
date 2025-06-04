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
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditorInternal;
#endif
    public static partial class Helpers
    {
        private static readonly WaitForEndOfFrame WaitForEndOfFrame = new();
        private static readonly Dictionary<Type, MethodInfo> AwakeMethodsByType = new();
        private static readonly Object LogObject = new();
        private static readonly Dictionary<string, Object> ObjectsByTag = new(
            StringComparer.Ordinal
        );

        internal static readonly Dictionary<string, string[]> CachedLabels = new(
            StringComparer.OrdinalIgnoreCase
        );

        public static bool IsRunningOnGitHubActions
        {
            get
            {
                string ghActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
                return string.Equals(
                    ghActions?.Trim(),
                    bool.TrueString,
                    StringComparison.OrdinalIgnoreCase
                );
            }
        }

        internal static string[] AllSpriteLabels;

        public static string[] GetAllSpriteLabelNames()
        {
            if (IsRunningOnGitHubActions)
            {
                return Array.Empty<string>();
            }

#if UNITY_EDITOR
            if (AllSpriteLabels != null)
            {
                return AllSpriteLabels;
            }

            HashSet<string> allLabels = new(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:Sprite");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null)
                {
                    continue;
                }

                string[] labels = AssetDatabase.GetLabels(asset);
                if (labels.Length != 0)
                {
                    CachedLabels[path] = labels;
                    allLabels.UnionWith(labels);
                }
            }

            AllSpriteLabels = allLabels.ToArray();
            Array.Sort(AllSpriteLabels);
            return AllSpriteLabels;
#else
            return Array.Empty<string>();
#endif
        }

        public static string[] GetAllLayerNames()
        {
#if UNITY_EDITOR
            return InternalEditorUtility.layers;
#else
            return Array.Empty<string>();
#endif
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
                _ => null,
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
            return gameObject
                .transform.IterateOverAllChildrenRecursively(includeSelf: true)
                .Select(t => t.gameObject)
                .FirstOrDefault(child => child.CompareTag(tag));
        }

        public static bool HasLineOfSight(
            Vector2 initialLocation,
            Vector2 direction,
            Transform transform,
            float totalDistance,
            float delta
        )
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
            yield return WaitForEndOfFrame;
            action();
        }

        public static bool HasEnoughTimePassed(float timestamp, float desiredDuration) =>
            timestamp + desiredDuration < Time.time;

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

        public static T CopyTo<T>(this T original, GameObject destination)
            where T : Component
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
                    original.LogWarn($"Failed to copy public field {field.Name}.");
                }
            }

            foreach (
                FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(field => Attribute.IsDefined(field, typeof(SerializeField)))
            )
            {
                try
                {
                    field.SetValue(copied, field.GetValue(original));
                }
                catch
                {
                    original.LogWarn($"Failed to copy non-public field {field.Name}.");
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
                    original.LogWarn($"Failed to copy property {property.Name}.");
                }
            }

            return copied;
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
            return gameObject
                .transform.IterateOverAllChildrenRecursively(includeSelf: true)
                .Select(t => t.gameObject)
                .FirstOrDefault(go => go.CompareTag(tag));
        }

        //https://answers.unity.com/questions/722748/refreshing-the-polygon-collider-2d-upon-sprite-cha.html
        public static void UpdateShapeToSprite(this Component component)
        {
            if (
                !component.TryGetComponent(out SpriteRenderer spriteRenderer)
                || component.TryGetComponent(out PolygonCollider2D collider)
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
        public static IEnumerable<GameObject> EnumeratePrefabs(
            IEnumerable<string> assetPaths = null
        )
        {
            assetPaths ??= new[] { "Assets/Prefabs", "Assets/Resources" };

            foreach (string assetGuid in AssetDatabase.FindAssets("t:prefab", assetPaths.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    yield return go;
                }
            }
        }

        public static IEnumerable<T> EnumerateScriptableObjects<T>(string[] assetPaths = null)
            where T : ScriptableObject
        {
            assetPaths ??= new[] { "Assets/Prefabs", "Assets/Resources", "Assets/TileMaps" };

            foreach (
                string assetGuid in AssetDatabase.FindAssets("t:" + typeof(T).Name, assetPaths)
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
                MethodInfo awakeInfo = AwakeMethodsByType.GetOrAdd(
                    script.GetType(),
                    type =>
                        type.GetMethod(
                            "Awake",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        )
                );
                if (awakeInfo != null)
                {
                    _ = awakeInfo.Invoke(script, null);
                }
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
