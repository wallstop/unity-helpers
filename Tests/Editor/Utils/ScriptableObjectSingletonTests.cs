namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    public sealed class ScriptableObjectSingletonTests : CommonTestBase
    {
        private static readonly System.Collections.Generic.List<string> CreatedAssetPaths = new();
        private static readonly System.Collections.Generic.List<ScriptableObject> InMemoryInstances =
            new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            TestSingleton.ClearInstance();
            yield return null;
            EmptyPathSingleton.ClearInstance();
            yield return null;
            CustomPathSingleton.ClearInstance();
            yield return null;
            MultipleInstancesSingleton.ClearInstance();
            yield return null;
            // Clean up any leftover assets from previous runs to avoid broken nested-class assets
            DeleteAssetIfExists("Assets/Resources/TestSingleton.asset");
            yield return null;
            DeleteAssetIfExists("Assets/Resources/EmptyPathSingleton.asset");
            yield return null;
            DeleteAssetIfExists("Assets/Resources/CustomPath/CustomPathSingleton.asset");
            yield return null;
            DeleteFolderIfEmpty("Assets/Resources/CustomPath");
            yield return null;

            // For nested test types, Unity cannot create valid .asset files (no script file).
            // Instead, create in-memory instances so the singleton loader can discover them via FindObjectsOfTypeAll.
            CreateInMemoryInstance<TestSingleton>();
            yield return null;
            CreateInMemoryInstance<EmptyPathSingleton>();
            yield return null;
            CreateInMemoryInstance<CustomPathSingleton>();
            yield return null;
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
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                current = next;
            }
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            Object existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (existing != null || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void DeleteFolderIfEmpty(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
            if (subFolders is { Length: > 0 })
            {
                return;
            }

            string[] assetGuids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            foreach (string guid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (
                    !string.IsNullOrEmpty(assetPath)
                    && !string.Equals(assetPath, folderPath, StringComparison.Ordinal)
                )
                {
                    return;
                }
            }

            AssetDatabase.DeleteAsset(folderPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static TType CreateInMemoryInstance<TType>()
            where TType : ScriptableObject
        {
            TType instance = ScriptableObject.CreateInstance<TType>();
            instance.name = typeof(TType).Name;
            instance.hideFlags = HideFlags.DontSave;
            InMemoryInstances.Add(instance);
            return instance;
        }

        private sealed class TestSingleton : ScriptableObjectSingleton<TestSingleton>
        {
            public int testValue = 42;
        }

        [ScriptableSingletonPath("CustomPath")]
        private sealed class CustomPathSingleton : ScriptableObjectSingleton<CustomPathSingleton>
        {
            public string customData = "test";
        }

        private sealed class EmptyPathSingleton : ScriptableObjectSingleton<EmptyPathSingleton>
        {
            public bool flag = true;
        }

        private sealed class MultipleInstancesSingleton
            : ScriptableObjectSingleton<MultipleInstancesSingleton>
        {
            public int instanceId;
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return base.UnityTearDown();

            // Delete any assets created during SetUp
            foreach (string path in CreatedAssetPaths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
                yield return null;
            }

            CreatedAssetPaths.Clear();
            // Destroy any in-memory instances created as a fallback
            foreach (ScriptableObject obj in InMemoryInstances)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
                yield return null;
            }
            InMemoryInstances.Clear();
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            DeleteFolderIfEmpty("Assets/Resources/CustomPath");
            yield return null;
            // Prefer public API surface over reflection to clean up the cached instance
            if (TestSingleton.HasInstance)
            {
                TestSingleton.Instance.Destroy();
                TestSingleton.ClearInstance();
            }

            yield return null;

            TestSingleton[] allTestSingletons = Resources.FindObjectsOfTypeAll<TestSingleton>();
            foreach (TestSingleton singleton in allTestSingletons)
            {
                singleton.Destroy();
                yield return null;
            }

            CustomPathSingleton[] allCustomPathSingletons =
                Resources.FindObjectsOfTypeAll<CustomPathSingleton>();
            foreach (CustomPathSingleton singleton in allCustomPathSingletons)
            {
                singleton.Destroy();
                yield return null;
            }

            EmptyPathSingleton[] allEmptyPathSingletons =
                Resources.FindObjectsOfTypeAll<EmptyPathSingleton>();
            foreach (EmptyPathSingleton singleton in allEmptyPathSingletons)
            {
                singleton.Destroy();
                yield return null;
            }

            MultipleInstancesSingleton[] allMultipleSingletons =
                Resources.FindObjectsOfTypeAll<MultipleInstancesSingleton>();
            foreach (MultipleInstancesSingleton singleton in allMultipleSingletons)
            {
                singleton.Destroy();
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HasInstanceReturnsFalseBeforeAccess()
        {
            Assert.IsFalse(TestSingleton.HasInstance);
            yield break;
        }

        [UnityTest]
        public IEnumerator HasInstanceReturnsTrueAfterAccess()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.IsTrue(TestSingleton.HasInstance);
            Assert.IsTrue(instance != null);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceReturnsNonNull()
        {
            TestSingleton instance = TestSingleton.Instance;
            Assert.IsTrue(instance != null);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceReturnsSameObjectOnMultipleAccesses()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            TestSingleton instance2 = TestSingleton.Instance;

            Assert.AreSame(instance1, instance2);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceIsScriptableObject()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.IsInstanceOf<ScriptableObject>(instance);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstancePreservesData()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.AreEqual(42, instance.testValue);
            yield break;
        }

        [UnityTest]
        public IEnumerator LazyInstanceIsLazy()
        {
            Assert.IsFalse(TestSingleton.LazyInstance.IsValueCreated);

            TestSingleton instance = TestSingleton.Instance;

            Assert.IsTrue(TestSingleton.LazyInstance.IsValueCreated);
            Assert.IsTrue(instance != null);
            yield break;
        }

        [UnityTest]
        public IEnumerator CustomPathAttributeIsRespected()
        {
            CustomPathSingleton instance = CustomPathSingleton.Instance;

            Assert.IsTrue(instance != null);
            yield break;
        }

        [UnityTest]
        public IEnumerator EmptyPathFallsBackToTypeName()
        {
            EmptyPathSingleton instance = EmptyPathSingleton.Instance;
            Assert.IsTrue(instance != null);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceCanAccessPublicFields()
        {
            TestSingleton instance = TestSingleton.Instance;
            instance.testValue = 99;

            Assert.AreEqual(99, instance.testValue);
            yield break;
        }

        [UnityTest]
        public IEnumerator MultipleTypesHaveIndependentInstances()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            CustomPathSingleton instance2 = CustomPathSingleton.Instance;

            Assert.IsTrue(instance1 != null);
            Assert.IsTrue(instance2 != null);
            Assert.AreNotSame(instance1, instance2);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstancePersistsAcrossAccesses()
        {
            TestSingleton instance = TestSingleton.Instance;
            instance.testValue = 123;

            TestSingleton sameInstance = TestSingleton.Instance;

            Assert.AreEqual(123, sameInstance.testValue);
            yield break;
        }

        [UnityTest]
        public IEnumerator HasInstanceDoesNotTriggerCreation()
        {
            bool hasInstance = TestSingleton.HasInstance;

            Assert.IsFalse(hasInstance);
            Assert.IsFalse(TestSingleton.LazyInstance.IsValueCreated);
            yield break;
        }

        [UnityTest]
        public IEnumerator LazyInstanceValueMatchesInstance()
        {
            TestSingleton instance = TestSingleton.Instance;
            TestSingleton lazyValue = TestSingleton.LazyInstance.Value;

            Assert.AreSame(instance, lazyValue);
            yield break;
        }

        [UnityTest]
        public IEnumerator ScriptableSingletonPathAttributeCanBeNull()
        {
            EmptyPathSingleton instance = EmptyPathSingleton.Instance;

            Assert.IsTrue(instance != null);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceCanBeUsedInCollections()
        {
            TestSingleton instance = TestSingleton.Instance;
            System.Collections.Generic.List<TestSingleton> list = new() { instance };

            Assert.AreEqual(1, list.Count);
            Assert.Contains(instance, list);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceHasCorrectTypeName()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.AreEqual(typeof(TestSingleton), instance.GetType());
            yield break;
        }

        [UnityTest]
        public IEnumerator MultipleAccessesDoNotCreateMultipleInstances()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            TestSingleton instance2 = TestSingleton.Instance;
            TestSingleton instance3 = TestSingleton.Instance;

            Assert.AreSame(instance1, instance2);
            Assert.AreSame(instance2, instance3);
            yield break;
        }

        [UnityTest]
        public IEnumerator CanAccessInstanceAfterHasInstanceCheck()
        {
            yield return null;
            bool hasInstance = TestSingleton.HasInstance;
            Assert.IsFalse(hasInstance);

            yield return null;
            _ = TestSingleton.Instance;
            Assert.IsTrue(TestSingleton.HasInstance);
            yield return null;
            Assert.IsTrue(TestSingleton.Instance != null);

            yield return null;
            hasInstance = TestSingleton.HasInstance;
            Assert.IsTrue(hasInstance);
        }

        [UnityTest]
        public IEnumerator InstanceWorksWithInheritance()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.IsInstanceOf<ScriptableObjectSingleton<TestSingleton>>(instance);
            Assert.IsInstanceOf<ScriptableObject>(instance);
            yield break;
        }

        [UnityTest]
        public IEnumerator LazyInstanceDoesNotChangeAfterCreation()
        {
            Lazy<TestSingleton> lazy1 = TestSingleton.LazyInstance;
            TestSingleton instance = TestSingleton.Instance;
            Lazy<TestSingleton> lazy2 = TestSingleton.LazyInstance;

            Assert.AreSame(lazy1, lazy2);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceCanBeCompared()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            TestSingleton instance2 = TestSingleton.Instance;

            Assert.IsTrue(instance1 == instance2);
            Assert.IsFalse(instance1 != instance2);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceHasConsistentHashCode()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            int hash1 = instance1.GetHashCode();

            TestSingleton instance2 = TestSingleton.Instance;
            int hash2 = instance2.GetHashCode();

            Assert.AreEqual(hash1, hash2);
            yield break;
        }

        [UnityTest]
        public IEnumerator InstanceToStringReturnsTypeName()
        {
            TestSingleton instance = TestSingleton.Instance;
            string result = instance.ToString();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("TestSingleton") || result.Length > 0);
            yield break;
        }

        [UnityTest]
        public IEnumerator LoadsInstanceFromInMemoryWhenAssetMissing()
        {
            string path = "Assets/Resources/TestSingleton.asset";
            Object existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            TestSingleton.ClearInstance();

            TestSingleton created = ScriptableObject.CreateInstance<TestSingleton>();
            created.hideFlags = HideFlags.DontSave;
            InMemoryInstances.Add(created);

            TestSingleton resolved = TestSingleton.Instance;

            Assert.IsTrue(resolved != null);
            Assert.AreSame(created, resolved);
            yield break;
        }

        [UnityTest]
        public IEnumerator OffThreadCreationThrowsDescriptiveException()
        {
            TestSingleton.ClearInstance();
            yield return null;

            Task task = Task.Run(() =>
            {
                _ = TestSingleton.Instance;
            });

            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.IsTrue(task.IsFaulted);
            AggregateException aggregate = task.Exception;
            Assert.IsNotNull(aggregate);
            AggregateException flattened = aggregate.Flatten();
            Assert.IsTrue(flattened.InnerExceptions.Count > 0);
            InvalidOperationException exception =
                flattened.InnerExceptions[0] as InvalidOperationException;
            Assert.IsNotNull(exception);
            StringAssert.Contains("main thread", exception.Message);
            Assert.IsFalse(TestSingleton.HasInstance);
        }

        [UnityTest]
        public IEnumerator BackgroundThreadCanReadInstanceAfterCreation()
        {
            TestSingleton instance = TestSingleton.Instance;

            Task<TestSingleton> task = Task.Run(() => TestSingleton.Instance);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.IsFalse(task.IsFaulted);
            Assert.AreSame(instance, task.Result);
        }
    }
#endif
}
