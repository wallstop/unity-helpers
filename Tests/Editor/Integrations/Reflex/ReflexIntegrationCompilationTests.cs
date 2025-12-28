#if REFLEX_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Reflex
{
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Integrations.Reflex;

    public sealed class ReflexIntegrationCompilationTests
    {
        [Test]
        public void CanReferenceReflexIntegrationTypes()
        {
            Assert.NotNull(
                typeof(RelationalComponentsInstaller),
                "RelationalComponentsInstaller should be available when REFLEX_PRESENT is defined."
            );
            Assert.NotNull(
                typeof(ContainerRelationalExtensions),
                "ContainerRelationalExtensions should be available when REFLEX_PRESENT is defined."
            );
            Assert.NotNull(
                typeof(RelationalSceneAssignmentOptions),
                "RelationalSceneAssignmentOptions should be available when REFLEX_PRESENT is defined."
            );
        }

        [Test]
        public void PublicExtensionMethodsAccessible()
        {
            MethodInfo method = typeof(ContainerRelationalExtensions).GetMethod(
                nameof(ContainerRelationalExtensions.AssignRelationalComponents),
                BindingFlags.Public | BindingFlags.Static
            );
            Assert.NotNull(
                method,
                "AssignRelationalComponents extension method should be discoverable."
            );
        }
    }
}
#endif
