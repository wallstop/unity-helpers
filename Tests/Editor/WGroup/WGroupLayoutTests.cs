#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;
    using WallstopStudios.UnityHelpers.Tests.Core;

    /// <summary>
    /// Comprehensive tests for WGroup layout functionality.
    /// </summary>
    [TestFixture]
    public sealed class WGroupLayoutTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            WGroupLayoutBuilder.ClearCache();
        }

        [TearDown]
        public override void TearDown()
        {
            WGroupLayoutBuilder.ClearCache();
            base.TearDown();
        }

        [Test]
        public void LayoutBuildsCorrectNumberOfGroups()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // Should have 3 groups: A, B, C
            Assert.That(layout.Groups, Has.Count.EqualTo(3));
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
                "Group A should exist"
            );
            Assert.That(groupA.PropertyPaths, Has.Count.EqualTo(2));
            Assert.That(groupA.PropertyPaths, Contains.Item("fieldA1"));
            Assert.That(groupA.PropertyPaths, Contains.Item("fieldA2"));

            // Get Group B
            Assert.That(
                layout.TryGetGroup("Group B", out WGroupDefinition groupB),
                Is.True,
                "Group B should exist"
            );
            Assert.That(groupB.PropertyPaths, Has.Count.EqualTo(2));
            Assert.That(groupB.PropertyPaths, Contains.Item("fieldB1"));
            Assert.That(groupB.PropertyPaths, Contains.Item("fieldB2"));

            // Get Group C
            Assert.That(
                layout.TryGetGroup("Group C", out WGroupDefinition groupC),
                Is.True,
                "Group C should exist"
            );
            Assert.That(groupC.PropertyPaths, Has.Count.EqualTo(1));
            Assert.That(groupC.PropertyPaths, Contains.Item("fieldC1"));
        }

        [Test]
        public void DisplayNameIsResolvedCorrectly()
        {
            WGroupLayoutTestTarget target = CreateScriptableObject<WGroupLayoutTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            Assert.That(layout.TryGetGroup("Group A", out WGroupDefinition groupA), Is.True);
            Assert.That(groupA.DisplayName, Is.EqualTo("Alpha Group"));

            Assert.That(layout.TryGetGroup("Group B", out WGroupDefinition groupB), Is.True);
            Assert.That(groupB.DisplayName, Is.EqualTo("Beta Group"));

            // Group C has no display name, should use group name
            Assert.That(layout.TryGetGroup("Group C", out WGroupDefinition groupC), Is.True);
            Assert.That(groupC.DisplayName, Is.EqualTo("Group C"));
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
            Assert.That(layout.GroupedPaths, Does.Not.Contain("ungroupedField"));

            // But it should appear in operations
            bool foundInOperations = layout.Operations.Any(op =>
                op.Type == WGroupDrawOperationType.Property && op.PropertyPath == "ungroupedField"
            );
            Assert.That(foundInOperations, Is.True, "Ungrouped field should appear in operations");
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
            Assert.That(layout.Operations, Is.Not.Empty);

            // Count group operations
            int groupOperations = layout.Operations.Count(op =>
                op.Type == WGroupDrawOperationType.Group
            );
            Assert.That(groupOperations, Is.EqualTo(3), "Should have 3 group operations");

            // Should have at least one property operation (for ungroupedField)
            int propertyOperations = layout.Operations.Count(op =>
                op.Type == WGroupDrawOperationType.Property
            );
            Assert.That(propertyOperations, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void MultiplePropertiesWithSameGroupNameAreMerged()
        {
            WGroupDeclarationOrderTestTarget target =
                CreateScriptableObject<WGroupDeclarationOrderTestTarget>();
            using SerializedObject serializedObject = new(target);

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, "m_Script");

            // First group should have first1 and first2
            Assert.That(layout.TryGetGroup("First", out WGroupDefinition first), Is.True);
            Assert.That(first.PropertyPaths, Has.Count.EqualTo(2));
            Assert.That(first.PropertyPaths, Contains.Item("first1"));
            Assert.That(first.PropertyPaths, Contains.Item("first2"));

            // Second group should have second1 and second2
            Assert.That(layout.TryGetGroup("Second", out WGroupDefinition second), Is.True);
            Assert.That(second.PropertyPaths, Has.Count.EqualTo(2));
            Assert.That(second.PropertyPaths, Contains.Item("second1"));
            Assert.That(second.PropertyPaths, Contains.Item("second2"));
        }
    }
}
#endif
