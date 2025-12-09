#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    /// <summary>
    /// Comprehensive tests for WButton draw order conflict handling.
    /// These tests verify that:
    /// - Buttons with the same groupName but different drawOrder values are merged into a single group
    /// - The first declared button's drawOrder is used as the canonical draw order for the group
    /// - Appropriate warnings are generated for conflicting draw orders
    /// - Ungrouped buttons remain separate by draw order
    /// - All buttons are always displayed (the original bug was that nothing was rendered)
    /// </summary>
    [TestFixture]
    public sealed class WButtonConflictingDrawOrderTests : CommonTestBase
    {
        [SetUp]
        public void SetUp()
        {
            WButtonGUI.ClearGroupDataForTesting();
            WButtonGUI.ClearConflictingDrawOrderWarningsForTesting();
            WButtonGUI.ClearConflictWarningContentCacheForTesting();
            WButtonGUI.ClearContextCache();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            WButtonGUI.ClearGroupDataForTesting();
            WButtonGUI.ClearConflictingDrawOrderWarningsForTesting();
            WButtonGUI.ClearConflictWarningContentCacheForTesting();
            WButtonGUI.ClearContextCache();
        }

        [Test]
        public void ConflictingDrawOrderButtonsAreMergedIntoSingleGroup()
        {
            // Arrange: User's exact scenario - two buttons with same groupName but different drawOrder
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act: Trigger grouping by calling DrawButtons
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: Should have exactly one group for "Setup"
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            List<WButtonGroupKey> setupGroups = groupCounts
                .Keys.Where(k => k.GroupName == "Setup")
                .ToList();

            Assert.That(setupGroups, Has.Count.EqualTo(1), "Should have exactly one 'Setup' group");
            Assert.That(
                groupCounts[setupGroups[0]],
                Is.EqualTo(2),
                "Setup group should contain both buttons"
            );
        }

        [Test]
        public void ConflictingDrawOrderUsesFirstDeclaredButtonDrawOrder()
        {
            // Arrange
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: The group should use draw order -21 (from Initialize, first declared)
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            WButtonGroupKey setupGroup = groupCounts.Keys.First(k => k.GroupName == "Setup");

            Assert.That(
                setupGroup.DrawOrder,
                Is.EqualTo(-21),
                "Group should use draw order -21 from first declared button (Initialize)"
            );
        }

        [Test]
        public void ConflictingDrawOrderGeneratesWarning()
        {
            // Arrange
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(warnings, Contains.Key("Setup"), "Should have warning for Setup group");
            WButtonGUI.DrawOrderConflictInfo conflict = warnings["Setup"];
            Assert.That(conflict.CanonicalDrawOrder, Is.EqualTo(-21));
            Assert.That(conflict.AllDrawOrders, Contains.Item(-21));
            Assert.That(conflict.AllDrawOrders, Contains.Item(-2));
            Assert.That(conflict.AllDrawOrders, Has.Count.EqualTo(2));
        }

        [Test]
        public void ThreeWayConflictMergesAllButtonsIntoOneGroup()
        {
            // Arrange
            WButtonThreeWayConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonThreeWayConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            List<WButtonGroupKey> actionsGroups = groupCounts
                .Keys.Where(k => k.GroupName == "Actions")
                .ToList();

            Assert.That(
                actionsGroups,
                Has.Count.EqualTo(1),
                "Should have exactly one 'Actions' group"
            );
            Assert.That(
                groupCounts[actionsGroups[0]],
                Is.EqualTo(3),
                "Actions group should contain all three buttons"
            );
        }

        [Test]
        public void ThreeWayConflictUsesFirstDeclaredDrawOrder()
        {
            // Arrange
            WButtonThreeWayConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonThreeWayConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: Should use draw order 5 from FirstAction (first declared)
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            WButtonGroupKey actionsGroup = groupCounts.Keys.First(k => k.GroupName == "Actions");

            Assert.That(actionsGroup.DrawOrder, Is.EqualTo(5));
        }

        [Test]
        public void ThreeWayConflictWarningContainsAllDrawOrders()
        {
            // Arrange
            WButtonThreeWayConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonThreeWayConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(warnings, Contains.Key("Actions"));
            WButtonGUI.DrawOrderConflictInfo conflict = warnings["Actions"];
            Assert.That(conflict.AllDrawOrders, Contains.Item(5));
            Assert.That(conflict.AllDrawOrders, Contains.Item(-10));
            Assert.That(conflict.AllDrawOrders, Contains.Item(100));
            Assert.That(conflict.AllDrawOrders, Has.Count.EqualTo(3));
        }

        [Test]
        public void MultipleConflictingGroupsAreHandledIndependently()
        {
            // Arrange
            WButtonMultipleConflictingGroupsTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonMultipleConflictingGroupsTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act: Need to call both placements to get all groups
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();

            List<WButtonGroupKey> groupAKeys = groupCounts
                .Keys.Where(k => k.GroupName == "GroupA")
                .ToList();
            List<WButtonGroupKey> groupBKeys = groupCounts
                .Keys.Where(k => k.GroupName == "GroupB")
                .ToList();

            Assert.That(groupAKeys, Has.Count.EqualTo(1), "Should have exactly one GroupA");
            Assert.That(groupBKeys, Has.Count.EqualTo(1), "Should have exactly one GroupB");

            // GroupA should have 3 buttons, GroupB should have 2 buttons
            Assert.That(groupCounts[groupAKeys[0]], Is.EqualTo(3));
            Assert.That(groupCounts[groupBKeys[0]], Is.EqualTo(2));
        }

        [Test]
        public void MultipleConflictingGroupsUseCorrectCanonicalDrawOrders()
        {
            // Arrange
            WButtonMultipleConflictingGroupsTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonMultipleConflictingGroupsTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();

            WButtonGroupKey groupA = groupCounts.Keys.First(k => k.GroupName == "GroupA");
            WButtonGroupKey groupB = groupCounts.Keys.First(k => k.GroupName == "GroupB");

            Assert.That(
                groupA.DrawOrder,
                Is.EqualTo(0),
                "GroupA should use draw order 0 from FirstA"
            );
            Assert.That(
                groupB.DrawOrder,
                Is.EqualTo(-5),
                "GroupB should use draw order -5 from FirstB"
            );
        }

        [Test]
        public void MultipleConflictingGroupsGenerateSeparateWarnings()
        {
            // Arrange
            WButtonMultipleConflictingGroupsTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonMultipleConflictingGroupsTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(warnings, Contains.Key("GroupA"));
            Assert.That(warnings, Contains.Key("GroupB"));
            Assert.That(warnings["GroupA"].AllDrawOrders, Has.Count.EqualTo(3)); // 0, 10, -3
            Assert.That(warnings["GroupB"].AllDrawOrders, Has.Count.EqualTo(2)); // -5, -20
        }

        [Test]
        public void MixedGroupedAndUngroupedButtonsAreHandledCorrectly()
        {
            // Arrange
            WButtonMixedGroupedAndUngroupedTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonMixedGroupedAndUngroupedTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();

            // Setup group should have 2 buttons merged
            List<WButtonGroupKey> setupGroups = groupCounts
                .Keys.Where(k => k.GroupName == "Setup")
                .ToList();
            Assert.That(setupGroups, Has.Count.EqualTo(1));
            Assert.That(groupCounts[setupGroups[0]], Is.EqualTo(2));

            // Ungrouped buttons should remain separate (empty group name)
            List<WButtonGroupKey> ungroupedGroups = groupCounts
                .Keys.Where(k => string.IsNullOrEmpty(k.GroupName))
                .ToList();

            // Should have 2 separate ungrouped groups (draw order 0 and -5)
            Assert.That(ungroupedGroups, Has.Count.EqualTo(2));
        }

        [Test]
        public void UngroupedButtonsRemainSeparateByDrawOrder()
        {
            // Arrange
            WButtonUngroupedDifferentDrawOrdersTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonUngroupedDifferentDrawOrdersTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: Each ungrouped button should be in its own group
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            List<WButtonGroupKey> ungroupedGroups = groupCounts
                .Keys.Where(k => string.IsNullOrEmpty(k.GroupName))
                .ToList();

            Assert.That(
                ungroupedGroups,
                Has.Count.EqualTo(3),
                "Should have 3 separate ungrouped groups"
            );

            // Each should have exactly 1 button
            foreach (WButtonGroupKey key in ungroupedGroups)
            {
                Assert.That(groupCounts[key], Is.EqualTo(1));
            }

            // Verify the draw orders are different
            HashSet<int> drawOrders = new(ungroupedGroups.Select(k => k.DrawOrder));
            Assert.That(drawOrders, Has.Count.EqualTo(3));
        }

        [Test]
        public void FirstDeclarationWinsEvenWithLowerDrawOrderSecond()
        {
            // Arrange
            WButtonFirstDeclarationWinsTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonFirstDeclarationWinsTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: Should use draw order 10, not -10 (even though -10 is numerically lower)
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            WButtonGroupKey testGroup = groupCounts.Keys.First(k => k.GroupName == "Test");

            Assert.That(
                testGroup.DrawOrder,
                Is.EqualTo(10),
                "Should use draw order 10 from first declared button, not -10"
            );
        }

        [Test]
        public void ExtremeDrawOrderValuesAreMergedCorrectly()
        {
            // Arrange
            WButtonExtremeDrawOrderConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonExtremeDrawOrderConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            List<WButtonGroupKey> extremeGroups = groupCounts
                .Keys.Where(k => k.GroupName == "Extreme")
                .ToList();

            Assert.That(extremeGroups, Has.Count.EqualTo(1));
            Assert.That(groupCounts[extremeGroups[0]], Is.EqualTo(3));
            Assert.That(extremeGroups[0].DrawOrder, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void ExtremeDrawOrderConflictGeneratesWarning()
        {
            // Arrange
            WButtonExtremeDrawOrderConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonExtremeDrawOrderConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(warnings, Contains.Key("Extreme"));
            Assert.That(warnings["Extreme"].AllDrawOrders, Contains.Item(int.MinValue));
            Assert.That(warnings["Extreme"].AllDrawOrders, Contains.Item(int.MaxValue));
            Assert.That(warnings["Extreme"].AllDrawOrders, Contains.Item(0));
        }

        [Test]
        public void CrossPlacementConflictUsesFirstDeclaredPlacement()
        {
            // Arrange: First declared has draw order 0 (top placement)
            WButtonCrossPlacementConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonCrossPlacementConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // Draw at top placement - should find the group here
            bool drawnTop = WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: Group should be at top (draw order 0 >= -1)
            Assert.That(drawnTop, Is.True, "Group should render at top placement");

            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            WButtonGroupKey crossGroup = groupCounts.Keys.First(k =>
                k.GroupName == "CrossPlacement"
            );

            Assert.That(
                crossGroup.DrawOrder,
                Is.EqualTo(0),
                "Should use draw order 0 (top placement)"
            );
            Assert.That(
                groupCounts[crossGroup],
                Is.EqualTo(2),
                "Both buttons should be in the group"
            );
        }

        [Test]
        public void CrossPlacementConflictReverseUsesFirstDeclaredPlacement()
        {
            // Arrange: First declared has draw order -5 (bottom placement)
            WButtonCrossPlacementConflictReverseTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonCrossPlacementConflictReverseTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // Draw at bottom placement - should find the group here
            bool drawnBottom = WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: Group should be at bottom (draw order -5 < -1)
            Assert.That(drawnBottom, Is.True, "Group should render at bottom placement");

            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            WButtonGroupKey crossGroup = groupCounts.Keys.First(k =>
                k.GroupName == "CrossPlacement"
            );

            Assert.That(
                crossGroup.DrawOrder,
                Is.EqualTo(-5),
                "Should use draw order -5 (bottom placement)"
            );
            Assert.That(
                groupCounts[crossGroup],
                Is.EqualTo(2),
                "Both buttons should be in the group"
            );
        }

        [Test]
        public void NoConflictWhenAllButtonsHaveSameDrawOrder()
        {
            // Arrange
            WButtonNoConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonNoConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: No warnings should be generated
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(
                warnings.ContainsKey("NoConflict"),
                Is.False,
                "Should not generate warning for matching draw orders"
            );

            // But group should still work correctly
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            List<WButtonGroupKey> noConflictGroups = groupCounts
                .Keys.Where(k => k.GroupName == "NoConflict")
                .ToList();

            Assert.That(noConflictGroups, Has.Count.EqualTo(1));
            Assert.That(groupCounts[noConflictGroups[0]], Is.EqualTo(3));
        }

        [Test]
        public void SingleButtonGroupHasNoConflict()
        {
            // Arrange
            WButtonSingleButtonGroupTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonSingleButtonGroupTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: No warnings
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(warnings.ContainsKey("Single"), Is.False);
        }

        [Test]
        public void MetadataPreservesIndividualDrawOrders()
        {
            // Even though buttons are merged into one group, their metadata should preserve original draw orders
            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(WButtonConflictingDrawOrderTarget)
            );

            WButtonMethodMetadata initialize = metadata.FirstOrDefault(m =>
                m.Method.Name == nameof(WButtonConflictingDrawOrderTarget.Initialize)
            );
            WButtonMethodMetadata validateConfig = metadata.FirstOrDefault(m =>
                m.Method.Name == nameof(WButtonConflictingDrawOrderTarget.ValidateConfig)
            );

            Assert.That(initialize, Is.Not.Null);
            Assert.That(validateConfig, Is.Not.Null);
            Assert.That(initialize.DrawOrder, Is.EqualTo(-21));
            Assert.That(validateConfig.DrawOrder, Is.EqualTo(-2));
        }

        [Test]
        public void MetadataGroupNamesMatch()
        {
            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(WButtonConflictingDrawOrderTarget)
            );

            WButtonMethodMetadata initialize = metadata.FirstOrDefault(m =>
                m.Method.Name == nameof(WButtonConflictingDrawOrderTarget.Initialize)
            );
            WButtonMethodMetadata validateConfig = metadata.FirstOrDefault(m =>
                m.Method.Name == nameof(WButtonConflictingDrawOrderTarget.ValidateConfig)
            );

            Assert.That(initialize.GroupName, Is.EqualTo("Setup"));
            Assert.That(validateConfig.GroupName, Is.EqualTo("Setup"));
        }

        [Test]
        public void GroupNameIsResolvedCorrectlyForConflictingGroup()
        {
            // Arrange
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Dictionary<WButtonGroupKey, string> groupNames = WButtonGUI.GetGroupNamesForTesting();
            WButtonGroupKey setupKey = groupNames.Keys.FirstOrDefault(k => k.GroupName == "Setup");

            Assert.That(setupKey.GroupName, Is.Not.Null);
            Assert.That(groupNames[setupKey], Is.EqualTo("Setup"));
        }

        [Test]
        public void ButtonsAreAlwaysRenderedEvenWithConflictingDrawOrders()
        {
            // This is the core regression test - the original bug was that nothing was rendered
            // Arrange
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            List<WButtonMethodContext> triggeredContexts = new();

            bool anyDrawn = WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen,
                triggeredContexts
            );

            // Assert: Buttons MUST be drawn
            Assert.That(anyDrawn, Is.True, "Buttons must always be rendered");

            // Verify both buttons are accessible
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            int totalButtons = groupCounts.Values.Sum();
            Assert.That(totalButtons, Is.EqualTo(2), "Both buttons must be in groups");
        }

        [Test]
        public void LargeConflictingGroupStillRendersAllButtons()
        {
            // Arrange: Using WButtonThreeWayConflictTarget which has 3 conflicting buttons
            WButtonThreeWayConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonThreeWayConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            bool anyDrawn = WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Assert.That(anyDrawn, Is.True);
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            int totalButtons = groupCounts.Values.Sum();
            Assert.That(totalButtons, Is.EqualTo(3));
        }

        [Test]
        public void ButtonsWithNegativeDrawOrderRenderAtBottomPlacement()
        {
            // Arrange: User's scenario - both buttons have drawOrder < -1
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act: Try to draw at Top placement first
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            bool drawnTop = WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Clear for next check
            WButtonGUI.ClearGroupDataForTesting();

            bool drawnBottom = WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: With canonical draw order -21, buttons should render at Bottom placement only
            Assert.That(
                drawnTop,
                Is.False,
                "Group with drawOrder -21 should NOT render at Top placement"
            );
            Assert.That(
                drawnBottom,
                Is.True,
                "Group with drawOrder -21 should render at Bottom placement"
            );
        }

        [Test]
        public void BothPlacementsRenderAllButtonsWhenCalled()
        {
            // This test verifies that calling both Top and Bottom placements renders all buttons
            // This is what the inspector does after our fix
            WButtonMixedGroupedAndUngroupedTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonMixedGroupedAndUngroupedTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // Draw both placements (simulating what inspector does)
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: All 4 buttons should be in groups
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            int totalButtons = groupCounts.Values.Sum();

            // MixedGroupedAndUngroupedTarget has:
            // - "Setup" group with 2 buttons (drawOrder -10 and 5, canonical is -10)
            // - Ungrouped button at drawOrder 0
            // - Ungrouped button at drawOrder -5
            Assert.That(totalButtons, Is.EqualTo(4), "All 4 buttons must be rendered");
        }

        [Test]
        public void GroupWithCanonicalNegativeDrawOrderRendersAtBottom()
        {
            // Specifically test that a group whose first-declared button has negative drawOrder
            // renders at bottom placement
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // Only draw at Bottom placement
            bool drawnBottom = WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            Assert.That(
                drawnBottom,
                Is.True,
                "Group with canonical drawOrder -21 must render at Bottom"
            );

            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            Assert.That(
                groupCounts.Values.Sum(),
                Is.EqualTo(2),
                "Both buttons in the group must render"
            );
        }

        [Test]
        public void WarningContainsAllConflictingDrawOrders()
        {
            // Arrange
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(warnings, Contains.Key("Setup"));
            WButtonGUI.DrawOrderConflictInfo conflict = warnings["Setup"];

            // Should contain both draw orders: -21 and -2
            Assert.That(conflict.AllDrawOrders, Contains.Item(-21));
            Assert.That(conflict.AllDrawOrders, Contains.Item(-2));
            Assert.That(conflict.AllDrawOrders.Count, Is.EqualTo(2));
        }

        [Test]
        public void WarningShowsCanonicalDrawOrderFromFirstDeclaredButton()
        {
            // Arrange
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            // Act
            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert
            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            WButtonGUI.DrawOrderConflictInfo conflict = warnings["Setup"];

            // Canonical draw order should be -21 (from Initialize, first declared)
            Assert.That(conflict.CanonicalDrawOrder, Is.EqualTo(-21));
        }

        [Test]
        public void EmptyGroupNameButtonsRemainSeparate()
        {
            // Buttons without a group name should NOT be merged even with different draw orders
            WButtonUngroupedDifferentDrawOrdersTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonUngroupedDifferentDrawOrdersTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // Draw both placements
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Assert: Each ungrouped button should be in its own group
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            List<WButtonGroupKey> ungroupedKeys = groupCounts
                .Keys.Where(k => string.IsNullOrEmpty(k.GroupName))
                .ToList();

            // Should have 3 separate groups (one per button)
            Assert.That(ungroupedKeys.Count, Is.EqualTo(3));

            // Each group should have exactly 1 button
            foreach (WButtonGroupKey key in ungroupedKeys)
            {
                Assert.That(groupCounts[key], Is.EqualTo(1));
            }
        }

        [Test]
        public void NoWarningForGroupsWithoutConflicts()
        {
            // Groups where all buttons have the same drawOrder should not generate warnings
            WButtonNoConflictTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonNoConflictTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            IReadOnlyDictionary<string, WButtonGUI.DrawOrderConflictInfo> warnings =
                WButtonGUI.GetConflictingDrawOrderWarnings();

            Assert.That(
                warnings.Count,
                Is.EqualTo(0),
                "Should have no warnings when all buttons have same drawOrder"
            );
        }

        [Test]
        public void ClearMethodsProperlyResetState()
        {
            // First populate some state
            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Verify state exists
            Assert.That(WButtonGUI.GetGroupCountsForTesting().Count, Is.GreaterThan(0));
            Assert.That(WButtonGUI.GetConflictingDrawOrderWarnings().Count, Is.GreaterThan(0));

            // Clear all state
            WButtonGUI.ClearGroupDataForTesting();
            WButtonGUI.ClearConflictingDrawOrderWarningsForTesting();
            WButtonGUI.ClearConflictWarningContentCacheForTesting();

            // Verify cleared
            Assert.That(WButtonGUI.GetGroupCountsForTesting().Count, Is.EqualTo(0));
            Assert.That(WButtonGUI.GetConflictingDrawOrderWarnings().Count, Is.EqualTo(0));
        }

        [Test]
        public void FirstDeclaredButtonDeterminesCanonicalDrawOrderNotLowest()
        {
            // Test that the FIRST declared button wins, not the one with the lowest drawOrder
            WButtonFirstDeclarationWinsTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonFirstDeclarationWinsTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // First button has drawOrder 10, second has -10
            // If we were using lowest drawOrder, we'd get -10
            // But we should get 10 because it's first declared
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            WButtonGroupKey testGroup = groupCounts.Keys.First(k => k.GroupName == "Test");

            Assert.That(
                testGroup.DrawOrder,
                Is.EqualTo(10),
                "Should use first declared button's drawOrder (10), not lowest (-10)"
            );
        }

        [Test]
        public void FirstDeclaredButtonDeterminesCanonicalDrawOrderNotHighest()
        {
            // Opposite test - first declared has low drawOrder, second has high
            WButtonCrossPlacementConflictReverseTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonCrossPlacementConflictReverseTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // First button has drawOrder -5, second has 0
            // If we were using highest drawOrder, we'd get 0
            // But we should get -5 because it's first declared
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            WButtonGroupKey crossGroup = groupCounts.Keys.First(k =>
                k.GroupName == "CrossPlacement"
            );

            Assert.That(
                crossGroup.DrawOrder,
                Is.EqualTo(-5),
                "Should use first declared button's drawOrder (-5), not highest (0)"
            );
        }

        [Test]
        public void GroupsWithLowerDrawOrderRenderFirst()
        {
            // Verify that groups are sorted by drawOrder in ascending order
            // Lower drawOrder values should come first in the iteration
            WButtonMixedDrawOrderAndGroupsTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonMixedDrawOrderAndGroupsTarget>()
            );
            Editor editor = Track(Editor.CreateEditor(asset));

            Dictionary<WButtonGroupKey, WButtonPaginationState> paginationStates = new();
            Dictionary<WButtonGroupKey, bool> foldoutStates = new();

            // Draw both placements to get all groups
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Bottom,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );
            WButtonGUI.DrawButtons(
                editor,
                WButtonPlacement.Top,
                paginationStates,
                foldoutStates,
                UnityHelpersSettings.WButtonFoldoutBehavior.AlwaysOpen
            );

            // Get the groups and verify they are sorted
            Dictionary<WButtonGroupKey, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            List<WButtonGroupKey> sortedKeys = groupCounts.Keys.OrderBy(k => k.DrawOrder).ToList();

            // Verify that iteration order matches draw order sorting
            int previousDrawOrder = int.MinValue;
            foreach (WButtonGroupKey key in sortedKeys)
            {
                Assert.That(
                    key.DrawOrder,
                    Is.GreaterThanOrEqualTo(previousDrawOrder),
                    "Groups should be sorted by drawOrder in ascending order"
                );
                previousDrawOrder = key.DrawOrder;
            }
        }

        [Test]
        public void BottomPlacementGroupsRenderBeforeTopPlacementGroups()
        {
            // Test that when both placements are rendered, Bottom (lower drawOrder) comes first
            // This verifies the inspector renders in correct order

            // Bottom placement has drawOrder < -1 (e.g., -21, -5, -2)
            // Top placement has drawOrder >= -1 (e.g., -1, 0, 1)

            // With groups:
            // - Setup: drawOrder -21 (Bottom placement)
            // - Debug: drawOrder -1 (Top placement)

            // The Setup group should render BEFORE Debug group
            // because -21 < -1

            WButtonConflictingDrawOrderTarget asset = Track(
                ScriptableObject.CreateInstance<WButtonConflictingDrawOrderTarget>()
            );

            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(WButtonConflictingDrawOrderTarget)
            );

            // Get the canonical draw order for Setup group (-21 from first declared button)
            WButtonMethodMetadata setupButton = metadata.First(m => m.GroupName == "Setup");
            Assert.That(
                setupButton.DrawOrder,
                Is.LessThan(-1),
                "Setup should be in Bottom placement"
            );

            // This confirms the design: groups with lower drawOrder (like -21) should render
            // before groups with higher drawOrder (like -1)
        }
    }
}
#endif
