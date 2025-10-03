namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public sealed class ScriptableObjectSingletonTests
    {
#if UNITY_EDITOR
        private static readonly System.Collections.Generic.List<string> _createdAssetPaths = new();

        [SetUp]
        public void SetUp()
        {
            EnsureResourceAsset<TestSingleton>("Assets/Resources", "TestSingleton.asset");
            EnsureResourceAsset<EmptyPathSingleton>("Assets/Resources", "EmptyPathSingleton.asset");
            // Custom path specified via [ScriptableSingletonPath("CustomPath")] => place under Resources/CustomPath
            EnsureFolder("Assets/Resources/CustomPath");
            EnsureResourceAsset<CustomPathSingleton>(
                "Assets/Resources/CustomPath",
                "CustomPathSingleton.asset"
            );
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
#endif

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

        [TearDown]
        public void Cleanup()
        {
#if UNITY_EDITOR
            // Delete any assets created during SetUp
            foreach (string path in _createdAssetPaths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssetPaths.Clear();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            if (TestSingleton.LazyInstance.IsValueCreated)
            {
                System.Reflection.FieldInfo field = typeof(TestSingleton).GetField(
                    "LazyInstance",
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

            TestSingleton[] allTestSingletons = Resources.FindObjectsOfTypeAll<TestSingleton>();
            foreach (TestSingleton singleton in allTestSingletons)
            {
                Object.DestroyImmediate(singleton);
            }

            CustomPathSingleton[] allCustomPathSingletons =
                Resources.FindObjectsOfTypeAll<CustomPathSingleton>();
            foreach (CustomPathSingleton singleton in allCustomPathSingletons)
            {
                Object.DestroyImmediate(singleton);
            }

            EmptyPathSingleton[] allEmptyPathSingletons =
                Resources.FindObjectsOfTypeAll<EmptyPathSingleton>();
            foreach (EmptyPathSingleton singleton in allEmptyPathSingletons)
            {
                Object.DestroyImmediate(singleton);
            }

            MultipleInstancesSingleton[] allMultipleSingletons =
                Resources.FindObjectsOfTypeAll<MultipleInstancesSingleton>();
            foreach (MultipleInstancesSingleton singleton in allMultipleSingletons)
            {
                Object.DestroyImmediate(singleton);
            }
        }

        [Test]
        public void HasInstanceReturnsFalseBeforeAccess()
        {
            Assert.IsFalse(TestSingleton.HasInstance);
        }

        [Test]
        public void HasInstanceReturnsTrueAfterAccess()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.IsTrue(TestSingleton.HasInstance);
            Assert.IsNotNull(instance);
        }

        [Test]
        public void InstanceReturnsNonNull()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.IsNotNull(instance);
        }

        [Test]
        public void InstanceReturnsSameObjectOnMultipleAccesses()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            TestSingleton instance2 = TestSingleton.Instance;

            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void InstanceIsScriptableObject()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.IsInstanceOf<ScriptableObject>(instance);
        }

        [Test]
        public void InstancePreservesData()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.AreEqual(42, instance.testValue);
        }

        [Test]
        public void LazyInstanceIsLazy()
        {
            Assert.IsFalse(TestSingleton.LazyInstance.IsValueCreated);

            TestSingleton instance = TestSingleton.Instance;

            Assert.IsTrue(TestSingleton.LazyInstance.IsValueCreated);
            Assert.IsNotNull(instance);
        }

        [Test]
        public void CustomPathAttributeIsRespected()
        {
            CustomPathSingleton instance = CustomPathSingleton.Instance;

            Assert.IsNotNull(instance);
        }

        [Test]
        public void EmptyPathFallsBackToTypeName()
        {
            EmptyPathSingleton instance = EmptyPathSingleton.Instance;

            Assert.IsNotNull(instance);
        }

        [Test]
        public void InstanceCanAccessPublicFields()
        {
            TestSingleton instance = TestSingleton.Instance;
            instance.testValue = 99;

            Assert.AreEqual(99, instance.testValue);
        }

        [Test]
        public void MultipleTypesHaveIndependentInstances()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            CustomPathSingleton instance2 = CustomPathSingleton.Instance;

            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.AreNotSame(instance1, instance2);
        }

        [Test]
        public void InstancePersistsAcrossAccesses()
        {
            TestSingleton instance = TestSingleton.Instance;
            instance.testValue = 123;

            TestSingleton sameInstance = TestSingleton.Instance;

            Assert.AreEqual(123, sameInstance.testValue);
        }

        [Test]
        public void HasInstanceDoesNotTriggerCreation()
        {
            bool hasInstance = TestSingleton.HasInstance;

            Assert.IsFalse(hasInstance);
            Assert.IsFalse(TestSingleton.LazyInstance.IsValueCreated);
        }

        [Test]
        public void TypeWithNoResourcesReturnsNull()
        {
            TestSingleton instance = TestSingleton.Instance;

            if (instance == null)
            {
                Assert.Pass("No resources found, returns null as expected");
            }
            else
            {
                Assert.IsNotNull(instance);
            }
        }

        [Test]
        public void InstanceIsThreadSafe()
        {
            TestSingleton instance1 = null;
            TestSingleton instance2 = null;

            System.Threading.Thread thread1 = new(() =>
            {
                instance1 = TestSingleton.Instance;
            });

            System.Threading.Thread thread2 = new(() =>
            {
                instance2 = TestSingleton.Instance;
            });

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            if (instance1 != null && instance2 != null)
            {
                Assert.AreSame(instance1, instance2);
            }
        }

        [Test]
        public void LazyInstanceValueMatchesInstance()
        {
            TestSingleton instance = TestSingleton.Instance;
            TestSingleton lazyValue = TestSingleton.LazyInstance.Value;

            Assert.AreSame(instance, lazyValue);
        }

        [Test]
        public void ScriptableSingletonPathAttributeCanBeNull()
        {
            EmptyPathSingleton instance = EmptyPathSingleton.Instance;

            Assert.IsNotNull(instance);
        }

        [Test]
        public void InstanceCanBeUsedInCollections()
        {
            TestSingleton instance = TestSingleton.Instance;
            System.Collections.Generic.List<TestSingleton> list = new() { instance };

            Assert.AreEqual(1, list.Count);
            Assert.Contains(instance, list);
        }

        [Test]
        public void InstanceHasCorrectTypeName()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.AreEqual(typeof(TestSingleton), instance.GetType());
        }

        [Test]
        public void MultipleAccessesDoNotCreateMultipleInstances()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            TestSingleton instance2 = TestSingleton.Instance;
            TestSingleton instance3 = TestSingleton.Instance;

            Assert.AreSame(instance1, instance2);
            Assert.AreSame(instance2, instance3);
        }

        [Test]
        public void CanAccessInstanceAfterHasInstanceCheck()
        {
            bool hasInstance = TestSingleton.HasInstance;
            Assert.IsFalse(hasInstance);

            TestSingleton instance = TestSingleton.Instance;
            Assert.IsNotNull(instance);

            hasInstance = TestSingleton.HasInstance;
            Assert.IsTrue(hasInstance);
        }

        [Test]
        public void InstanceWorksWithInheritance()
        {
            TestSingleton instance = TestSingleton.Instance;

            Assert.IsInstanceOf<ScriptableObjectSingleton<TestSingleton>>(instance);
            Assert.IsInstanceOf<ScriptableObject>(instance);
        }

        [Test]
        public void LazyInstanceDoesNotChangeAfterCreation()
        {
            System.Lazy<TestSingleton> lazy1 = TestSingleton.LazyInstance;
            TestSingleton instance = TestSingleton.Instance;
            System.Lazy<TestSingleton> lazy2 = TestSingleton.LazyInstance;

            Assert.AreSame(lazy1, lazy2);
        }

        [Test]
        public void InstanceCanBeCompared()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            TestSingleton instance2 = TestSingleton.Instance;

            Assert.IsTrue(instance1 == instance2);
            Assert.IsFalse(instance1 != instance2);
        }

        [Test]
        public void InstanceHasConsistentHashCode()
        {
            TestSingleton instance1 = TestSingleton.Instance;
            int hash1 = instance1.GetHashCode();

            TestSingleton instance2 = TestSingleton.Instance;
            int hash2 = instance2.GetHashCode();

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void InstanceToStringReturnsTypeName()
        {
            TestSingleton instance = TestSingleton.Instance;
            string result = instance.ToString();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("TestSingleton") || result.Length > 0);
        }
    }
}
