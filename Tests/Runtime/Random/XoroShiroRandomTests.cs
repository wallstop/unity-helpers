namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class XoroShiroRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() =>
            new XoroShiroRandom(DeterministicSeed64, DeterministicSeed64B);
    }
}
