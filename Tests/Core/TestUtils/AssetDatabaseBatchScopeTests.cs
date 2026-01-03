// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestUtils
{
#if UNITY_EDITOR

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEditor;

    [TestFixture]
    [Category("Editor")]
    public sealed class AssetDatabaseBatchScopeTests
    {
        [SetUp]
        public void SetUp()
        {
            AssetDatabaseBatchHelper.ResetBatchDepth();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabaseBatchHelper.ResetBatchDepth();
        }

        [Test]
        public void CounterIncrementsOnScopeEnter()
        {
            int depthBefore = AssetDatabaseBatchHelper.CurrentBatchDepth;

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                int depthDuring = AssetDatabaseBatchHelper.CurrentBatchDepth;

                Assert.That(
                    depthDuring,
                    Is.EqualTo(depthBefore + 1),
                    $"Batch depth should increment by 1 when entering scope. Before: {depthBefore}, During: {depthDuring}"
                );
            }
        }

        [Test]
        public void CounterDecrementsOnScopeExit()
        {
            int depthBefore = AssetDatabaseBatchHelper.CurrentBatchDepth;

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(depthBefore + 1),
                    "Depth should be incremented during scope"
                );
            }

            int depthAfter = AssetDatabaseBatchHelper.CurrentBatchDepth;
            Assert.That(
                depthAfter,
                Is.EqualTo(depthBefore),
                $"Batch depth should return to original value after scope exit. Before: {depthBefore}, After: {depthAfter}"
            );
        }

        [Test]
        public void IsCurrentlyBatchingReturnsTrueInsideScope()
        {
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching before entering scope"
            );

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should be batching inside scope"
                );
            }
        }

        [Test]
        public void IsCurrentlyBatchingReturnsFalseOutsideScope()
        {
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching initially"
            );

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should be batching during scope"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after scope exits"
            );
        }

        [Test]
        public void BeginBatchCreatesValidScope()
        {
            AssetDatabaseBatchScope scope = AssetDatabaseBatchHelper.BeginBatch();

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.True,
                "BeginBatch should create active batching scope"
            );

            scope.Dispose();

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Disposing scope should end batching"
            );
        }

        [Test]
        public void RefreshIfNotBatchingDoesNothingInsideBatch()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should be batching"
                );

                AssetDatabaseBatchHelper.RefreshIfNotBatching();

                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should still be batching after RefreshIfNotBatching call inside batch"
                );
            }
        }

        [Test]
        public void RefreshIfNotBatchingExecutesOutsideBatch()
        {
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching initially"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should still not be batching after RefreshIfNotBatching outside batch"
            );
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after RefreshIfNotBatching"
            );
        }

        [Test]
        public void NestedScopesTrackProperDepth()
        {
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Initial depth should be 0"
            );

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(1),
                    "First scope should set depth to 1"
                );

                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    Assert.That(
                        AssetDatabaseBatchHelper.CurrentBatchDepth,
                        Is.EqualTo(2),
                        "Second nested scope should set depth to 2"
                    );

                    using (AssetDatabaseBatchHelper.BeginBatch())
                    {
                        Assert.That(
                            AssetDatabaseBatchHelper.CurrentBatchDepth,
                            Is.EqualTo(3),
                            "Third nested scope should set depth to 3"
                        );
                    }

                    Assert.That(
                        AssetDatabaseBatchHelper.CurrentBatchDepth,
                        Is.EqualTo(2),
                        "Depth should return to 2 after third scope exits"
                    );
                }

                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(1),
                    "Depth should return to 1 after second scope exits"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should return to 0 after all scopes exit"
            );
        }

        [Test]
        public void InnerScopeDisposalDoesNotTriggerRefresh()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should be batching in outer scope"
                );

                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    Assert.That(
                        AssetDatabaseBatchHelper.IsCurrentlyBatching,
                        Is.True,
                        "Should still be batching in inner scope"
                    );
                }

                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should still be batching after inner scope exits (outer still active)"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after outer scope exits"
            );
        }

        [Test]
        public void OnlyOutermostScopeTriggersBatchingEnd()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    using (AssetDatabaseBatchHelper.BeginBatch())
                    {
                        Assert.That(
                            AssetDatabaseBatchHelper.CurrentBatchDepth,
                            Is.EqualTo(3),
                            "Should be at depth 3"
                        );
                    }

                    Assert.That(
                        AssetDatabaseBatchHelper.IsCurrentlyBatching,
                        Is.True,
                        "Should still be batching at depth 2"
                    );
                }

                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should still be batching at depth 1"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Only outermost scope ending should stop batching"
            );
        }

        [Test]
        [TestCase(true, TestName = "RefreshOnDispose.True.EndsNormally")]
        [TestCase(false, TestName = "RefreshOnDispose.False.SkipsRefresh")]
        public void RefreshOnDisposeParameterWorksCorrectly(bool refreshOnDispose)
        {
            using (AssetDatabaseBatchHelper.BeginBatch(refreshOnDispose: refreshOnDispose))
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    $"Should be batching with refreshOnDispose: {refreshOnDispose}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                $"Should not be batching after scope with refreshOnDispose: {refreshOnDispose} exits"
            );
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 regardless of refreshOnDispose setting"
            );
        }

        [Test]
        public void RefreshOnDisposeTrueIsDefault()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Default scope should enable batching"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Scope should end batching when disposed"
            );
        }

        [Test]
        [TestCase(1, TestName = "SequentialScopes.Count1")]
        [TestCase(5, TestName = "SequentialScopes.Count5")]
        [TestCase(10, TestName = "SequentialScopes.Count10")]
        [TestCase(50, TestName = "SequentialScopes.Count50")]
        public void MultipleSequentialScopesWorkCorrectly(int scopeCount)
        {
            for (int i = 0; i < scopeCount; i++)
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.False,
                    $"Should not be batching before scope {i}"
                );

                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    Assert.That(
                        AssetDatabaseBatchHelper.IsCurrentlyBatching,
                        Is.True,
                        $"Should be batching during scope {i}"
                    );
                    Assert.That(
                        AssetDatabaseBatchHelper.CurrentBatchDepth,
                        Is.EqualTo(1),
                        $"Depth should be 1 during scope {i}"
                    );
                }

                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.False,
                    $"Should not be batching after scope {i}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Final depth should be 0 after {scopeCount} sequential scopes"
            );
        }

        [Test]
        public void ScopeProperlyCleansUpOnException()
        {
            try
            {
                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    Assert.That(
                        AssetDatabaseBatchHelper.IsCurrentlyBatching,
                        Is.True,
                        "Should be batching before exception"
                    );
                    throw new InvalidOperationException("Test exception");
                }
            }
            catch (InvalidOperationException)
            {
                // Expected exception
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Scope should clean up even when exception is thrown"
            );
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Batch depth should be reset after exception"
            );
        }

        [Test]
        public void NestedScopeProperlyCleansUpOnException()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                try
                {
                    using (AssetDatabaseBatchHelper.BeginBatch())
                    {
                        Assert.That(
                            AssetDatabaseBatchHelper.CurrentBatchDepth,
                            Is.EqualTo(2),
                            "Should be at depth 2 before exception"
                        );
                        throw new InvalidOperationException("Test exception in nested scope");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Expected exception
                }

                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(1),
                    "Should return to depth 1 after nested scope exception"
                );
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Outer scope should still be active after inner scope exception"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after all scopes complete"
            );
        }

        [Test]
        [TestCaseSource(nameof(DeeplyNestedScopeCases))]
        public void DeeplyNestedScopesWorkCorrectly(int nestingLevel)
        {
            List<AssetDatabaseBatchScope> scopes = new List<AssetDatabaseBatchScope>();

            for (int i = 0; i < nestingLevel; i++)
            {
                scopes.Add(AssetDatabaseBatchHelper.BeginBatch());
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(i + 1),
                    $"Depth should be {i + 1} after entering scope {i}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(nestingLevel),
                $"Should be at depth {nestingLevel}"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.True,
                "Should be batching at maximum depth"
            );

            for (int i = nestingLevel - 1; i >= 0; i--)
            {
                scopes[i].Dispose();
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(i),
                    $"Depth should be {i} after disposing scope {i}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after all scopes disposed"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after all scopes disposed"
            );
        }

        private static IEnumerable<TestCaseData> DeeplyNestedScopeCases()
        {
            yield return new TestCaseData(10).SetName("NestedScope.Depth10.TracksCorrectly");
            yield return new TestCaseData(15).SetName("NestedScope.Depth15.TracksCorrectly");
            yield return new TestCaseData(20).SetName("NestedScope.Depth20.TracksCorrectly");
            yield return new TestCaseData(50).SetName("NestedScope.Depth50.TracksCorrectly");
            yield return new TestCaseData(100).SetName("NestedScope.Depth100.TracksCorrectly");
        }

        [Test]
        [TestCase(100, TestName = "RapidCycles.Count100")]
        [TestCase(500, TestName = "RapidCycles.Count500")]
        [TestCase(1000, TestName = "RapidCycles.Count1000")]
        public void RapidOpenCloseCyclesWorkCorrectly(int cycleCount)
        {
            for (int cycle = 0; cycle < cycleCount; cycle++)
            {
                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    Assert.That(
                        AssetDatabaseBatchHelper.IsCurrentlyBatching,
                        Is.True,
                        $"Should be batching in cycle {cycle}"
                    );
                }
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Depth should be 0 after {cycleCount} rapid cycles"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after rapid cycles"
            );
        }

        [Test]
        [TestCase(50, TestName = "RapidNestedCycles.Count50")]
        [TestCase(100, TestName = "RapidNestedCycles.Count100")]
        public void RapidNestedOpenCloseCyclesWorkCorrectly(int cycleCount)
        {
            for (int cycle = 0; cycle < cycleCount; cycle++)
            {
                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    using (AssetDatabaseBatchHelper.BeginBatch())
                    {
                        Assert.That(
                            AssetDatabaseBatchHelper.CurrentBatchDepth,
                            Is.EqualTo(2),
                            $"Should be at depth 2 in cycle {cycle}"
                        );
                    }
                }
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after rapid nested cycles"
            );
        }

        [Test]
        public void IncrementBatchDepthReturnsCorrectlyForOutermostScope()
        {
            bool isOutermost = AssetDatabaseBatchHelper.IncrementBatchDepth();

            Assert.That(
                isOutermost,
                Is.True,
                "First increment should return true (outermost scope)"
            );
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(1),
                "Depth should be 1 after first increment"
            );

            AssetDatabaseBatchHelper.DecrementBatchDepth();
        }

        [Test]
        public void IncrementBatchDepthReturnsCorrectlyForNestedScope()
        {
            AssetDatabaseBatchHelper.IncrementBatchDepth();

            bool isOutermost = AssetDatabaseBatchHelper.IncrementBatchDepth();

            Assert.That(
                isOutermost,
                Is.False,
                "Second increment should return false (nested scope)"
            );
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(2),
                "Depth should be 2 after second increment"
            );

            AssetDatabaseBatchHelper.DecrementBatchDepth();
            AssetDatabaseBatchHelper.DecrementBatchDepth();
        }

        [Test]
        [TestCase(2, TestName = "IncrementMultiple.Count2")]
        [TestCase(5, TestName = "IncrementMultiple.Count5")]
        [TestCase(10, TestName = "IncrementMultiple.Count10")]
        public void MultipleIncrementsReturnCorrectOutermostFlag(int incrementCount)
        {
            List<bool> results = new List<bool>();

            for (int i = 0; i < incrementCount; i++)
            {
                results.Add(AssetDatabaseBatchHelper.IncrementBatchDepth());
            }

            Assert.That(results[0], Is.True, "First increment should return true");

            for (int i = 1; i < incrementCount; i++)
            {
                Assert.That(
                    results[i],
                    Is.False,
                    $"Increment {i} should return false (not outermost)"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(incrementCount),
                $"Final depth should be {incrementCount}"
            );
        }

        [Test]
        public void DecrementBatchDepthReturnsCorrectlyWhenReturningToZero()
        {
            AssetDatabaseBatchHelper.IncrementBatchDepth();

            bool wasOutermost = AssetDatabaseBatchHelper.DecrementBatchDepth();

            Assert.That(
                wasOutermost,
                Is.True,
                "Decrement from 1 to 0 should return true (was outermost)"
            );
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after decrement"
            );
        }

        [Test]
        public void DecrementBatchDepthReturnsCorrectlyWhenNotReturningToZero()
        {
            AssetDatabaseBatchHelper.IncrementBatchDepth();
            AssetDatabaseBatchHelper.IncrementBatchDepth();

            bool wasOutermost = AssetDatabaseBatchHelper.DecrementBatchDepth();

            Assert.That(
                wasOutermost,
                Is.False,
                "Decrement from 2 to 1 should return false (not outermost)"
            );
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(1),
                "Depth should be 1 after decrement"
            );

            AssetDatabaseBatchHelper.DecrementBatchDepth();
        }

        [Test]
        public void DecrementBelowZeroResetsToZero()
        {
            bool wasOutermost = AssetDatabaseBatchHelper.DecrementBatchDepth();

            Assert.That(wasOutermost, Is.False, "Decrement below zero should return false");
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be reset to 0 when decremented below zero"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after decrement below zero"
            );
        }

        [Test]
        [TestCase(1, TestName = "DecrementBelowZero.Count1")]
        [TestCase(5, TestName = "DecrementBelowZero.Count5")]
        [TestCase(10, TestName = "DecrementBelowZero.Count10")]
        public void MultipleDecrementsFromZeroRemainAtZero(int decrementCount)
        {
            for (int i = 0; i < decrementCount; i++)
            {
                bool result = AssetDatabaseBatchHelper.DecrementBatchDepth();

                Assert.That(result, Is.False, $"Decrement {i} from zero should return false");
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(0),
                    $"Depth should remain 0 after decrement {i}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Final depth should be 0 after {decrementCount} decrements from zero"
            );
        }

        [Test]
        public void ResetBatchDepthResetsToZero()
        {
            AssetDatabaseBatchHelper.IncrementBatchDepth();
            AssetDatabaseBatchHelper.IncrementBatchDepth();
            AssetDatabaseBatchHelper.IncrementBatchDepth();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(3),
                "Depth should be 3 before reset"
            );

            AssetDatabaseBatchHelper.ResetBatchDepth();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after reset"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after reset"
            );
        }

        [Test]
        [TestCase(1, TestName = "ResetFromDepth.Depth1")]
        [TestCase(10, TestName = "ResetFromDepth.Depth10")]
        [TestCase(50, TestName = "ResetFromDepth.Depth50")]
        public void ResetBatchDepthWorksFromAnyDepth(int initialDepth)
        {
            for (int i = 0; i < initialDepth; i++)
            {
                AssetDatabaseBatchHelper.IncrementBatchDepth();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(initialDepth),
                $"Depth should be {initialDepth} before reset"
            );

            AssetDatabaseBatchHelper.ResetBatchDepth();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Depth should be 0 after reset from depth {initialDepth}"
            );
        }

        [Test]
        public void ResetBatchDepthWhenAlreadyZeroRemainsZero()
        {
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Initial depth should be 0"
            );

            AssetDatabaseBatchHelper.ResetBatchDepth();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after reset"
            );
        }

        [Test]
        public void CurrentBatchDepthReturnsCorrectValue()
        {
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Initial depth should be 0"
            );

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(1),
                    "Depth should be 1 in first scope"
                );

                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    Assert.That(
                        AssetDatabaseBatchHelper.CurrentBatchDepth,
                        Is.EqualTo(2),
                        "Depth should be 2 in second scope"
                    );
                }
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should return to 0"
            );
        }

        [Test]
        [TestCaseSource(nameof(MixedRefreshOnDisposeCases))]
        public void MixedRefreshOnDisposeSettingsWorkCorrectly(bool[] refreshSettings)
        {
            List<AssetDatabaseBatchScope> scopes = new List<AssetDatabaseBatchScope>();

            for (int i = 0; i < refreshSettings.Length; i++)
            {
                scopes.Add(
                    AssetDatabaseBatchHelper.BeginBatch(refreshOnDispose: refreshSettings[i])
                );
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(i + 1),
                    $"Depth should be {i + 1} after adding scope with refreshOnDispose={refreshSettings[i]}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(refreshSettings.Length),
                $"Should be at depth {refreshSettings.Length} with mixed refresh settings"
            );

            for (int i = refreshSettings.Length - 1; i >= 0; i--)
            {
                scopes[i].Dispose();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after mixed refresh settings scopes"
            );
        }

        private static IEnumerable<TestCaseData> MixedRefreshOnDisposeCases()
        {
            yield return new TestCaseData(new[] { true, false, true }).SetName(
                "MixedRefresh.TrueFalseTrue"
            );
            yield return new TestCaseData(new[] { false, true, false }).SetName(
                "MixedRefresh.FalseTrueFalse"
            );
            yield return new TestCaseData(new[] { true, true, true, false, false }).SetName(
                "MixedRefresh.ThreeTrueTwoFalse"
            );
            yield return new TestCaseData(new[] { false, false, false, true }).SetName(
                "MixedRefresh.ThreeFalseOneTrue"
            );
            yield return new TestCaseData(new[] { true, false, true, false, true, false }).SetName(
                "MixedRefresh.Alternating"
            );
        }

        [Test]
        public void DisposingSameScopeTwiceDoesNotCauseNegativeDepth()
        {
            AssetDatabaseBatchScope scope = AssetDatabaseBatchHelper.BeginBatch();

            scope.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after first dispose"
            );

            scope.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after second dispose (not negative)"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after double dispose"
            );
        }

        [Test]
        [TestCase(2, TestName = "MultipleDispose.Count2")]
        [TestCase(5, TestName = "MultipleDispose.Count5")]
        [TestCase(10, TestName = "MultipleDispose.Count10")]
        public void DisposingSameScopeMultipleTimesDoesNotCauseNegativeDepth(int disposeCount)
        {
            AssetDatabaseBatchScope scope = AssetDatabaseBatchHelper.BeginBatch();

            for (int i = 0; i < disposeCount; i++)
            {
                scope.Dispose();
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(0),
                    $"Depth should be 0 after dispose {i + 1}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                $"Should not be batching after {disposeCount} disposes"
            );
        }

        [Test]
        public void DoubleDisposeWithNestedScopesHandledCorrectly()
        {
            AssetDatabaseBatchScope outer = AssetDatabaseBatchHelper.BeginBatch();
            AssetDatabaseBatchScope inner = AssetDatabaseBatchHelper.BeginBatch();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(2),
                "Should be at depth 2"
            );

            inner.Dispose();
            inner.Dispose();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after double inner dispose (protection against negative)"
            );

            outer.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after outer dispose"
            );
        }

        [Test]
        public void ScopeCreatedViaConstructorWorksCorrectly()
        {
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Initial depth should be 0"
            );

            using (
                AssetDatabaseBatchScope scope = new AssetDatabaseBatchScope(refreshOnDispose: true)
            )
            {
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(1),
                    "Depth should be 1 when using constructor directly"
                );
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should be batching when using constructor directly"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should return to 0 after scope disposed"
            );
        }

        [Test]
        [TestCase(true, TestName = "ConstructorDirect.RefreshTrue")]
        [TestCase(false, TestName = "ConstructorDirect.RefreshFalse")]
        public void ScopeCreatedViaConstructorWithDifferentRefreshSettings(bool refreshOnDispose)
        {
            using (
                AssetDatabaseBatchScope scope = new AssetDatabaseBatchScope(
                    refreshOnDispose: refreshOnDispose
                )
            )
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    $"Should be batching with constructor refreshOnDispose={refreshOnDispose}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Depth should be 0 after constructor scope with refreshOnDispose={refreshOnDispose}"
            );
        }

        [Test]
        public void RefreshIfNotBatchingWithImportOptionsDoesNothingInsideBatch()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should be batching"
                );

                AssetDatabaseBatchHelper.RefreshIfNotBatching(ImportAssetOptions.ForceUpdate);

                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should still be batching after RefreshIfNotBatching with options"
                );
            }
        }

        [Test]
        [TestCase(ImportAssetOptions.Default, TestName = "RefreshOptions.Default")]
        [TestCase(ImportAssetOptions.ForceUpdate, TestName = "RefreshOptions.ForceUpdate")]
        [TestCase(
            ImportAssetOptions.ForceSynchronousImport,
            TestName = "RefreshOptions.ForceSynchronousImport"
        )]
        [TestCase(ImportAssetOptions.ImportRecursive, TestName = "RefreshOptions.ImportRecursive")]
        public void RefreshIfNotBatchingWithVariousImportOptionsInsideBatch(
            ImportAssetOptions options
        )
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                int depthBefore = AssetDatabaseBatchHelper.CurrentBatchDepth;

                AssetDatabaseBatchHelper.RefreshIfNotBatching(options);

                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(depthBefore),
                    $"Depth should remain unchanged after RefreshIfNotBatching with {options}"
                );
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    $"Should still be batching after RefreshIfNotBatching with {options}"
                );
            }
        }

        [Test]
        [TestCase(ImportAssetOptions.Default, TestName = "RefreshOptionsOutside.Default")]
        [TestCase(ImportAssetOptions.ForceUpdate, TestName = "RefreshOptionsOutside.ForceUpdate")]
        public void RefreshIfNotBatchingWithImportOptionsOutsideBatch(ImportAssetOptions options)
        {
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching initially"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching(options);

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Depth should remain 0 after RefreshIfNotBatching with {options} outside batch"
            );
        }

        [Test]
        [TestCaseSource(nameof(AlternatingDepthPatternCases))]
        public void AlternatingDepthPatternsWorkCorrectly(int[] depthPattern)
        {
            List<AssetDatabaseBatchScope> activeScopes = new List<AssetDatabaseBatchScope>();

            foreach (int targetDepth in depthPattern)
            {
                while (AssetDatabaseBatchHelper.CurrentBatchDepth < targetDepth)
                {
                    activeScopes.Add(AssetDatabaseBatchHelper.BeginBatch());
                }

                while (AssetDatabaseBatchHelper.CurrentBatchDepth > targetDepth)
                {
                    int lastIndex = activeScopes.Count - 1;
                    activeScopes[lastIndex].Dispose();
                    activeScopes.RemoveAt(lastIndex);
                }

                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(targetDepth),
                    $"Depth should match target depth {targetDepth}"
                );
            }

            while (activeScopes.Count > 0)
            {
                int lastIndex = activeScopes.Count - 1;
                activeScopes[lastIndex].Dispose();
                activeScopes.RemoveAt(lastIndex);
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after cleanup"
            );
        }

        private static IEnumerable<TestCaseData> AlternatingDepthPatternCases()
        {
            yield return new TestCaseData(new[] { 1, 2, 1, 3, 2, 4, 0 }).SetName(
                "AlternatingDepth.UpDownPattern.TracksCorrectly"
            );
            yield return new TestCaseData(new[] { 5, 3, 4, 2, 1, 0 }).SetName(
                "AlternatingDepth.DecreasingWithBumps.TracksCorrectly"
            );
            yield return new TestCaseData(new[] { 1, 1, 1, 2, 2, 0 }).SetName(
                "AlternatingDepth.PlateausPattern.TracksCorrectly"
            );
            yield return new TestCaseData(new[] { 0, 5, 0, 10, 0 }).SetName(
                "AlternatingDepth.SpikesPattern.TracksCorrectly"
            );
            yield return new TestCaseData(new[] { 1, 2, 3, 4, 5, 4, 3, 2, 1, 0 }).SetName(
                "AlternatingDepth.PyramidPattern.TracksCorrectly"
            );
        }

        [Test]
        public void IsCurrentlyBatchingReturnsCorrectValueInBatch()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                bool result1 = AssetDatabaseBatchHelper.IsCurrentlyBatching;
                bool result2 = AssetDatabaseBatchHelper.IsCurrentlyBatching;
                bool result3 = AssetDatabaseBatchHelper.IsCurrentlyBatching;

                Assert.That(result1, Is.True, "First read should return true");
                Assert.That(result2, Is.True, "Second read should return true");
                Assert.That(result3, Is.True, "Third read should return true");
            }
        }

        [Test]
        public void CurrentBatchDepthReturnsCorrectValueInBatch()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                int depth1 = AssetDatabaseBatchHelper.CurrentBatchDepth;
                int depth2 = AssetDatabaseBatchHelper.CurrentBatchDepth;
                int depth3 = AssetDatabaseBatchHelper.CurrentBatchDepth;

                Assert.That(depth1, Is.EqualTo(1), "First depth read should be 1");
                Assert.That(depth2, Is.EqualTo(1), "Second depth read should be 1");
                Assert.That(depth3, Is.EqualTo(1), "Third depth read should be 1");
            }
        }

        [Test]
        [TestCase(4, 100, TestName = "ConcurrentAccess.4Threads100Iterations")]
        [TestCase(8, 50, TestName = "ConcurrentAccess.8Threads50Iterations")]
        [TestCase(16, 25, TestName = "ConcurrentAccess.16Threads25Iterations")]
        public void ConcurrentIncrementDecrementMaintainsConsistency(
            int threadCount,
            int iterationsPerThread
        )
        {
            int totalOperations = threadCount * iterationsPerThread;
            CountdownEvent startSignal = new CountdownEvent(1);
            CountdownEvent completionSignal = new CountdownEvent(threadCount);
            Exception[] exceptions = new Exception[threadCount];

            Thread[] threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        startSignal.Wait();

                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            AssetDatabaseBatchHelper.IncrementBatchDepth();
                            Thread.SpinWait(10);
                            AssetDatabaseBatchHelper.DecrementBatchDepth();
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions[threadIndex] = ex;
                    }
                    finally
                    {
                        completionSignal.Signal();
                    }
                });
                threads[t].Start();
            }

            startSignal.Signal();

            bool completed = completionSignal.Wait(TimeSpan.FromSeconds(30));
            Assert.That(completed, Is.True, "All threads should complete within timeout");

            foreach (Exception ex in exceptions)
            {
                Assert.That(ex, Is.Null, $"No thread should throw exception: {ex?.Message}");
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Final depth should be 0 after {totalOperations} balanced increment/decrement operations across {threadCount} threads"
            );
        }

        [Test]
        [TestCase(4, 50, TestName = "ConcurrentScopes.4Threads50Iterations")]
        [TestCase(8, 25, TestName = "ConcurrentScopes.8Threads25Iterations")]
        public void ConcurrentScopeUsageMaintainsConsistency(
            int threadCount,
            int iterationsPerThread
        )
        {
            CountdownEvent startSignal = new CountdownEvent(1);
            CountdownEvent completionSignal = new CountdownEvent(threadCount);
            Exception[] exceptions = new Exception[threadCount];

            Thread[] threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        startSignal.Wait();

                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            using (AssetDatabaseBatchHelper.BeginBatch())
                            {
                                Thread.SpinWait(5);
                                using (AssetDatabaseBatchHelper.BeginBatch())
                                {
                                    Thread.SpinWait(5);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions[threadIndex] = ex;
                    }
                    finally
                    {
                        completionSignal.Signal();
                    }
                });
                threads[t].Start();
            }

            startSignal.Signal();

            bool completed = completionSignal.Wait(TimeSpan.FromSeconds(30));
            Assert.That(completed, Is.True, "All threads should complete within timeout");

            foreach (Exception ex in exceptions)
            {
                Assert.That(ex, Is.Null, $"No thread should throw exception: {ex?.Message}");
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Final depth should be 0 after concurrent scope usage across {threadCount} threads"
            );
        }

        [Test]
        public void ConcurrentReadsDuringBatchingAreConsistent()
        {
            const int readCount = 1000;
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                bool[] readResults = new bool[readCount];
                int[] depthResults = new int[readCount];

                Parallel.For(
                    0,
                    readCount,
                    i =>
                    {
                        readResults[i] = AssetDatabaseBatchHelper.IsCurrentlyBatching;
                        depthResults[i] = AssetDatabaseBatchHelper.CurrentBatchDepth;
                    }
                );

                for (int i = 0; i < readCount; i++)
                {
                    Assert.That(
                        readResults[i],
                        Is.True,
                        $"Read {i}: IsCurrentlyBatching should be true"
                    );
                    Assert.That(
                        depthResults[i],
                        Is.GreaterThanOrEqualTo(1),
                        $"Read {i}: CurrentBatchDepth should be >= 1"
                    );
                }
            }
        }

        [Test]
        public void ScopeStructIsReadonly()
        {
            AssetDatabaseBatchScope scope = AssetDatabaseBatchHelper.BeginBatch();

            Assert.That(
                typeof(AssetDatabaseBatchScope).IsValueType,
                Is.True,
                "AssetDatabaseBatchScope should be a value type (struct)"
            );

            scope.Dispose();
        }

        [Test]
        public void ScopeImplementsIDisposable()
        {
            Assert.That(
                typeof(IDisposable).IsAssignableFrom(typeof(AssetDatabaseBatchScope)),
                Is.True,
                "AssetDatabaseBatchScope should implement IDisposable"
            );
        }

        [Test]
        public void ScopeIsReadonlyStruct()
        {
            Type scopeType = typeof(AssetDatabaseBatchScope);

            Assert.That(scopeType.IsValueType, Is.True, "Should be a struct");

            System.Reflection.FieldInfo[] fields = scopeType.GetFields(
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public
            );

            foreach (System.Reflection.FieldInfo field in fields)
            {
                Assert.That(field.IsInitOnly, Is.True, $"Field '{field.Name}' should be readonly");
            }
        }

        [Test]
        public void BeginBatchDefaultParameterIsTrue()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Default parameter should enable batching"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Batching should end after scope with default parameter"
            );
        }

        [Test]
        [TestCase(1000, TestName = "ExtremeRapidCycles.Count1000")]
        [TestCase(5000, TestName = "ExtremeRapidCycles.Count5000")]
        public void ExtremelyRapidOpenCloseCyclesDoNotBreakState(int cycleCount)
        {
            for (int i = 0; i < cycleCount; i++)
            {
                AssetDatabaseBatchScope scope = AssetDatabaseBatchHelper.BeginBatch();
                scope.Dispose();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                $"Depth should be 0 after {cycleCount} rapid cycles"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after extreme rapid cycles"
            );
        }

        [Test]
        [TestCase(25, TestName = "ManyNestedMixed.Depth25")]
        [TestCase(50, TestName = "ManyNestedMixed.Depth50")]
        public void ManyNestedScopesWithMixedRefreshSettings(int depth)
        {
            List<AssetDatabaseBatchScope> scopes = new List<AssetDatabaseBatchScope>();

            for (int i = 0; i < depth; i++)
            {
                bool shouldRefresh = i % 2 == 0;
                scopes.Add(AssetDatabaseBatchHelper.BeginBatch(refreshOnDispose: shouldRefresh));
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(depth),
                $"Should be at depth {depth}"
            );

            for (int i = depth - 1; i >= 0; i--)
            {
                scopes[i].Dispose();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after all scopes disposed"
            );
        }

        [Test]
        public void OutOfOrderDisposalHandledGracefully()
        {
            AssetDatabaseBatchScope scope1 = AssetDatabaseBatchHelper.BeginBatch();
            AssetDatabaseBatchScope scope2 = AssetDatabaseBatchHelper.BeginBatch();
            AssetDatabaseBatchScope scope3 = AssetDatabaseBatchHelper.BeginBatch();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(3),
                "Should be at depth 3"
            );

            scope1.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(2),
                "Depth should be 2 after disposing first scope (out of order)"
            );

            scope3.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(1),
                "Depth should be 1 after disposing third scope"
            );

            scope2.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after disposing second scope"
            );
        }

        [Test]
        [TestCaseSource(nameof(OutOfOrderDisposalPatternCases))]
        public void VariousOutOfOrderDisposalPatternsHandledGracefully(int[] disposalOrder)
        {
            int scopeCount = disposalOrder.Length;
            AssetDatabaseBatchScope[] scopes = new AssetDatabaseBatchScope[scopeCount];

            for (int i = 0; i < scopeCount; i++)
            {
                scopes[i] = AssetDatabaseBatchHelper.BeginBatch();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(scopeCount),
                $"Should be at depth {scopeCount} before disposal"
            );

            for (int i = 0; i < disposalOrder.Length; i++)
            {
                int scopeIndex = disposalOrder[i];
                scopes[scopeIndex].Dispose();

                int expectedDepth = scopeCount - i - 1;
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(expectedDepth),
                    $"Depth should be {expectedDepth} after disposing scope {scopeIndex}"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Final depth should be 0"
            );
        }

        private static IEnumerable<TestCaseData> OutOfOrderDisposalPatternCases()
        {
            yield return new TestCaseData(new[] { 0, 1, 2 }).SetName("OutOfOrder.FirstToLast");
            yield return new TestCaseData(new[] { 2, 1, 0 }).SetName("OutOfOrder.LastToFirst");
            yield return new TestCaseData(new[] { 1, 0, 2 }).SetName("OutOfOrder.MiddleFirstLast");
            yield return new TestCaseData(new[] { 1, 2, 0 }).SetName("OutOfOrder.MiddleLastFirst");
            yield return new TestCaseData(new[] { 0, 2, 1 }).SetName("OutOfOrder.FirstLastMiddle");
            yield return new TestCaseData(new[] { 2, 0, 1 }).SetName("OutOfOrder.LastFirstMiddle");
            yield return new TestCaseData(new[] { 0, 1, 2, 3, 4 }).SetName(
                "OutOfOrder.FiveInOrder"
            );
            yield return new TestCaseData(new[] { 4, 3, 2, 1, 0 }).SetName(
                "OutOfOrder.FiveReverse"
            );
            yield return new TestCaseData(new[] { 2, 0, 4, 1, 3 }).SetName(
                "OutOfOrder.FiveScattered"
            );
        }

        [Test]
        public void MixedDoubleDisposalAndNormalDisposal()
        {
            AssetDatabaseBatchScope scope1 = AssetDatabaseBatchHelper.BeginBatch();
            AssetDatabaseBatchScope scope2 = AssetDatabaseBatchHelper.BeginBatch();
            AssetDatabaseBatchScope scope3 = AssetDatabaseBatchHelper.BeginBatch();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(3),
                "Should start at depth 3"
            );

            scope2.Dispose();
            scope2.Dispose();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(1),
                "Depth should be 1 after double-disposing scope2"
            );

            scope1.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after disposing scope1"
            );

            scope3.Dispose();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after disposing scope3 (already below zero protection)"
            );
        }

        [Test]
        public void ExceptionInMiddleOfNestedScopesHandledCorrectly()
        {
            AssetDatabaseBatchScope outer = AssetDatabaseBatchHelper.BeginBatch();

            try
            {
                using (AssetDatabaseBatchHelper.BeginBatch())
                {
                    using (AssetDatabaseBatchHelper.BeginBatch())
                    {
                        Assert.That(
                            AssetDatabaseBatchHelper.CurrentBatchDepth,
                            Is.EqualTo(3),
                            "Should be at depth 3"
                        );
                        throw new InvalidOperationException("Exception in middle scope");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(1),
                "Depth should be 1 (outer scope still active)"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.True,
                "Should still be batching in outer scope"
            );

            outer.Dispose();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after outer scope disposed"
            );
        }

        [Test]
        [TestCaseSource(nameof(MultipleExceptionScenarioCases))]
        public void MultipleExceptionsInNestedScopesHandledCorrectly(
            int throwAtDepth,
            int totalDepth
        )
        {
            List<AssetDatabaseBatchScope> scopes = new List<AssetDatabaseBatchScope>();

            for (int i = 0; i < totalDepth - throwAtDepth; i++)
            {
                scopes.Add(AssetDatabaseBatchHelper.BeginBatch());
            }

            try
            {
                for (int i = 0; i < throwAtDepth; i++)
                {
                    using (AssetDatabaseBatchHelper.BeginBatch())
                    {
                        if (i == throwAtDepth - 1)
                        {
                            throw new InvalidOperationException($"Exception at depth {totalDepth}");
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            int expectedDepth = totalDepth - throwAtDepth;
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(expectedDepth),
                $"Depth should be {expectedDepth} after exception at depth {totalDepth}"
            );

            foreach (AssetDatabaseBatchScope scope in scopes)
            {
                scope.Dispose();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after cleanup"
            );
        }

        private static IEnumerable<TestCaseData> MultipleExceptionScenarioCases()
        {
            yield return new TestCaseData(1, 3).SetName("Exception.AtDepth3.BaseDepth2");
            yield return new TestCaseData(2, 4).SetName("Exception.AtDepth4.BaseDepth2");
            yield return new TestCaseData(3, 5).SetName("Exception.AtDepth5.BaseDepth2");
            yield return new TestCaseData(1, 1).SetName("Exception.AtDepth1.BaseDepth0");
            yield return new TestCaseData(5, 10).SetName("Exception.AtDepth10.BaseDepth5");
        }

        [Test]
        public void ScopeCreationAfterResetWorksCorrectly()
        {
            AssetDatabaseBatchHelper.IncrementBatchDepth();
            AssetDatabaseBatchHelper.IncrementBatchDepth();
            AssetDatabaseBatchHelper.IncrementBatchDepth();

            AssetDatabaseBatchHelper.ResetBatchDepth();

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(1),
                    "Depth should be 1 after reset and new scope"
                );
                Assert.That(
                    AssetDatabaseBatchHelper.IsCurrentlyBatching,
                    Is.True,
                    "Should be batching after reset and new scope"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should return to 0 after scope disposal"
            );
        }

        [Test]
        public void InterleaveIncrementDecrementWithScopes()
        {
            AssetDatabaseBatchHelper.IncrementBatchDepth();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(1),
                "Depth should be 1 after increment"
            );

            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(2),
                    "Depth should be 2 after scope"
                );

                AssetDatabaseBatchHelper.IncrementBatchDepth();
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(3),
                    "Depth should be 3 after nested increment"
                );

                AssetDatabaseBatchHelper.DecrementBatchDepth();
                Assert.That(
                    AssetDatabaseBatchHelper.CurrentBatchDepth,
                    Is.EqualTo(2),
                    "Depth should be 2 after nested decrement"
                );
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(1),
                "Depth should be 1 after scope exits"
            );

            AssetDatabaseBatchHelper.DecrementBatchDepth();
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 after final decrement"
            );
        }

        [Test]
        public void BatchHelperStaticClassHasExpectedMembers()
        {
            Type helperType = typeof(AssetDatabaseBatchHelper);

            System.Reflection.PropertyInfo isCurrentlyBatching = helperType.GetProperty(
                nameof(AssetDatabaseBatchHelper.IsCurrentlyBatching)
            );
            Assert.That(
                isCurrentlyBatching,
                Is.Not.Null,
                "IsCurrentlyBatching property should exist"
            );
            Assert.That(
                isCurrentlyBatching.PropertyType,
                Is.EqualTo(typeof(bool)),
                "IsCurrentlyBatching should return bool"
            );

            System.Reflection.PropertyInfo currentBatchDepth = helperType.GetProperty(
                nameof(AssetDatabaseBatchHelper.CurrentBatchDepth)
            );
            Assert.That(currentBatchDepth, Is.Not.Null, "CurrentBatchDepth property should exist");
            Assert.That(
                currentBatchDepth.PropertyType,
                Is.EqualTo(typeof(int)),
                "CurrentBatchDepth should return int"
            );

            System.Reflection.MethodInfo beginBatch = helperType.GetMethod(
                nameof(AssetDatabaseBatchHelper.BeginBatch)
            );
            Assert.That(beginBatch, Is.Not.Null, "BeginBatch method should exist");
            Assert.That(
                beginBatch.ReturnType,
                Is.EqualTo(typeof(AssetDatabaseBatchScope)),
                "BeginBatch should return AssetDatabaseBatchScope"
            );
        }

        [Test]
        public void ZeroDepthStateIsConsistent()
        {
            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Initial depth should be 0"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching at depth 0"
            );

            AssetDatabaseBatchHelper.ResetBatchDepth();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should still be 0 after reset"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should still not be batching after reset"
            );
        }

        [Test]
        public void HighDepthValuesWorkCorrectly()
        {
            const int highDepth = 1000;

            for (int i = 0; i < highDepth; i++)
            {
                AssetDatabaseBatchHelper.IncrementBatchDepth();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(highDepth),
                $"Depth should be {highDepth}"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.True,
                "Should be batching at high depth"
            );

            for (int i = 0; i < highDepth; i++)
            {
                AssetDatabaseBatchHelper.DecrementBatchDepth();
            }

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should return to 0"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching at depth 0"
            );
        }

        [Test]
        public void ScopeDefaultValueIsNotUsable()
        {
            AssetDatabaseBatchScope defaultScope = default;

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be 0 before default scope dispose"
            );

            defaultScope.Dispose();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after default scope dispose (protected against negative)"
            );
        }

        [Test]
        public void RefreshIfNotBatchingDoesNotAffectBatchState()
        {
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after RefreshIfNotBatching"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after RefreshIfNotBatching"
            );
        }

        [Test]
        public void RefreshIfNotBatchingWithOptionsDoesNotAffectBatchState()
        {
            AssetDatabaseBatchHelper.RefreshIfNotBatching(ImportAssetOptions.ForceUpdate);

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should remain 0 after RefreshIfNotBatching with options"
            );
            Assert.That(
                AssetDatabaseBatchHelper.IsCurrentlyBatching,
                Is.False,
                "Should not be batching after RefreshIfNotBatching with options"
            );
        }

        [Test]
        public void DisposeCalledOnScopeWithoutIncrementingHandledGracefully()
        {
            int depthBefore = AssetDatabaseBatchHelper.CurrentBatchDepth;

            AssetDatabaseBatchHelper.DecrementBatchDepth();

            Assert.That(
                AssetDatabaseBatchHelper.CurrentBatchDepth,
                Is.EqualTo(0),
                "Depth should be clamped to 0, not negative"
            );
        }

        [Test]
        public void IncrementAndDecrementReturnValuesAreSymmetric()
        {
            bool firstIncrement = AssetDatabaseBatchHelper.IncrementBatchDepth();
            Assert.That(firstIncrement, Is.True, "First increment returns true (outermost)");

            bool secondIncrement = AssetDatabaseBatchHelper.IncrementBatchDepth();
            Assert.That(secondIncrement, Is.False, "Second increment returns false (nested)");

            bool firstDecrement = AssetDatabaseBatchHelper.DecrementBatchDepth();
            Assert.That(firstDecrement, Is.False, "First decrement returns false (still nested)");

            bool secondDecrement = AssetDatabaseBatchHelper.DecrementBatchDepth();
            Assert.That(secondDecrement, Is.True, "Second decrement returns true (now at zero)");
        }

        [Test]
        public void BatchingStateTransitionsAreAtomic()
        {
            bool previouslyBatching = AssetDatabaseBatchHelper.IsCurrentlyBatching;
            Assert.That(previouslyBatching, Is.False, "Should not be batching initially");

            AssetDatabaseBatchHelper.IncrementBatchDepth();

            bool nowBatching = AssetDatabaseBatchHelper.IsCurrentlyBatching;
            Assert.That(nowBatching, Is.True, "Should be batching after increment");

            int depth = AssetDatabaseBatchHelper.CurrentBatchDepth;
            Assert.That(depth, Is.EqualTo(1), "Depth should be 1");

            AssetDatabaseBatchHelper.DecrementBatchDepth();

            bool afterDecrement = AssetDatabaseBatchHelper.IsCurrentlyBatching;
            Assert.That(afterDecrement, Is.False, "Should not be batching after decrement");
        }
    }

#endif
}
