namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class XorShiftRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new XorShiftRandom(DeterministicSeedInt);
    }
}
