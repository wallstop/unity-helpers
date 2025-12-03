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
        private const string SupportedSignatureDescription =
            "Supported signatures: () with no parameters; (AssetChangeContext context); or (TAsset[] createdAssets, string[] deletedAssetPaths) where TAsset derives from UnityEngine.Object.";

        private enum SubscriptionParameterMode
        {
            None,
            Context,
            CreatedAndDeleted,
        }

        private sealed class MethodSubscription
        {
            internal Type DeclaringType;
            internal MethodInfo Method;
            internal AssetChangeFlags Flags;
            internal SubscriptionParameterMode ParameterMode;
            internal Type CreatedParameterElementType;
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

        internal static bool ValidateMethodSignatureForTesting(
            Type declaringType,
            string methodName
        )
        {
            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException(nameof(methodName));
            }

            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic;
            MethodInfo method = declaringType.GetMethod(methodName, flags);
            if (method == null)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Method {0}.{1} was not found.",
                        declaringType.FullName,
                        methodName
                    ),
                    nameof(methodName)
                );
            }

            return TryResolveParameterMode(declaringType, method, out _, out _);
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

                List<UnityEngine.Object> createdAssetInstances = null;
                Dictionary<Type, Array> createdAssetArrays = null;
                string[] deletedPathsArray = null;

                foreach (MethodSubscription subscription in watcher.Subscriptions)
                {
                    AssetChangeFlags relevant = subscription.Flags & triggeredFlags;
                    if (relevant == AssetChangeFlags.None)
                    {
                        continue;
                    }

                    object[] args = BuildInvocationArguments(
                        subscription,
                        watcher.AssetType,
                        relevant,
                        createdPaths,
                        deletedPaths,
                        ref createdAssetInstances,
                        ref createdAssetArrays,
                        ref deletedPathsArray
                    );

                    InvokeSubscription(subscription, args);
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

        private static object[] BuildInvocationArguments(
            MethodSubscription subscription,
            Type assetType,
            AssetChangeFlags relevantFlags,
            IReadOnlyList<string> createdPaths,
            IReadOnlyList<string> deletedPaths,
            ref List<UnityEngine.Object> createdAssetInstances,
            ref Dictionary<Type, Array> createdAssetArrays,
            ref string[] deletedPathsArray
        )
        {
            switch (subscription.ParameterMode)
            {
                case SubscriptionParameterMode.None:
                    return Array.Empty<object>();
                case SubscriptionParameterMode.Context:
                    return new object[]
                    {
                        new AssetChangeContext(
                            assetType,
                            relevantFlags,
                            relevantFlags.HasFlag(AssetChangeFlags.Created)
                                ? (IReadOnlyList<string>)createdPaths
                                : Array.Empty<string>(),
                            relevantFlags.HasFlag(AssetChangeFlags.Deleted)
                                ? (IReadOnlyList<string>)deletedPaths
                                : Array.Empty<string>()
                        ),
                    };
                case SubscriptionParameterMode.CreatedAndDeleted:
                    Array createdArgument = relevantFlags.HasFlag(AssetChangeFlags.Created)
                        ? GetCreatedAssetsArgument(
                            subscription,
                            assetType,
                            createdPaths,
                            ref createdAssetInstances,
                            ref createdAssetArrays
                        )
                        : Array.CreateInstance(subscription.CreatedParameterElementType, 0);
                    string[] deletedArgument = relevantFlags.HasFlag(AssetChangeFlags.Deleted)
                        ? GetDeletedPathsArgument(deletedPaths, ref deletedPathsArray)
                        : Array.Empty<string>();
                    return new object[] { createdArgument, deletedArgument };
                default:
                    return Array.Empty<object>();
            }
        }

        private static Array GetCreatedAssetsArgument(
            MethodSubscription subscription,
            Type assetType,
            IReadOnlyList<string> createdPaths,
            ref List<UnityEngine.Object> createdAssetInstances,
            ref Dictionary<Type, Array> createdAssetArrays
        )
        {
            if (createdPaths == null || createdPaths.Count == 0)
            {
                return Array.CreateInstance(subscription.CreatedParameterElementType, 0);
            }

            createdAssetInstances ??= LoadCreatedAssetInstances(assetType, createdPaths);
            createdAssetArrays ??= new Dictionary<Type, Array>();

            if (
                !createdAssetArrays.TryGetValue(
                    subscription.CreatedParameterElementType,
                    out Array typedArray
                )
            )
            {
                typedArray = Array.CreateInstance(
                    subscription.CreatedParameterElementType,
                    createdAssetInstances.Count
                );
                for (int i = 0; i < createdAssetInstances.Count; i++)
                {
                    typedArray.SetValue(createdAssetInstances[i], i);
                }

                createdAssetArrays.Add(subscription.CreatedParameterElementType, typedArray);
            }

            return typedArray;
        }

        private static List<UnityEngine.Object> LoadCreatedAssetInstances(
            Type assetType,
            IReadOnlyList<string> createdPaths
        )
        {
            List<UnityEngine.Object> instances = new(createdPaths.Count);
            for (int i = 0; i < createdPaths.Count; i++)
            {
                string path = createdPaths[i];
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, assetType);
                instances.Add(asset);
            }

            return instances;
        }

        private static string[] GetDeletedPathsArgument(
            IReadOnlyList<string> deletedPaths,
            ref string[] deletedPathsArray
        )
        {
            if (deletedPaths == null || deletedPaths.Count == 0)
            {
                return Array.Empty<string>();
            }

            if (deletedPathsArray == null)
            {
                deletedPathsArray = new string[deletedPaths.Count];
                for (int i = 0; i < deletedPaths.Count; i++)
                {
                    deletedPathsArray[i] = deletedPaths[i];
                }
            }

            return deletedPathsArray;
        }

        private static void InvokeSubscription(MethodSubscription subscription, object[] args)
        {
            if (subscription.Method.IsStatic)
            {
                InvokeSubscriptionMethod(subscription, null, args);
                return;
            }

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

                InvokeSubscriptionMethod(subscription, instance, args);
            }
        }

        private static void InvokeSubscriptionMethod(
            MethodSubscription subscription,
            UnityEngine.Object target,
            object[] args
        )
        {
            try
            {
                subscription.Method.Invoke(target, args);
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
                    target
                );
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
                    | BindingFlags.Static
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

                    if (
                        !TryResolveParameterMode(
                            type,
                            method,
                            out SubscriptionParameterMode parameterMode,
                            out Type createdElementType
                        )
                    )
                    {
                        continue;
                    }

                    for (int i = 0; i < attributes.Length; i++)
                    {
                        DetectAssetChangedAttribute attribute = attributes[i];
                        if (
                            parameterMode == SubscriptionParameterMode.CreatedAndDeleted
                            && !ResolutionSupportsAssetType(createdElementType, attribute.AssetType)
                        )
                        {
                            Debug.LogWarning(
                                $"[DetectAssetChanged] {type.FullName}.{method.Name} expects created asset parameter type {createdElementType.FullName}, which is not compatible with watched asset type {attribute.AssetType.FullName}."
                            );
                            continue;
                        }

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
                            ParameterMode = parameterMode,
                            CreatedParameterElementType = createdElementType,
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

        private static bool TryResolveParameterMode(
            Type declaringType,
            MethodInfo method,
            out SubscriptionParameterMode mode,
            out Type createdElementType
        )
        {
            mode = SubscriptionParameterMode.None;
            createdElementType = null;

            if (method.ReturnType != typeof(void))
            {
                LogUnsupportedSignature(
                    declaringType,
                    method,
                    "must return void to receive DetectAssetChanged notifications."
                );
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                mode = SubscriptionParameterMode.None;
                return true;
            }

            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(AssetChangeContext))
            {
                mode = SubscriptionParameterMode.Context;
                return true;
            }

            if (
                parameters.Length == 2
                && TryResolveCreatedParameterType(parameters[0].ParameterType, out Type elementType)
                && parameters[1].ParameterType == typeof(string[])
            )
            {
                mode = SubscriptionParameterMode.CreatedAndDeleted;
                createdElementType = elementType;
                return true;
            }

            LogUnsupportedSignature(
                declaringType,
                method,
                "has an unsupported parameter signature for DetectAssetChanged."
            );
            return false;
        }

        private static bool TryResolveCreatedParameterType(Type parameterType, out Type elementType)
        {
            elementType = null;
            if (parameterType == null || !parameterType.IsArray)
            {
                return false;
            }

            elementType = parameterType.GetElementType();
            if (elementType == null || !typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                elementType = null;
                return false;
            }

            return true;
        }

        private static bool ResolutionSupportsAssetType(Type parameterElementType, Type assetType)
        {
            if (parameterElementType == null || assetType == null)
            {
                return true;
            }

            return parameterElementType.IsAssignableFrom(assetType);
        }

        private static void LogUnsupportedSignature(
            Type declaringType,
            MethodInfo method,
            string detail
        )
        {
            Debug.LogError(
                $"[DetectAssetChanged] {declaringType.FullName}.{method.Name} {detail} {SupportedSignatureDescription}"
            );
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
