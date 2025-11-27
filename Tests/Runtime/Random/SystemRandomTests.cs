namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class SystemRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SystemRandom(DeterministicSeedInt);
    }
}
