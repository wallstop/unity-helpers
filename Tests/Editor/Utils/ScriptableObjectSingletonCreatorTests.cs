namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System.Collections;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class ScriptableObjectSingletonCreatorTests : CommonTestBase
    {
        private const string TestRoot = "Assets/Resources/CreatorTests";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            ScriptableObjectSingletonCreator.VerboseLogging = true;
            EnsureFolder("Assets/Resources");
            EnsureFolder(TestRoot);
            yield break;
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            IEnumerator baseEnumerator = base.UnityTearDown();
            while (baseEnumerator.MoveNext())
            {
                yield return baseEnumerator.Current;
            }

            // Clean up any assets created under our test root
            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { TestRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
            }

            // Try to delete empty folders bottom-up
            TryDeleteFolder(TestRoot);
            TryDeleteFolder("Assets/Resources/Collision");
            TryDeleteFolder("Assets/Resources/CaseTest");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [UnityTest]
        public IEnumerator DoesNotCreateDuplicateSubfolderOnCaseMismatch()
        {
            // Arrange: create wrong-cased subfolder under Resources
            EnsureFolder("Assets/Resources/cASEtest");
            string assetPath = "Assets/Resources/cASEtest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(assetPath);
            LogAssert.ignoreFailingMessages = true;

            // Act: trigger creation for a singleton targeting "CaseTest" path
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no duplicate folder created and asset placed in reused folder
            Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/Resources/cASEtest"));
            Assert.IsFalse(AssetDatabase.IsValidFolder("Assets/Resources/CaseTest 1"));
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null);
        }

        [UnityTest]
        public IEnumerator SkipsCreationWhenTargetPathOccupied()
        {
            // Arrange: Create an occupying asset at the target path
            string targetFolder = TestRoot;
            EnsureFolder(targetFolder);
            string occupiedPath = targetFolder + "/Duplicate.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(occupiedPath) == null)
            {
                TextAsset ta = new("occupied");
                AssetDatabase.CreateAsset(ta, occupiedPath);
            }

            // Act: run ensure and expect a warning about occupied target
            LogAssert.Expect(LogType.Warning, new Regex("target path already occupied"));
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no duplicate asset created alongside
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(targetFolder + "/Duplicate 1.asset") == null
            );
        }

        [UnityTest]
        public IEnumerator WarnsOnTypeNameCollision()
        {
            // Arrange: ensure collision folder exists
            EnsureFolder("Assets/Resources/CreatorTests/Collision");

            // Act: ensure logs a collision warning and does not create the overlapping asset
            LogAssert.Expect(LogType.Warning, new Regex("Type name collision"));
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no asset created at the ambiguous path
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(
                    "Assets/Resources/CreatorTests/Collision/NameCollision.asset"
                ) == null
            );
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void TryDeleteFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] contents = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            if (contents == null || contents.Length == 0)
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        // Types used by the tests
        [ScriptableSingletonPath("CaseTest")]
        private sealed class CaseMismatch : ScriptableObjectSingleton<CaseMismatch> { }

        [ScriptableSingletonPath("CreatorTests")]
        private sealed class Duplicate : ScriptableObjectSingleton<Duplicate> { }
    }
#endif
}

// Name collision types in different namespaces
namespace A
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Collision")]
    internal sealed class NameCollision : ScriptableObjectSingleton<NameCollision> { }

#endif
}

// Name collision types in different namespaces
namespace B
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Collision")]
    internal sealed class NameCollision : ScriptableObjectSingleton<NameCollision> { }

#endif
}
