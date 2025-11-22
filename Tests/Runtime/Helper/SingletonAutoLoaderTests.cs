namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class SingletonAutoLoaderTests : CommonTestBase
    {
        [SetUp]
        public void Reset()
        {
            AutoRuntimeSingleton.ClearForTests();
            AutoScriptableSingleton.ClearForTests();
        }

        private sealed class AutoRuntimeSingleton : RuntimeSingleton<AutoRuntimeSingleton>
        {
            public static int awakenCount;

            protected override void Awake()
            {
                base.Awake();
                awakenCount++;
            }

            public static void ClearForTests()
            {
                awakenCount = 0;
                if (HasInstance)
                {
                    Object.DestroyImmediate(_instance.gameObject);
                }
                _instance = null;
            }
        }

        private sealed class AutoScriptableSingleton
            : ScriptableObjectSingleton<AutoScriptableSingleton>
        {
            public static int createdCount;

            private AutoScriptableSingleton()
            {
                createdCount++;
            }

            public static void ClearForTests()
            {
                LazyInstance = CreateLazy();
            }
        }

        [UnityTest]
        public IEnumerator AutoLoaderInitializesRuntimeSingletons()
        {
            SingletonAutoLoader.ExecuteForTests(
                SingletonAutoLoadDescriptor.Runtime<AutoRuntimeSingleton>(
                    RuntimeInitializeLoadType.BeforeSplashScreen
                )
            );

            yield return null;

            Assert.IsTrue(AutoRuntimeSingleton.HasInstance);
            Assert.GreaterOrEqual(AutoRuntimeSingleton.awakenCount, 1);
            Track(AutoRuntimeSingleton.Instance.gameObject);
        }

        [UnityTest]
        public IEnumerator AutoLoaderInitializesScriptableSingletons()
        {
            AutoScriptableSingleton instance =
                ScriptableObject.CreateInstance<AutoScriptableSingleton>();
            instance.name = nameof(AutoScriptableSingleton);
            instance.hideFlags = HideFlags.DontSave;
            Track(instance);

            SingletonAutoLoader.ExecuteForTests(
                SingletonAutoLoadDescriptor.ScriptableObject<AutoScriptableSingleton>(
                    RuntimeInitializeLoadType.BeforeSplashScreen
                )
            );

            yield return null;

            Assert.IsTrue(AutoScriptableSingleton.HasInstance);
            Assert.GreaterOrEqual(AutoScriptableSingleton.createdCount, 1);
        }
    }
}
