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
        private bool _previousWGroupStartCollapsed;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            WGroupLayoutBuilder.ClearCache();
            _previousConfiguration = UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();
            _previousWGroupStartCollapsed = UnityHelpersSettings
                .instance
                .WGroupFoldoutsStartCollapsed;
        }

        [TearDown]
        public override void TearDown()
        {
            WGroupLayoutBuilder.ClearCache();
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                _previousConfiguration.Mode,
                _previousConfiguration.RowCount
            );
            UnityHelpersSettings.instance.WGroupFoldoutsStartCollapsed =
                _previousWGroupStartCollapsed;
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
            using SerializedObject serializedObject = new SerializedObject(asset);
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
            using SerializedObject serializedObject = new SerializedObject(asset);
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
            using SerializedObject serializedObject = new SerializedObject(asset);
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
            using SerializedObject serializedObject = new SerializedObject(asset);
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
            using SerializedObject serializedObject = new SerializedObject(asset);
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
        public void CollapsibleGroupsHonorSettingsDefaultWhenNotExplicit()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            settings.WGroupFoldoutsStartCollapsed = true;

            CollapsibleDefaultAsset collapsedAsset =
                CreateScriptableObject<CollapsibleDefaultAsset>();
            using SerializedObject collapsedSerialized = new SerializedObject(collapsedAsset);
            collapsedSerialized.Update();

            SerializedProperty collapsedScript = collapsedSerialized.FindProperty("m_Script");
            string collapsedScriptPath =
                collapsedScript != null ? collapsedScript.propertyPath : null;

            WGroupLayout collapsedLayout = WGroupLayoutBuilder.Build(
                collapsedSerialized,
                collapsedScriptPath
            );
            Assert.IsTrue(
                collapsedLayout.TryGetGroup(
                    "DefaultGroup",
                    out WGroupDefinition collapsedDefinition
                ),
                "Failed to find DefaultGroup in layout"
            );
            Assert.That(
                collapsedDefinition.StartCollapsed,
                Is.True,
                $"Expected StartCollapsed=true when WGroupFoldoutsStartCollapsed=true. Actual StartCollapsed={collapsedDefinition.StartCollapsed}, Settings.WGroupFoldoutsStartCollapsed={settings.WGroupFoldoutsStartCollapsed}"
            );

            settings.WGroupFoldoutsStartCollapsed = false;
            CollapsibleDefaultAsset expandedAsset =
                CreateScriptableObject<CollapsibleDefaultAsset>();
            using SerializedObject expandedSerialized = new SerializedObject(expandedAsset);
            expandedSerialized.Update();

            SerializedProperty expandedScript = expandedSerialized.FindProperty("m_Script");
            string expandedScriptPath = expandedScript != null ? expandedScript.propertyPath : null;

            WGroupLayout expandedLayout = WGroupLayoutBuilder.Build(
                expandedSerialized,
                expandedScriptPath
            );
            Assert.IsTrue(
                expandedLayout.TryGetGroup("DefaultGroup", out WGroupDefinition expandedDefinition),
                "Failed to find DefaultGroup in layout after settings change"
            );
            Assert.That(
                expandedDefinition.StartCollapsed,
                Is.False,
                $"Expected StartCollapsed=false when WGroupFoldoutsStartCollapsed=false. Actual StartCollapsed={expandedDefinition.StartCollapsed}, Settings.WGroupFoldoutsStartCollapsed={settings.WGroupFoldoutsStartCollapsed}"
            );
        }

        [Test]
        public void ExplicitStartCollapsedOverridesSettingsDefault()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            settings.WGroupFoldoutsStartCollapsed = false;

            CollapsibleExplicitCollapsedAsset asset =
                CreateScriptableObject<CollapsibleExplicitCollapsedAsset>();
            using SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(
                layout.TryGetGroup("ExplicitCollapsed", out WGroupDefinition explicitDefinition)
            );
            Assert.That(explicitDefinition.StartCollapsed, Is.True);
        }

        [Test]
        public void ExplicitStartExpandedOverridesSettingsDefault()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            settings.WGroupFoldoutsStartCollapsed = true;

            CollapsibleExplicitExpandedAsset asset =
                CreateScriptableObject<CollapsibleExplicitExpandedAsset>();
            using SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = scriptProperty != null ? scriptProperty.propertyPath : null;

            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPath);
            Assert.IsTrue(
                layout.TryGetGroup("ExplicitExpanded", out WGroupDefinition explicitDefinition)
            );
            Assert.That(explicitDefinition.StartCollapsed, Is.False);
        }

        [Test]
        public void SettingsChangeInvalidatesCacheAndReflectsNewDefault()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            settings.WGroupFoldoutsStartCollapsed = true;

            CollapsibleDefaultAsset asset1 = CreateScriptableObject<CollapsibleDefaultAsset>();
            using SerializedObject serialized1 = new SerializedObject(asset1);
            serialized1.Update();

            SerializedProperty script1 = serialized1.FindProperty("m_Script");
            string scriptPath1 = script1 != null ? script1.propertyPath : null;

            WGroupLayout layout1 = WGroupLayoutBuilder.Build(serialized1, scriptPath1);
            Assert.IsTrue(
                layout1.TryGetGroup("DefaultGroup", out WGroupDefinition definition1),
                "First layout should contain DefaultGroup"
            );
            Assert.That(
                definition1.StartCollapsed,
                Is.True,
                "First layout should have StartCollapsed=true"
            );

            settings.WGroupFoldoutsStartCollapsed = false;

            CollapsibleDefaultAsset asset2 = CreateScriptableObject<CollapsibleDefaultAsset>();
            using SerializedObject serialized2 = new SerializedObject(asset2);
            serialized2.Update();

            SerializedProperty script2 = serialized2.FindProperty("m_Script");
            string scriptPath2 = script2 != null ? script2.propertyPath : null;

            WGroupLayout layout2 = WGroupLayoutBuilder.Build(serialized2, scriptPath2);
            Assert.IsTrue(
                layout2.TryGetGroup("DefaultGroup", out WGroupDefinition definition2),
                "Second layout should contain DefaultGroup after cache invalidation"
            );
            Assert.That(
                definition2.StartCollapsed,
                Is.False,
                $"Second layout should have StartCollapsed=false after settings change to false. Actual={definition2.StartCollapsed}"
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
            using SerializedObject serializedObject = new SerializedObject(asset);
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

        private sealed class CollapsibleDefaultAsset : ScriptableObject
        {
            [WGroup("DefaultGroup", collapsible: true)]
            public int first;
        }

        private sealed class CollapsibleExplicitCollapsedAsset : ScriptableObject
        {
            [WGroup("ExplicitCollapsed", collapsible: true, startCollapsed: true)]
            public int first;
        }

        private sealed class CollapsibleExplicitExpandedAsset : ScriptableObject
        {
            [WGroup(
                "ExplicitExpanded",
                collapsible: true,
                CollapseBehavior = WGroupAttribute.WGroupCollapseBehavior.ForceExpanded
            )]
            public int first;
        }

        private sealed class ColorKeyAsset : ScriptableObject
        {
            [WGroup("PaletteGroup", colorKey: "TestPalette_WGroup", hideHeader: true)]
            public float value;
        }
    }
}
#endif
