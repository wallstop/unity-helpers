namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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
    public sealed class RuntimeMarkerAttribute : Attribute { }

    [RuntimeMarker]
    public sealed class RuntimeMarkerTarget
    {
        [RuntimeMarker]
        public int markedField;

        [RuntimeMarker]
        public int MarkedProperty { get; set; }

        [RuntimeMarker]
        public void MarkedMethod() { }

        public void Overload(int x) { }

        public void Overload(string s) { }
    }

    public sealed class RuntimeScriptableObjectTarget : ScriptableObject { }

    [TestFixture]
    public sealed class ReflectionHelpersMemberLookupTests
    {
        [Test]
        public void GetTypesWithAttributeRuntimeMarkerIncludesType()
        {
            IEnumerable<Type> types =
                ReflectionHelpers.GetTypesWithAttribute<RuntimeMarkerAttribute>(
                    includeAbstract: false
                );
            Assert.IsTrue(
                types.Any(t => t == typeof(RuntimeMarkerTarget)),
                "Expected RuntimeMarkerTarget to be discovered."
            );
        }

        [Test]
        public void GetFieldsWithAttributeWithinReturnsField()
        {
            IEnumerable<FieldInfo> fields =
                ReflectionHelpers.GetFieldsWithAttribute<RuntimeMarkerAttribute>(
                    typeof(RuntimeMarkerTarget)
                );
            FieldInfo fi = fields.FirstOrDefault(f =>
                f.Name == nameof(RuntimeMarkerTarget.markedField)
            );
            Assert.IsNotNull(fi, "Expected MarkedField to be discovered.");
        }

        [Test]
        public void GetMethodsWithAttributeWithinReturnsMethod()
        {
            IEnumerable<MethodInfo> methods =
                ReflectionHelpers.GetMethodsWithAttribute<RuntimeMarkerAttribute>(
                    typeof(RuntimeMarkerTarget)
                );
            MethodInfo mi = methods.FirstOrDefault(m =>
                m.Name == nameof(RuntimeMarkerTarget.MarkedMethod)
            );
            Assert.IsNotNull(mi, "Expected MarkedMethod to be discovered.");
        }

        [Test]
        public void GetPropertiesWithAttributeWithinReturnsProperty()
        {
            IEnumerable<PropertyInfo> props =
                ReflectionHelpers.GetPropertiesWithAttribute<RuntimeMarkerAttribute>(
                    typeof(RuntimeMarkerTarget)
                );
            PropertyInfo pi = props.FirstOrDefault(p =>
                p.Name == nameof(RuntimeMarkerTarget.MarkedProperty)
            );
            Assert.IsNotNull(pi, "Expected MarkedProperty to be discovered.");
        }

        [Test]
        public void TryGetFieldFindsFieldWithCache()
        {
            bool ok = ReflectionHelpers.TryGetField(
                typeof(RuntimeMarkerTarget),
                nameof(RuntimeMarkerTarget.markedField),
                out FieldInfo fi
            );
            Assert.IsTrue(ok, "TryGetField should succeed.");
            Assert.IsNotNull(fi);
            bool ok2 = ReflectionHelpers.TryGetField(
                typeof(RuntimeMarkerTarget),
                nameof(RuntimeMarkerTarget.markedField),
                out FieldInfo fi2
            );
            Assert.IsTrue(ok2);
            Assert.AreSame(fi, fi2, "Expected cached FieldInfo instance.");
        }

        [Test]
        public void TryGetPropertyFindsPropertyWithCache()
        {
            bool ok = ReflectionHelpers.TryGetProperty(
                typeof(RuntimeMarkerTarget),
                nameof(RuntimeMarkerTarget.MarkedProperty),
                out PropertyInfo pi
            );
            Assert.IsTrue(ok, "TryGetProperty should succeed.");
            Assert.IsNotNull(pi);
            bool ok2 = ReflectionHelpers.TryGetProperty(
                typeof(RuntimeMarkerTarget),
                nameof(RuntimeMarkerTarget.MarkedProperty),
                out PropertyInfo pi2
            );
            Assert.IsTrue(ok2);
            Assert.AreSame(pi, pi2, "Expected cached PropertyInfo instance.");
        }

        [Test]
        public void TryGetMethodFindsOverloadByParams()
        {
            bool okInt = ReflectionHelpers.TryGetMethod(
                typeof(RuntimeMarkerTarget),
                nameof(RuntimeMarkerTarget.Overload),
                out MethodInfo mInt,
                new[] { typeof(int) }
            );
            bool okStr = ReflectionHelpers.TryGetMethod(
                typeof(RuntimeMarkerTarget),
                nameof(RuntimeMarkerTarget.Overload),
                out MethodInfo mStr,
                new[] { typeof(string) }
            );
            Assert.IsTrue(okInt && okStr, "Expected both overloads to resolve.");
            Assert.AreNotSame(mInt, mStr, "Expected different overloads.");
        }

        [Test]
        public void GetComponentAndScriptableTypesIncludeTargets()
        {
            IEnumerable<Type> components = ReflectionHelpers.GetComponentTypes();
            bool hasTester = components.Any(t => t == typeof(PrewarmTesterComponent));
            Assert.IsTrue(hasTester, "Expected PrewarmTesterComponent among component types.");

            IEnumerable<Type> sos = ReflectionHelpers.GetScriptableObjectTypes();
            bool hasSO = sos.Any(t => t == typeof(RuntimeScriptableObjectTarget));
            Assert.IsTrue(
                hasSO,
                "Expected RuntimeScriptableObjectTarget among scriptable object types."
            );
        }

        [Test]
        public void GetTypesWithAttributeNonGenericFindsRuntimeMarker()
        {
            IEnumerable<Type> types = ReflectionHelpers.GetTypesWithAttribute(
                typeof(RuntimeMarkerAttribute)
            );
            Assert.IsTrue(
                types.Any(t => t == typeof(RuntimeMarkerTarget)),
                "Expected RuntimeMarkerTarget discovered via non-generic attribute query."
            );
        }

        [Test]
        public void TryGetFieldPropertyMethodReturnFalseForMissing()
        {
            Assert.IsFalse(
                ReflectionHelpers.TryGetField(typeof(RuntimeMarkerTarget), "Nope", out _)
            );
            Assert.IsFalse(
                ReflectionHelpers.TryGetProperty(typeof(RuntimeMarkerTarget), "Nope", out _)
            );
            Assert.IsFalse(
                ReflectionHelpers.TryGetMethod(typeof(RuntimeMarkerTarget), "Nope", out _)
            );
        }
    }
}
