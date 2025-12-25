namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class TestAssemblyHelperTests : CommonTestBase
    {
        [TestCase("MyGame.Tests", true)]
        [TestCase("Tests.Core", true)]
        [TestCase("Unit.Test", true)]
        [TestCase("IntegrationTestHelpers", true)]
        [TestCase("Game.Production", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void ContainsTestMarkerDetectsCommonPatterns(string value, bool expected)
        {
            Assert.AreEqual(expected, TestAssemblyHelper.ContainsTestMarker(value));
        }

        [Test]
        public void IsTestAssemblyDetectsTestAssemblies()
        {
            Assembly testAssembly = typeof(TestAssemblyHelperTests).Assembly;
            Assert.IsTrue(TestAssemblyHelper.IsTestAssembly(testAssembly));
        }

        [Test]
        public void IsTestAssemblyReturnsFalseForEngineAssemblies()
        {
            Assembly unityAssembly = typeof(GameObject).Assembly;
            Assert.IsFalse(TestAssemblyHelper.IsTestAssembly(unityAssembly));
        }

        [Test]
        public void IsTestTypeEvaluatesNamespaceMarkers()
        {
            Assert.IsTrue(TestAssemblyHelper.IsTestType(typeof(TestAssemblyHelperTests)));
            Assert.IsFalse(TestAssemblyHelper.IsTestType(typeof(Vector3)));
            Assert.IsTrue(TestAssemblyHelper.IsTestType(typeof(NamespaceTests.MarkerType)));
            Assert.IsTrue(TestAssemblyHelper.IsTestType(typeof(NamespaceWithoutMarker.PlainType)));
            Assert.IsTrue(TestAssemblyHelper.IsTestType(typeof(GlobalPlainType)));
            Assert.IsTrue(TestAssemblyHelper.IsTestType(typeof(GlobalTestType)));
        }
    }

    namespace NamespaceTests
    {
        internal sealed class MarkerType { }
    }

    namespace NamespaceWithoutMarker
    {
        internal sealed class PlainType { }
    }
}

internal sealed class GlobalPlainType { }

internal sealed class GlobalTestType { }
