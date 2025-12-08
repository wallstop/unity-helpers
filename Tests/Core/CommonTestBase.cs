namespace WallstopStudios.UnityHelpers.Tests.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using Object = UnityEngine.Object;
#if UNITY_EDITOR
    using UnityEditor.SceneManagement;
    using WallstopStudios.UnityHelpers.Editor.Utils;
#endif

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

#if UNITY_EDITOR
        /// <summary>
        /// Tracks folders created by this test instance for cleanup.
        /// Stored in order of creation (deepest paths may come later).
        /// </summary>
        protected readonly List<string> _trackedFolders = new();

        /// <summary>
        /// Tracks asset paths created by this test instance for cleanup.
        /// </summary>
        protected readonly List<string> _trackedAssetPaths = new();

        private bool _previousEditorUiSuppress;
#endif

        [SetUp]
        public virtual void BaseSetUp()
        {
#if REFLEX_PRESENT
            EnsureReflexSettings();
#endif
#if UNITY_EDITOR
            _previousEditorUiSuppress = EditorUi.Suppress;
            EditorUi.Suppress = true;
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

            EditorUi.Suppress = _previousEditorUiSuppress;
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

#if UNITY_EDITOR
            EditorUi.Suppress = _previousEditorUiSuppress;
#endif
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

        /// <summary>
        /// Copies an asset file without triggering Unity's internal modal dialogs.
        /// This uses file system operations followed by asset database import instead of
        /// AssetDatabase.CopyAsset which can show dialogs in certain scenarios.
        /// </summary>
        /// <param name="sourcePath">Source asset path (relative to project root, e.g., "Assets/...").</param>
        /// <param name="destinationPath">Destination asset path (relative to project root).</param>
        /// <returns>True if the copy succeeded, false otherwise.</returns>
        protected static bool TryCopyAssetSilent(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
            {
                return false;
            }

            string absoluteSource = System
                .IO.Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    sourcePath
                )
                .SanitizePath();

            string absoluteDest = System
                .IO.Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    destinationPath
                )
                .SanitizePath();

            if (!System.IO.File.Exists(absoluteSource))
            {
                return false;
            }

            try
            {
                string destDir = System.IO.Path.GetDirectoryName(absoluteDest);
                if (!string.IsNullOrEmpty(destDir) && !System.IO.Directory.Exists(destDir))
                {
                    System.IO.Directory.CreateDirectory(destDir);
                }

                if (System.IO.File.Exists(absoluteDest))
                {
                    System.IO.File.Delete(absoluteDest);
                }

                System.IO.File.Copy(absoluteSource, absoluteDest);

                UnityEditor.AssetDatabase.ImportAsset(
                    destinationPath,
                    UnityEditor.ImportAssetOptions.ForceSynchronousImport
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ensures a folder exists both on disk and in the AssetDatabase.
        /// This prevents Unity's internal "Moving file failed" modal dialog.
        /// Tracks created folders for automatic cleanup during TearDown.
        /// </summary>
        /// <param name="folderPath">Unity relative path (e.g., Assets/Resources/Test).</param>
        /// <returns>List of folder paths that were created (for external tracking if needed).</returns>
        protected List<string> EnsureFolder(string folderPath)
        {
            List<string> createdFolders = new();

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return createdFolders;
            }

            folderPath = folderPath.SanitizePath();
            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);

            // Process each path segment to handle case-insensitive folder matching
            string[] parts = folderPath.Split('/');
            string current = parts[0]; // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                string desiredName = parts[i];
                string intendedNext = current + "/" + desiredName;

                // First, check if folder already exists in AssetDatabase (exact match)
                if (UnityEditor.AssetDatabase.IsValidFolder(intendedNext))
                {
                    current = intendedNext;
                    continue;
                }

                // Check for case-insensitive match on disk first
                string actualFolderName = FindExistingFolderCaseInsensitive(
                    projectRoot,
                    current,
                    desiredName
                );
                if (actualFolderName != null)
                {
                    // Folder exists on disk with potentially different casing
                    string actualPath = current + "/" + actualFolderName;

                    // Import it into AssetDatabase if not already there
                    if (!UnityEditor.AssetDatabase.IsValidFolder(actualPath))
                    {
                        UnityEditor.AssetDatabase.ImportAsset(
                            actualPath,
                            UnityEditor.ImportAssetOptions.ForceSynchronousImport
                        );
                    }

                    current = actualPath;
                    continue;
                }

                // Folder doesn't exist on disk or in AssetDatabase - create it
                // First create on disk
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    string absoluteDirectory = System.IO.Path.Combine(projectRoot, intendedNext);
                    try
                    {
                        if (!System.IO.Directory.Exists(absoluteDirectory))
                        {
                            System.IO.Directory.CreateDirectory(absoluteDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"CommonTestBase.EnsureFolder: Failed to create directory on disk '{absoluteDirectory}': {ex.Message}"
                        );
                        return createdFolders;
                    }

                    // Import the newly created folder
                    UnityEditor.AssetDatabase.ImportAsset(
                        intendedNext,
                        UnityEditor.ImportAssetOptions.ForceSynchronousImport
                    );
                }

                // If it's still not valid, create via AssetDatabase (fallback)
                if (!UnityEditor.AssetDatabase.IsValidFolder(intendedNext))
                {
                    UnityEditor.AssetDatabase.CreateFolder(current, desiredName);
                }

                // Track folders we actually created (not pre-existing ones)
                TrackFolder(intendedNext);
                createdFolders.Add(intendedNext);
                current = intendedNext;
            }

            return createdFolders;
        }

        /// <summary>
        /// Finds an existing folder on disk that matches the desired name case-insensitively.
        /// Returns the actual folder name as it exists on disk, or null if not found.
        /// </summary>
        private static string FindExistingFolderCaseInsensitive(
            string projectRoot,
            string parentUnityPath,
            string desiredName
        )
        {
            if (string.IsNullOrEmpty(projectRoot))
            {
                return null;
            }

            string parentAbsolutePath = System.IO.Path.Combine(projectRoot, parentUnityPath);
            if (!System.IO.Directory.Exists(parentAbsolutePath))
            {
                return null;
            }

            try
            {
                foreach (string dir in System.IO.Directory.GetDirectories(parentAbsolutePath))
                {
                    string name = System.IO.Path.GetFileName(dir);
                    if (string.Equals(name, desiredName, StringComparison.OrdinalIgnoreCase))
                    {
                        return name;
                    }
                }
            }
            catch
            {
                // Ignore enumeration errors
            }

            return null;
        }

        /// <summary>
        /// Static version of EnsureFolder that does not track folders.
        /// Use the instance method EnsureFolder() when you need automatic cleanup.
        /// </summary>
        protected static void EnsureFolderStatic(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            folderPath = folderPath.SanitizePath();
            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);

            // Process each path segment to handle case-insensitive folder matching
            string[] parts = folderPath.Split('/');
            string current = parts[0]; // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                string desiredName = parts[i];
                string intendedNext = current + "/" + desiredName;

                // First, check if folder already exists in AssetDatabase (exact match)
                if (UnityEditor.AssetDatabase.IsValidFolder(intendedNext))
                {
                    current = intendedNext;
                    continue;
                }

                // Check for case-insensitive match on disk first
                string actualFolderName = FindExistingFolderCaseInsensitive(
                    projectRoot,
                    current,
                    desiredName
                );
                if (actualFolderName != null)
                {
                    // Folder exists on disk with potentially different casing
                    string actualPath = current + "/" + actualFolderName;

                    // Import it into AssetDatabase if not already there
                    if (!UnityEditor.AssetDatabase.IsValidFolder(actualPath))
                    {
                        UnityEditor.AssetDatabase.ImportAsset(
                            actualPath,
                            UnityEditor.ImportAssetOptions.ForceSynchronousImport
                        );
                    }

                    current = actualPath;
                    continue;
                }

                // Folder doesn't exist on disk or in AssetDatabase - create it
                // First create on disk
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    string absoluteDirectory = System.IO.Path.Combine(projectRoot, intendedNext);
                    try
                    {
                        if (!System.IO.Directory.Exists(absoluteDirectory))
                        {
                            System.IO.Directory.CreateDirectory(absoluteDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"CommonTestBase.EnsureFolderStatic: Failed to create directory on disk '{absoluteDirectory}': {ex.Message}"
                        );
                        return;
                    }

                    // Import the newly created folder
                    UnityEditor.AssetDatabase.ImportAsset(
                        intendedNext,
                        UnityEditor.ImportAssetOptions.ForceSynchronousImport
                    );
                }

                // If it's still not valid, create via AssetDatabase (fallback)
                if (!UnityEditor.AssetDatabase.IsValidFolder(intendedNext))
                {
                    UnityEditor.AssetDatabase.CreateFolder(current, desiredName);
                }

                current = intendedNext;
            }
        }

        /// <summary>
        /// Tracks a folder path for cleanup during TearDown.
        /// Only tracked folders will be deleted - pre-existing user folders are safe.
        /// </summary>
        /// <param name="folderPath">The Unity-relative folder path (e.g., "Assets/Temp/MyTest")</param>
        protected void TrackFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            string normalized = folderPath.SanitizePath();
            if (!_trackedFolders.Contains(normalized))
            {
                _trackedFolders.Add(normalized);
            }
        }

        /// <summary>
        /// Tracks an asset path for cleanup during TearDown.
        /// Only tracked assets will be deleted - pre-existing user assets are safe.
        /// </summary>
        /// <param name="assetPath">The Unity-relative asset path (e.g., "Assets/Temp/MyAsset.asset")</param>
        protected void TrackAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string normalized = assetPath.SanitizePath();
            if (!_trackedAssetPaths.Contains(normalized))
            {
                _trackedAssetPaths.Add(normalized);
            }
        }

        /// <summary>
        /// Cleans up all tracked folders and assets that were created by this test.
        /// Only deletes folders/assets that were explicitly tracked - user data is safe.
        /// Folders are deleted in reverse order (deepest first) to handle nested structures.
        /// </summary>
        protected void CleanupTrackedFoldersAndAssets()
        {
#if UNITY_EDITOR
            // First, delete tracked assets
            foreach (string assetPath in _trackedAssetPaths)
            {
                if (
                    !string.IsNullOrEmpty(assetPath)
                    && UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null
                )
                {
                    UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                }
            }
            _trackedAssetPaths.Clear();

            // Sort folders by depth (deepest first) to delete children before parents
            List<string> sortedFolders = new(_trackedFolders);
            sortedFolders.Sort((a, b) => b.Split('/').Length.CompareTo(a.Split('/').Length));

            foreach (string folderPath in sortedFolders)
            {
                if (
                    !string.IsNullOrEmpty(folderPath)
                    && UnityEditor.AssetDatabase.IsValidFolder(folderPath)
                )
                {
                    // Only delete if the folder is empty or contains only items we created
                    // For safety, we'll delete the folder - if it has unexpected contents,
                    // Unity will fail the delete which is fine
                    UnityEditor.AssetDatabase.DeleteAsset(folderPath);
                }
            }
            _trackedFolders.Clear();

            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// Cleans up all known test folders in Assets and Assets/Resources, including duplicates.
        /// This should be called in OneTimeSetUp and OneTimeTearDown to ensure clean test state.
        /// </summary>
        /// <remarks>
        /// Handles folders like:
        /// - Assets/Resources: CreatorTests, Deep, Lifecycle, Loose, Multi, etc.
        /// - Assets: Temp and its duplicates (Temp 1, Temp 2, etc.)
        /// - Assets/Resources: Wallstop Studios duplicates (Wallstop Studios 1, etc.)
        /// </remarks>
        protected static void CleanupAllKnownTestFolders()
        {
            // List of test folder patterns to clean up (relative to Assets/Resources)
            string[] resourcesTestFolderPatterns = new[]
            {
                "CreatorTests",
                "Deep",
                "Lifecycle",
                "Loose",
                "Multi",
                "MultiNatural",
                "SingleLevel",
                "Tests",
                "DuplicateCleanupTests",
                "CaseTest",
                "cASEtest",
                "CASETEST",
                "casetest",
                "CaseTEST",
                "CustomPath",
                "Missing",
            };

            // List of test folder patterns to clean up (relative to Assets)
            // Note: "Temp" will also match "Temp 1", "Temp 2", etc. due to duplicate handling
            string[] assetsTestFolderPatterns = new[]
            {
                "Temp",
                "TempMultiFileSelectorTests",
                "TempSpriteApplierTests",
                "TempSpriteApplierAdditional",
                "TempSpriteHelpersTests",
                "TempObjectHelpersEditorTests",
                "TempHelpersPrefabs",
                "TempHelpersScriptables",
                "TempColorExtensionTests",
            };

            // Also clean up duplicate Wallstop Studios folders
            string[] wallstopDuplicatePatterns = new[] { "Wallstop Studios" };

            // Clean up duplicate "Unity Helpers" folders inside Wallstop Studios
            string[] unityHelpersDuplicatePatterns = new[] { "Unity Helpers" };

            string resourcesRoot = "Assets/Resources";
            string assetsRoot = "Assets";
            string wallstopStudiosRoot = "Assets/Resources/Wallstop Studios";

            // First, refresh to ensure we have current state
            UnityEditor.AssetDatabase.Refresh();

            // Clean up test folders in Assets/Resources and their duplicates
            foreach (string pattern in resourcesTestFolderPatterns)
            {
                CleanupFolderAndDuplicates(resourcesRoot, pattern);
            }

            // Clean up test folders in Assets and their duplicates
            foreach (string pattern in assetsTestFolderPatterns)
            {
                CleanupFolderAndDuplicates(assetsRoot, pattern);
            }

            // Clean up Wallstop Studios duplicates (not the main folder)
            foreach (string pattern in wallstopDuplicatePatterns)
            {
                CleanupDuplicateFoldersOnly(resourcesRoot, pattern);
            }

            // Clean up Unity Helpers duplicates inside Wallstop Studios folder (not the main folder)
            foreach (string pattern in unityHelpersDuplicatePatterns)
            {
                CleanupDuplicateFoldersOnly(wallstopStudiosRoot, pattern);
            }

            // Also clean up from disk to handle orphaned folders
            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string resourcesOnDisk = System.IO.Path.Combine(projectRoot, "Assets", "Resources");
                if (System.IO.Directory.Exists(resourcesOnDisk))
                {
                    foreach (string pattern in resourcesTestFolderPatterns)
                    {
                        CleanupFolderAndDuplicatesOnDisk(resourcesOnDisk, pattern);
                    }

                    foreach (string pattern in wallstopDuplicatePatterns)
                    {
                        CleanupDuplicateFoldersOnlyOnDisk(resourcesOnDisk, pattern);
                    }

                    // Clean up Unity Helpers duplicates inside Wallstop Studios folder on disk
                    string wallstopOnDisk = System.IO.Path.Combine(
                        resourcesOnDisk,
                        "Wallstop Studios"
                    );
                    if (System.IO.Directory.Exists(wallstopOnDisk))
                    {
                        foreach (string pattern in unityHelpersDuplicatePatterns)
                        {
                            CleanupDuplicateFoldersOnlyOnDisk(wallstopOnDisk, pattern);
                        }
                    }
                }

                // Clean up Temp folders in Assets
                string assetsOnDisk = System.IO.Path.Combine(projectRoot, "Assets");
                if (System.IO.Directory.Exists(assetsOnDisk))
                {
                    foreach (string pattern in assetsTestFolderPatterns)
                    {
                        CleanupFolderAndDuplicatesOnDisk(assetsOnDisk, pattern);
                    }
                }
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }

        /// <summary>
        /// Deletes a folder and all its duplicates (e.g., "Folder", "Folder 1", "Folder 2").
        /// </summary>
        private static void CleanupFolderAndDuplicates(string parentPath, string folderName)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(parentPath))
            {
                return;
            }

            string[] subFolders = UnityEditor.AssetDatabase.GetSubFolders(parentPath);
            if (subFolders == null)
            {
                return;
            }

            foreach (string folder in subFolders)
            {
                string name = System.IO.Path.GetFileName(folder);
                if (name == null)
                {
                    continue;
                }

                // Check exact match or duplicate pattern (e.g., "Folder 1", "Folder 2")
                if (
                    string.Equals(name, folderName, StringComparison.OrdinalIgnoreCase)
                    || IsDuplicateFolder(name, folderName)
                )
                {
                    DeleteFolderRecursivelyWithContents(folder);
                }
            }
        }

        /// <summary>
        /// Deletes only duplicate folders (e.g., "Folder 1", "Folder 2") but NOT the main folder.
        /// </summary>
        private static void CleanupDuplicateFoldersOnly(string parentPath, string folderName)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(parentPath))
            {
                return;
            }

            string[] subFolders = UnityEditor.AssetDatabase.GetSubFolders(parentPath);
            if (subFolders == null)
            {
                return;
            }

            foreach (string folder in subFolders)
            {
                string name = System.IO.Path.GetFileName(folder);
                if (name == null)
                {
                    continue;
                }

                // Only delete duplicates, not the main folder
                if (IsDuplicateFolder(name, folderName))
                {
                    DeleteFolderRecursivelyWithContents(folder);
                }
            }
        }

        /// <summary>
        /// Checks if a folder name matches the pattern "BaseName N" where N is a number.
        /// </summary>
        private static bool IsDuplicateFolder(string actualName, string baseName)
        {
            if (!actualName.StartsWith(baseName + " ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string suffix = actualName.Substring(baseName.Length + 1);
            return int.TryParse(suffix, out _);
        }

        /// <summary>
        /// List of protected production folder paths that should NEVER be deleted by tests.
        /// These paths are case-insensitive.
        /// </summary>
        private static readonly string[] ProtectedFolders = new[]
        {
            "Assets/Resources/Wallstop Studios",
            "Assets/Plugins",
            "Assets/Editor Default Resources",
            "Assets/StreamingAssets",
        };

        /// <summary>
        /// Known folder base names whose numbered duplicates (e.g., "Unity Helpers 1") should be
        /// considered pollution and NOT protected. The main folders remain protected.
        /// </summary>
        private static readonly (
            string parentPath,
            string baseName
        )[] KnownDuplicateFolderPatterns = new[]
        {
            ("Assets/Resources/Wallstop Studios", "Unity Helpers"),
            ("Assets/Resources", "Wallstop Studios"),
        };

        /// <summary>
        /// Checks if a path represents a numbered duplicate folder that is pollution, not production.
        /// For example, "Assets/Resources/Wallstop Studios/Unity Helpers 1" is pollution.
        /// </summary>
        private static bool IsKnownDuplicatePollution(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string normalizedPath = path.SanitizePath();
            foreach ((string parentPath, string baseName) in KnownDuplicateFolderPatterns)
            {
                string prefix = parentPath + "/" + baseName + " ";
                if (normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string remainder = normalizedPath.Substring(prefix.Length);
                    int slashIndex = remainder.IndexOf('/');
                    string folderSuffix =
                        slashIndex >= 0 ? remainder.Substring(0, slashIndex) : remainder;
                    if (int.TryParse(folderSuffix, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a path is or is under a protected production folder.
        /// Returns false for known duplicate pollution folders (e.g., "Unity Helpers 1").
        /// </summary>
        private static bool IsProtectedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string normalizedPath = path.SanitizePath();

            if (IsKnownDuplicatePollution(normalizedPath))
            {
                return false;
            }

            foreach (string protectedFolder in ProtectedFolders)
            {
                if (
                    string.Equals(
                        normalizedPath,
                        protectedFolder,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || normalizedPath.StartsWith(
                        protectedFolder + "/",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Internal test hooks for verifying protection path logic.
        /// </summary>
        protected internal static class ProtectionTestHooks
        {
            /// <summary>
            /// Exposes IsProtectedPath for testing.
            /// </summary>
            public static bool TestIsProtectedPath(string path) => IsProtectedPath(path);

            /// <summary>
            /// Exposes IsKnownDuplicatePollution for testing.
            /// </summary>
            public static bool TestIsKnownDuplicatePollution(string path) =>
                IsKnownDuplicatePollution(path);

            /// <summary>
            /// Gets the list of protected folders for verification.
            /// </summary>
            public static string[] GetProtectedFolders() => ProtectedFolders;

            /// <summary>
            /// Gets the list of known duplicate folder patterns for verification.
            /// </summary>
            public static (
                string parentPath,
                string baseName
            )[] GetKnownDuplicateFolderPatterns() => KnownDuplicateFolderPatterns;
        }

        /// <summary>
        /// Deletes a folder and all its contents through AssetDatabase.
        /// IMPORTANT: Will NOT delete protected production folders.
        /// </summary>
        private static void DeleteFolderRecursivelyWithContents(string folderPath)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            // CRITICAL: Never delete protected production folders
            if (IsProtectedPath(folderPath))
            {
                Debug.LogWarning(
                    $"[CommonTestBase] Refusing to delete protected production folder: {folderPath}. "
                        + "This is a safety measure to prevent accidental deletion of production assets during tests."
                );
                return;
            }

            // First delete all assets in this folder (not recursively - subfolders will be handled)
            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets(
                string.Empty,
                new[] { folderPath }
            );
            if (assetGuids != null)
            {
                foreach (string guid in assetGuids)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (
                        !string.IsNullOrEmpty(assetPath)
                        && !UnityEditor.AssetDatabase.IsValidFolder(assetPath)
                    )
                    {
                        // Double-check this asset is not in a protected folder
                        if (IsProtectedPath(assetPath))
                        {
                            Debug.LogWarning(
                                $"[CommonTestBase] Refusing to delete protected asset: {assetPath}"
                            );
                            continue;
                        }

                        UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                    }
                }
            }

            // Then delete subfolders recursively
            string[] subFolders = UnityEditor.AssetDatabase.GetSubFolders(folderPath);
            if (subFolders != null)
            {
                foreach (string sub in subFolders)
                {
                    DeleteFolderRecursivelyWithContents(sub);
                }
            }

            // Finally delete the folder itself (only if not protected)
            if (!IsProtectedPath(folderPath))
            {
                UnityEditor.AssetDatabase.DeleteAsset(folderPath);
            }
        }

        /// <summary>
        /// Converts a disk path to a Unity relative path for protection checking.
        /// </summary>
        private static string DiskPathToUnityRelativePath(string diskPath)
        {
            if (string.IsNullOrEmpty(diskPath))
            {
                return string.Empty;
            }

            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                return string.Empty;
            }

            string normalizedDiskPath = diskPath.SanitizePath();
            string normalizedProjectRoot = projectRoot.SanitizePath();

            if (
                normalizedDiskPath.StartsWith(
                    normalizedProjectRoot + "/",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return normalizedDiskPath.Substring(normalizedProjectRoot.Length + 1);
            }

            return string.Empty;
        }

        /// <summary>
        /// Cleans up folders on disk (handles orphaned folders not in AssetDatabase).
        /// IMPORTANT: Will NOT delete protected production folders.
        /// </summary>
        private static void CleanupFolderAndDuplicatesOnDisk(string parentPath, string folderName)
        {
            if (!System.IO.Directory.Exists(parentPath))
            {
                return;
            }

            try
            {
                foreach (string dir in System.IO.Directory.GetDirectories(parentPath))
                {
                    string name = System.IO.Path.GetFileName(dir);
                    if (name == null)
                    {
                        continue;
                    }

                    if (
                        string.Equals(name, folderName, StringComparison.OrdinalIgnoreCase)
                        || IsDuplicateFolder(name, folderName)
                    )
                    {
                        // Check if this would be a protected path
                        string unityPath = DiskPathToUnityRelativePath(dir);
                        if (!string.IsNullOrEmpty(unityPath) && IsProtectedPath(unityPath))
                        {
                            Debug.LogWarning(
                                $"[CommonTestBase] Refusing to delete protected folder on disk: {dir}"
                            );
                            continue;
                        }

                        try
                        {
                            System.IO.Directory.Delete(dir, recursive: true);
                        }
                        catch
                        {
                            // Ignore - folder may be locked
                        }
                    }
                }
            }
            catch
            {
                // Ignore enumeration errors
            }
        }

        /// <summary>
        /// Cleans up duplicate folders on disk (not the main folder).
        /// IMPORTANT: Will NOT delete protected production folders.
        /// </summary>
        private static void CleanupDuplicateFoldersOnlyOnDisk(string parentPath, string folderName)
        {
            if (!System.IO.Directory.Exists(parentPath))
            {
                return;
            }

            try
            {
                foreach (string dir in System.IO.Directory.GetDirectories(parentPath))
                {
                    string name = System.IO.Path.GetFileName(dir);
                    if (name == null)
                    {
                        continue;
                    }

                    if (IsDuplicateFolder(name, folderName))
                    {
                        // Check if this would be a protected path
                        string unityPath = DiskPathToUnityRelativePath(dir);
                        if (!string.IsNullOrEmpty(unityPath) && IsProtectedPath(unityPath))
                        {
                            Debug.LogWarning(
                                $"[CommonTestBase] Refusing to delete protected folder on disk: {dir}"
                            );
                            continue;
                        }

                        try
                        {
                            System.IO.Directory.Delete(dir, recursive: true);
                        }
                        catch
                        {
                            // Ignore - folder may be locked
                        }
                    }
                }
            }
            catch
            {
                // Ignore enumeration errors
            }
        }
#endif
    }
}
