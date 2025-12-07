namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class CoroutineHandlerTests : CommonTestBase
    {
        // Tracking handled by CommonTestBase

        [UnityTest]
        public IEnumerator CreatesInstanceOnFirstAccess()
        {
            Assert.IsFalse(CoroutineHandler.HasInstance);

            CoroutineHandler instance = CoroutineHandler.Instance;
            Track(instance.gameObject);

            Assert.IsTrue(CoroutineHandler.HasInstance);
            Assert.IsTrue(instance != null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReturnsSameInstanceOnMultipleAccesses()
        {
            CoroutineHandler instance1 = CoroutineHandler.Instance;
            Track(instance1.gameObject);
            CoroutineHandler instance2 = CoroutineHandler.Instance;

            Assert.AreSame(instance1, instance2);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanStartCoroutine()
        {
            bool coroutineRan = false;

            CoroutineHandler inst = CoroutineHandler.Instance;
            Track(inst.gameObject);
            inst.StartCoroutine(TestCoroutine());

            yield return null;
            yield return null;

            Assert.IsTrue(coroutineRan);
            yield break;

            IEnumerator TestCoroutine()
            {
                yield return null;
                coroutineRan = true;
            }
        }

        [UnityTest]
        public IEnumerator CanStopCoroutine()
        {
            int counter = 0;

            CoroutineHandler inst2 = CoroutineHandler.Instance;
            Track(inst2.gameObject);
            Coroutine coroutine = inst2.StartCoroutine(TestCoroutine());

            yield return null;
            yield return null;

            int countBeforeStop = counter;
            CoroutineHandler.Instance.StopCoroutine(coroutine);

            yield return null;
            yield return null;

            Assert.AreEqual(countBeforeStop, counter);
            yield break;

            IEnumerator TestCoroutine()
            {
                while (true)
                {
                    counter++;
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator CanStopAllCoroutines()
        {
            int counter1 = 0;
            int counter2 = 0;

            CoroutineHandler inst3 = CoroutineHandler.Instance;
            Track(inst3.gameObject);
            inst3.StartCoroutine(TestCoroutine1());
            inst3.StartCoroutine(TestCoroutine2());

            yield return null;
            yield return null;

            int count1BeforeStop = counter1;
            int count2BeforeStop = counter2;

            inst3.StopAllCoroutines();

            yield return null;
            yield return null;

            Assert.AreEqual(count1BeforeStop, counter1);
            Assert.AreEqual(count2BeforeStop, counter2);
            yield break;

            IEnumerator TestCoroutine1()
            {
                while (true)
                {
                    counter1++;
                    yield return null;
                }
            }

            IEnumerator TestCoroutine2()
            {
                while (true)
                {
                    counter2++;
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator CoroutineRunsOverMultipleFrames()
        {
            int frameCount = 0;

            CoroutineHandler inst4 = CoroutineHandler.Instance;
            Track(inst4.gameObject);
            inst4.StartCoroutine(TestCoroutine());

            for (int i = 0; i < 6; i++)
            {
                yield return null;
            }

            Assert.AreEqual(5, frameCount);
            yield break;

            IEnumerator TestCoroutine()
            {
                for (int i = 0; i < 5; i++)
                {
                    frameCount++;
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator CoroutineCanWaitForSeconds()
        {
            bool completed = false;
            float startTime = Time.time;

            CoroutineHandler.Instance.StartCoroutine(TestCoroutine());

            Assert.IsFalse(completed);

            yield return new WaitForSeconds(0.15f);

            Assert.IsTrue(completed);
            Assert.Greater(Time.time - startTime, 0.1f);
            yield break;

            IEnumerator TestCoroutine()
            {
                yield return new WaitForSeconds(0.1f);
                completed = true;
            }
        }

        [UnityTest]
        public IEnumerator CanRunMultipleCoroutinesConcurrently()
        {
            bool coroutine1Completed = false;
            bool coroutine2Completed = false;
            bool coroutine3Completed = false;

            CoroutineHandler.Instance.StartCoroutine(TestCoroutine1());
            CoroutineHandler.Instance.StartCoroutine(TestCoroutine2());
            CoroutineHandler.Instance.StartCoroutine(TestCoroutine3());

            yield return null;
            yield return null;

            Assert.IsTrue(coroutine1Completed);
            Assert.IsTrue(coroutine2Completed);
            Assert.IsTrue(coroutine3Completed);
            yield break;

            IEnumerator TestCoroutine1()
            {
                yield return null;
                coroutine1Completed = true;
            }

            IEnumerator TestCoroutine2()
            {
                yield return null;
                coroutine2Completed = true;
            }

            IEnumerator TestCoroutine3()
            {
                yield return null;
                coroutine3Completed = true;
            }
        }

        [UnityTest]
        public IEnumerator CoroutineCanYieldNestedCoroutine()
        {
            bool innerCompleted = false;
            bool outerCompleted = false;

            CoroutineHandler.Instance.StartCoroutine(OuterCoroutine());

            yield return null;
            yield return null;

            Assert.IsTrue(innerCompleted);
            Assert.IsTrue(outerCompleted);
            yield break;

            IEnumerator InnerCoroutine()
            {
                yield return null;
                innerCompleted = true;
            }

            IEnumerator OuterCoroutine()
            {
                yield return InnerCoroutine();
                outerCompleted = true;
            }
        }

        [UnityTest]
        public IEnumerator SingletonPersistsAcrossFrames()
        {
            CoroutineHandler instance1 = CoroutineHandler.Instance;

            yield return null;
            yield return null;

            CoroutineHandler instance2 = CoroutineHandler.Instance;

            Assert.AreSame(instance1, instance2);
        }

        [UnityTest]
        public IEnumerator InstanceIsDontDestroyOnLoad()
        {
            CoroutineHandler instance = CoroutineHandler.Instance;

            yield return null;

            Assert.IsTrue(
                instance.gameObject.scene.name == "DontDestroyOnLoad"
                    || instance.gameObject.hideFlags.HasFlag(HideFlags.DontSave)
            );
        }

        [UnityTest]
        public IEnumerator StoppingNonexistentCoroutineDoesNotThrow()
        {
            Coroutine coroutine = CoroutineHandler.Instance.StartCoroutine(DummyCoroutine());

            yield return null;
            yield return null;

            CoroutineHandler.Instance.StopCoroutine(coroutine);

            yield return null;

            Assert.Pass();
            yield break;

            IEnumerator DummyCoroutine()
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator HandlesExceptionInCoroutine()
        {
            bool continueAfterException = false;

            LogAssert.Expect(
                LogType.Exception,
                new System.Text.RegularExpressions.Regex(".*Test exception.*")
            );
            CoroutineHandler.Instance.StartCoroutine(ThrowingCoroutine());
            CoroutineHandler.Instance.StartCoroutine(SafeCoroutine());

            yield return null;
            yield return null;
            yield return null;

            Assert.IsTrue(continueAfterException);
            yield break;

            IEnumerator ThrowingCoroutine()
            {
                yield return null;
                throw new System.Exception("Test exception");
            }

            IEnumerator SafeCoroutine()
            {
                yield return null;
                yield return null;
                continueAfterException = true;
            }
        }

        [UnityTest]
        public IEnumerator CoroutineStopsWhenObjectDestroyed()
        {
            int counter = 0;

            CoroutineHandler instance = CoroutineHandler.Instance;
            instance.StartCoroutine(TestCoroutine());

            yield return null;
            yield return null;

            int countBeforeDestroy = counter;

            Object.Destroy(instance.gameObject);

            yield return null;
            yield return null;

            Assert.AreEqual(countBeforeDestroy, counter);
            yield break;

            IEnumerator TestCoroutine()
            {
                while (true)
                {
                    counter++;
                    yield return null;
                }
            }
        }
    }
}
