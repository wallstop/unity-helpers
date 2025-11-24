namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Collections;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tags;
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
            public static int AwakenCount;

            protected override void Awake()
            {
                base.Awake();
                AwakenCount++;
            }

            public static void ClearForTests()
            {
                AwakenCount = 0;
                if (HasInstance)
                {
                    DestroyImmediate(_instance.gameObject);
                }
                _instance = null;
            }
        }

        private sealed class AutoScriptableSingleton
            : ScriptableObjectSingleton<AutoScriptableSingleton>
        {
            public static int CreatedCount;

            private AutoScriptableSingleton()
            {
                CreatedCount++;
            }

            public static void ClearForTests()
            {
                LazyInstance = CreateLazy();
            }
        }

        [UnityTest]
        public IEnumerator AutoLoaderInitializesRuntimeSingletons()
        {
            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: true,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                CreateRuntimeEntry<AutoRuntimeSingleton>()
            );

            yield return null;

            Assert.IsTrue(AutoRuntimeSingleton.HasInstance);
            Assert.GreaterOrEqual(AutoRuntimeSingleton.AwakenCount, 1);
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

            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: true,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                CreateScriptableEntry<AutoScriptableSingleton>()
            );

            yield return null;

            Assert.IsTrue(AutoScriptableSingleton.HasInstance);
            Assert.GreaterOrEqual(AutoScriptableSingleton.CreatedCount, 1);
        }

        [UnityTest]
        public IEnumerator AutoLoaderSkipsWhenNotPlaying()
        {
            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: false,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                CreateRuntimeEntry<AutoRuntimeSingleton>()
            );

            yield return null;

            Assert.IsFalse(AutoRuntimeSingleton.HasInstance);
            Assert.AreEqual(0, AutoRuntimeSingleton.AwakenCount);
        }

        [UnityTest]
        public IEnumerator AutoLoaderSkipsScriptableSingletonsWhenNotPlaying()
        {
            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: false,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                CreateScriptableEntry<AutoScriptableSingleton>()
            );

            yield return null;

            Assert.IsFalse(AutoScriptableSingleton.HasInstance);
            Assert.AreEqual(0, AutoScriptableSingleton.CreatedCount);
        }

        [UnityTest]
        public IEnumerator AutoLoaderLogsWarningWhenTypeCannotBeResolved()
        {
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Unable to resolve type", RegexOptions.IgnoreCase)
            );

            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: true,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                new AttributeMetadataCache.AutoLoadSingletonEntry(
                    "Missing.Type, UnknownAssembly",
                    SingletonAutoLoadKind.Runtime,
                    RuntimeInitializeLoadType.BeforeSplashScreen
                )
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator AutoLoaderWarnsWhenRuntimeEntryTargetsScriptableSingleton()
        {
            LogAssert.Expect(
                LogType.Warning,
                new Regex("does not derive from RuntimeSingleton", RegexOptions.IgnoreCase)
            );

            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: true,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                CreateRuntimeEntry<AutoScriptableSingleton>()
            );

            yield return null;

            Assert.IsFalse(AutoScriptableSingleton.HasInstance);
        }

        [UnityTest]
        public IEnumerator AutoLoaderWarnsWhenScriptableEntryTargetsRuntimeSingleton()
        {
            LogAssert.Expect(
                LogType.Warning,
                new Regex("does not derive from ScriptableObjectSingleton", RegexOptions.IgnoreCase)
            );

            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: true,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                CreateScriptableEntry<AutoRuntimeSingleton>()
            );

            yield return null;

            Assert.IsFalse(AutoRuntimeSingleton.HasInstance);
        }

        [UnityTest]
        public IEnumerator AutoLoaderSkipsEntriesWithDifferentLoadType()
        {
            SingletonAutoLoader.ExecuteEntriesForTests(
                simulatePlayMode: true,
                RuntimeInitializeLoadType.AfterSceneLoad,
                new AttributeMetadataCache.AutoLoadSingletonEntry(
                    typeof(AutoRuntimeSingleton).AssemblyQualifiedName,
                    SingletonAutoLoadKind.Runtime,
                    RuntimeInitializeLoadType.BeforeSplashScreen
                )
            );

            yield return null;

            Assert.IsFalse(AutoRuntimeSingleton.HasInstance);
            Assert.AreEqual(0, AutoRuntimeSingleton.AwakenCount);
        }

        private static AttributeMetadataCache.AutoLoadSingletonEntry CreateRuntimeEntry<T>()
        {
            return new AttributeMetadataCache.AutoLoadSingletonEntry(
                typeof(T).AssemblyQualifiedName,
                SingletonAutoLoadKind.Runtime,
                RuntimeInitializeLoadType.BeforeSplashScreen
            );
        }

        private static AttributeMetadataCache.AutoLoadSingletonEntry CreateScriptableEntry<T>()
        {
            return new AttributeMetadataCache.AutoLoadSingletonEntry(
                typeof(T).AssemblyQualifiedName,
                SingletonAutoLoadKind.ScriptableObject,
                RuntimeInitializeLoadType.BeforeSplashScreen
            );
        }
    }
}
