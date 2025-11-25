namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class TestAssemblyHelperTests : CommonTestBase
    {
        [Test]
        public void ContainsTestMarkerDetectsCommonPatterns()
        {
            Assert.IsTrue(TestAssemblyHelper.ContainsTestMarker("MyGame.Tests"));
            Assert.IsTrue(TestAssemblyHelper.ContainsTestMarker("TestSuite"));
            Assert.IsFalse(TestAssemblyHelper.ContainsTestMarker("ProductionAssembly"));
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
        }
    }
}
