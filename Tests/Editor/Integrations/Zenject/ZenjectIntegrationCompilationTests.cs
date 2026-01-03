// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;

    public sealed class ZenjectIntegrationCompilationTests
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
            MethodInfo method = typeof(DiContainerRelationalExtensions).GetMethod(
                nameof(DiContainerRelationalExtensions.AssignRelationalComponents)
            );
            Assert.NotNull(
                method,
                "Extension method AssignRelationalComponents should be accessible"
            );
        }
    }
}
#endif
