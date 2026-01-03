// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    /// <summary>
    /// Comprehensive tests for WButton conflict warning display functionality.
    /// Verifies warning generation, content accuracy, caching behavior,
    /// and handling of all conflict types (placement, priority, draw order).
    /// </summary>
    [TestFixture]
    public sealed class WButtonConflictWarningDisplayTests : CommonTestBase
    {
        [SetUp]
        public void SetUp()
        {
            ClearAllCaches();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            ClearAllCaches();
        }

        private static void ClearAllCaches()
        {
            WButtonGUI.ClearGroupDataForTesting();
            WButtonGUI.ClearConflictingDrawOrderWarningsForTesting();
            WButtonGUI.ClearConflictingGroupPriorityWarningsForTesting();
            WButtonGUI.ClearConflictingGroupPlacementWarningsForTesting();
            WButtonGUI.ClearConflictWarningContentCacheForTesting();
            WButtonGUI.ClearContextCache();
        }

        private static void DrawButtonsWithDefaults(
            Editor editor,
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates,
            Dictionary<WButtonGroupKey, bool> foldoutStates
        )
        {
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen,
                triggeredContexts: null,
                globalPlacementIsTop: true
            );
        }

        private T CreateAssetAndEditor<T>(out Editor editor)
            where T : ScriptableObject
        {
            T asset = Track(ScriptableObject.CreateInstance<T>());
            editor = Track(Editor.CreateEditor(asset));
            return asset;
        }

        private ScriptableObject CreateAssetAndEditor(Type targetType, out Editor editor)
        {
            ScriptableObject asset = Track(
                ScriptableObject.CreateInstance(targetType) as ScriptableObject
            );
            editor = Track(Editor.CreateEditor(asset));
            return asset;
        }

        private static IReadOnlyDictionary<
            string,
            WButtonGUI.GroupPlacementConflictInfo
        > GetPlacementWarnings()
        {
            return WButtonGUI.GetConflictingGroupPlacementWarnings();
        }

        private static IReadOnlyDictionary<
            string,
            WButtonGUI.GroupPriorityConflictInfo
        > GetPriorityWarnings()
        {
            return WButtonGUI.GetConflictingGroupPriorityWarnings();
        }

        private static IReadOnlyDictionary<
            string,
            WButtonGUI.DrawOrderConflictInfo
        > GetDrawOrderWarnings()
        {
            return WButtonGUI.GetConflictingDrawOrderWarnings();
        }

        [Test]
        [TestCaseSource(nameof(PlacementConflictDetectionCases))]
        public void PlacementConflictWarningHandlesAllScenarios(
            Type targetType,
            string groupName,
            bool shouldHaveConflict,
            int expectedConflictCount
        )
        {
            CreateAssetAndEditor(targetType, out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.GroupPlacementConflictInfo> warnings =
                GetPlacementWarnings();

            Assert.AreEqual(
                shouldHaveConflict,
                warnings.ContainsKey(groupName),
                shouldHaveConflict
                    ? $"Expected conflict warning for group '{groupName}'"
                    : $"Expected no conflict warning for group '{groupName}'"
            );

            if (shouldHaveConflict && expectedConflictCount > 0)
            {
                Assert.AreEqual(
                    expectedConflictCount,
                    warnings[groupName]._allGroupPlacements.Count,
                    $"Expected {expectedConflictCount} conflicting placement values"
                );
            }
        }

        private static IEnumerable<TestCaseData> PlacementConflictDetectionCases()
        {
            yield return new TestCaseData(
                typeof(WButtonGroupPlacementConflictTarget),
                "ConflictGroup",
                true,
                2
            ).SetName("Placement.ConflictingValues.GeneratesWarning");

            yield return new TestCaseData(
                typeof(WButtonGroupPlacementTopTarget),
                "TopGroup",
                false,
                0
            ).SetName("Placement.ConsistentTop.NoWarning");

            yield return new TestCaseData(
                typeof(WButtonMixedExplicitAndDefaultPlacementTarget),
                "Debug Tools",
                false,
                0
            ).SetName("Placement.MixedExplicitAndDefault.NoWarning");

            yield return new TestCaseData(
                typeof(WButtonAllDefaultPlacementTarget),
                "DefaultGroup",
                false,
                0
            ).SetName("Placement.AllDefaults.NoWarning");

            yield return new TestCaseData(
                typeof(WButtonAllSameExplicitPlacementTarget),
                "SameGroup",
                false,
                0
            ).SetName("Placement.AllSameExplicit.NoWarning");
        }

        [Test]
        [TestCaseSource(nameof(PlacementConflictContentCases))]
        public void PlacementConflictWarningContainsExpectedContent(
            WButtonGroupPlacement expectedCanonicalPlacement,
            WButtonGroupPlacement[] expectedConflictingPlacements
        )
        {
            CreateAssetAndEditor<WButtonGroupPlacementConflictTarget>(out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.GroupPlacementConflictInfo> warnings =
                GetPlacementWarnings();
            WButtonGUI.GroupPlacementConflictInfo conflict = warnings["ConflictGroup"];

            Assert.AreEqual(
                expectedCanonicalPlacement,
                conflict._canonicalGroupPlacement,
                "Canonical placement should match first declared button"
            );

            for (int i = 0; i < expectedConflictingPlacements.Length; i++)
            {
                WButtonGroupPlacement expected = expectedConflictingPlacements[i];
                Assert.IsTrue(
                    conflict._allGroupPlacements.Contains(expected),
                    $"Should include {expected} placement"
                );
            }

            Assert.IsFalse(
                conflict._allGroupPlacements.Contains(WButtonGroupPlacement.UseGlobalSetting),
                "Should exclude default UseGlobalSetting value"
            );
        }

        private static IEnumerable<TestCaseData> PlacementConflictContentCases()
        {
            yield return new TestCaseData(
                WButtonGroupPlacement.Top,
                new[] { WButtonGroupPlacement.Top, WButtonGroupPlacement.Bottom }
            ).SetName("Content.ConflictGroup.ContainsTopAndBottom");
        }

        [Test]
        [TestCaseSource(nameof(PriorityConflictDetectionCases))]
        public void PriorityConflictWarningHandlesAllScenarios(
            Type targetType,
            string groupName,
            bool shouldHaveConflict,
            int expectedConflictCount
        )
        {
            CreateAssetAndEditor(targetType, out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.GroupPriorityConflictInfo> warnings =
                GetPriorityWarnings();

            Assert.AreEqual(
                shouldHaveConflict,
                warnings.ContainsKey(groupName),
                shouldHaveConflict
                    ? $"Expected conflict warning for group '{groupName}'"
                    : $"Expected no conflict warning for group '{groupName}'"
            );

            if (shouldHaveConflict && expectedConflictCount > 0)
            {
                Assert.AreEqual(
                    expectedConflictCount,
                    warnings[groupName]._allGroupPriorities.Count,
                    $"Expected {expectedConflictCount} conflicting priority values"
                );
            }
        }

        private static IEnumerable<TestCaseData> PriorityConflictDetectionCases()
        {
            yield return new TestCaseData(
                typeof(WButtonGroupPriorityConflictTarget),
                "ConflictGroup",
                true,
                2
            ).SetName("Priority.ConflictingValues.GeneratesWarning");

            yield return new TestCaseData(
                typeof(WButtonAllSameExplicitPriorityTarget),
                "SameGroup",
                false,
                0
            ).SetName("Priority.AllSameExplicit.NoWarning");

            yield return new TestCaseData(
                typeof(WButtonMixedExplicitAndDefaultPriorityTarget),
                "Setup",
                false,
                0
            ).SetName("Priority.MixedExplicitAndDefault.NoWarning");

            yield return new TestCaseData(
                typeof(WButtonAllDefaultPriorityTarget),
                "DefaultGroup",
                false,
                0
            ).SetName("Priority.AllDefaults.NoWarning");
        }

        [Test]
        [TestCaseSource(nameof(PriorityConflictContentCases))]
        public void PriorityConflictWarningContainsExpectedContent(
            int expectedCanonicalPriority,
            int[] expectedConflictingPriorities
        )
        {
            CreateAssetAndEditor<WButtonGroupPriorityConflictTarget>(out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.GroupPriorityConflictInfo> warnings =
                GetPriorityWarnings();
            WButtonGUI.GroupPriorityConflictInfo conflict = warnings["ConflictGroup"];

            Assert.AreEqual(
                expectedCanonicalPriority,
                conflict._canonicalGroupPriority,
                "Canonical priority should match first declared button"
            );

            for (int i = 0; i < expectedConflictingPriorities.Length; i++)
            {
                int expected = expectedConflictingPriorities[i];
                Assert.IsTrue(
                    conflict._allGroupPriorities.Contains(expected),
                    $"Should include priority {expected}"
                );
            }

            Assert.IsFalse(
                conflict._allGroupPriorities.Contains(WButtonAttribute.NoGroupPriority),
                "Should exclude default NoGroupPriority value"
            );
        }

        private static IEnumerable<TestCaseData> PriorityConflictContentCases()
        {
            yield return new TestCaseData(0, new[] { 0, 10 }).SetName(
                "Content.ConflictGroup.ContainsZeroAndTen"
            );
        }

        [Test]
        [TestCaseSource(nameof(DrawOrderConflictDetectionCases))]
        public void DrawOrderConflictWarningHandlesAllScenarios(
            Type targetType,
            string groupName,
            bool shouldHaveConflict,
            int minExpectedConflictCount
        )
        {
            CreateAssetAndEditor(targetType, out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                GetDrawOrderWarnings();

            Assert.AreEqual(
                shouldHaveConflict,
                warnings.ContainsKey(groupName),
                shouldHaveConflict
                    ? $"Expected conflict warning for group '{groupName}'"
                    : $"Expected no conflict warning for group '{groupName}'"
            );

            if (shouldHaveConflict && minExpectedConflictCount > 0)
            {
                Assert.IsTrue(
                    warnings[groupName]._allDrawOrders.Count >= minExpectedConflictCount,
                    $"Expected at least {minExpectedConflictCount} conflicting draw order values"
                );
            }
        }

        private static IEnumerable<TestCaseData> DrawOrderConflictDetectionCases()
        {
            yield return new TestCaseData(
                typeof(WButtonConflictingDrawOrderTarget),
                "ConflictGroup",
                true,
                2
            ).SetName("DrawOrder.ConflictingValues.GeneratesWarning");

            yield return new TestCaseData(
                typeof(WButtonGroupPlacementTopTarget),
                "TopGroup",
                false,
                0
            ).SetName("DrawOrder.ConsistentValues.NoWarning");

            yield return new TestCaseData(
                typeof(WButtonThreeWayConflictTarget),
                "Actions",
                true,
                3
            ).SetName("DrawOrder.ThreeWayConflict.GeneratesWarning");
        }

        [Test]
        public void DrawOrderConflictWarningIndicatesCanonicalValueIsIncluded()
        {
            CreateAssetAndEditor<WButtonConflictingDrawOrderTarget>(out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                GetDrawOrderWarnings();
            WButtonGUI.DrawOrderConflictInfo conflict = warnings["ConflictGroup"];

            Assert.IsTrue(
                conflict._allDrawOrders.Contains(conflict._canonicalDrawOrder),
                "Canonical draw order should be one of the conflicting values"
            );
        }

        [Test]
        [TestCaseSource(nameof(CachingBehaviorCases))]
        public void ConflictWarningCachingBehavesCorrectly(bool shouldClearCache)
        {
            CreateAssetAndEditor<WButtonGroupPlacementConflictTarget>(out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.GroupPlacementConflictInfo> firstWarnings =
                GetPlacementWarnings();

            if (shouldClearCache)
            {
                WButtonGUI.ClearConflictingGroupPlacementWarningsForTesting();
                WButtonGUI.ClearConflictWarningContentCacheForTesting();
            }

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            IReadOnlyDictionary<string, WButtonGUI.GroupPlacementConflictInfo> secondWarnings =
                GetPlacementWarnings();

            if (shouldClearCache)
            {
                Assert.AreEqual(
                    0,
                    secondWarnings.Count,
                    "Warnings should be cleared after cache clear"
                );
            }
            else
            {
                Assert.IsTrue(
                    firstWarnings.ContainsKey("ConflictGroup"),
                    "First draw should generate warnings"
                );
                Assert.IsTrue(
                    secondWarnings.ContainsKey("ConflictGroup"),
                    "Second draw should use cached warnings"
                );
            }
        }

        private static IEnumerable<TestCaseData> CachingBehaviorCases()
        {
            yield return new TestCaseData(false).SetName("Caching.NoClear.WarningsPersist");
            yield return new TestCaseData(true).SetName("Caching.WithClear.WarningsCleared");
        }

        [Test]
        public void MultipleConflictTypesGenerateIndependentWarnings()
        {
            CreateAssetAndEditor<WButtonThreeWayConflictTarget>(out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);

            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> drawOrderWarnings =
                GetDrawOrderWarnings();
            IReadOnlyDictionary<string, WButtonGUI.GroupPriorityConflictInfo> priorityWarnings =
                GetPriorityWarnings();
            IReadOnlyDictionary<string, WButtonGUI.GroupPlacementConflictInfo> placementWarnings =
                GetPlacementWarnings();

            bool hasDrawOrderConflict = drawOrderWarnings.ContainsKey("Actions");
            bool hasPriorityConflict = priorityWarnings.ContainsKey("Actions");
            bool hasPlacementConflict = placementWarnings.ContainsKey("Actions");

            int conflictCount =
                (hasDrawOrderConflict ? 1 : 0)
                + (hasPriorityConflict ? 1 : 0)
                + (hasPlacementConflict ? 1 : 0);

            Assert.IsTrue(
                conflictCount >= 1,
                "Should have at least one type of conflict for ThreeWayConflictTarget"
            );
        }

        [Test]
        [TestCaseSource(nameof(EdgeCaseTargetsCases))]
        public void EdgeCaseTargetsGenerateNoConflictWarnings(Type targetType)
        {
            CreateAssetAndEditor(targetType, out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);

            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> drawOrderWarnings =
                GetDrawOrderWarnings();
            IReadOnlyDictionary<string, WButtonGUI.GroupPriorityConflictInfo> priorityWarnings =
                GetPriorityWarnings();
            IReadOnlyDictionary<string, WButtonGUI.GroupPlacementConflictInfo> placementWarnings =
                GetPlacementWarnings();

            Assert.AreEqual(0, drawOrderWarnings.Count, "No draw order warnings expected");
            Assert.AreEqual(0, priorityWarnings.Count, "No priority warnings expected");
            Assert.AreEqual(0, placementWarnings.Count, "No placement warnings expected");
        }

        private static IEnumerable<TestCaseData> EdgeCaseTargetsCases()
        {
            yield return new TestCaseData(typeof(WButtonSingleButtonTarget)).SetName(
                "EdgeCase.SingleButton.NoWarnings"
            );
            yield return new TestCaseData(typeof(WButtonUngroupedPlacementTarget)).SetName(
                "EdgeCase.UngroupedButtons.NoWarnings"
            );
            yield return new TestCaseData(
                typeof(WButtonMixedExplicitAndDefaultPlacementTarget)
            ).SetName("EdgeCase.MixedExplicitDefault.NoWarnings");
        }

        [Test]
        [TestCaseSource(nameof(NullAndDestroyedEditorCases))]
        public void NullAndDestroyedEditorHandledGracefully(bool destroyEditor)
        {
            CreateAssetAndEditor<WButtonGroupPlacementConflictTarget>(out Editor editor);

            if (destroyEditor)
            {
                UnityEngine.Object.DestroyImmediate(editor); // UNH-SUPPRESS: Test verifies behavior with destroyed editor
                _trackedObjects.Remove(editor);
            }

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            if (destroyEditor)
            {
                Assert.DoesNotThrow(() =>
                {
                    DrawButtonsWithDefaults(null, paginationStates, foldoutStates);
                });
            }
            else
            {
                Assert.DoesNotThrow(() =>
                {
                    DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
                });
            }
        }

        private static IEnumerable<TestCaseData> NullAndDestroyedEditorCases()
        {
            yield return new TestCaseData(false).SetName("Negative.ValidEditor.NoThrow");
            yield return new TestCaseData(true).SetName("Negative.DestroyedEditor.NoThrow");
        }

        [Test]
        [TestCaseSource(nameof(InvalidEnumValueCases))]
        public void InvalidEnumValuesHandledGracefully(
            WButtonGroupPlacement invalidPlacement,
            int invalidPriority
        )
        {
            CreateAssetAndEditor<WButtonGroupPlacementConflictTarget>(out Editor editor);
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            Assert.DoesNotThrow(() =>
            {
                DrawButtonsWithDefaults(editor, paginationStates, foldoutStates);
            });
        }

        private static IEnumerable<TestCaseData> InvalidEnumValueCases()
        {
            yield return new TestCaseData((WButtonGroupPlacement)999, int.MinValue).SetName(
                "Impossible.InvalidEnumValues.HandledGracefully"
            );
            yield return new TestCaseData((WButtonGroupPlacement)(-1), int.MaxValue).SetName(
                "Impossible.NegativeEnumValue.HandledGracefully"
            );
        }

        [Test]
        [TestCaseSource(nameof(ExtremeScaleTestCases))]
        public void ExtremeScaleHandledCorrectly(int buttonCount)
        {
            List<GameObject> gameObjects = new List<GameObject>(buttonCount);

            for (int i = 0; i < buttonCount; i++)
            {
                GameObject go = Track(new GameObject($"TestButton{i}"));
                gameObjects.Add(go);
            }

            Assert.AreEqual(buttonCount, gameObjects.Count);
        }

        private static IEnumerable<TestCaseData> ExtremeScaleTestCases()
        {
            yield return new TestCaseData(1000).SetName("Extreme.ThousandButtons.HandlesScale");
            yield return new TestCaseData(10000).SetName("Extreme.TenThousandButtons.HandlesScale");
        }
    }
}
#endif
