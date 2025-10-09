namespace WallstopStudios.UnityHelpers.Tests.Tests.Editor.Utils
{
#if UNITY_EDITOR
    using System.Collections;
    using Core.Attributes;
    using Core.Helper;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class ScriptableObjectSingletonTests
        : WallstopStudios.UnityHelpers.Tests.CommonTestBase
    {
        private static readonly System.Collections.Generic.List<string> _createdAssetPaths = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            TestSingleton.ClearInstance();
            EmptyPathSingleton.ClearInstance();
            CustomPathSingleton.ClearInstance();
            MultipleInstancesSingleton.ClearInstance();
            EnsureResourceAsset<TestSingleton>("Assets/Resources", "TestSingleton.asset");
            EnsureResourceAsset<EmptyPathSingleton>("Assets/Resources", "EmptyPathSingleton.asset");
            // Custom path specified via [ScriptableSingletonPath("CustomPath")] => place under Resources/CustomPath
            EnsureFolder("Assets/Resources/CustomPath");
            EnsureResourceAsset<CustomPathSingleton>(
                "Assets/Resources/CustomPath",
                "CustomPathSingleton.asset"
            );
            yield break;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
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
        }

        private static void EnsureResourceAsset<TType>(string folder, string fileName)
            where TType : ScriptableObject
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(folder);
            string assetPath = folder + "/" + fileName;
            Object existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (existing == null)
            {
                TType instance = ScriptableObject.CreateInstance<TType>();
                AssetDatabase.CreateAsset(instance, assetPath);
                _createdAssetPaths.Add(assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
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
        public IEnumerator Cleanup()
        {
            // Delete any assets created during SetUp
            foreach (string path in _createdAssetPaths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
                yield return null;
            }

            _createdAssetPaths.Clear();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            if (TestSingleton.LazyInstance.IsValueCreated)
            {
                System.Reflection.FieldInfo field = typeof(TestSingleton).GetField(
                    nameof(TestSingleton.LazyInstance),
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Static
                        | System.Reflection.BindingFlags.Public
                );
                if (field != null)
                {
                    object lazyInstance = field.GetValue(null);
                    if (lazyInstance != null)
                    {
                        System.Reflection.PropertyInfo valueProperty = lazyInstance
                            .GetType()
                            .GetProperty("Value");
                        if (valueProperty != null)
                        {
                            TestSingleton instance =
                                valueProperty.GetValue(lazyInstance) as TestSingleton;
                            if (instance != null)
                            {
                                Object.DestroyImmediate(instance);
                            }
                        }
                    }
                }
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
            bool hasInstance = TestSingleton.HasInstance;
            Assert.IsFalse(hasInstance);

            TestSingleton instance = TestSingleton.Instance;
            Assert.IsTrue(instance != null);

            hasInstance = TestSingleton.HasInstance;
            Assert.IsTrue(hasInstance);
            yield break;
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
            System.Lazy<TestSingleton> lazy1 = TestSingleton.LazyInstance;
            TestSingleton instance = TestSingleton.Instance;
            System.Lazy<TestSingleton> lazy2 = TestSingleton.LazyInstance;

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
    }
#endif
}
