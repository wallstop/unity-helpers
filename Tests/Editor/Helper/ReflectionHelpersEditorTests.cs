namespace WallstopStudios.UnityHelpers.Tests.Editor.Helper
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Attributes;

    [AttributeUsage(
        AttributeTargets.Class
            | AttributeTargets.Field
            | AttributeTargets.Method
            | AttributeTargets.Property
    )]
    public sealed class EditorMarkerAttribute : Attribute { }

    [EditorMarker]
    public sealed class EditorMarkerTarget
    {
        [EditorMarker]
        public int markedField;

        public EditorMarkerTarget(int markedField)
        {
            this.markedField = markedField;
        }

        [EditorMarker]
        public int MarkedProperty { get; set; }

        [EditorMarker]
        public void MarkedMethod() { }
    }

    [TestFixture]
    public sealed class ReflectionHelpersEditorTests
    {
        [Test]
        public void EditorGetTypesDerivedFromUsesTypeCacheFallback()
        {
            IEnumerable<Type> derived = ReflectionHelpers.GetTypesDerivedFrom(
                typeof(Component),
                includeAbstract: false
            );
            bool found = derived.Any(t => t == typeof(PrewarmTesterComponent));
            Assert.IsTrue(
                found,
                "Expected PrewarmTesterComponent in editor type cache derived set."
            );
        }

        [Test]
        public void EditorGetTypesWithAttributeUsesTypeCache()
        {
            var types = ReflectionHelpers.GetTypesWithAttribute<EditorMarkerAttribute>(
                includeAbstract: false
            );
            Assert.IsTrue(
                types.Any(t => t == typeof(EditorMarkerTarget)),
                "Expected EditorMarkerTarget discovered via TypeCache."
            );
        }

        [Test]
        public void EditorGetMethodsAndFieldsWithAttributeUseTypeCache()
        {
            var methods = ReflectionHelpers.GetMethodsWithAttribute<EditorMarkerAttribute>(
                typeof(EditorMarkerTarget)
            );
            Assert.IsTrue(
                methods.Any(m => m.Name == nameof(EditorMarkerTarget.MarkedMethod)),
                "Expected MarkedMethod discovered via TypeCache."
            );

            var fields = ReflectionHelpers.GetFieldsWithAttribute<EditorMarkerAttribute>(
                typeof(EditorMarkerTarget)
            );
            Assert.IsTrue(
                fields.Any(f => f.Name == nameof(EditorMarkerTarget.markedField)),
                "Expected MarkedField discovered via TypeCache."
            );
        }

        [Test]
        public void EditorGetPropertiesWithAttributeFallsBackToReflection()
        {
            var props = ReflectionHelpers.GetPropertiesWithAttribute<EditorMarkerAttribute>(
                typeof(EditorMarkerTarget)
            );
            Assert.IsTrue(
                props.Any(p => p.Name == nameof(EditorMarkerTarget.MarkedProperty)),
                "Expected MarkedProperty discovered via reflection fallback."
            );
        }
    }
#endif
}
