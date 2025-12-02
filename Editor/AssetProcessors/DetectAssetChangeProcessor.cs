namespace WallstopStudios.UnityHelpers.Editor.AssetProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;

    internal sealed class DetectAssetChangeProcessor : AssetPostprocessor
    {
        private const string TestAssetFolderMarker = "__DetectAssetChangedTests__";

        private sealed class MethodSubscription
        {
            internal Type DeclaringType;
            internal MethodInfo Method;
            internal AssetChangeFlags Flags;
            internal bool AcceptsContext;
        }

        private sealed class AssetWatcher
        {
            internal AssetWatcher(Type assetType)
            {
                AssetType = assetType;
                KnownAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Subscriptions = new List<MethodSubscription>();
            }

            internal Type AssetType { get; }
            internal HashSet<string> KnownAssetPaths { get; }
            internal List<MethodSubscription> Subscriptions { get; }
        }

        private static readonly Dictionary<Type, AssetWatcher> WatchersByAssetType = new();
        private static bool initialized;
        private static bool includeTestAssets;

        internal static bool IncludeTestAssets
        {
            get => includeTestAssets;
            set => includeTestAssets = value;
        }

        static DetectAssetChangeProcessor()
        {
            EditorApplication.delayCall += EnsureInitialized;
        }

        internal static void ProcessChangesForTesting(
            string[] imported,
            string[] deleted,
            string[] moved,
            string[] movedFrom
        )
        {
            EnsureInitialized();
            HandleAssetChanges(
                imported ?? Array.Empty<string>(),
                deleted ?? Array.Empty<string>(),
                moved ?? Array.Empty<string>(),
                movedFrom ?? Array.Empty<string>()
            );
        }

        internal static void ResetForTesting()
        {
            initialized = false;
            WatchersByAssetType.Clear();
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            EnsureInitialized();
            if (WatchersByAssetType.Count == 0)
            {
                return;
            }

            HandleAssetChanges(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        private static void HandleAssetChanges(
            IReadOnlyList<string> importedAssets,
            IReadOnlyList<string> deletedAssets,
            IReadOnlyList<string> movedAssets,
            IReadOnlyList<string> movedFromAssetPaths
        )
        {
            foreach (AssetWatcher watcher in WatchersByAssetType.Values)
            {
                List<string> createdPaths = CollectCreatedAssets(
                    watcher.AssetType,
                    importedAssets,
                    movedAssets
                );
                List<string> deletedPaths = CollectDeletedAssets(
                    watcher,
                    deletedAssets,
                    movedFromAssetPaths
                );

                AssetChangeFlags triggeredFlags = AssetChangeFlags.None;
                if (createdPaths.Count > 0)
                {
                    triggeredFlags |= AssetChangeFlags.Created;
                }

                if (deletedPaths.Count > 0)
                {
                    triggeredFlags |= AssetChangeFlags.Deleted;
                }

                if (triggeredFlags == AssetChangeFlags.None)
                {
                    continue;
                }

                foreach (MethodSubscription subscription in watcher.Subscriptions)
                {
                    AssetChangeFlags relevant = subscription.Flags & triggeredFlags;
                    if (relevant == AssetChangeFlags.None)
                    {
                        continue;
                    }

                    AssetChangeContext context = subscription.AcceptsContext
                        ? new AssetChangeContext(
                            watcher.AssetType,
                            relevant,
                            relevant.HasFlag(AssetChangeFlags.Created)
                                ? (IReadOnlyList<string>)createdPaths
                                : Array.Empty<string>(),
                            relevant.HasFlag(AssetChangeFlags.Deleted)
                                ? (IReadOnlyList<string>)deletedPaths
                                : Array.Empty<string>()
                        )
                        : null;

                    InvokeSubscription(subscription, context);
                }

                if (createdPaths.Count > 0)
                {
                    for (int i = 0; i < createdPaths.Count; i++)
                    {
                        watcher.KnownAssetPaths.Add(createdPaths[i]);
                    }
                }

                if (deletedPaths.Count > 0)
                {
                    for (int i = 0; i < deletedPaths.Count; i++)
                    {
                        watcher.KnownAssetPaths.Remove(deletedPaths[i]);
                    }
                }
            }
        }

        private static void InvokeSubscription(
            MethodSubscription subscription,
            AssetChangeContext context
        )
        {
            object[] args = subscription.AcceptsContext
                ? new object[] { context }
                : Array.Empty<object>();
            foreach (
                UnityEngine.Object instance in EnumeratePersistedInstances(
                    subscription.DeclaringType
                )
            )
            {
                if (instance == null)
                {
                    continue;
                }

                try
                {
                    subscription.Method.Invoke(instance, args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(
                        new InvalidOperationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Failed invoking DetectAssetChanged watcher {0}.{1}",
                                subscription.DeclaringType.FullName,
                                subscription.Method.Name
                            ),
                            ex
                        ),
                        instance
                    );
                }
            }
        }

        private static IEnumerable<UnityEngine.Object> EnumeratePersistedInstances(
            Type declaringType
        )
        {
            string filter = $"t:{declaringType.Name}";
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (ShouldSkipPath(path))
                {
                    continue;
                }

                UnityEngine.Object instance = AssetDatabase.LoadAssetAtPath(path, declaringType);
                if (instance != null)
                {
                    yield return instance;
                }
            }
        }

        private static List<string> CollectCreatedAssets(
            Type assetType,
            IReadOnlyList<string> importedAssets,
            IReadOnlyList<string> movedAssets
        )
        {
            List<string> buffer = new();
            AppendCreatedAssets(assetType, importedAssets, buffer);
            AppendCreatedAssets(assetType, movedAssets, buffer);
            return buffer;
        }

        private static void AppendCreatedAssets(
            Type assetType,
            IReadOnlyList<string> candidatePaths,
            List<string> buffer
        )
        {
            if (candidatePaths == null)
            {
                return;
            }

            for (int i = 0; i < candidatePaths.Count; i++)
            {
                string path = candidatePaths[i];
                if (ShouldSkipPath(path))
                {
                    continue;
                }

                Type mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (mainType == null || !assetType.IsAssignableFrom(mainType))
                {
                    continue;
                }

                buffer.Add(path);
            }
        }

        private static List<string> CollectDeletedAssets(
            AssetWatcher watcher,
            IReadOnlyList<string> deletedAssets,
            IReadOnlyList<string> movedFromAssetPaths
        )
        {
            List<string> buffer = new();
            AppendDeletedAssets(watcher, deletedAssets, buffer);
            AppendDeletedAssets(watcher, movedFromAssetPaths, buffer);
            return buffer;
        }

        private static void AppendDeletedAssets(
            AssetWatcher watcher,
            IReadOnlyList<string> candidatePaths,
            List<string> buffer
        )
        {
            if (candidatePaths == null)
            {
                return;
            }

            for (int i = 0; i < candidatePaths.Count; i++)
            {
                string path = candidatePaths[i];
                if (ShouldSkipPath(path))
                {
                    continue;
                }

                if (!watcher.KnownAssetPaths.Contains(path))
                {
                    continue;
                }

                buffer.Add(path);
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            BuildWatchers();
        }

        private static void BuildWatchers()
        {
            WatchersByAssetType.Clear();

            IEnumerable<Type> loadedTypes = ReflectionHelpers.GetAllLoadedTypes();
            foreach (Type type in loadedTypes)
            {
                if (type == null || type.IsAbstract)
                {
                    continue;
                }

                BindingFlags flags =
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly;
                MethodInfo[] methods = type.GetMethods(flags);
                foreach (MethodInfo method in methods)
                {
                    DetectAssetChangedAttribute[] attributes = method
                        .GetCustomAttributes(typeof(DetectAssetChangedAttribute), true)
                        .OfType<DetectAssetChangedAttribute>()
                        .ToArray();
                    if (attributes.Length == 0)
                    {
                        continue;
                    }

                    if (!ValidateMethodSignature(type, method))
                    {
                        continue;
                    }

                    bool acceptsContext =
                        method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(AssetChangeContext);

                    for (int i = 0; i < attributes.Length; i++)
                    {
                        DetectAssetChangedAttribute attribute = attributes[i];
                        if (
                            !WatchersByAssetType.TryGetValue(
                                attribute.AssetType,
                                out AssetWatcher watcher
                            )
                        )
                        {
                            watcher = new AssetWatcher(attribute.AssetType);
                            PopulateKnownAssetPaths(watcher);
                            WatchersByAssetType.Add(attribute.AssetType, watcher);
                        }

                        MethodSubscription subscription = new()
                        {
                            DeclaringType = type,
                            Method = method,
                            Flags = attribute.Flags,
                            AcceptsContext = acceptsContext,
                        };
                        watcher.Subscriptions.Add(subscription);
                    }
                }
            }
        }

        private static void PopulateKnownAssetPaths(AssetWatcher watcher)
        {
            string filter = $"t:{watcher.AssetType.Name}";
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (ShouldSkipPath(path))
                {
                    continue;
                }

                Type mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (mainType != null && watcher.AssetType.IsAssignableFrom(mainType))
                {
                    watcher.KnownAssetPaths.Add(path);
                }
            }
        }

        private static bool ValidateMethodSignature(Type declaringType, MethodInfo method)
        {
            if (method.IsStatic)
            {
                Debug.LogWarning(
                    $"[DetectAssetChanged] {declaringType.FullName}.{method.Name} must be an instance method."
                );
                return false;
            }

            if (method.ReturnType != typeof(void))
            {
                Debug.LogWarning(
                    $"[DetectAssetChanged] {declaringType.FullName}.{method.Name} must return void."
                );
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (
                parameters.Length > 1
                || (
                    parameters.Length == 1
                    && parameters[0].ParameterType != typeof(AssetChangeContext)
                )
            )
            {
                Debug.LogWarning(
                    $"[DetectAssetChanged] {declaringType.FullName}.{method.Name} must declare zero parameters or a single {nameof(AssetChangeContext)} parameter."
                );
                return false;
            }

            return true;
        }

        private static bool ShouldSkipPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return true;
            }

            if (
                !includeTestAssets
                && assetPath.IndexOf(TestAssetFolderMarker, StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                return true;
            }

            return false;
        }
    }
}
