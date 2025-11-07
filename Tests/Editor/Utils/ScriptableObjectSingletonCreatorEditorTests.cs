namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System.Collections;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class ScriptableObjectSingletonCreatorEditorTests : CommonTestBase
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string TargetFolder = ResourcesRoot + "/Tests/CreatorPath";
        private const string TargetAssetPath = TargetFolder + "/CreatorPathSingleton.asset";
        private const string WrongFolder = ResourcesRoot + "/Tests/WrongPath";
        private const string WrongAssetPath = WrongFolder + "/CreatorPathSingleton.asset";
        private const string WrongFolderCaseVariant = ResourcesRoot + "/TestS/WrongPath";
        private const string WrongAssetPathCaseVariant =
            WrongFolderCaseVariant + "/CreatorPathSingleton.asset";

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            DeleteAssetIfExists(TargetAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPathCaseVariant);
            yield return null;
            DeleteFolderHierarchy(TargetFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolderCaseVariant);
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return base.UnityTearDown();
            yield return null;
            DeleteAssetIfExists(TargetAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPathCaseVariant);
            yield return null;
            DeleteFolderHierarchy(TargetFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolderCaseVariant);
            yield return null;
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = false;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreatesAssetAtAttributePath()
        {
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            CreatorPathSingleton asset = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(asset != null);
        }

        [UnityTest]
        public IEnumerator RelocatesExistingAssetToAttributePath()
        {
            EnsureFolder(ResourcesRoot);
            yield return null;
            EnsureFolder(WrongFolder);
            yield return null;
            CreatorPathSingleton instance = ScriptableObject.CreateInstance<CreatorPathSingleton>();
            AssetDatabase.CreateAsset(instance, WrongAssetPath);
            AssetDatabase.SaveAssets();
            yield return null;

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            CreatorPathSingleton relocated = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(relocated != null);
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(WrongAssetPath) == null
            );
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string desired = parts[i];
                string target = current + "/" + desired;

                if (!AssetDatabase.IsValidFolder(target))
                {
                    string[] subs = AssetDatabase.GetSubFolders(current);
                    string match = null;
                    if (subs != null)
                    {
                        for (int s = 0; s < subs.Length; s++)
                        {
                            string sub = subs[s];
                            int last = sub.LastIndexOf('/', sub.Length - 1);
                            string name = last >= 0 ? sub.Substring(last + 1) : sub;
                            if (
                                string.Equals(
                                    name,
                                    desired,
                                    System.StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                match = sub;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(match))
                    {
                        AssetDatabase.CreateFolder(current, desired);
                        current = target;
                    }
                    else
                    {
                        current = match;
                    }
                }
                else
                {
                    current = target;
                }
            }
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void DeleteFolderHierarchy(string folderPath)
        {
            string path = ResolveExistingFolderPath(folderPath);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            while (
                !string.IsNullOrEmpty(path)
                && !string.Equals(path, ResourcesRoot, System.StringComparison.OrdinalIgnoreCase)
            )
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    path = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    continue;
                }

                if (!AssetDatabase.DeleteAsset(path))
                {
                    break;
                }

                path = Path.GetDirectoryName(path)?.Replace('\\', '/');
            }
        }

        private static string ResolveExistingFolderPath(string intended)
        {
            if (string.IsNullOrWhiteSpace(intended))
            {
                return null;
            }

            intended = intended.SanitizePath();
            string[] parts = intended.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            string current = parts[0];
            if (!string.Equals(current, "Assets", System.StringComparison.OrdinalIgnoreCase))
            {
                return intended;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string desired = parts[i];
                string next = current + "/" + desired;
                if (AssetDatabase.IsValidFolder(next))
                {
                    current = next;
                    continue;
                }

                string[] subs = AssetDatabase.GetSubFolders(current);
                if (subs == null || subs.Length == 0)
                {
                    return intended;
                }

                string match = null;
                for (int s = 0; s < subs.Length; s++)
                {
                    string sub = subs[s];
                    int last = sub.LastIndexOf('/', sub.Length - 1);
                    string name = last >= 0 ? sub.Substring(last + 1) : sub;
                    if (string.Equals(name, desired, System.StringComparison.OrdinalIgnoreCase))
                    {
                        match = sub;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(match))
                {
                    return intended;
                }

                current = match;
            }

            return current;
        }

        [UnityTest]
        public IEnumerator RelocatesExistingAssetToAttributePathFromMismatchedParentCase()
        {
            EnsureFolder(ResourcesRoot);
            yield return null;
            EnsureFolder(WrongFolderCaseVariant);
            yield return null;
            CreatorPathSingleton instance = ScriptableObject.CreateInstance<CreatorPathSingleton>();
            AssetDatabase.CreateAsset(instance, WrongAssetPathCaseVariant);
            AssetDatabase.SaveAssets();
            yield return null;
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            CreatorPathSingleton relocated = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(relocated != null);
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(WrongAssetPathCaseVariant)
                    == null
            );
        }

        [ScriptableSingletonPath("Tests/CreatorPath")]
        private sealed class CreatorPathSingleton
            : ScriptableObjectSingleton<CreatorPathSingleton> { }
    }
#endif
}
