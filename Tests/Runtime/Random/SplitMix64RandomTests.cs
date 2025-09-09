namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class SplitMix64RandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SplitMix64();
    }
}
