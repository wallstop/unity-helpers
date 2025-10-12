#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;

    public class ZenjectIntegrationCompilationTests
    {
        [Test]
        public void CanReferenceZenjectIntegrationTypes()
        {
            Assert.NotNull(
                typeof(RelationalComponentsInstaller),
                "Integration type RelationalComponentsInstaller should be resolvable"
            );
            Assert.NotNull(
                typeof(RelationalComponentSceneInitializer),
                "Integration type RelationalComponentSceneInitializer should be resolvable"
            );
            Assert.NotNull(
                typeof(DiContainerRelationalExtensions),
                "Integration type DiContainerRelationalExtensions should be resolvable"
            );
        }

        [Test]
        public void PublicAPIAccessible()
        {
            var method = typeof(DiContainerRelationalExtensions).GetMethod(
                "AssignRelationalComponents"
            );
            Assert.NotNull(
                method,
                "Extension method AssignRelationalComponents should be accessible"
            );
        }
    }
}
#endif
