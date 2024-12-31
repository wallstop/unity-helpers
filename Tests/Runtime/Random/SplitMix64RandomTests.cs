namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class SplitMix64RandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SplitMix64();
    }
}
