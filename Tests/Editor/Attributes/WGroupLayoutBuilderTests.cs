#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    [TestFixture]
    public sealed class WGroupLayoutBuilderTests : CommonTestBase
    {
        private UnityHelpersSettings.WGroupAutoIncludeConfiguration _previousConfiguration;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _previousConfiguration = UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();
        }

        [TearDown]
        public override void TearDown()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                _previousConfiguration.Mode,
                _previousConfiguration.RowCount
            );
            base.TearDown();
        }

        [Test]
        public void FiniteAutoIncludeCapturesConfiguredCount()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                2
            );

            FiniteGroupAsset asset = CreateScriptableObject<FiniteGroupAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(layout.TryGetGroup("Stats", out WGroupDefinition statsGroup));

            Assert.That(
                statsGroup.PropertyPaths,
                Is.EqualTo(
                    new[]
                    {
                        nameof(FiniteGroupAsset.primary),
                        nameof(FiniteGroupAsset.secondary),
                        nameof(FiniteGroupAsset.tertiary),
                    }
                )
            );
            Assert.That(statsGroup.Collapsible, Is.False);
            Assert.That(statsGroup.HideHeader, Is.False);
        }

        [Test]
        public void InfiniteAutoIncludeTerminatesAtEndAttribute()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );

            InfiniteGroupAsset asset = CreateScriptableObject<InfiniteGroupAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(layout.TryGetGroup("Stream", out WGroupDefinition streamGroup));

            Assert.That(
                streamGroup.PropertyPaths,
                Is.EqualTo(
                    new[]
                    {
                        nameof(InfiniteGroupAsset.start),
                        nameof(InfiniteGroupAsset.mid),
                        nameof(InfiniteGroupAsset.terminator),
                    }
                )
            );
        }

        [Test]
        public void NamedGroupEndStopsSpecifiedGroup()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );

            NamedEndGroupAsset asset = CreateScriptableObject<NamedEndGroupAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);

            Assert.IsTrue(layout.TryGetGroup("Alpha", out WGroupDefinition alphaGroup));
            Assert.That(
                alphaGroup.PropertyPaths,
                Is.EqualTo(
                    new[]
                    {
                        nameof(NamedEndGroupAsset.alphaStart),
                        nameof(NamedEndGroupAsset.alphaMid),
                        nameof(NamedEndGroupAsset.alphaStop),
                    }
                )
            );

            Assert.IsTrue(layout.TryGetGroup("Beta", out WGroupDefinition betaGroup));
            Assert.That(
                betaGroup.PropertyPaths,
                Is.EqualTo(
                    new[]
                    {
                        nameof(NamedEndGroupAsset.betaStart),
                        nameof(NamedEndGroupAsset.betaTail),
                    }
                )
            );
        }

        [Test]
        public void MultipleDeclarationsAnchorToFirstOccurrence()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );

            MultipleSegmentAsset asset = CreateScriptableObject<MultipleSegmentAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);

            Assert.IsTrue(layout.TryGetGroup("Segments", out WGroupDefinition definition));
            Assert.That(
                definition.AnchorPropertyPath,
                Is.EqualTo(nameof(MultipleSegmentAsset.first))
            );
            Assert.That(
                definition.PropertyPaths,
                Is.EqualTo(
                    new[]
                    {
                        nameof(MultipleSegmentAsset.first),
                        nameof(MultipleSegmentAsset.second),
                        nameof(MultipleSegmentAsset.third),
                    }
                )
            );

            IReadOnlyList<WGroupDrawOperation> operations = layout.Operations;
            Assert.That(operations.Count, Is.EqualTo(2));
            Assert.That(operations[0].Type, Is.EqualTo(WGroupDrawOperationType.Group));
            Assert.That(operations[1].Type, Is.EqualTo(WGroupDrawOperationType.Property));
            Assert.That(
                operations[1].PropertyPath,
                Is.EqualTo(nameof(MultipleSegmentAsset.outside))
            );
        }

        [Test]
        public void CollapsibleMetadataIsPreserved()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );

            CollapsibleAsset asset = CreateScriptableObject<CollapsibleAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(layout.TryGetGroup("ToggleGroup", out WGroupDefinition definition));
            Assert.That(definition.Collapsible, Is.True);
            Assert.That(definition.StartCollapsed, Is.True);
            Assert.That(
                definition.PropertyPaths,
                Is.EqualTo(
                    new[] { nameof(CollapsibleAsset.first), nameof(CollapsibleAsset.second) }
                )
            );
        }

        [Test]
        public void ColorKeyRegistrationUsesPalette()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );

            ColorKeyAsset asset = CreateScriptableObject<ColorKeyAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(layout.TryGetGroup("PaletteGroup", out WGroupDefinition definition));

            Assert.That(definition.HideHeader, Is.True);
            Assert.That(definition.Collapsible, Is.False);

            Assert.That(definition.ColorKey, Is.EqualTo("TestPalette_WGroup"));

            UnityHelpersSettings.WGroupPaletteEntry entry =
                UnityHelpersSettings.ResolveWGroupPalette(definition.ColorKey);
            Assert.That(entry.BackgroundColor.a, Is.GreaterThan(0f));
            Assert.IsTrue(UnityHelpersSettings.HasWGroupPaletteColorKey(definition.ColorKey));
        }

        [Test]
        public void FoldoutGroupProducesDefinitionAndOperation()
        {
            FoldoutGroupAsset asset = CreateScriptableObject<FoldoutGroupAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(
                layout.TryGetFoldoutGroup("Details", out WFoldoutGroupDefinition foldoutGroup)
            );

            Assert.That(
                foldoutGroup.PropertyPaths,
                Is.EqualTo(
                    new[] { nameof(FoldoutGroupAsset.health), nameof(FoldoutGroupAsset.mana) }
                )
            );
            Assert.IsTrue(foldoutGroup.StartCollapsed);

            IReadOnlyList<WGroupDrawOperation> operations = layout.Operations;
            Assert.That(operations[0].Type, Is.EqualTo(WGroupDrawOperationType.FoldoutGroup));
            Assert.AreSame(foldoutGroup, operations[0].FoldoutGroup);
            Assert.That(operations[1].Type, Is.EqualTo(WGroupDrawOperationType.Property));
            Assert.That(operations[1].PropertyPath, Is.EqualTo(nameof(FoldoutGroupAsset.stamina)));
        }

        [Test]
        public void FoldoutAutoIncludeRespectsFiniteBudget()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.Infinite,
                0
            );

            FoldoutFiniteAsset asset = CreateScriptableObject<FoldoutFiniteAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(layout.TryGetFoldoutGroup("Stats", out WFoldoutGroupDefinition foldout));

            Assert.That(
                foldout.PropertyPaths,
                Is.EqualTo(
                    new[]
                    {
                        nameof(FoldoutFiniteAsset.primary),
                        nameof(FoldoutFiniteAsset.secondary),
                    }
                )
            );

            IReadOnlyList<WGroupDrawOperation> operations = layout.Operations;
            Assert.That(operations[0].Type, Is.EqualTo(WGroupDrawOperationType.FoldoutGroup));
            Assert.That(operations[1].PropertyPath, Is.EqualTo(nameof(FoldoutFiniteAsset.outside)));
        }

        [Test]
        public void FoldoutHideHeaderMetadataIsPreserved()
        {
            HiddenHeaderFoldoutAsset asset = CreateScriptableObject<HiddenHeaderFoldoutAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(layout.TryGetFoldoutGroup("Hidden", out WFoldoutGroupDefinition foldout));
            Assert.IsTrue(foldout.HideHeader);
            Assert.IsFalse(foldout.StartCollapsed);
        }

        [Test]
        public void MixedGroupAndFoldoutOperationsMaintainOrder()
        {
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.Finite,
                1
            );

            MixedGroupFoldoutAsset asset = CreateScriptableObject<MixedGroupFoldoutAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            IReadOnlyList<WGroupDrawOperation> operations = layout.Operations;

            Assert.That(operations[0].Type, Is.EqualTo(WGroupDrawOperationType.Group));
            Assert.That(
                operations[0].Group.PropertyPaths,
                Is.EqualTo(
                    new[]
                    {
                        nameof(MixedGroupFoldoutAsset.grouped),
                        nameof(MixedGroupFoldoutAsset.groupMember),
                    }
                )
            );

            Assert.That(operations[1].Type, Is.EqualTo(WGroupDrawOperationType.FoldoutGroup));
            Assert.That(
                operations[1].FoldoutGroup.PropertyPaths,
                Is.EqualTo(new[] { nameof(MixedGroupFoldoutAsset.foldoutValue) })
            );

            Assert.That(
                operations[2].PropertyPath,
                Is.EqualTo(nameof(MixedGroupFoldoutAsset.trailing))
            );
        }

        private sealed class FiniteGroupAsset : ScriptableObject
        {
            [WGroup("Stats")]
            public int primary;

            public int secondary;

            public int tertiary;

            public int outside;
        }

        private sealed class InfiniteGroupAsset : ScriptableObject
        {
            [WGroup("Stream", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
            public string start;

            public string mid;

            [WGroupEnd]
            public string terminator;

            public string trailing;
        }

        private sealed class FoldoutGroupAsset : ScriptableObject
        {
            [WFoldoutGroup("Details", autoIncludeCount: 1, startCollapsed: true)]
            public int health;

            public int mana;

            public int stamina;
        }

        private sealed class FoldoutFiniteAsset : ScriptableObject
        {
            [WFoldoutGroup("Stats", autoIncludeCount: 1)]
            public int primary;

            public int secondary;

            public int outside;
        }

        private sealed class HiddenHeaderFoldoutAsset : ScriptableObject
        {
            [WFoldoutGroup("Hidden", autoIncludeCount: 0, hideHeader: true, startCollapsed: false)]
            public float value;
        }

        private sealed class MixedGroupFoldoutAsset : ScriptableObject
        {
            [WGroup("Config", autoIncludeCount: 1, collapsible: true)]
            public int grouped;

            public int groupMember;

            [WFoldoutGroup("Advanced", autoIncludeCount: 0)]
            public float foldoutValue;

            public float trailing;
        }

        private sealed class NamedEndGroupAsset : ScriptableObject
        {
            [WGroup("Alpha", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
            public int alphaStart;

            public int alphaMid;

            [WGroup("Beta", autoIncludeCount: 1)]
            public int betaStart;

            public int betaTail;

            [WGroupEnd("Alpha")]
            public int alphaStop;

            public int alphaOutside;
        }

        private sealed class MultipleSegmentAsset : ScriptableObject
        {
            [WGroup("Segments", autoIncludeCount: 0)]
            public int first;

            public int outside;

            [WGroup("Segments", autoIncludeCount: 1)]
            public int second;

            public int third;
        }

        private sealed class CollapsibleAsset : ScriptableObject
        {
            [WGroup("ToggleGroup", autoIncludeCount: 1, collapsible: true, startCollapsed: true)]
            public int first;

            public int second;
        }

        private sealed class ColorKeyAsset : ScriptableObject
        {
            [WGroup("PaletteGroup", colorKey: "TestPalette_WGroup", hideHeader: true)]
            public float value;
        }
    }
}
#endif
