namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class SplitMix64RandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SplitMix64(DeterministicSeed64);
    }
}
