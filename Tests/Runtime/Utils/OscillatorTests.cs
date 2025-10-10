namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class OscillatorTests : CommonTestBase
    {
        // Tracking handled by CommonTestBase

        [UnityTest]
        public IEnumerator StoresInitialPositionOnAwake()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = new Vector3(5f, 10f, 0f) },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();

            oscillator.SendMessage("Awake");
            yield return null;

            Assert.AreEqual(new Vector3(5f, 10f, 0f), oscillator._initialLocalPosition);
        }

        [UnityTest]
        public IEnumerator OscillatesAroundInitialPosition()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 2f;
            oscillator.height = 3f;

            oscillator.SendMessage("Awake");
            yield return null;

            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(Vector3.zero, go.transform.localPosition);
        }

        [UnityTest]
        public IEnumerator OscillatesWithPositiveSpeed()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 2f;
            oscillator.width = 1f;
            oscillator.height = 1f;

            oscillator.SendMessage("Awake");

            Vector3 position1 = Vector3.zero;
            Vector3 position2 = Vector3.zero;

            oscillator.SendMessage("Update");
            position1 = go.transform.localPosition;
            yield return new WaitForSeconds(0.1f);

            oscillator.SendMessage("Update");
            position2 = go.transform.localPosition;
            yield return null;

            Assert.AreNotEqual(position1, position2);
        }

        [UnityTest]
        public IEnumerator WidthAffectsXAxis()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 5f;
            oscillator.height = 0f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(0f, go.transform.localPosition.x);
            Assert.AreEqual(0f, go.transform.localPosition.y);
        }

        [UnityTest]
        public IEnumerator HeightAffectsYAxis()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 0f;
            oscillator.height = 5f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreEqual(0f, go.transform.localPosition.x);
            Assert.AreNotEqual(0f, go.transform.localPosition.y);
        }

        [UnityTest]
        public IEnumerator ZeroWidthAndHeightResultsInNoMovement()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 0f;
            oscillator.height = 0f;

            oscillator.SendMessage("Awake");

            for (int i = 0; i < 10; i++)
            {
                oscillator.SendMessage("Update");
                yield return null;
            }

            Assert.AreEqual(Vector3.zero, go.transform.localPosition);
        }

        [UnityTest]
        public IEnumerator ZeroSpeedStillOscillates()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 0f;
            oscillator.width = 2f;
            oscillator.height = 2f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            float x = Mathf.Cos(Time.time * 0f) * 2f;
            float y = Mathf.Sin(Time.time * 0f) * 2f;
            Vector3 expected = new(x, y, 0f);

            Assert.AreEqual(expected.x, go.transform.localPosition.x, 0.001f);
            Assert.AreEqual(expected.y, go.transform.localPosition.y, 0.001f);
        }

        [UnityTest]
        public IEnumerator NegativeWidthWorks()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = -2f;
            oscillator.height = 0f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(0f, go.transform.localPosition.x);
        }

        [UnityTest]
        public IEnumerator NegativeHeightWorks()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 0f;
            oscillator.height = -2f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(0f, go.transform.localPosition.y);
        }

        [UnityTest]
        public IEnumerator NegativeSpeedWorks()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = -1f;
            oscillator.width = 2f;
            oscillator.height = 2f;

            oscillator.SendMessage("Awake");

            Vector3 position1 = Vector3.zero;
            Vector3 position2 = Vector3.zero;

            oscillator.SendMessage("Update");
            position1 = go.transform.localPosition;
            yield return new WaitForSeconds(0.1f);

            oscillator.SendMessage("Update");
            position2 = go.transform.localPosition;
            yield return null;

            Assert.AreNotEqual(position1, position2);
        }

        [UnityTest]
        public IEnumerator LargeSpeedValueWorks()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 100f;
            oscillator.width = 1f;
            oscillator.height = 1f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(Vector3.zero, go.transform.localPosition);
        }

        [UnityTest]
        public IEnumerator LargeWidthAndHeightWork()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 1000f;
            oscillator.height = 1000f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(Vector3.zero, go.transform.localPosition);
        }

        [UnityTest]
        public IEnumerator SmallSpeedValueWorks()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 0.01f;
            oscillator.width = 1f;
            oscillator.height = 1f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(Vector3.zero, go.transform.localPosition);
        }

        [UnityTest]
        public IEnumerator PreservesZAxis()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = new Vector3(0f, 0f, 5f) },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 2f;
            oscillator.height = 2f;

            oscillator.SendMessage("Awake");

            for (int i = 0; i < 10; i++)
            {
                oscillator.SendMessage("Update");
                yield return null;
            }

            Assert.AreEqual(5f, go.transform.localPosition.z);
        }

        [UnityTest]
        public IEnumerator NonZeroInitialPositionWorks()
        {
            GameObject go = Track(new GameObject("Oscillator", typeof(Oscillator)));
            Vector3 initialPosition = new(10f, 20f, 30f);
            go.transform.localPosition = initialPosition;
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 2f;
            oscillator.height = 2f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(initialPosition, go.transform.localPosition);
            Assert.AreEqual(initialPosition.z, go.transform.localPosition.z);
        }

        [UnityTest]
        public IEnumerator OscillatesRelativeToInitialPosition()
        {
            GameObject go = Track(new GameObject("Oscillator", typeof(Oscillator)));
            Vector3 initialPosition = new(5f, 10f, 0f);
            go.transform.localPosition = initialPosition;
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 1f;
            oscillator.height = 1f;

            oscillator.SendMessage("Awake");

            Vector3 minPosition = initialPosition;
            Vector3 maxPosition = initialPosition;

            for (int i = 0; i < 100; i++)
            {
                oscillator.SendMessage("Update");
                yield return null;

                Vector3 currentPosition = go.transform.localPosition;
                minPosition = Vector3.Min(minPosition, currentPosition);
                maxPosition = Vector3.Max(maxPosition, currentPosition);
            }

            Vector3 center = (minPosition + maxPosition) / 2f;
            Assert.AreEqual(initialPosition.x, center.x, 0.5f);
            Assert.AreEqual(initialPosition.y, center.y, 0.5f);
        }

        [UnityTest]
        public IEnumerator MultipleOscillatorsWorkIndependently()
        {
            GameObject go1 = Track(
                new GameObject("Oscillator1", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator1 = go1.GetComponent<Oscillator>();
            oscillator1.speed = 1f;
            oscillator1.width = 2f;
            oscillator1.height = 2f;

            GameObject go2 = Track(
                new GameObject("Oscillator2", typeof(Oscillator))
                {
                    transform = { localPosition = new Vector3(10f, 10f, 0f) },
                }
            );
            Oscillator oscillator2 = go2.GetComponent<Oscillator>();
            oscillator2.speed = 2f;
            oscillator2.width = 1f;
            oscillator2.height = 1f;

            oscillator1.SendMessage("Awake");
            oscillator2.SendMessage("Awake");

            oscillator1.SendMessage("Update");
            oscillator2.SendMessage("Update");
            yield return null;

            Assert.AreNotEqual(go1.transform.localPosition, go2.transform.localPosition);
        }

        [UnityTest]
        public IEnumerator ChangingSpeedDynamically()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 2f;
            oscillator.height = 2f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            Vector3 position1 = go.transform.localPosition;
            yield return null;

            oscillator.speed = 10f;
            yield return new WaitForSeconds(0.1f);
            oscillator.SendMessage("Update");
            Vector3 position2 = go.transform.localPosition;
            yield return null;

            Assert.AreNotEqual(position1, position2);
        }

        [UnityTest]
        public IEnumerator ChangingDimensionsDynamically()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 1f;
            oscillator.height = 1f;

            oscillator.SendMessage("Awake");

            oscillator.SendMessage("Update");
            float distance1 = Vector3.Distance(go.transform.localPosition, Vector3.zero);
            yield return null;

            oscillator.width = 10f;
            oscillator.height = 10f;
            yield return new WaitForSeconds(0.5f);
            oscillator.SendMessage("Update");
            float distance2 = Vector3.Distance(go.transform.localPosition, Vector3.zero);
            yield return null;

            Assert.Greater(distance2, distance1);
        }

        [UnityTest]
        public IEnumerator UsesTimeBasedCalculation()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 2f;
            oscillator.height = 2f;

            oscillator.SendMessage("Awake");

            float time1 = Time.time;
            oscillator.SendMessage("Update");
            Vector3 position1 = go.transform.localPosition;

            yield return new WaitForSeconds(0.5f);

            float time2 = Time.time;
            oscillator.SendMessage("Update");
            Vector3 position2 = go.transform.localPosition;

            yield return null;

            Assert.AreNotEqual(time1, time2);
            Assert.AreNotEqual(position1, position2);
        }

        [UnityTest]
        public IEnumerator DisablingStopsOscillation()
        {
            GameObject go = Track(
                new GameObject("Oscillator", typeof(Oscillator))
                {
                    transform = { localPosition = Vector3.zero },
                }
            );
            Oscillator oscillator = go.GetComponent<Oscillator>();
            oscillator.speed = 1f;
            oscillator.width = 2f;
            oscillator.height = 2f;

            oscillator.SendMessage("Awake");
            oscillator.SendMessage("Update");
            yield return null;

            oscillator.enabled = false;
            Vector3 disabledPosition = go.transform.localPosition;

            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(disabledPosition, go.transform.localPosition);
        }
    }
}
