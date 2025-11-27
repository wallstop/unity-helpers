namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Attributes;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    [TestFixture]
    public sealed class ReflectionHelpersTypeScanningTests : CommonTestBase
    {
        [Test]
        public void GetAllLoadedTypesIncludesTestAssemblyType()
        {
            IEnumerable<Type> types = ReflectionHelpers.GetAllLoadedTypes();
            bool found = types.Any(t => t == typeof(ReflectionHelpersTypeScanningTests));
            Assert.IsTrue(found, "Expected test type to be present in loaded types.");
        }

        [Test]
        public void GetTypesDerivedFromComponentIncludesTester()
        {
            IEnumerable<Type> types = ReflectionHelpers.GetTypesDerivedFrom<Component>(
                includeAbstract: false
            );
            bool found = types.Any(t => t == typeof(PrewarmTesterComponent));
            Assert.IsTrue(found, "Expected PrewarmTesterComponent to be found as a Component.");
        }

        [Test]
        public void TryResolveTypeFindsAssemblyQualifiedName()
        {
            string aqn = typeof(PrewarmTesterComponent).AssemblyQualifiedName;
            Type t = ReflectionHelpers.TryResolveType(aqn);
            Assert.IsNotNull(t, "Resolution by assembly qualified name returned null.");
            Assert.AreEqual(typeof(PrewarmTesterComponent), t);
        }

        [Test]
        public void TryResolveTypeFindsNonQualifiedName()
        {
            string fullName = typeof(PrewarmTesterComponent).FullName;
            Type t = ReflectionHelpers.TryResolveType(fullName);
            Assert.IsNotNull(t, "Resolution by full name returned null.");
            Assert.AreEqual(typeof(PrewarmTesterComponent), t);
        }

        [Test]
        public void GetTypesFromAssemblyNullReturnsEmpty()
        {
            Type[] types = ReflectionHelpers.GetTypesFromAssembly(null);
            Assert.IsNotNull(types);
            Assert.AreEqual(0, types.Length, "Expected empty array for null assembly.");
        }
    }
}
