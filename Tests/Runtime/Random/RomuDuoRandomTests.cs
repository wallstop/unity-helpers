namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class RomuDuoRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() =>
            new RomuDuo(DeterministicSeed64, DeterministicSeed64B);
    }
}
