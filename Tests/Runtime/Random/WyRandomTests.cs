namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class WyRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new WyRandom(DeterministicSeed64);
    }
}
