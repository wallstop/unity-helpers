#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Editor.Helper
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class TypedHelperEditorTarget
    {
        public int Field;
        public int Property { get; set; }
    }

    [TestFixture]
    public sealed class ReflectionHelpersTypedEditorTests
    {
        [Test]
        public void EditorTypedFieldSetterFallbacksWhenCapabilitiesDisabled()
        {
            FieldInfo field = typeof(TypedHelperEditorTarget).GetField(
                nameof(TypedHelperEditorTarget.Field)
            );
            using (
                ReflectionHelpers.OverrideReflectionCapabilities(
                    expressions: false,
                    dynamicIl: false
                )
            )
            {
                FieldSetter<TypedHelperEditorTarget, int> setter = ReflectionHelpers.GetFieldSetter<
                    TypedHelperEditorTarget,
                    int
                >(field);
                TypedHelperEditorTarget instance = new();
                setter(ref instance, 12);
                Assert.AreEqual(12, instance.Field);
            }
        }

        [Test]
        public void EditorTypedPropertySetterFallbacksWhenCapabilitiesDisabled()
        {
            PropertyInfo property = typeof(TypedHelperEditorTarget).GetProperty(
                nameof(TypedHelperEditorTarget.Property)
            );
            using (
                ReflectionHelpers.OverrideReflectionCapabilities(
                    expressions: false,
                    dynamicIl: false
                )
            )
            {
                Action<TypedHelperEditorTarget, int> setter = ReflectionHelpers.GetPropertySetter<
                    TypedHelperEditorTarget,
                    int
                >(property);
                TypedHelperEditorTarget instance = new();
                setter(instance, 34);
                Assert.AreEqual(34, instance.Property);
            }
        }

        [Test]
        public void EditorTypedPropertyGetterFallbacksWhenCapabilitiesDisabled()
        {
            PropertyInfo property = typeof(TypedHelperEditorTarget).GetProperty(
                nameof(TypedHelperEditorTarget.Property)
            );
            TypedHelperEditorTarget instance = new() { Property = 56 };
            using (
                ReflectionHelpers.OverrideReflectionCapabilities(
                    expressions: false,
                    dynamicIl: false
                )
            )
            {
                Func<TypedHelperEditorTarget, int> getter = ReflectionHelpers.GetPropertyGetter<
                    TypedHelperEditorTarget,
                    int
                >(property);
                Assert.AreEqual(56, getter(instance));
            }
        }
    }
}
#endif
