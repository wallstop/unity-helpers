#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;
    using WallstopStudios.UnityHelpers.Tests.Core;

    /// <summary>
    /// Comprehensive tests for WGroup layout functionality.
    /// These tests use AutoIncludeMode.None to ensure only explicitly marked
    /// fields are included in groups, making assertions predictable.
    /// </summary>
    [TestFixture]
    public sealed class WGroupLayoutTests : CommonTestBase
    {
        private UnityHelpersSettings.WGroupAutoIncludeConfiguration _previousConfiguration;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            WGroupLayoutBuilder.ClearCache();
            // Store previous configuration and set to None for predictable test behavior
            _previousConfiguration = UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );
        }

        [TearDown]
        public override void TearDown()
        {
            WGroupLayoutBuilder.ClearCache();
            // Restore previous configuration
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                _previousConfiguration.Mode,
                _previousConfiguration.RowCount
            );
            base.TearDown();
        }

        /// <summary>
        /// Formats layout information for diagnostic output when tests fail.
        /// </summary>
        private static string FormatLayoutDiagnostics(WGroupLayout layout)
        {
            List<string> lines = new()
            {
                "=== Layout Diagnostics ===",
                $"Total Groups: {layout.Groups.Count}",
                $"Total Operations: {layout.Operations.Count}",
                $"Grouped Paths: [{string.Join(", ", layout.GroupedPaths)}]",
                "\n--- Groups ---",
            };

            for (int i = 0; i < layout.Groups.Count; i++)
            {
                WGroupDefinition group = layout.Groups[i];
                lines.Add(
                    $"  Group '{group.Name}' (DisplayName='{group.DisplayName}', DeclarationOrder={group.DeclarationOrder}):"
                );
                lines.Add($"    Anchor: {group.AnchorPropertyPath}");
                lines.Add($"    Properties: [{string.Join(", ", group.PropertyPaths)}]");
            }

            lines.Add("\n--- Operations ---");
            for (int i = 0; i < layout.Operations.Count; i++)
            {
                WGroupDrawOperation op = layout.Operations[i];
                if (op.Type == WGroupDrawOperationType.Group)
                {
                    lines.Add($"  [{i}] Group: {op.Group?.Name ?? "(null)"}");
                }
                else
                {
                    lines.Add($"  [{i}] Property: {op.PropertyPath}");
                }
            }

            return string.Join("\n", lines);
        }

        [Test]
        public void LayoutBuildsCorrectNumberOfGroups()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Should have 3 groups: A, B, C
            Assert.That(
                layout.Groups,
                Has.Count.EqualTo(3),
                () =>
                    $"Expected 3 groups but found {layout.Groups.Count}.\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        [Test]
        public void GroupsPreserveDeclarationOrder()
        {
            WGroupDeclarationOrderTestTarget target =
                CreateScriptableObject<WGroupDeclarationOrderTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Groups should be in declaration order: First, Second, Third
            Assert.That(layout.Groups, Has.Count.EqualTo(3));

            List<WGroupDefinition> sortedGroups = layout
                .Groups.OrderBy(g => g.DeclarationOrder)
                .ToList();
            Assert.That(sortedGroups[0].Name, Is.EqualTo("First"));
            Assert.That(sortedGroups[1].Name, Is.EqualTo("Second"));
            Assert.That(sortedGroups[2].Name, Is.EqualTo("Third"));
        }

        [Test]
        public void GroupsContainCorrectProperties()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Get Group A
            Assert.That(
                layout.TryGetGroup("Group A", out WGroupDefinition groupA),
                Is.True,
                () => $"Group A should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                groupA.PropertyPaths,
                Has.Count.EqualTo(2),
                () =>
                    $"Group A should have 2 properties but has {groupA.PropertyPaths.Count}: [{string.Join(", ", groupA.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(groupA.PropertyPaths, Contains.Item("fieldA1"));
            Assert.That(groupA.PropertyPaths, Contains.Item("fieldA2"));

            // Get Group B
            Assert.That(
                layout.TryGetGroup("Group B", out WGroupDefinition groupB),
                Is.True,
                () => $"Group B should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                groupB.PropertyPaths,
                Has.Count.EqualTo(2),
                () =>
                    $"Group B should have 2 properties but has {groupB.PropertyPaths.Count}: [{string.Join(", ", groupB.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(groupB.PropertyPaths, Contains.Item("fieldB1"));
            Assert.That(groupB.PropertyPaths, Contains.Item("fieldB2"));

            // Get Group C
            Assert.That(
                layout.TryGetGroup("Group C", out WGroupDefinition groupC),
                Is.True,
                () => $"Group C should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                groupC.PropertyPaths,
                Has.Count.EqualTo(1),
                () =>
                    $"Group C should have 1 property but has {groupC.PropertyPaths.Count}: [{string.Join(", ", groupC.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(groupC.PropertyPaths, Contains.Item("fieldC1"));
        }

        [Test]
        public void DisplayNameIsResolvedCorrectly()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            Assert.That(
                layout.TryGetGroup("Group A", out WGroupDefinition groupA),
                Is.True,
                () => $"Group A should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                groupA.DisplayName,
                Is.EqualTo("Alpha Group"),
                () =>
                    $"Group A display name expected 'Alpha Group' but was '{groupA.DisplayName}'.\n{FormatLayoutDiagnostics(layout)}"
            );

            Assert.That(
                layout.TryGetGroup("Group B", out WGroupDefinition groupB),
                Is.True,
                () => $"Group B should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                groupB.DisplayName,
                Is.EqualTo("Beta Group"),
                () =>
                    $"Group B display name expected 'Beta Group' but was '{groupB.DisplayName}'.\n{FormatLayoutDiagnostics(layout)}"
            );

            // Group C has no display name, should use group name
            Assert.That(
                layout.TryGetGroup("Group C", out WGroupDefinition groupC),
                Is.True,
                () => $"Group C should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                groupC.DisplayName,
                Is.EqualTo("Group C"),
                () =>
                    $"Group C display name expected 'Group C' but was '{groupC.DisplayName}'.\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        /// <summary>
        /// Test cases for display name resolution behavior.
        /// Tests various scenarios where display names are set on different fields.
        /// </summary>
        private static IEnumerable<TestCaseData> DisplayNameResolutionTestCases()
        {
            // Display name on first field is preserved even when subsequent fields don't specify it
            yield return new TestCaseData("GroupA", "Custom Display A", 3).SetName(
                "DisplayName.FirstFieldHasCustomName.Preserved"
            );

            // Display name on second field overrides the default (group name)
            yield return new TestCaseData("GroupB", "Custom Display B", 3).SetName(
                "DisplayName.SecondFieldHasCustomName.Wins"
            );

            // Display name on last field overrides the default
            yield return new TestCaseData("GroupC", "Custom Display C", 3).SetName(
                "DisplayName.LastFieldHasCustomName.Wins"
            );

            // No explicit display name, should use group name
            yield return new TestCaseData("GroupD", "GroupD", 2).SetName(
                "DisplayName.NoExplicitName.UsesGroupName"
            );

            // Multiple conflicting display names, last explicit one wins
            yield return new TestCaseData("GroupE", "Second Display E", 2).SetName(
                "DisplayName.ConflictingNames.LastExplicitWins"
            );
        }

        [Test]
        [TestCaseSource(nameof(DisplayNameResolutionTestCases))]
        public void DisplayNameResolutionFollowsExpectedBehavior(
            string groupName,
            string expectedDisplayName,
            int expectedPropertyCount
        )
        {
            WGroupDisplayNameTestTarget target =
                CreateScriptableObject<WGroupDisplayNameTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            Assert.That(
                layout.TryGetGroup(groupName, out WGroupDefinition group),
                Is.True,
                () => $"{groupName} should exist.\n{FormatLayoutDiagnostics(layout)}"
            );

            Assert.That(
                group.DisplayName,
                Is.EqualTo(expectedDisplayName),
                () =>
                    $"{groupName} display name expected '{expectedDisplayName}' but was '{group.DisplayName}'.\n{FormatLayoutDiagnostics(layout)}"
            );

            Assert.That(
                group.PropertyPaths,
                Has.Count.EqualTo(expectedPropertyCount),
                () =>
                    $"{groupName} expected {expectedPropertyCount} properties but has {group.PropertyPaths.Count}: [{string.Join(", ", group.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        [Test]
        public void GroupNameLookupIsCaseInsensitive()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            Assert.That(layout.TryGetGroup("group a", out WGroupDefinition lower), Is.True);
            Assert.That(layout.TryGetGroup("GROUP A", out WGroupDefinition upper), Is.True);
            Assert.That(layout.TryGetGroup("Group A", out WGroupDefinition mixed), Is.True);

            Assert.That(lower.Name, Is.EqualTo(upper.Name));
            Assert.That(upper.Name, Is.EqualTo(mixed.Name));
        }

        [Test]
        public void UngroupedFieldNotInAnyGroup()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // ungroupedField should not be in any group
            Assert.That(
                layout.GroupedPaths,
                Does.Not.Contain("ungroupedField"),
                () =>
                    $"ungroupedField should not be in any group but was found in GroupedPaths.\n{FormatLayoutDiagnostics(layout)}"
            );

            // But it should appear in operations
            bool foundInOperations = layout.Operations.Any(op =>
                op.Type == WGroupDrawOperationType.Property && op.PropertyPath == "ungroupedField"
            );
            Assert.That(
                foundInOperations,
                Is.True,
                () =>
                    $"Ungrouped field should appear in operations as a Property type.\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        [Test]
        public void AnchorPropertyPathIsFirstPropertyInGroup()
        {
            WGroupDeclarationOrderTestTarget target =
                CreateScriptableObject<WGroupDeclarationOrderTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            Assert.That(layout.TryGetGroup("First", out WGroupDefinition first), Is.True);
            Assert.That(first.AnchorPropertyPath, Is.EqualTo("first1"));

            Assert.That(layout.TryGetGroup("Second", out WGroupDefinition second), Is.True);
            Assert.That(second.AnchorPropertyPath, Is.EqualTo("second1"));
        }

        [Test]
        public void LayoutCachingWorks()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout1 = WGroupLayoutBuilder.Build(serializedObject, "m_Script");
            WGroupLayout layout2 = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Should return same cached instance
            Assert.That(layout1, Is.SameAs(layout2));
        }

        [Test]
        public void ClearCacheInvalidatesCache()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout1 = WGroupLayoutBuilder.Build(serializedObject, "m_Script");
            WGroupLayoutBuilder.ClearCache();
            WGroupLayout layout2 = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Should be different instances after cache clear
            Assert.That(layout1, Is.Not.SameAs(layout2));
        }

        [Test]
        public void OperationsInCorrectOrder()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Operations should include groups and ungrouped properties
            Assert.That(
                layout.Operations,
                Is.Not.Empty,
                () => $"Operations should not be empty.\n{FormatLayoutDiagnostics(layout)}"
            );

            // Count group operations
            int groupOperations = layout.Operations.Count(op =>
                op.Type == WGroupDrawOperationType.Group
            );
            Assert.That(
                groupOperations,
                Is.EqualTo(3),
                () =>
                    $"Should have 3 group operations but found {groupOperations}.\n{FormatLayoutDiagnostics(layout)}"
            );

            // Should have at least one property operation (for ungroupedField)
            int propertyOperations = layout.Operations.Count(op =>
                op.Type == WGroupDrawOperationType.Property
            );
            Assert.That(
                propertyOperations,
                Is.GreaterThanOrEqualTo(1),
                () =>
                    $"Should have at least 1 property operation but found {propertyOperations}.\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        [Test]
        public void MultiplePropertiesWithSameGroupNameAreMerged()
        {
            WGroupDeclarationOrderTestTarget target =
                CreateScriptableObject<WGroupDeclarationOrderTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // First group should have first1 and first2
            Assert.That(
                layout.TryGetGroup("First", out WGroupDefinition first),
                Is.True,
                () => $"First group should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                first.PropertyPaths,
                Has.Count.EqualTo(2),
                () =>
                    $"First group should have 2 properties but has {first.PropertyPaths.Count}: [{string.Join(", ", first.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(first.PropertyPaths, Contains.Item("first1"));
            Assert.That(first.PropertyPaths, Contains.Item("first2"));

            // Second group should have second1 and second2
            Assert.That(
                layout.TryGetGroup("Second", out WGroupDefinition second),
                Is.True,
                () => $"Second group should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                second.PropertyPaths,
                Has.Count.EqualTo(2),
                () =>
                    $"Second group should have 2 properties but has {second.PropertyPaths.Count}: [{string.Join(", ", second.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(second.PropertyPaths, Contains.Item("second1"));
            Assert.That(second.PropertyPaths, Contains.Item("second2"));
        }

        /// <summary>
        /// Test cases for auto-include mode behavior using WGroupAutoIncludeTestTarget.
        /// The target has: [WGroup("Auto Group")] autoGroupFirst (uses UseGlobalAutoInclude),
        /// then autoIncluded1, autoIncluded2, notAutoIncluded (no attributes).
        ///
        /// IMPORTANT: The attribute uses default autoIncludeCount (UseGlobalAutoInclude = -2)
        /// which means the global WGroupAutoIncludeConfiguration controls how many
        /// subsequent fields are captured. If an attribute explicitly specifies
        /// autoIncludeCount (e.g., autoIncludeCount: 2), that value would override the
        /// global setting entirely.
        ///
        /// Note: WGroupLayoutTestTarget is NOT suitable for these tests because it has
        /// explicit [WGroup] attributes on most fields. Auto-include only captures
        /// fields that don't have explicit group assignments.
        ///
        /// Each case specifies: mode, row count, expected property count for "Auto Group",
        /// and whether notAutoIncluded should be in any group.
        /// </summary>
        private static IEnumerable<TestCaseData> AutoIncludeModeTestCases()
        {
            // None mode: Only explicitly attributed field is in the group
            // Global setting Mode.None means no auto-include regardless of subsequent fields
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0,
                1, // Auto Group: autoGroupFirst only
                false // notAutoIncluded NOT in any group
            ).SetName("AutoInclude.None.OnlyExplicitFields");

            // Infinite mode: All subsequent unattributed fields are captured until end of type
            // or until a WGroupEnd attribute is encountered
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Infinite,
                0,
                4, // Auto Group: autoGroupFirst, autoIncluded1, autoIncluded2, notAutoIncluded
                true // notAutoIncluded IS in the group
            ).SetName("AutoInclude.Infinite.CapturesAllSubsequent");

            // Finite mode with 1: Global setting (rowCount=1) limits capture to 1 extra field
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                1,
                2, // Auto Group: autoGroupFirst, autoIncluded1
                false // notAutoIncluded NOT in any group
            ).SetName("AutoInclude.Finite1.CapturesOneExtra");

            // Finite mode with 2: Captures exactly 2 additional fields
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                2,
                3, // Auto Group: autoGroupFirst, autoIncluded1, autoIncluded2
                false // notAutoIncluded NOT in any group
            ).SetName("AutoInclude.Finite2.CapturesTwoExtra");

            // Finite mode with 3: Captures all 3 unattributed fields
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                3,
                4, // Auto Group: all 4 fields
                true // notAutoIncluded IS in the group
            ).SetName("AutoInclude.Finite3.CapturesThreeExtra");
        }

        [Test]
        [TestCaseSource(nameof(AutoIncludeModeTestCases))]
        public void AutoIncludeModeAffectsGroupCapture(
            UnityHelpersSettings.WGroupAutoIncludeMode mode,
            int rowCount,
            int expectedGroupPropertyCount,
            bool expectNotAutoIncludedInGroup
        )
        {
            // Arrange: Set the auto-include mode for this test
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(mode, rowCount);
            WGroupLayoutBuilder.ClearCache();

            WGroupAutoIncludeTestTarget target =
                CreateScriptableObject<WGroupAutoIncludeTestTarget>();
            using SerializedObject serializedObject = new(target);

            // Act
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Assert: Check group exists
            Assert.That(
                layout.TryGetGroup("Auto Group", out WGroupDefinition autoGroup),
                Is.True,
                () => $"Auto Group should exist.\n{FormatLayoutDiagnostics(layout)}"
            );

            // Assert: Check property count
            Assert.That(
                autoGroup.PropertyPaths,
                Has.Count.EqualTo(expectedGroupPropertyCount),
                () =>
                    $"Mode={mode}, RowCount={rowCount}: Auto Group expected {expectedGroupPropertyCount} properties but has {autoGroup.PropertyPaths.Count}: [{string.Join(", ", autoGroup.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );

            // Assert: Check if notAutoIncluded is in the group
            bool notAutoIncludedInGroup = layout.GroupedPaths.Contains("notAutoIncluded");
            Assert.That(
                notAutoIncludedInGroup,
                Is.EqualTo(expectNotAutoIncludedInGroup),
                () =>
                    $"Mode={mode}, RowCount={rowCount}: notAutoIncluded expected in groups: {expectNotAutoIncludedInGroup}, actual: {notAutoIncludedInGroup}.\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        /// <summary>
        /// Test cases for auto-include behavior using WGroupLayoutTestTarget.
        /// This target has multiple groups with explicit [WGroup] attributes.
        /// Auto-include only affects the single unattributed field: ungroupedField.
        ///
        /// Field layout:
        /// - fieldA1, fieldA2: explicit Group A
        /// - fieldB1, fieldB2: explicit Group B
        /// - ungroupedField: NO attribute (can be auto-included)
        /// - fieldC1: explicit Group C
        ///
        /// In infinite mode, the last active group (Group B) should capture ungroupedField.
        /// </summary>
        private static IEnumerable<TestCaseData> MultiGroupAutoIncludeTestCases()
        {
            // None mode: ungroupedField stays ungrouped
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0,
                false, // ungroupedField NOT in any group
                2, // Group B has 2 properties: fieldB1, fieldB2
                "Group B"
            ).SetName("MultiGroup.None.UngroupedStaysUngrouped");

            // Infinite mode: ungroupedField gets captured by the last active group (Group B)
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Infinite,
                0,
                true, // ungroupedField IS in a group
                3, // Group B has 3 properties: fieldB1, fieldB2, ungroupedField
                "Group B"
            ).SetName("MultiGroup.Infinite.UngroupedCapturedByLastActiveGroup");

            // Finite(1) mode: Group B can capture 1 extra field
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                1,
                true, // ungroupedField IS in a group (captured by Group B)
                3, // Group B has 3 properties: fieldB1, fieldB2, ungroupedField
                "Group B"
            ).SetName("MultiGroup.Finite1.UngroupedCapturedByGroupB");
        }

        [Test]
        [TestCaseSource(nameof(MultiGroupAutoIncludeTestCases))]
        public void AutoIncludeModeWithMultipleGroups(
            UnityHelpersSettings.WGroupAutoIncludeMode mode,
            int rowCount,
            bool expectUngroupedInAnyGroup,
            int expectedGroupBPropertyCount,
            string expectedCapturingGroup
        )
        {
            // Arrange
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(mode, rowCount);
            WGroupLayoutBuilder.ClearCache();

            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            // Act
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Assert: Check if ungroupedField is in any group
            bool ungroupedFieldInGroups = layout.GroupedPaths.Contains("ungroupedField");
            Assert.That(
                ungroupedFieldInGroups,
                Is.EqualTo(expectUngroupedInAnyGroup),
                () =>
                    $"Mode={mode}, RowCount={rowCount}: ungroupedField expected in groups: {expectUngroupedInAnyGroup}, actual: {ungroupedFieldInGroups}.\n{FormatLayoutDiagnostics(layout)}"
            );

            // Assert: Check the expected capturing group's property count
            Assert.That(
                layout.TryGetGroup(expectedCapturingGroup, out WGroupDefinition group),
                Is.True,
                () => $"{expectedCapturingGroup} should exist.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                group.PropertyPaths,
                Has.Count.EqualTo(expectedGroupBPropertyCount),
                () =>
                    $"Mode={mode}, RowCount={rowCount}: {expectedCapturingGroup} expected {expectedGroupBPropertyCount} properties but has {group.PropertyPaths.Count}: [{string.Join(", ", group.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );

            // If ungroupedField should be in a group, verify it's in the expected one
            if (expectUngroupedInAnyGroup)
            {
                Assert.That(
                    group.PropertyPaths,
                    Contains.Item("ungroupedField"),
                    () =>
                        $"Mode={mode}: ungroupedField expected in {expectedCapturingGroup}.\n{FormatLayoutDiagnostics(layout)}"
                );
            }
        }

        /// <summary>
        /// Test cases for explicit autoIncludeCount on the attribute.
        /// When an attribute specifies an explicit count, it should override global settings entirely.
        ///
        /// Each case specifies: global mode, global row count, expected property count for the group.
        /// The explicit count is always 2, so regardless of global settings, exactly 2 additional
        /// fields should be captured.
        /// </summary>
        private static IEnumerable<TestCaseData> ExplicitAutoIncludeCountTestCases()
        {
            // Even with global None mode, explicit count should capture 2 fields
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0,
                3, // Explicit Group: explicitGroupFirst, captured1, captured2
                new[] { "explicitGroupFirst", "captured1", "captured2" }
            ).SetName("ExplicitCount.OverridesGlobalNone");

            // Even with global Infinite mode, explicit count should only capture 2 fields
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Infinite,
                0,
                3, // Explicit Group: explicitGroupFirst, captured1, captured2 (not notCaptured)
                new[] { "explicitGroupFirst", "captured1", "captured2" }
            ).SetName("ExplicitCount.OverridesGlobalInfinite");

            // Even with global Finite(5) mode, explicit count should only capture 2 fields
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                5,
                3, // Explicit Group: explicitGroupFirst, captured1, captured2
                new[] { "explicitGroupFirst", "captured1", "captured2" }
            ).SetName("ExplicitCount.OverridesGlobalFiniteHigher");

            // Even with global Finite(1) mode, explicit count should capture 2 fields
            yield return new TestCaseData(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                1,
                3, // Explicit Group: explicitGroupFirst, captured1, captured2
                new[] { "explicitGroupFirst", "captured1", "captured2" }
            ).SetName("ExplicitCount.OverridesGlobalFiniteLower");
        }

        [Test]
        [TestCaseSource(nameof(ExplicitAutoIncludeCountTestCases))]
        public void ExplicitAutoIncludeCountOverridesGlobalSettings(
            UnityHelpersSettings.WGroupAutoIncludeMode mode,
            int rowCount,
            int expectedGroupPropertyCount,
            string[] expectedProperties
        )
        {
            // Arrange: Set global configuration (should be ignored due to explicit count)
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(mode, rowCount);
            WGroupLayoutBuilder.ClearCache();

            WGroupExplicitAutoIncludeTestTarget target =
                CreateScriptableObject<WGroupExplicitAutoIncludeTestTarget>();
            using SerializedObject serializedObject = new(target);

            // Act
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Assert: Check group exists
            Assert.That(
                layout.TryGetGroup("Explicit Group", out WGroupDefinition group),
                Is.True,
                () => $"Explicit Group should exist.\n{FormatLayoutDiagnostics(layout)}"
            );

            // Assert: Check property count
            Assert.That(
                group.PropertyPaths,
                Has.Count.EqualTo(expectedGroupPropertyCount),
                () =>
                    $"Mode={mode}, RowCount={rowCount}: Explicit Group expected {expectedGroupPropertyCount} properties but has {group.PropertyPaths.Count}: [{string.Join(", ", group.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );

            // Assert: Check specific properties are included
            foreach (string expectedProperty in expectedProperties)
            {
                Assert.That(
                    group.PropertyPaths,
                    Contains.Item(expectedProperty),
                    () =>
                        $"Explicit Group should contain '{expectedProperty}'.\n{FormatLayoutDiagnostics(layout)}"
                );
            }

            // Assert: notCaptured should NOT be in any group
            Assert.That(
                layout.GroupedPaths.Contains("notCaptured"),
                Is.False,
                () =>
                    $"notCaptured should NOT be in any group with explicit count=2.\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        /// <summary>
        /// Tests that explicit InfiniteAutoInclude (-1) on an attribute captures all subsequent fields.
        /// </summary>
        [Test]
        public void ExplicitInfiniteAutoIncludeCapturesAllSubsequent()
        {
            // Arrange: Set global to None (should be ignored due to explicit infinite)
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );
            WGroupLayoutBuilder.ClearCache();

            WGroupInfiniteAutoIncludeTestTarget target =
                CreateScriptableObject<WGroupInfiniteAutoIncludeTestTarget>();
            using SerializedObject serializedObject = new(target);

            // Act
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Assert: Check group exists and has all 4 fields
            Assert.That(
                layout.TryGetGroup("Infinite Group", out WGroupDefinition group),
                Is.True,
                () => $"Infinite Group should exist.\n{FormatLayoutDiagnostics(layout)}"
            );

            Assert.That(
                group.PropertyPaths,
                Has.Count.EqualTo(4),
                () =>
                    $"Infinite Group should have all 4 fields but has {group.PropertyPaths.Count}: [{string.Join(", ", group.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );

            // Verify all expected properties
            string[] expectedProperties =
            {
                "infiniteGroupFirst",
                "capturedA",
                "capturedB",
                "capturedC",
            };
            foreach (string expectedProperty in expectedProperties)
            {
                Assert.That(
                    group.PropertyPaths,
                    Contains.Item(expectedProperty),
                    () =>
                        $"Infinite Group should contain '{expectedProperty}'.\n{FormatLayoutDiagnostics(layout)}"
                );
            }
        }

        /// <summary>
        /// Tests that explicit autoIncludeCount: 0 captures no subsequent fields.
        /// </summary>
        [Test]
        public void ExplicitZeroAutoIncludeCapturesNoSubsequent()
        {
            // Arrange: Set global to Infinite (should be ignored due to explicit zero)
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.Infinite,
                0
            );
            WGroupLayoutBuilder.ClearCache();

            WGroupZeroAutoIncludeTestTarget target =
                CreateScriptableObject<WGroupZeroAutoIncludeTestTarget>();
            using SerializedObject serializedObject = new(target);

            // Act
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Assert: Check group exists and has only the explicit field
            Assert.That(
                layout.TryGetGroup("Zero Group", out WGroupDefinition group),
                Is.True,
                () => $"Zero Group should exist.\n{FormatLayoutDiagnostics(layout)}"
            );

            Assert.That(
                group.PropertyPaths,
                Has.Count.EqualTo(1),
                () =>
                    $"Zero Group should have only 1 field but has {group.PropertyPaths.Count}: [{string.Join(", ", group.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );

            Assert.That(
                group.PropertyPaths,
                Contains.Item("zeroGroupFirst"),
                () =>
                    $"Zero Group should contain 'zeroGroupFirst'.\n{FormatLayoutDiagnostics(layout)}"
            );

            // Verify subsequent fields are NOT in any group
            Assert.That(
                layout.GroupedPaths.Contains("notCaptured1"),
                Is.False,
                () => $"notCaptured1 should NOT be in any group.\n{FormatLayoutDiagnostics(layout)}"
            );
            Assert.That(
                layout.GroupedPaths.Contains("notCaptured2"),
                Is.False,
                () => $"notCaptured2 should NOT be in any group.\n{FormatLayoutDiagnostics(layout)}"
            );
        }

        /// <summary>
        /// Tests that Finite mode with rowCount 0 behaves like None mode.
        /// </summary>
        [Test]
        public void FiniteModeWithZeroRowCountBehavesLikeNone()
        {
            // Arrange
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                0
            );
            WGroupLayoutBuilder.ClearCache();

            WGroupAutoIncludeTestTarget target =
                CreateScriptableObject<WGroupAutoIncludeTestTarget>();
            using SerializedObject serializedObject = new(target);

            // Act
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Assert: Should behave like None - only the explicit field
            Assert.That(
                layout.TryGetGroup("Auto Group", out WGroupDefinition group),
                Is.True,
                () => $"Auto Group should exist.\n{FormatLayoutDiagnostics(layout)}"
            );

            Assert.That(
                group.PropertyPaths,
                Has.Count.EqualTo(1),
                () =>
                    $"Finite(0) should behave like None - only 1 field expected but has {group.PropertyPaths.Count}: [{string.Join(", ", group.PropertyPaths)}].\n{FormatLayoutDiagnostics(layout)}"
            );
        }
    }
}
#endif
