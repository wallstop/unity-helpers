namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;
    using Object = UnityEngine.Object;

    public sealed class HelpersTests
    {
        private readonly List<Object> _spawned = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (Object spawned in _spawned.ToArray())
            {
                if (spawned == null)
                {
                    continue;
                }

                Object.Destroy(spawned);
                yield return null;
            }

            _spawned.Clear();
        }

        [Test]
        public void IsRunningInBatchModeReflectsApplication()
        {
            Assert.AreEqual(Application.isBatchMode, Helpers.IsRunningInBatchMode);
        }

        [Test]
        public void IsRunningInContinuousIntegrationRespectsEnvironmentVariables()
        {
            string originalGitHub = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            string originalCi = Environment.GetEnvironmentVariable("CI");
            string originalJenkins = Environment.GetEnvironmentVariable("JENKINS_URL");
            string originalGitlab = Environment.GetEnvironmentVariable("GITLAB_CI");

            try
            {
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
                Environment.SetEnvironmentVariable("CI", null);
                Environment.SetEnvironmentVariable("JENKINS_URL", null);
                Environment.SetEnvironmentVariable("GITLAB_CI", null);

                Assert.IsFalse(Helpers.IsRunningInContinuousIntegration);

                Environment.SetEnvironmentVariable("CI", "true");
                Assert.IsTrue(Helpers.IsRunningInContinuousIntegration);

                Environment.SetEnvironmentVariable("CI", null);
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "1");
                Assert.IsTrue(Helpers.IsRunningInContinuousIntegration);

                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
                Environment.SetEnvironmentVariable("JENKINS_URL", "http://localhost");
                Assert.IsTrue(Helpers.IsRunningInContinuousIntegration);

                Environment.SetEnvironmentVariable("JENKINS_URL", null);
                Environment.SetEnvironmentVariable("GITLAB_CI", "true");
                Assert.IsTrue(Helpers.IsRunningInContinuousIntegration);
            }
            finally
            {
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", originalGitHub);
                Environment.SetEnvironmentVariable("CI", originalCi);
                Environment.SetEnvironmentVariable("JENKINS_URL", originalJenkins);
                Environment.SetEnvironmentVariable("GITLAB_CI", originalGitlab);
            }
        }

        [Test]
        public void GetAllSpriteLabelNamesReturnsEmptyWhenBatchOrCi()
        {
            string originalCi = Environment.GetEnvironmentVariable("CI");
            FieldInfo cacheField = typeof(Helpers).GetField(
                nameof(Helpers.AllSpriteLabels),
                BindingFlags.Static | BindingFlags.NonPublic
            );
            string[] cached = cacheField?.GetValue(null) as string[];

            try
            {
                Environment.SetEnvironmentVariable("CI", "true");
                cacheField?.SetValue(null, null);

                string[] labels = Helpers.GetAllSpriteLabelNames();
                Assert.IsNotNull(labels);
                Assert.IsEmpty(labels);
            }
            finally
            {
                Environment.SetEnvironmentVariable("CI", originalCi);
                cacheField?.SetValue(null, cached);
            }
        }

        [Test]
        public void GetAllLayerNamesMatchesUnity()
        {
            string[] layerNames = Helpers.GetAllLayerNames();
            Assert.AreNotEqual(0, layerNames.Length, string.Join(", ", layerNames));
        }

        [UnityTest]
        public IEnumerator PredictCurrentTargetReturnsPositionWhenNonPredictive()
        {
            GameObject target = new("PredictTarget_NonPredictive");
            _spawned.Add(target);

            target.transform.position = new Vector3(5f, 2f, 0f);
            Vector2 predicted = target.PredictCurrentTarget(
                Vector2.zero,
                10f,
                predictiveFiring: false,
                targetVelocity: Vector2.one
            );

            Assert.AreEqual((Vector2)target.transform.position, predicted);
            yield break;
        }

        [UnityTest]
        public IEnumerator PredictCurrentTargetComputesIntercept()
        {
            GameObject target = new("PredictTarget_Predictive");
            _spawned.Add(target);

            Vector2 launch = Vector2.zero;
            target.transform.position = new Vector3(10f, 5f, 0f);
            const float projectileSpeed = 12f;
            Vector2 velocity = new(2f, -1f);

            Vector2 predicted = target.PredictCurrentTarget(
                launch,
                projectileSpeed,
                predictiveFiring: true,
                targetVelocity: velocity
            );

            Vector2 current = target.transform.position;
            float a = velocity.sqrMagnitude - projectileSpeed * projectileSpeed;
            float b =
                2f * (velocity.x * (current.x - launch.x) + velocity.y * (current.y - launch.y));
            float c = (current - launch).sqrMagnitude;
            float disc = b * b - 4f * a * c;
            Assert.GreaterOrEqual(disc, 0f, "Discriminant must be non-negative");
            float sqrt = Mathf.Sqrt(disc);
            float t1 = (-b + sqrt) / (2f * a);
            float t2 = (-b - sqrt) / (2f * a);
            float expectedTime = Mathf.Max(t1, t2);
            Vector2 expected = current + velocity * expectedTime;

            Assert.AreEqual(expected.x, predicted.x, 1e-3f);
            Assert.AreEqual(expected.y, predicted.y, 1e-3f);
            yield break;
        }

        [UnityTest]
        public IEnumerator GetComponentReturnsComponentForUnityObjects()
        {
            GameObject go = new("Helpers_GetComponent", typeof(SpriteRenderer));
            _spawned.Add(go);

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);

            Assert.AreSame(renderer, Helpers.GetComponent<SpriteRenderer>(go));
            Assert.AreSame(renderer, Helpers.GetComponent<SpriteRenderer>(renderer));

            Assert.IsNull(Helpers.GetComponent<SpriteRenderer>(null));
            Assert.IsNull(Helpers.GetComponent<SpriteRenderer>(null));
            yield break;
        }

        [UnityTest]
        public IEnumerator GetComponentsReturnsAllComponents()
        {
            GameObject go = new(
                "Helpers_GetComponents",
                typeof(SpriteRenderer),
                typeof(BoxCollider2D)
            );
            _spawned.Add(go);

            Component[] expected = go.GetComponents<Component>();
            Component[] fromGo = Helpers.GetComponents<Component>(go);
            Component[] fromRenderer = Helpers.GetComponents<Component>(
                go.GetComponent<SpriteRenderer>()
            );

            Assert.That(fromGo, Is.EquivalentTo(expected));
            Assert.That(fromRenderer, Is.EquivalentTo(expected));
            yield break;
        }

        [UnityTest]
        public IEnumerator TryGetComponentWrapsUnityImplementation()
        {
            GameObject go = new("Helpers_TryGetComponent", typeof(SpriteRenderer));
            _spawned.Add(go);

            Assert.IsTrue(Helpers.TryGetComponent(go, out SpriteRenderer renderer));
            Assert.IsNotNull(renderer);

            Assert.IsFalse(Helpers.TryGetComponent<BoxCollider2D>(go, out _));
            Assert.IsFalse(Helpers.TryGetComponent<SpriteRenderer>(null, out _));
            yield break;
        }

        [UnityTest]
        public IEnumerator FindChildGameObjectWithTagFindsFirstMatch()
        {
            GameObject parent = new("Helpers_FindTagParent");
            _spawned.Add(parent);
            GameObject child = new("Helpers_FindTagChild");
            _spawned.Add(child);
            GameObject grandChild = new("Helpers_FindTagGrandChild");
            _spawned.Add(grandChild);

            child.transform.SetParent(parent.transform);
            grandChild.transform.SetParent(child.transform);

            bool originalIgnore = LogAssert.ignoreFailingMessages;
            try
            {
                // Suppress possible tag-not-defined errors
                LogAssert.ignoreFailingMessages = true;

                bool playerTagSet = true;
                try
                {
                    // May throw if tag does not exist in this project
                    grandChild.tag = "Player";
                }
                catch (UnityException)
                {
                    playerTagSet = false;
                }

                GameObject foundPlayer = parent.FindChildGameObjectWithTag("Player");
                if (playerTagSet)
                {
                    Assert.AreSame(grandChild, foundPlayer);
                }
                else
                {
                    Assert.IsNull(foundPlayer);
                }

                // Whether the tag "NonExistentTag" exists or not, there should be no matching child
                GameObject foundNonExistent = parent.FindChildGameObjectWithTag("NonExistentTag");
                Assert.IsNull(foundNonExistent);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = originalIgnore;
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator StartFunctionAsCoroutineInvokesActionRepeatedly()
        {
            CoroutineHost host = CreateHost();
            _spawned.Add(host.gameObject);

            Coroutine coroutine = host.StartFunctionAsCoroutine(host.Increment, 0.05f);
            yield return null;
            Assert.GreaterOrEqual(host.InvocationCount, 1);

            int recorded = host.InvocationCount;
            yield return new WaitForSeconds(0.1f);
            Assert.Greater(host.InvocationCount, recorded);
            host.StopCoroutine(coroutine);
        }

        [UnityTest]
        public IEnumerator StartFunctionAsCoroutineWaitsBeforeFirstInvocationWhenRequested()
        {
            CoroutineHost host = CreateHost();
            _spawned.Add(host.gameObject);

            Coroutine coroutine = host.StartFunctionAsCoroutine(
                host.Increment,
                0.01f,
                useJitter: false,
                waitBefore: true
            );

            yield return null;
            Assert.AreEqual(0, host.InvocationCount);

            while (host.InvocationCount == 0)
            {
                yield return null;
            }

            host.StopCoroutine(coroutine);
        }

        [UnityTest]
        public IEnumerator ExecuteFunctionAfterDelayInvokesOnce()
        {
            CoroutineHost host = CreateHost();
            _spawned.Add(host.gameObject);

            float delay = 0.05f;
            Coroutine coroutine = host.ExecuteFunctionAfterDelay(host.SetFlagTrue, delay);
            Assert.IsFalse(host.Flag);

            yield return new WaitForSeconds(delay + 0.02f);
            Assert.IsTrue(host.Flag);

            host.StopCoroutine(coroutine);
        }

        [UnityTest]
        public IEnumerator ExecuteFunctionNextFrameInvokesOnSubsequentFrame()
        {
            CoroutineHost host = CreateHost();
            _spawned.Add(host.gameObject);

            Coroutine coroutine = host.ExecuteFunctionNextFrame(host.Increment);
            Assert.AreEqual(0, host.InvocationCount);
            yield return null;
            Assert.AreEqual(1, host.InvocationCount);
            host.StopCoroutine(coroutine);
        }

        [UnityTest]
        public IEnumerator ExecuteFunctionAfterFrameInvokesAfterEndOfFrame()
        {
            CoroutineHost host = CreateHost();
            _spawned.Add(host.gameObject);

            Coroutine coroutine = host.ExecuteFunctionAfterFrame(host.Increment);
            Assert.AreEqual(0, host.InvocationCount);
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.AreEqual(1, host.InvocationCount);
            host.StopCoroutine(coroutine);
        }

        [UnityTest]
        public IEnumerator ExecuteOverTimeRespectsCountAndDuration()
        {
            CoroutineHost host = CreateHost();
            _spawned.Add(host.gameObject);
            int totalInvocations;

            IEnumerator routine = Helpers.ExecuteOverTime(host.Increment, 3, 0.05f, delay: false);
            host.StartCoroutine(routine);

            float timeout = Time.time + 1f;
            while (host.InvocationCount < 3 && Time.time < timeout)
            {
                yield return null;
            }
            totalInvocations = host.InvocationCount;

            Assert.IsTrue(
                3 <= totalInvocations,
                $"Expecteed total invocations of at least 3, got {totalInvocations}"
            );
        }

        [UnityTest]
        public IEnumerator HasEnoughTimePassedTracksElapsedTime()
        {
            float timestamp = Time.time;
            Assert.IsFalse(Helpers.HasEnoughTimePassed(timestamp, 0.05f));
            yield return new WaitForSeconds(0.051f);
            Assert.IsTrue(Helpers.HasEnoughTimePassed(timestamp, 0.05f));
        }

        [Test]
        public void OppositeReturnsNegatedVector()
        {
            Vector2 v2 = new(1.5f, -2f);
            Vector3 v3 = new(3f, -4f, 5f);
            Assert.AreEqual(-v2, v2.Opposite());
            Assert.AreEqual(-v3, v3.Opposite());
        }

        [Test]
        public void IterateAreaYieldsAllPositionsWithinBounds()
        {
            BoundsInt bounds = new(0, 0, 0, 2, 2, 2);
            List<Vector3Int> positions = bounds.IterateArea().ToList();
            Assert.AreEqual(bounds.size.x * bounds.size.y * bounds.size.z, positions.Count);
            List<Vector3Int> expected = new();
            foreach (Vector3Int position in bounds.allPositionsWithin)
            {
                expected.Add(position);
            }
            Assert.That(positions, Is.EquivalentTo(expected));
        }

        [Test]
        public void IterateBoundsIncludesPadding()
        {
            BoundsInt bounds = new(0, 0, 0, 1, 1, 1);
            List<Vector3Int> positions = bounds.IterateBounds(1).ToList();
            Assert.IsTrue(positions.Contains(new Vector3Int(-1, 0, 0)));
            Assert.IsTrue(positions.Contains(new Vector3Int(2, 2, 0)));
            Assert.IsTrue(positions.Contains(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void VectorConversionsAreConsistent()
        {
            Vector3Int v3 = new(1, 2, 3);
            Assert.AreEqual(v3, (1, 2, 3).AsVector3Int());
            Assert.AreEqual(v3, ((uint)1, (uint)2, (uint)3).AsVector3Int());
            Assert.AreEqual(v3, new Vector3(1.4f, 1.6f, 3.2f).AsVector3Int());
            Assert.AreEqual((Vector3)v3, v3.AsVector3());
            Assert.AreEqual(new Vector2(v3.x, v3.y), v3.AsVector2());
            Assert.AreEqual(new Vector2Int(v3.x, v3.y), v3.AsVector2Int());
            Assert.AreEqual(new Vector3Int(1, 2, 0), new Vector2Int(v3.x, v3.y).AsVector3Int());
            Assert.AreEqual(new Vector3(1f, 2f, 3f), ((uint)1, (uint)2, (uint)3).AsVector3());
        }

        [Test]
        public void AsRectConvertsBoundsInt()
        {
            BoundsInt bounds = new(1, 2, 0, 3, 4, 1);
            Rect rect = bounds.AsRect();
            Assert.AreEqual(new Rect(1, 2, 3, 4), rect);
        }

        [Test]
        public void GetRandomPointInCircleUsesProvidedRandom()
        {
            DeterministicRandom random = new(new double[] { 0.25, 0.0 });
            Vector2 point = Helpers.GetRandomPointInCircle(new Vector2(1f, 1f), 2f, random);
            Assert.AreEqual(2f, point.x, 1e-4f);
            Assert.AreEqual(1f, point.y, 1e-4f);
        }

        [UnityTest]
        public IEnumerator GetPlayerObjectInChildHierarchyFindsTaggedChild()
        {
            GameObject parent = new("Helpers_PlayerParent");
            _spawned.Add(parent);
            GameObject child = new("Helpers_PlayerChild");
            _spawned.Add(child);

            child.transform.SetParent(parent.transform);

            bool originalIgnore = LogAssert.ignoreFailingMessages;
            try
            {
                // Suppress possible tag-not-defined errors
                LogAssert.ignoreFailingMessages = true;

                bool playerTagSet = true;
                try
                {
                    // May throw if tag does not exist in this project
                    child.tag = "Player";
                }
                catch (UnityException)
                {
                    playerTagSet = false;
                }

                GameObject foundPlayerDirect = parent.GetPlayerObjectInChildHierarchy();
                GameObject foundPlayerTag = parent.GetTagObjectInChildHierarchy("Player");

                if (playerTagSet)
                {
                    Assert.AreSame(child, foundPlayerDirect);
                    Assert.AreSame(child, foundPlayerTag);
                }
                else
                {
                    Assert.IsNull(foundPlayerDirect);
                    Assert.IsNull(foundPlayerTag);
                }

                GameObject foundNonExistent = parent.GetTagObjectInChildHierarchy("NonExistent");
                Assert.IsNull(foundNonExistent);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = originalIgnore;
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator UpdateShapeToSpriteCopiesPhysicsShape()
        {
            Texture2D texture = new(4, 4);
            _spawned.Add(texture);

            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                1f
            );
            _spawned.Add(sprite);
            Vector2[] originalShape = { new(0f, 0f), new(0f, 4f), new(4f, 4f), new(4f, 0f) };
            sprite.OverridePhysicsShape(new List<Vector2[]> { originalShape });

            /*
                Unity stores physics shape points in the Sprite's local space (relative to pivot).
                Read back the effective physics shape from the Sprite to use as the ground truth.
             */
            List<Vector2> expectedShape = new();
            _ = sprite.GetPhysicsShape(0, expectedShape);

            GameObject go = new(
                "Helpers_UpdateShape",
                typeof(SpriteRenderer),
                typeof(PolygonCollider2D)
            );
            _spawned.Add(go);

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            PolygonCollider2D collider = go.GetComponent<PolygonCollider2D>();

            renderer.UpdateShapeToSprite();
            Assert.AreEqual(1, collider.pathCount);
            Vector2[] result = collider.GetPath(0);
            Assert.That(result, Is.EqualTo(expectedShape));
            yield break;
        }

        [Test]
        public void CrossComputesIntegerCrossProduct()
        {
            Vector3Int a = new(1, 2, 3);
            Vector3Int b = new(-4, 5, -6);
            Vector3 cross = Vector3.Cross(a, b);
            Vector3Int result = a.Cross(b);
            Assert.AreEqual(Mathf.RoundToInt(cross.x), result.x);
            Assert.AreEqual(Mathf.RoundToInt(cross.y), result.y);
            Assert.AreEqual(Mathf.RoundToInt(cross.z), result.z);
        }

        [UnityTest]
        public IEnumerator TryGetClosestParentWithComponentIncludingSelfFindsNearestMatch()
        {
            GameObject root = new("Helpers_ParentRoot");
            _spawned.Add(root);
            GameObject middle = new("Helpers_ParentMiddle", typeof(CopyProbeComponent));
            _spawned.Add(middle);
            GameObject leaf = new("Helpers_ParentLeaf");
            _spawned.Add(leaf);

            middle.transform.SetParent(root.transform);
            leaf.transform.SetParent(middle.transform);

            Assert.AreSame(
                middle,
                leaf.TryGetClosestParentWithComponentIncludingSelf<CopyProbeComponent>()
            );

            Assert.IsNull(root.TryGetClosestParentWithComponentIncludingSelf<CopyProbeComponent>());
            yield break;
        }

        [UnityTest]
        public IEnumerator NameEqualsIgnoresCloneSuffix()
        {
            GameObject original = new("Helpers_NameEquals");
            _spawned.Add(original);
            GameObject clone = Object.Instantiate(original);
            _spawned.Add(clone);

            Assert.IsTrue(Helpers.NameEquals(original, clone));
            Assert.IsTrue(Helpers.NameEquals(clone, original));

            GameObject other = new("DifferentName");
            _spawned.Add(other);

            Assert.IsFalse(Helpers.NameEquals(original, other));
            yield break;
        }

        [Test]
        public void ChangeColorBrightnessAdjustsChannels()
        {
            Color baseColor = new(0.2f, 0.4f, 0.6f, 1f);
            Color lighter = baseColor.ChangeColorBrightness(0.5f);
            Color darker = baseColor.ChangeColorBrightness(-0.5f);

            Assert.Greater(lighter.r, baseColor.r);
            Assert.Greater(lighter.g, baseColor.g);
            Assert.Greater(lighter.b, baseColor.b);

            Assert.Less(darker.r, baseColor.r);
            Assert.Less(darker.g, baseColor.g);
            Assert.Less(darker.b, baseColor.b);
            Assert.AreEqual(baseColor.a, lighter.a);
            Assert.AreEqual(baseColor.a, darker.a);
        }

        [UnityTest]
        public IEnumerator AwakeObjectInvokesAwakeOnComponents()
        {
            GameObject go = new("Helpers_Awake", typeof(AwakeProbe));
            _spawned.Add(go);

            AwakeProbe probe = go.GetComponent<AwakeProbe>();
            probe.ResetCount();
            go.AwakeObject();
            Assert.AreEqual(1, probe.InvocationCount);
            yield break;
        }

        [UnityTest]
        public IEnumerator GetAngleWithSpeedRotatesTowardsTarget()
        {
            yield return null;
            Vector2 current = Vector2.right;
            Vector2 target = Vector2.up;
            Vector2 rotated = Helpers.GetAngleWithSpeed(target, current, 180f);
            Assert.Greater(rotated.y, current.y);
            Assert.Less(rotated.x, current.x);
        }

        [Test]
        public void Extend2DUpdatesBoundsExtents()
        {
            BoundsInt bounds = new(0, 0, 0, 1, 1, 1);
            FastVector3Int position = new(2, -1, 0);
            Helpers.Extend2D(ref bounds, position);
            Assert.AreEqual(0, bounds.xMin);
            Assert.AreEqual(2, bounds.xMax);
            Assert.AreEqual(-1, bounds.yMin);
            Assert.AreEqual(1, bounds.yMax);
        }

        private static CoroutineHost CreateHost()
        {
            GameObject go = new("Helpers_CoroutineHost", typeof(CoroutineHost));
            return go.GetComponent<CoroutineHost>();
        }
    }

    internal sealed class CoroutineHost : MonoBehaviour
    {
        public int InvocationCount { get; private set; }
        public bool Flag { get; private set; }

        public void Increment()
        {
            ++InvocationCount;
        }

        public void SetFlagTrue()
        {
            Flag = true;
        }

        public void ResetState()
        {
            InvocationCount = 0;
            Flag = false;
        }
    }

    internal sealed class CopyProbeComponent : MonoBehaviour
    {
        public int PublicValue;

        [SerializeField]
        private string _serializedValue;
        public Vector3 AutomaticProperty { get; private set; }

        [SerializeField]
        private List<int> _values = new();

        public IReadOnlyList<int> Values => _values;

        public string SerializedValue => _serializedValue;

        public void Configure(int value, string serialized, Vector3 vector)
        {
            PublicValue = value;
            _serializedValue = serialized;
            AutomaticProperty = vector;
            _values.Clear();
            _values.AddRange(new[] { 1, 2, 3 });
        }
    }

    internal sealed class AwakeProbe : MonoBehaviour
    {
        public int InvocationCount { get; private set; }

        private void Awake()
        {
            ++InvocationCount;
        }

        public void ResetCount()
        {
            InvocationCount = 0;
        }
    }

    internal sealed class DeterministicRandom : AbstractRandom
    {
        private readonly Queue<double> _doubles;

        public DeterministicRandom(IEnumerable<double> doubles)
        {
            _doubles = new Queue<double>(doubles ?? Array.Empty<double>());
        }

        public override RandomState InternalState => new RandomState(0);

        public override IRandom Copy()
        {
            return new DeterministicRandom(_doubles.ToArray());
        }

        public override uint NextUint()
        {
            return (uint)(NextDouble() * uint.MaxValue);
        }

        public override double NextDouble()
        {
            return _doubles.Count > 0 ? _doubles.Dequeue() : 0d;
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentException(nameof(buffer));
            }

            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = (byte)(NextDouble() * byte.MaxValue);
            }
        }
    }
}
