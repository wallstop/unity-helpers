namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class UnityRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new UnityRandom(DeterministicSeedInt);
    }
}
