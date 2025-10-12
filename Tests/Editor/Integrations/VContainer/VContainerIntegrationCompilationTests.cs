#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;

    public class VContainerIntegrationCompilationTests
    {
        [Test]
        public void CanReferenceVContainerIntegrationTypes()
        {
            Assert.NotNull(
                typeof(RelationalComponentsBuilderExtensions),
                "Integration type RelationalComponentsBuilderExtensions should be resolvable"
            );
            Assert.NotNull(
                typeof(RelationalComponentEntryPoint),
                "Integration type RelationalComponentEntryPoint should be resolvable"
            );
            Assert.NotNull(
                typeof(RelationalSceneAssignmentOptions),
                "Integration type RelationalSceneAssignmentOptions should be resolvable"
            );
        }

        [Test]
        public void PublicAPIAccessible()
        {
            MethodInfo method = typeof(RelationalComponentsBuilderExtensions).GetMethod(
                "RegisterRelationalComponents"
            );
            Assert.NotNull(
                method,
                "Extension method RegisterRelationalComponents should be accessible"
            );
        }
    }
}
#endif
